#property strict

#include <Trade/Trade.mqh>

input string RelayBaseUrl = "http://127.0.0.1:8000";
input string RelaySecret = "<RELAY_SECRET>";
input int PollSeconds = 1;

input string RelayFuturesSymbol = "/GCJ26:XCEC";
input string TradeSymbol = "XAUUSD";
input bool UseChartSymbolForExecution = true;
input bool IgnoreNonRelayFuturesSymbol = true;
input bool StrictQuantowerGoldOnly = true;
input bool RetryWithoutStopsOnInvalidStops = true;
input bool TrySetStopsAfterFill = true;

input int DefaultSLTicks = 40;
input int DefaultTP1Ticks = 40;
input int DefaultTP2Ticks = 80;

input bool EnableS1 = true;
input double RiskS1 = 0.25;
input long MagicS1 = 260201;
input string CommentS1 = "S1_ABSORB";

input bool EnableS2 = true;
input double RiskS2 = 0.25;
input long MagicS2 = 260202;
input string CommentS2 = "S2_VACUUM";

input bool EnableS3 = true;
input double RiskS3 = 0.25;
input long MagicS3 = 260203;
input string CommentS3 = "S3_RETEST";

input int CooldownSeconds = 60;
input int MaxSpreadFilter = 120;
input int MaxSlippage = 100;
input bool UseTP1TP2 = true;
input double PartialClosePercentAtTP1 = 50.0;
input bool MoveStopToBEAfterTP1 = true;
input double FallbackFixedLots = 0.01;
input bool DryRunOnly = false;

CTrade g_trade;
int g_last_id = 0;

datetime g_last_exec_s1 = 0;
datetime g_last_exec_s2 = 0;
datetime g_last_exec_s3 = 0;

#define MAX_MANAGED 16

struct StrategyConfig
{
   bool enabled;
   double riskPercent;
   long magic;
   string commentPrefix;
};

struct ManagedPosition
{
   bool active;
   long signalId;
   string strategyId;
   string symbol;
   string side;
   double tp1Price;
   bool tp1Done;
   long magic;
   string comment;
};

ManagedPosition g_managed[MAX_MANAGED];

bool IsWhitespace(const int c)
{
   return (c == ' ' || c == '\t' || c == '\r' || c == '\n');
}

bool IsDigit(const int c)
{
   return (c >= '0' && c <= '9');
}

void SkipWhitespace(const string text, int &p)
{
   while (p < StringLen(text) && IsWhitespace(StringGetCharacter(text, p)))
      p++;
}

int FindJsonValueStart(const string json, const string key)
{
   string marker = "\"" + key + "\":";
   int p = StringFind(json, marker);
   if (p < 0)
      return -1;

   p += StringLen(marker);
   SkipWhitespace(json, p);
   return p;
}

bool JsonTryGetString(const string json, const string key, string &value)
{
   int p = FindJsonValueStart(json, key);
   if (p < 0 || p >= StringLen(json))
      return false;

   if (StringGetCharacter(json, p) != '"')
      return false;

   p++;
   int start = p;
   while (p < StringLen(json))
   {
      int ch = StringGetCharacter(json, p);
      if (ch == '"' && StringGetCharacter(json, p - 1) != '\\')
         break;
      p++;
   }

   if (p <= start)
      return false;

   value = StringSubstr(json, start, p - start);
   return true;
}

bool JsonTryGetInt(const string json, const string key, int &value)
{
   int p = FindJsonValueStart(json, key);
   if (p < 0)
      return false;

   int e = p;
   if (e < StringLen(json) && (StringGetCharacter(json, e) == '-' || StringGetCharacter(json, e) == '+'))
      e++;

   while (e < StringLen(json) && IsDigit(StringGetCharacter(json, e)))
      e++;

   if (e <= p)
      return false;

   value = (int)StringToInteger(StringSubstr(json, p, e - p));
   return true;
}

