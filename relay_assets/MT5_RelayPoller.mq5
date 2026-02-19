#property strict

#include <Trade/Trade.mqh>

input string RelayBaseUrl = "http://127.0.0.1:8000";
input string RelaySecret = "<RELAY_SECRET>";
input int PollSeconds = 1;

input string RelayFuturesSymbol = "/GCJ26:XCEC";
input string DefaultTradeSymbol = "XAUUSD";
input bool UseChartSymbolForExecution = true;
input bool IgnoreNonRelayFuturesSymbol = true;
input bool StrictQuantowerGoldOnly = true;
input bool RetryWithoutStopsOnInvalidStops = true;
input bool TrySetStopsAfterFill = true;

input int DefaultSLTicks = 40;
input int DefaultTPTicks = 80;
input double DefaultRiskLots = 0.25;

input long MagicNumber = 260219;
input int MaxDeviationPoints = 100;
input bool DryRunOnly = false;

CTrade g_trade;
int g_last_id = 0;

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
      return DefaultTradeSymbol;

   return payloadSymbol;
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
      volume = DefaultRiskLots;

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

bool ExecuteMarketSignal(const int signalId,
                         const string side,
                         const string tradeSymbol,
                         const int slTicks,
                         const int tpTicks,
                         const double riskLots,
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

   int digits = (int)SymbolInfoInteger(tradeSymbol, SYMBOL_DIGITS);
   double tickSize = GetTickSize(tradeSymbol);
   double volume = NormalizeVolume(tradeSymbol, riskLots);

   double price = 0.0;
   double sl = 0.0;
   double tp = 0.0;

   string sideUpper = ToUpperSafe(side);

   if (sideUpper == "BUY")
   {
      price = tick.ask;
      if (slTicks > 0)
         sl = NormalizeDouble(price - slTicks * tickSize, digits);
      if (tpTicks > 0)
         tp = NormalizeDouble(price + tpTicks * tickSize, digits);
   }
   else if (sideUpper == "SELL")
   {
      price = tick.bid;
      if (slTicks > 0)
         sl = NormalizeDouble(price + slTicks * tickSize, digits);
      if (tpTicks > 0)
         tp = NormalizeDouble(price - tpTicks * tickSize, digits);
   }
   else
   {
      PrintFormat("Signal id=%d rejected: unsupported side '%s'", signalId, side);
      return false;
   }

   AdjustStopsForBroker(tradeSymbol, sideUpper, tick.bid, tick.ask, sl, tp);

   g_trade.SetExpertMagicNumber((ulong)MagicNumber);
   g_trade.SetDeviationInPoints(MaxDeviationPoints);
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
      PrintFormat("Signal id=%d retrying without SL/TP due to invalid stops.", signalId);

      if (sideUpper == "BUY")
         ok = g_trade.Buy(volume, tradeSymbol, price, 0.0, 0.0, signalComment);
      else
         ok = g_trade.Sell(volume, tradeSymbol, price, 0.0, 0.0, signalComment);

      retcode = g_trade.ResultRetcode();
      retText = g_trade.ResultRetcodeDescription();
      sl = 0.0;
      tp = 0.0;

      if (ok)
         TryApplyPostFillStops(tradeSymbol, sideUpper, slTicks, tpTicks);
   }

   if (ok)
   {
      PrintFormat("Order success id=%d side=%s symbol=%s vol=%.2f price=%.5f sl=%.5f tp=%.5f ret=%u %s",
                  signalId, sideUpper, tradeSymbol, volume, price, sl, tp, retcode, retText);
      return true;
   }

   PrintFormat("Order failed id=%d side=%s symbol=%s vol=%.2f ret=%u %s comment=%s",
               signalId, sideUpper, tradeSymbol, volume, retcode, retText, g_trade.ResultComment());
   return false;
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
   string comment = "relay_signal";

   int slTicks = DefaultSLTicks;
   int tpTicks = DefaultTPTicks;
   double riskLots = DefaultRiskLots;

   JsonTryGetString(body, "side", side);
   JsonTryGetString(body, "source", source);
   JsonTryGetString(body, "symbol", sourceSymbol);
   JsonTryGetString(body, "trade_symbol", payloadTradeSymbol);
   JsonTryGetString(body, "comment", comment);
   JsonTryGetInt(body, "sl_ticks", slTicks);
   JsonTryGetInt(body, "tp_ticks", tpTicks);
   JsonTryGetDouble(body, "risk", riskLots);

   side = ToUpperSafe(side);
   source = ToUpperSafe(source);

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

   string tradeSymbol = ResolveTradeSymbol(sourceSymbol, payloadTradeSymbol);

   PrintFormat("Signal received id=%d side=%s src_symbol=%s trade_symbol=%s sl_ticks=%d tp_ticks=%d risk=%.2f comment=%s",
               signalId, side, sourceSymbol, tradeSymbol, slTicks, tpTicks, riskLots, comment);

   if (DryRunOnly)
   {
      PrintFormat("DryRunOnly=true -> order not sent for signal id=%d", signalId);
      g_last_id = signalId;
      return;
   }

   bool ok = ExecuteMarketSignal(signalId, side, tradeSymbol, slTicks, tpTicks, riskLots, comment);
   g_last_id = signalId;

   if (!ok)
      PrintFormat("Signal id=%d handled with trade failure (debounced to avoid duplicates).", signalId);
}

int OnInit()
{
   EventSetTimer(PollSeconds);
   g_trade.SetAsyncMode(false);
   PrintFormat("Relay poller started. chart_symbol=%s use_chart_symbol=%s filter_source=%s strict_gold_only=%s",
               _Symbol,
               (UseChartSymbolForExecution ? "true" : "false"),
               (IgnoreNonRelayFuturesSymbol ? "true" : "false"),
               (StrictQuantowerGoldOnly ? "true" : "false"));
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
      return;
   }

   ProcessSignalPayload(body);
}