bool JsonTryGetDouble(const string json, const string key, double &value)
{
   int p = FindJsonValueStart(json, key);
   if (p < 0)
      return false;

   int e = p;
   if (e < StringLen(json) && (StringGetCharacter(json, e) == '-' || StringGetCharacter(json, e) == '+'))
      e++;

   while (e < StringLen(json))
   {
      int ch = StringGetCharacter(json, e);
      if (!IsDigit(ch) && ch != '.' && ch != 'e' && ch != 'E' && ch != '+' && ch != '-')
         break;
      e++;
   }

   if (e <= p)
      return false;

   value = StringToDouble(StringSubstr(json, p, e - p));
   return true;
}

bool JsonTryGetBool(const string json, const string key, bool &value)
{
   int p = FindJsonValueStart(json, key);
   if (p < 0)
      return false;

   if (StringSubstr(json, p, 4) == "true")
   {
      value = true;
      return true;
   }

   if (StringSubstr(json, p, 5) == "false")
   {
      value = false;
      return true;
   }

   return false;
}

string ToUpperSafe(string text)
{
   StringToUpper(text);
   return text;
}

string ResolveTradeSymbol(const string payloadSymbol, const string payloadTradeSymbol)
{
   if (UseChartSymbolForExecution)
      return _Symbol;

   if (payloadTradeSymbol != "")
      return payloadTradeSymbol;

   if (payloadSymbol == "" || payloadSymbol == RelayFuturesSymbol)
      return TradeSymbol;

   return TradeSymbol;
}

double GetTickSize(const string symbol)
{
   double tickSize = 0.0;
   if (!SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_SIZE, tickSize) || tickSize <= 0.0)
   {
      if (!SymbolInfoDouble(symbol, SYMBOL_POINT, tickSize) || tickSize <= 0.0)
         tickSize = 0.01;
   }

   return tickSize;
}

void AdjustStopsForBroker(const string symbol,
                          const string sideUpper,
                          const double bid,
                          const double ask,
                          double &sl,
                          double &tp)
{
   int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
   double point = 0.0;
   if (!SymbolInfoDouble(symbol, SYMBOL_POINT, point) || point <= 0.0)
      point = GetTickSize(symbol);

   long stopsLevelPoints = (long)SymbolInfoInteger(symbol, SYMBOL_TRADE_STOPS_LEVEL);
   long freezeLevelPoints = (long)SymbolInfoInteger(symbol, SYMBOL_TRADE_FREEZE_LEVEL);
   long minPoints = (stopsLevelPoints > freezeLevelPoints ? stopsLevelPoints : freezeLevelPoints);
   if (minPoints < 0)
      minPoints = 0;

   double minDistance = minPoints * point;
   double oneTick = GetTickSize(symbol);
   if (minDistance < oneTick)
      minDistance = oneTick;

   if (sideUpper == "BUY")
   {
      if (sl > 0.0)
      {
         double maxAllowedSL = bid - minDistance;
         if (sl > maxAllowedSL)
            sl = maxAllowedSL;
      }

      if (tp > 0.0)
      {
         double minAllowedTP = ask + minDistance;
         if (tp < minAllowedTP)
            tp = minAllowedTP;
      }
   }
   else if (sideUpper == "SELL")
   {
      if (sl > 0.0)
      {
         double minAllowedSL = ask + minDistance;
         if (sl < minAllowedSL)
            sl = minAllowedSL;
      }

      if (tp > 0.0)
      {
         double maxAllowedTP = bid - minDistance;
         if (tp > maxAllowedTP)
            tp = maxAllowedTP;
      }
   }

   if (sl > 0.0)
      sl = NormalizeDouble(sl, digits);
   if (tp > 0.0)
      tp = NormalizeDouble(tp, digits);
}

bool TryApplyPostFillStops(const string tradeSymbol,
                           const string sideUpper,
                           const int slTicks,
                           const int tpTicks)
{
   if (!TrySetStopsAfterFill || (slTicks <= 0 && tpTicks <= 0))
      return true;

   if (!PositionSelect(tradeSymbol))
   {
      PrintFormat("Post-fill SL/TP skipped: no position selected for %s", tradeSymbol);
      return false;
   }

   double openPrice = PositionGetDouble(POSITION_PRICE_OPEN);
   if (openPrice <= 0.0)
      return false;

   int digits = (int)SymbolInfoInteger(tradeSymbol, SYMBOL_DIGITS);
   double tickSize = GetTickSize(tradeSymbol);
   MqlTick tick;
   if (!SymbolInfoTick(tradeSymbol, tick))
      return false;

   for (int attempt = 0; attempt < 5; attempt++)
   {
      int slTry = slTicks > 0 ? (int)MathCeil(slTicks * MathPow(1.5, attempt)) : 0;
      int tpTry = tpTicks > 0 ? (int)MathCeil(tpTicks * MathPow(1.5, attempt)) : 0;

      double sl = 0.0;
      double tp = 0.0;

      if (sideUpper == "BUY")
      {
         if (slTry > 0)
            sl = NormalizeDouble(openPrice - slTry * tickSize, digits);
         if (tpTry > 0)
            tp = NormalizeDouble(openPrice + tpTry * tickSize, digits);
      }
      else
      {
         if (slTry > 0)
            sl = NormalizeDouble(openPrice + slTry * tickSize, digits);
         if (tpTry > 0)
            tp = NormalizeDouble(openPrice - tpTry * tickSize, digits);
      }

      AdjustStopsForBroker(tradeSymbol, sideUpper, tick.bid, tick.ask, sl, tp);

      if (g_trade.PositionModify(tradeSymbol, sl, tp))
      {
         PrintFormat("Post-fill SL/TP set symbol=%s sl=%.5f tp=%.5f attempt=%d",
                     tradeSymbol, sl, tp, attempt + 1);
         return true;
      }

      PrintFormat("Post-fill SL/TP attempt %d failed for %s ret=%u %s",
                  attempt + 1,
                  tradeSymbol,
                  g_trade.ResultRetcode(),
                  g_trade.ResultRetcodeDescription());

      SymbolInfoTick(tradeSymbol, tick);
   }

   return false;
}

double NormalizeVolume(const string symbol, const double rawVolume)
{
   double minVol = 0.01;
   double maxVol = 100.0;
   double step = 0.01;

   SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN, minVol);
   SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX, maxVol);
   SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP, step);

   if (step <= 0.0)
      step = minVol;

   double volume = rawVolume;
   if (volume <= 0.0)
      volume = FallbackFixedLots;

   volume = MathFloor(volume / step) * step;
   if (volume < minVol)
      volume = minVol;
   if (volume > maxVol)
      volume = maxVol;

   int digits = 2;
   if (step >= 1.0)
      digits = 0;
   else if (step >= 0.1)
      digits = 1;
   else if (step >= 0.01)
      digits = 2;
   else if (step >= 0.001)
      digits = 3;

   return NormalizeDouble(volume, digits);
}

bool GetStrategyConfig(const string strategyId, StrategyConfig &cfg)
{
   string sid = ToUpperSafe(strategyId);

   if (sid == "S1")
   {
      cfg.enabled = EnableS1;
      cfg.riskPercent = RiskS1;
      cfg.magic = MagicS1;
      cfg.commentPrefix = CommentS1;
      return true;
   }

   if (sid == "S2")
   {
      cfg.enabled = EnableS2;
      cfg.riskPercent = RiskS2;
      cfg.magic = MagicS2;
      cfg.commentPrefix = CommentS2;
      return true;
   }

   if (sid == "S3")
   {
      cfg.enabled = EnableS3;
      cfg.riskPercent = RiskS3;
      cfg.magic = MagicS3;
      cfg.commentPrefix = CommentS3;
      return true;
   }

   return false;
}

datetime GetLastExec(const string strategyId)
{
   string sid = ToUpperSafe(strategyId);
   if (sid == "S1")
      return g_last_exec_s1;
   if (sid == "S2")
      return g_last_exec_s2;
   if (sid == "S3")
      return g_last_exec_s3;
   return 0;
}

void SetLastExec(const string strategyId, datetime ts)
{
   string sid = ToUpperSafe(strategyId);
   if (sid == "S1")
      g_last_exec_s1 = ts;
   else if (sid == "S2")
      g_last_exec_s2 = ts;
   else if (sid == "S3")
      g_last_exec_s3 = ts;
}

string TrimComment(const string commentText)
{
   if (StringLen(commentText) <= 31)
      return commentText;
   return StringSubstr(commentText, 0, 31);
}

string BuildOrderComment(const string prefix, const string sideUpper, const string payloadComment)
{
   string finalComment = payloadComment;

   if (finalComment == "")
      finalComment = prefix + "_" + sideUpper;

   string prefixUpper = ToUpperSafe(prefix);
   string finalUpper = ToUpperSafe(finalComment);
   if (prefixUpper != "" && StringFind(finalUpper, prefixUpper) < 0)
      finalComment = prefix + "_" + finalComment;

   if (StringFind(ToUpperSafe(finalComment), sideUpper) < 0)
      finalComment = finalComment + "_" + sideUpper;

   return TrimComment(finalComment);
}

double ComputeLotsFromRiskPercent(const string symbol,
                                  const int slTicks,
                                  const double riskPercent)
{
   if (slTicks <= 0)
      return NormalizeVolume(symbol, FallbackFixedLots);

   double rp = riskPercent;
   if (rp <= 0.0)
      rp = 0.25;

   double equity = AccountInfoDouble(ACCOUNT_EQUITY);
   if (equity <= 0.0)
      return NormalizeVolume(symbol, FallbackFixedLots);

   double tickSize = GetTickSize(symbol);
   double tickValue = 0.0;
   if (!SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_VALUE, tickValue) || tickValue <= 0.0)
      tickValue = 1.0;

   double moneyRisk = equity * (rp / 100.0);
   double moneyPerLot = slTicks * tickValue;
   if (moneyPerLot <= 0.0)
      return NormalizeVolume(symbol, FallbackFixedLots);

   double rawLots = moneyRisk / moneyPerLot;
   if (rawLots <= 0.0)
      rawLots = FallbackFixedLots;

   return NormalizeVolume(symbol, rawLots);
}

bool IsSpreadAllowed(const string symbol, const MqlTick &tick)
{
   if (MaxSpreadFilter <= 0)
      return true;

   double point = 0.0;
   if (!SymbolInfoDouble(symbol, SYMBOL_POINT, point) || point <= 0.0)
      point = GetTickSize(symbol);

   if (point <= 0.0)
      return true;

   double spreadPoints = (tick.ask - tick.bid) / point;
   return (spreadPoints <= MaxSpreadFilter);
}

void RegisterManagedPosition(const int signalId,
                             const string strategyId,
                             const string tradeSymbol,
                             const string sideUpper,
                             const double tp1Price,
                             const long magic,
                             const string comment)
{
   if (!UseTP1TP2)
      return;

   for (int i = 0; i < MAX_MANAGED; i++)
   {
      if (!g_managed[i].active)
      {
         g_managed[i].active = true;
         g_managed[i].signalId = signalId;
         g_managed[i].strategyId = strategyId;
         g_managed[i].symbol = tradeSymbol;
         g_managed[i].side = sideUpper;
         g_managed[i].tp1Price = tp1Price;
         g_managed[i].tp1Done = false;
         g_managed[i].magic = magic;
         g_managed[i].comment = comment;
         return;
      }
   }
}

void ProcessManagedPositions()
{
   if (!UseTP1TP2)
      return;

   double partPct = PartialClosePercentAtTP1;
   if (partPct <= 0.0 || partPct >= 100.0)
      partPct = 50.0;

   for (int i = 0; i < MAX_MANAGED; i++)
   {
      if (!g_managed[i].active || g_managed[i].tp1Done)
         continue;

      string symbol = g_managed[i].symbol;
      if (!PositionSelect(symbol))
      {
         g_managed[i].active = false;
         continue;
      }

      MqlTick tick;
      if (!SymbolInfoTick(symbol, tick))
         continue;

      bool reached = false;
      if (g_managed[i].side == "BUY")
         reached = (tick.bid >= g_managed[i].tp1Price);
      else
         reached = (tick.ask <= g_managed[i].tp1Price);

      if (!reached)
         continue;

      double posVol = PositionGetDouble(POSITION_VOLUME);
      if (posVol <= 0.0)
      {
         g_managed[i].active = false;
         continue;
      }

      double closeVol = NormalizeVolume(symbol, posVol * partPct / 100.0);
      if (closeVol >= posVol)
         closeVol = NormalizeVolume(symbol, posVol * 0.5);

      g_trade.SetExpertMagicNumber((ulong)g_managed[i].magic);
      g_trade.SetDeviationInPoints(MaxSlippage);

      bool closeOk = false;
      if (closeVol > 0.0 && closeVol < posVol)
         closeOk = g_trade.PositionClosePartial(symbol, closeVol, MaxSlippage);

      if (closeOk)
      {
         PrintFormat("TP1 partial close strategy=%s signal=%d symbol=%s closed=%.2f",
                     g_managed[i].strategyId,
                     g_managed[i].signalId,
                     symbol,
                     closeVol);
      }
      else
      {
         PrintFormat("TP1 partial close failed strategy=%s signal=%d symbol=%s ret=%u %s",
                     g_managed[i].strategyId,
                     g_managed[i].signalId,
                     symbol,
                     g_trade.ResultRetcode(),
                     g_trade.ResultRetcodeDescription());
      }

      if (MoveStopToBEAfterTP1 && PositionSelect(symbol))
      {
         double openPrice = PositionGetDouble(POSITION_PRICE_OPEN);
         double tp = PositionGetDouble(POSITION_TP);
         double tickSize = GetTickSize(symbol);
         int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);

         double newSL = openPrice;
         if (g_managed[i].side == "BUY")
            newSL = NormalizeDouble(openPrice + tickSize, digits);
         else
            newSL = NormalizeDouble(openPrice - tickSize, digits);

         if (!g_trade.PositionModify(symbol, newSL, tp))
         {
            PrintFormat("Move-to-BE failed strategy=%s signal=%d symbol=%s ret=%u %s",
                        g_managed[i].strategyId,
                        g_managed[i].signalId,
                        symbol,
                        g_trade.ResultRetcode(),
                        g_trade.ResultRetcodeDescription());
         }
      }

      g_managed[i].tp1Done = true;
   }
}

bool ExecuteMarketSignal(const int signalId,
                         const string strategyId,
                         const StrategyConfig &cfg,
                         const string side,
                         const string tradeSymbol,
                         const int slTicks,
                         const int tp1Ticks,
                         const int tp2Ticks,
                         const double riskPercent,
                         const string signalComment)
{
   if (!SymbolSelect(tradeSymbol, true))
   {
      PrintFormat("Signal id=%d rejected: unable to select symbol '%s'", signalId, tradeSymbol);
      return false;
   }

   MqlTick tick;
   if (!SymbolInfoTick(tradeSymbol, tick))
   {
      PrintFormat("Signal id=%d rejected: no market tick for '%s'", signalId, tradeSymbol);
      return false;
   }

   if (!IsSpreadAllowed(tradeSymbol, tick))
   {
      PrintFormat("Signal id=%d rejected: spread filter exceeded on %s", signalId, tradeSymbol);
      return false;
   }

   int digits = (int)SymbolInfoInteger(tradeSymbol, SYMBOL_DIGITS);
   double tickSize = GetTickSize(tradeSymbol);
   double effectiveRisk = riskPercent > 0.0 ? riskPercent : cfg.riskPercent;
   double volume = ComputeLotsFromRiskPercent(tradeSymbol, slTicks, effectiveRisk);

   double price = 0.0;
   double sl = 0.0;
   double tp = 0.0;
   double tp1Price = 0.0;
   int finalTpTicks = tp2Ticks;
   if (finalTpTicks <= 0)
      finalTpTicks = tp1Ticks;
   if (finalTpTicks <= 0)
      finalTpTicks = DefaultTP2Ticks;

   string sideUpper = ToUpperSafe(side);

   if (sideUpper == "BUY")
   {
      price = tick.ask;
      if (slTicks > 0)
         sl = NormalizeDouble(price - slTicks * tickSize, digits);
      if (tp1Ticks > 0)
         tp1Price = NormalizeDouble(price + tp1Ticks * tickSize, digits);
      if (finalTpTicks > 0)
         tp = NormalizeDouble(price + finalTpTicks * tickSize, digits);
   }
   else if (sideUpper == "SELL")
   {
      price = tick.bid;
      if (slTicks > 0)
         sl = NormalizeDouble(price + slTicks * tickSize, digits);
      if (tp1Ticks > 0)
         tp1Price = NormalizeDouble(price - tp1Ticks * tickSize, digits);
      if (finalTpTicks > 0)
         tp = NormalizeDouble(price - finalTpTicks * tickSize, digits);
   }
   else
   {
      PrintFormat("Signal id=%d rejected: unsupported side '%s'", signalId, side);
      return false;
   }

   AdjustStopsForBroker(tradeSymbol, sideUpper, tick.bid, tick.ask, sl, tp);

   g_trade.SetExpertMagicNumber((ulong)cfg.magic);
   g_trade.SetDeviationInPoints(MaxSlippage);
   g_trade.SetTypeFillingBySymbol(tradeSymbol);

   bool ok = false;
   if (sideUpper == "BUY")
      ok = g_trade.Buy(volume, tradeSymbol, price, sl, tp, signalComment);
   else
      ok = g_trade.Sell(volume, tradeSymbol, price, sl, tp, signalComment);

   ulong retcode = g_trade.ResultRetcode();
   string retText = g_trade.ResultRetcodeDescription();

   if (!ok && RetryWithoutStopsOnInvalidStops && retcode == TRADE_RETCODE_INVALID_STOPS)
   {
      PrintFormat("Signal id=%d strategy=%s retrying without SL/TP due to invalid stops.", signalId, strategyId);

      if (sideUpper == "BUY")
         ok = g_trade.Buy(volume, tradeSymbol, price, 0.0, 0.0, signalComment);
      else
         ok = g_trade.Sell(volume, tradeSymbol, price, 0.0, 0.0, signalComment);

      retcode = g_trade.ResultRetcode();
      retText = g_trade.ResultRetcodeDescription();
      sl = 0.0;
      tp = 0.0;

      if (ok)
         TryApplyPostFillStops(tradeSymbol, sideUpper, slTicks, finalTpTicks);
   }

   if (ok)
   {
      double entryFilled = g_trade.ResultPrice();
      if (entryFilled <= 0.0)
         entryFilled = price;

      if (tp1Ticks > 0)
      {
         if (sideUpper == "BUY")
            tp1Price = NormalizeDouble(entryFilled + tp1Ticks * tickSize, digits);
         else
            tp1Price = NormalizeDouble(entryFilled - tp1Ticks * tickSize, digits);
      }

      if (UseTP1TP2 && tp1Ticks > 0 && tp1Price > 0.0)
         RegisterManagedPosition(signalId, strategyId, tradeSymbol, sideUpper, tp1Price, cfg.magic, signalComment);

      PrintFormat("%s placed %s id=%d symbol=%s lots=%.2f sl=%.5f tp1=%.5f tp2=%.5f magic=%I64d comment=%s ret=%u %s",
                  strategyId,
                  sideUpper,
                  signalId,
                  tradeSymbol,
                  volume,
                  sl,
                  tp1Price,
                  tp,
                  cfg.magic,
                  signalComment,
                  retcode,
                  retText);
      return true;
   }

   PrintFormat("%s order failed id=%d side=%s symbol=%s lots=%.2f ret=%u %s comment=%s",
               strategyId,
               signalId,
               sideUpper,
               tradeSymbol,
               volume,
               retcode,
               retText,
               g_trade.ResultComment());
   return false;
}

string InferStrategyId(const string payloadStrategyId, const string payloadComment)
{
   string sid = ToUpperSafe(payloadStrategyId);
   if (sid == "S1" || sid == "S2" || sid == "S3")
      return sid;

   string c = ToUpperSafe(payloadComment);
   if (StringFind(c, "S1") >= 0)
      return "S1";
   if (StringFind(c, "S2") >= 0)
      return "S2";
   if (StringFind(c, "S3") >= 0)
      return "S3";

   return "";
}

void ProcessSignalPayload(const string body)
{
   bool updated = false;
   if (!JsonTryGetBool(body, "updated", updated))
   {
      Print("Relay response missing 'updated' field.");
      return;
   }

   if (!updated)
      return;

   int signalId = -1;
   if (!JsonTryGetInt(body, "id", signalId) || signalId <= g_last_id)
      return;

   string side = "";
   string source = "";
   string sourceSymbol = "";
   string payloadTradeSymbol = "";
   string payloadComment = "relay_signal";
   string payloadStrategyId = "";

   int slTicks = DefaultSLTicks;
   int tp1Ticks = DefaultTP1Ticks;
   int tp2Ticks = DefaultTP2Ticks;
   int tpTicksCompat = 0;
   double payloadRisk = 0.0;

   JsonTryGetString(body, "side", side);
   JsonTryGetString(body, "source", source);
   JsonTryGetString(body, "symbol", sourceSymbol);
   JsonTryGetString(body, "trade_symbol", payloadTradeSymbol);
   JsonTryGetString(body, "comment", payloadComment);
   JsonTryGetString(body, "strategy_id", payloadStrategyId);
   JsonTryGetInt(body, "sl_ticks", slTicks);
    JsonTryGetInt(body, "tp1_ticks", tp1Ticks);
    JsonTryGetInt(body, "tp2_ticks", tp2Ticks);
    JsonTryGetInt(body, "tp_ticks", tpTicksCompat);
    JsonTryGetDouble(body, "risk", payloadRisk);

   side = ToUpperSafe(side);
   source = ToUpperSafe(source);

   string strategyId = InferStrategyId(payloadStrategyId, payloadComment);
   if (strategyId == "")
   {
      PrintFormat("Signal id=%d ignored: missing/invalid strategy_id", signalId);
      g_last_id = signalId;
      return;
   }

   if (StrictQuantowerGoldOnly)
   {
      if (source != "QUANTOWER")
      {
         PrintFormat("Signal id=%d ignored: source=%s (expected quantower)", signalId, source);
         g_last_id = signalId;
         return;
      }

      if (sourceSymbol == "" || StringCompare(sourceSymbol, RelayFuturesSymbol, false) != 0)
      {
         PrintFormat("Signal id=%d ignored: src_symbol=%s (expected %s)",
                     signalId, sourceSymbol, RelayFuturesSymbol);
         g_last_id = signalId;
         return;
      }
   }

   if (IgnoreNonRelayFuturesSymbol &&
       RelayFuturesSymbol != "" &&
       sourceSymbol != "" &&
       StringCompare(sourceSymbol, RelayFuturesSymbol, false) != 0)
   {
      PrintFormat("Signal id=%d ignored: src_symbol=%s does not match RelayFuturesSymbol=%s",
                  signalId, sourceSymbol, RelayFuturesSymbol);
      g_last_id = signalId;
      return;
   }

   StrategyConfig cfg;
   if (!GetStrategyConfig(strategyId, cfg))
   {
      PrintFormat("Signal id=%d ignored: strategy config not found for %s", signalId, strategyId);
      g_last_id = signalId;
      return;
   }

   if (!cfg.enabled)
   {
      PrintFormat("Signal id=%d ignored: %s disabled", signalId, strategyId);
      g_last_id = signalId;
      return;
   }

   datetime nowTs = TimeCurrent();
   datetime lastExec = GetLastExec(strategyId);
   if (lastExec > 0 && (nowTs - lastExec) < CooldownSeconds)
   {
      PrintFormat("Signal id=%d ignored: %s cooldown active (%d sec)", signalId, strategyId, (int)(nowTs - lastExec));
      g_last_id = signalId;
      return;
   }

   if (slTicks <= 0)
      slTicks = DefaultSLTicks;

   if (tp1Ticks <= 0)
      tp1Ticks = DefaultTP1Ticks;
   if (tp2Ticks <= 0)
      tp2Ticks = tpTicksCompat > 0 ? tpTicksCompat : DefaultTP2Ticks;

   double effectiveRisk = payloadRisk > 0.0 ? payloadRisk : cfg.riskPercent;
   string finalComment = BuildOrderComment(cfg.commentPrefix, side, payloadComment);
   string tradeSymbol = ResolveTradeSymbol(sourceSymbol, payloadTradeSymbol);

   PrintFormat("Signal received id=%d strategy=%s side=%s src_symbol=%s trade_symbol=%s sl=%d tp1=%d tp2=%d risk=%.2f comment=%s",
               signalId,
               strategyId,
               side,
               sourceSymbol,
               tradeSymbol,
               slTicks,
               tp1Ticks,
               tp2Ticks,
               effectiveRisk,
               finalComment);

   if (DryRunOnly)
   {
      PrintFormat("DryRunOnly=true -> order not sent for signal id=%d", signalId);
      g_last_id = signalId;
      return;
   }

   bool ok = ExecuteMarketSignal(signalId,
                                 strategyId,
                                 cfg,
                                 side,
                                 tradeSymbol,
                                 slTicks,
                                 tp1Ticks,
                                 tp2Ticks,
                                 effectiveRisk,
                                 finalComment);

   g_last_id = signalId;
   SetLastExec(strategyId, nowTs);

   if (!ok)
      PrintFormat("Signal id=%d handled with trade failure (debounced to avoid duplicates).", signalId);
}

int OnInit()
{
   EventSetTimer(PollSeconds);
   g_trade.SetAsyncMode(false);

   for (int i = 0; i < MAX_MANAGED; i++)
      g_managed[i].active = false;

   PrintFormat("Relay poller started. chart_symbol=%s use_chart_symbol=%s strict_gold_only=%s S1=%s S2=%s S3=%s tp1tp2=%s",
               _Symbol,
               (UseChartSymbolForExecution ? "true" : "false"),
               (StrictQuantowerGoldOnly ? "true" : "false"),
               (EnableS1 ? "on" : "off"),
               (EnableS2 ? "on" : "off"),
               (EnableS3 ? "on" : "off"),
               (UseTP1TP2 ? "true" : "false"));
   return INIT_SUCCEEDED;
}

void OnDeinit(const int reason)
{
   EventKillTimer();
}

void OnTimer()
{
   string url = StringFormat("%s/signal?id=%d&auth=%s", RelayBaseUrl, g_last_id, RelaySecret);

   char requestBody[];
   char responseBody[];
   string responseHeaders;

   ResetLastError();
   int status = WebRequest("GET", url, "", 5000, requestBody, responseBody, responseHeaders);
   if (status == -1)
   {
      PrintFormat("WebRequest failed err=%d", GetLastError());
      return;
   }

   string body = CharArrayToString(responseBody, 0, -1, CP_UTF8);

   if (status != 200)
   {
      PrintFormat("Relay HTTP %d: %s", status, body);
      ProcessManagedPositions();
      return;
   }

   ProcessSignalPayload(body);
   ProcessManagedPositions();
}
