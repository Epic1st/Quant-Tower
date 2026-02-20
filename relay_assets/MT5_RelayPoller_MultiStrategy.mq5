#property strict

#include <Trade/Trade.mqh>

input string RelayBaseUrl = "http://127.0.0.1:8000";
input string RelaySecret = "";
input int PollSeconds = 1;
input bool PersistLastSignalId = true;
input bool EnableRelayFailureAlert = true;
input int RelayFailureAlertThreshold = 5;

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

input bool EnableS4 = true;
input double RiskS4 = 0.25;
input long MagicS4 = 260204;
input string CommentS4 = "S4_ORB_RET";

input bool EnableS5 = true;
input double RiskS5 = 0.25;
input long MagicS5 = 260205;
input string CommentS5 = "S5_FA_REV";

input bool EnableS6 = true;
input double RiskS6 = 0.25;
input long MagicS6 = 260206;
input string CommentS6 = "S6_LVN_ROT";

input bool EnableS7 = true;
input double RiskS7 = 0.25;
input long MagicS7 = 260207;
input string CommentS7 = "S7_POC_CONT";

input bool EnableS8 = true;
input double RiskS8 = 0.25;
input long MagicS8 = 260208;
input string CommentS8 = "S8_VWAP_MR";

input bool EnableS9 = true;
input double RiskS9 = 0.25;
input long MagicS9 = 260209;
input string CommentS9 = "S9_VWAP_TF";

input bool EnableS10 = true;
input double RiskS10 = 0.25;
input long MagicS10 = 260210;
input string CommentS10 = "S10_ICE_BRK";

input bool EnableS11 = true;
input double RiskS11 = 0.25;
input long MagicS11 = 260211;
input string CommentS11 = "S11_SWEEP_RECL";

input bool EnableS12 = true;
input double RiskS12 = 0.25;
input long MagicS12 = 260212;
input string CommentS12 = "S12_DELTA_DIV";

input bool EnableS13 = true;
input double RiskS13 = 0.25;
input long MagicS13 = 260213;
input string CommentS13 = "S13_MTF_PULL";

input int CooldownSeconds = 20;
input int MaxSpreadFilter = 120;
input int MaxSlippage = 100;
input bool UseTP1TP2 = true;
input double PartialClosePercentAtTP1 = 50.0;
input bool MoveStopToBEAfterTP1 = true;
input double FallbackFixedLots = 0.01;
input bool DryRunOnly = false;

input bool UseSignalDirectionOnly = true;
input string DirectionOnlyStrategyId = "S1";
input int GlobalLotMode = 1; // 0 = fixed lots, 1 = risk percent
input double GlobalFixedLots = 0.01;
input double GlobalRiskPercent = 0.25;
input double GlobalSLPips = 150.0;
input bool GlobalUseRiskReward = true;
input double GlobalRiskReward = 2.0;
input double GlobalTPPips = 300.0;
input double GlobalTP1Fraction = 0.5;
input bool EnableGlobalTrailingSL = true;
input double TrailingStartPips = 100.0;
input double TrailingDistancePips = 70.0;
input double TrailingStepPips = 10.0;

CTrade g_trade;
int g_last_id = 0;
int g_consecutive_poll_failures = 0;

datetime g_last_exec_s1 = 0;
datetime g_last_exec_s2 = 0;
datetime g_last_exec_s3 = 0;
datetime g_last_exec_s4 = 0;
datetime g_last_exec_s5 = 0;
datetime g_last_exec_s6 = 0;
datetime g_last_exec_s7 = 0;
datetime g_last_exec_s8 = 0;
datetime g_last_exec_s9 = 0;
datetime g_last_exec_s10 = 0;
datetime g_last_exec_s11 = 0;
datetime g_last_exec_s12 = 0;
datetime g_last_exec_s13 = 0;

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
   ulong positionTicket;
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

bool IsSecretConfigured(const string secret)
{
   string s = secret;
   StringTrimLeft(s);
   StringTrimRight(s);
   if (s == "")
      return false;
   return (StringCompare(s, "<RELAY_SECRET>", false) != 0);
}

void TrackRelayPollFailure(const string reason)
{
   g_consecutive_poll_failures++;
   if (!EnableRelayFailureAlert)
      return;

   int threshold = RelayFailureAlertThreshold;
   if (threshold < 1)
      threshold = 1;

   if (g_consecutive_poll_failures == threshold ||
       (g_consecutive_poll_failures > threshold && (g_consecutive_poll_failures % threshold) == 0))
   {
      string msg = StringFormat("Relay poll failures=%d. Last error: %s", g_consecutive_poll_failures, reason);
      Print(msg);
      Alert(msg);
   }
}

void ResetRelayPollFailureState()
{
   g_consecutive_poll_failures = 0;
}

bool IsAsciiLetter(const int ch)
{
   return (ch >= 'A' && ch <= 'Z');
}

string ExtractFuturesRoot(const string symbolText)
{
   string sym = ToUpperSafe(symbolText);
   int n = StringLen(sym);
   int p = 0;

   while (p < n && (StringGetCharacter(sym, p) == ' ' || StringGetCharacter(sym, p) == '\t'))
      p++;

   if (p < n && StringGetCharacter(sym, p) == '/')
      p++;

   int start = p;
   while (p < n)
   {
      int ch = StringGetCharacter(sym, p);
      if (!IsAsciiLetter(ch))
         break;
      p++;
   }

   if (p <= start)
      return "";

   return StringSubstr(sym, start, p - start);
}

string NormalizeGoldRoot(const string symbolText)
{
   string root = ExtractFuturesRoot(symbolText);
   if (root == "")
      return "";

   if (StringLen(root) >= 3 && StringSubstr(root, 0, 3) == "MGC")
      return "MGC";

   if (StringLen(root) >= 2 && StringSubstr(root, 0, 2) == "GC")
      return "GC";

   return root;
}

bool IsGoldSymbolLike(const string symbolText)
{
   string sym = ToUpperSafe(symbolText);
   if (sym == "")
      return false;

   if (StringFind(sym, "XAU") >= 0 || StringFind(sym, "GOLD") >= 0)
      return true;

   string root = NormalizeGoldRoot(sym);
   if (root == "GC" || root == "MGC")
      return true;

   return false;
}

bool IsRelaySymbolMatch(const string sourceSymbol)
{
   if (sourceSymbol == "")
      return false;

   if (RelayFuturesSymbol == "")
      return IsGoldSymbolLike(sourceSymbol);

   if (StringCompare(sourceSymbol, RelayFuturesSymbol, false) == 0)
      return true;

   string sourceRoot = NormalizeGoldRoot(sourceSymbol);
   string relayRoot = NormalizeGoldRoot(RelayFuturesSymbol);
   if (sourceRoot != "" && relayRoot != "" && sourceRoot == relayRoot)
      return true;

   return false;
}

string UrlEncode(const string value)
{
   string encoded = "";
   int n = StringLen(value);

   for (int i = 0; i < n; i++)
   {
      int ch = StringGetCharacter(value, i);
      bool safe =
         (ch >= 'a' && ch <= 'z') ||
         (ch >= 'A' && ch <= 'Z') ||
         (ch >= '0' && ch <= '9') ||
         ch == '-' || ch == '_' || ch == '.' || ch == '~';

      if (safe)
      {
         encoded += ShortToString((ushort)ch);
      }
      else if (ch >= 0 && ch <= 255)
      {
         encoded += "%" + StringFormat("%02X", ch);
      }
      else
      {
         encoded += "%3F";
      }
   }

   return encoded;
}

string LastIdGlobalKey()
{
   long login = (long)AccountInfoInteger(ACCOUNT_LOGIN);
   return StringFormat("relay_ms_last_id_%I64d", login);
}

void SaveLastSignalId()
{
   if (!PersistLastSignalId)
      return;

   string key = LastIdGlobalKey();
   double current = 0.0;
   if (GlobalVariableCheck(key))
      current = GlobalVariableGet(key);

   if ((double)g_last_id >= current)
      GlobalVariableSet(key, (double)g_last_id);
}

void LoadLastSignalId()
{
   if (!PersistLastSignalId)
      return;

   string key = LastIdGlobalKey();
   if (!GlobalVariableCheck(key))
      return;

   int persisted = (int)MathRound(GlobalVariableGet(key));
   if (persisted > g_last_id)
   {
      g_last_id = persisted;
      PrintFormat("Loaded persisted last signal id=%d", g_last_id);
   }
}

void AdvanceLastId(const int signalId)
{
   if (signalId > g_last_id)
      g_last_id = signalId;

   SaveLastSignalId();
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

int TicksFromPercent(const double entryPrice, const double percent, const double tickSize)
{
   if (entryPrice <= 0.0 || percent <= 0.0 || tickSize <= 0.0)
      return 0;

   double distance = entryPrice * (percent / 100.0);
   if (distance <= 0.0)
      return 0;

   return (int)MathMax(1.0, MathCeil(distance / tickSize));
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

   if (sid == "S4")
   {
      cfg.enabled = EnableS4;
      cfg.riskPercent = RiskS4;
      cfg.magic = MagicS4;
      cfg.commentPrefix = CommentS4;
      return true;
   }

   if (sid == "S5")
   {
      cfg.enabled = EnableS5;
      cfg.riskPercent = RiskS5;
      cfg.magic = MagicS5;
      cfg.commentPrefix = CommentS5;
      return true;
   }

   if (sid == "S6")
   {
      cfg.enabled = EnableS6;
      cfg.riskPercent = RiskS6;
      cfg.magic = MagicS6;
      cfg.commentPrefix = CommentS6;
      return true;
   }

   if (sid == "S7")
   {
      cfg.enabled = EnableS7;
      cfg.riskPercent = RiskS7;
      cfg.magic = MagicS7;
      cfg.commentPrefix = CommentS7;
      return true;
   }

   if (sid == "S8")
   {
      cfg.enabled = EnableS8;
      cfg.riskPercent = RiskS8;
      cfg.magic = MagicS8;
      cfg.commentPrefix = CommentS8;
      return true;
   }

   if (sid == "S9")
   {
      cfg.enabled = EnableS9;
      cfg.riskPercent = RiskS9;
      cfg.magic = MagicS9;
      cfg.commentPrefix = CommentS9;
      return true;
   }

   if (sid == "S10")
   {
      cfg.enabled = EnableS10;
      cfg.riskPercent = RiskS10;
      cfg.magic = MagicS10;
      cfg.commentPrefix = CommentS10;
      return true;
   }

   if (sid == "S11")
   {
      cfg.enabled = EnableS11;
      cfg.riskPercent = RiskS11;
      cfg.magic = MagicS11;
      cfg.commentPrefix = CommentS11;
      return true;
   }

   if (sid == "S12")
   {
      cfg.enabled = EnableS12;
      cfg.riskPercent = RiskS12;
      cfg.magic = MagicS12;
      cfg.commentPrefix = CommentS12;
      return true;
   }

   if (sid == "S13")
   {
      cfg.enabled = EnableS13;
      cfg.riskPercent = RiskS13;
      cfg.magic = MagicS13;
      cfg.commentPrefix = CommentS13;
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
   if (sid == "S4")
      return g_last_exec_s4;
   if (sid == "S5")
      return g_last_exec_s5;
   if (sid == "S6")
      return g_last_exec_s6;
   if (sid == "S7")
      return g_last_exec_s7;
   if (sid == "S8")
      return g_last_exec_s8;
   if (sid == "S9")
      return g_last_exec_s9;
   if (sid == "S10")
      return g_last_exec_s10;
   if (sid == "S11")
      return g_last_exec_s11;
   if (sid == "S12")
      return g_last_exec_s12;
   if (sid == "S13")
      return g_last_exec_s13;
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
   else if (sid == "S4")
      g_last_exec_s4 = ts;
   else if (sid == "S5")
      g_last_exec_s5 = ts;
   else if (sid == "S6")
      g_last_exec_s6 = ts;
   else if (sid == "S7")
      g_last_exec_s7 = ts;
   else if (sid == "S8")
      g_last_exec_s8 = ts;
   else if (sid == "S9")
      g_last_exec_s9 = ts;
   else if (sid == "S10")
      g_last_exec_s10 = ts;
   else if (sid == "S11")
      g_last_exec_s11 = ts;
   else if (sid == "S12")
      g_last_exec_s12 = ts;
   else if (sid == "S13")
      g_last_exec_s13 = ts;
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

bool IsStrategyIdValid(const string sid)
{
   return (sid == "S1" || sid == "S2" || sid == "S3" ||
           sid == "S4" || sid == "S5" || sid == "S6" ||
           sid == "S7" || sid == "S8" || sid == "S9" ||
           sid == "S10" || sid == "S11" || sid == "S12" || sid == "S13");
}

string NormalizeStrategyId(const string rawId)
{
   string sid = ToUpperSafe(rawId);
   if (IsStrategyIdValid(sid))
      return sid;
   return "";
}

double GetPipSize(const string symbol)
{
   double point = 0.0;
   if (!SymbolInfoDouble(symbol, SYMBOL_POINT, point) || point <= 0.0)
      point = GetTickSize(symbol);

   int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
   if (digits == 3 || digits == 5)
      return point * 10.0;

   return point;
}

int PipsToTicks(const string symbol, const double pips, const double tickSize)
{
   if (pips <= 0.0 || tickSize <= 0.0)
      return 0;

   double pipSize = GetPipSize(symbol);
   if (pipSize <= 0.0)
      return 0;

   double distance = pips * pipSize;
   if (distance <= 0.0)
      return 0;

   return (int)MathMax(1.0, MathCeil(distance / tickSize));
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

double ResolvePositionVolume(const string symbol,
                             const int slTicks,
                             const double payloadRiskPercent,
                             const StrategyConfig &cfg)
{
   if (UseSignalDirectionOnly)
   {
      if (GlobalLotMode == 0)
      {
         double fixedLots = GlobalFixedLots > 0.0 ? GlobalFixedLots : FallbackFixedLots;
         return NormalizeVolume(symbol, fixedLots);
      }

      double globalRisk = GlobalRiskPercent > 0.0 ? GlobalRiskPercent : 0.25;
      return ComputeLotsFromRiskPercent(symbol, slTicks, globalRisk);
   }

   double effectiveRisk = payloadRiskPercent > 0.0 ? payloadRiskPercent : cfg.riskPercent;
   return ComputeLotsFromRiskPercent(symbol, slTicks, effectiveRisk);
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
                             const ulong positionTicket,
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
         g_managed[i].positionTicket = positionTicket;
         g_managed[i].strategyId = strategyId;
         g_managed[i].symbol = tradeSymbol;
         g_managed[i].side = sideUpper;
         g_managed[i].tp1Price = tp1Price;
         g_managed[i].tp1Done = false;
         g_managed[i].magic = magic;
         g_managed[i].comment = comment;
         PrintFormat("Registered managed position strategy=%s signal=%d ticket=%I64u symbol=%s tp1=%.5f",
                     strategyId,
                     signalId,
                     positionTicket,
                     tradeSymbol,
                     tp1Price);
         return;
      }
   }
}

ulong ResolvePositionTicket(const string tradeSymbol,
                            const string sideUpper,
                            const long magic,
                            const string signalComment)
{
   ulong dealTicket = g_trade.ResultDeal();
   if (dealTicket > 0 && HistoryDealSelect(dealTicket))
   {
      long posId = HistoryDealGetInteger(dealTicket, DEAL_POSITION_ID);
      if (posId > 0)
         return (ulong)posId;
   }

   long expectedType = (sideUpper == "BUY") ? POSITION_TYPE_BUY : POSITION_TYPE_SELL;
   datetime bestOpenTime = 0;
   ulong bestTicket = 0;

   for (int i = PositionsTotal() - 1; i >= 0; i--)
   {
      ulong ticket = PositionGetTicket(i);
      if (ticket == 0 || !PositionSelectByTicket(ticket))
         continue;

      if (PositionGetString(POSITION_SYMBOL) != tradeSymbol)
         continue;

      if ((long)PositionGetInteger(POSITION_MAGIC) != magic)
         continue;

      if ((long)PositionGetInteger(POSITION_TYPE) != expectedType)
         continue;

      string posComment = PositionGetString(POSITION_COMMENT);
      if (signalComment != "" &&
          StringFind(posComment, signalComment) < 0 &&
          StringFind(signalComment, posComment) < 0)
      {
         continue;
      }

      datetime opened = (datetime)PositionGetInteger(POSITION_TIME);
      if (opened >= bestOpenTime)
      {
         bestOpenTime = opened;
         bestTicket = ticket;
      }
   }

   return bestTicket;
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
      ulong ticket = g_managed[i].positionTicket;
      bool selected = false;
      if (ticket > 0)
         selected = PositionSelectByTicket(ticket);
      else
         selected = PositionSelect(symbol);

      if (!selected)
      {
         g_managed[i].active = false;
         continue;
      }

      if (ticket == 0)
      {
         ticket = (ulong)PositionGetInteger(POSITION_TICKET);
         g_managed[i].positionTicket = ticket;
      }

      symbol = PositionGetString(POSITION_SYMBOL);
      if (symbol != g_managed[i].symbol)
         g_managed[i].symbol = symbol;

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
      bool closeAttempted = false;
      if (closeVol > 0.0 && closeVol < posVol)
      {
         closeAttempted = true;
         closeOk = g_trade.PositionClosePartial(ticket, closeVol, MaxSlippage);
      }

      if (!closeAttempted)
      {
         PrintFormat("TP1 partial close skipped strategy=%s signal=%d ticket=%I64u symbol=%s volume=%.2f",
                     g_managed[i].strategyId,
                     g_managed[i].signalId,
                     ticket,
                     symbol,
                     posVol);
         g_managed[i].tp1Done = true;
         continue;
      }

      if (closeOk)
      {
         PrintFormat("TP1 partial close strategy=%s signal=%d ticket=%I64u symbol=%s closed=%.2f",
                     g_managed[i].strategyId,
                     g_managed[i].signalId,
                     ticket,
                     symbol,
                     closeVol);
      }
      else
      {
         PrintFormat("TP1 partial close failed strategy=%s signal=%d ticket=%I64u symbol=%s ret=%u %s",
                     g_managed[i].strategyId,
                     g_managed[i].signalId,
                     ticket,
                     symbol,
                     g_trade.ResultRetcode(),
                     g_trade.ResultRetcodeDescription());
         continue;
      }

      if (MoveStopToBEAfterTP1 && PositionSelectByTicket(ticket))
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

         if (!g_trade.PositionModify(ticket, newSL, tp))
         {
            PrintFormat("Move-to-BE failed strategy=%s signal=%d ticket=%I64u symbol=%s ret=%u %s",
                        g_managed[i].strategyId,
                        g_managed[i].signalId,
                        ticket,
                        symbol,
                        g_trade.ResultRetcode(),
                        g_trade.ResultRetcodeDescription());
         }
      }

      g_managed[i].tp1Done = true;
   }
}

bool IsKnownMagic(const long magic)
{
   return (magic == MagicS1 || magic == MagicS2 || magic == MagicS3 ||
           magic == MagicS4 || magic == MagicS5 || magic == MagicS6 ||
           magic == MagicS7 || magic == MagicS8 || magic == MagicS9 ||
           magic == MagicS10 || magic == MagicS11 || magic == MagicS12 ||
           magic == MagicS13);
}

void ProcessTrailingStops()
{
   if (!EnableGlobalTrailingSL)
      return;

   double startPips = TrailingStartPips;
   if (startPips <= 0.0)
      startPips = 100.0;

   double distancePips = TrailingDistancePips;
   if (distancePips <= 0.0)
      distancePips = 70.0;

   double stepPips = TrailingStepPips;
   if (stepPips <= 0.0)
      stepPips = 10.0;

   for (int i = PositionsTotal() - 1; i >= 0; i--)
   {
      ulong ticket = PositionGetTicket(i);
      if (ticket == 0 || !PositionSelectByTicket(ticket))
         continue;

      string symbol = PositionGetString(POSITION_SYMBOL);
      long magic = (long)PositionGetInteger(POSITION_MAGIC);
      if (!IsKnownMagic(magic))
         continue;

      long posType = (long)PositionGetInteger(POSITION_TYPE);
      if (posType != POSITION_TYPE_BUY && posType != POSITION_TYPE_SELL)
         continue;

      MqlTick tick;
      if (!SymbolInfoTick(symbol, tick))
         continue;

      double openPrice = PositionGetDouble(POSITION_PRICE_OPEN);
      double currentSL = PositionGetDouble(POSITION_SL);
      double currentTP = PositionGetDouble(POSITION_TP);
      int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
      double tickSize = GetTickSize(symbol);

      double pipSize = GetPipSize(symbol);
      if (pipSize <= 0.0)
         continue;

      double startDistance = startPips * pipSize;
      double trailDistance = distancePips * pipSize;
      double stepDistance = stepPips * pipSize;

      if (startDistance <= 0.0 || trailDistance <= 0.0 || stepDistance <= 0.0)
         continue;

      bool shouldModify = false;
      string side = (posType == POSITION_TYPE_BUY ? "BUY" : "SELL");
      double newSL = currentSL;

      if (posType == POSITION_TYPE_BUY)
      {
         double profitDistance = tick.bid - openPrice;
         if (profitDistance < startDistance)
            continue;

         double candidateSL = tick.bid - trailDistance;
         double minLock = openPrice + tickSize;
         if (candidateSL < minLock)
            candidateSL = minLock;

         if (currentSL <= 0.0 || candidateSL > (currentSL + stepDistance))
         {
            newSL = candidateSL;
            shouldModify = true;
         }
      }
      else
      {
         double profitDistance = openPrice - tick.ask;
         if (profitDistance < startDistance)
            continue;

         double candidateSL = tick.ask + trailDistance;
         double maxLock = openPrice - tickSize;
         if (candidateSL > maxLock)
            candidateSL = maxLock;

         if (currentSL <= 0.0 || candidateSL < (currentSL - stepDistance))
         {
            newSL = candidateSL;
            shouldModify = true;
         }
      }

      if (!shouldModify)
         continue;

      newSL = NormalizeDouble(newSL, digits);
      double adjustedSL = newSL;
      double adjustedTP = currentTP;
      AdjustStopsForBroker(symbol, side, tick.bid, tick.ask, adjustedSL, adjustedTP);

      if (adjustedSL <= 0.0 || adjustedSL == currentSL)
         continue;

      g_trade.SetExpertMagicNumber((ulong)magic);
      g_trade.SetDeviationInPoints(MaxSlippage);

      if (!g_trade.PositionModify(ticket, adjustedSL, currentTP))
      {
         PrintFormat("Trailing SL failed ticket=%I64u symbol=%s ret=%u %s",
                     ticket,
                     symbol,
                     g_trade.ResultRetcode(),
                     g_trade.ResultRetcodeDescription());
      }
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
                         const double slPercent,
                         const double tp1Percent,
                         const double tp2Percent,
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

   double price = 0.0;
   double sl = 0.0;
   double tp = 0.0;
   double tp1Price = 0.0;

   string sideUpper = ToUpperSafe(side);

   if (sideUpper == "BUY")
      price = tick.ask;
   else if (sideUpper == "SELL")
      price = tick.bid;
   else
   {
      PrintFormat("Signal id=%d rejected: unsupported side '%s'", signalId, side);
      return false;
   }

   int finalSlTicks = slTicks > 0 ? slTicks : DefaultSLTicks;
   int finalTp1Ticks = tp1Ticks > 0 ? tp1Ticks : DefaultTP1Ticks;
   int finalTpTicks = tp2Ticks;
   if (finalTpTicks <= 0)
      finalTpTicks = tp1Ticks > 0 ? tp1Ticks : DefaultTP2Ticks;

   if (UseSignalDirectionOnly)
   {
      int globalSlTicks = PipsToTicks(tradeSymbol, GlobalSLPips, tickSize);
      if (globalSlTicks > 0)
         finalSlTicks = globalSlTicks;

      if (finalSlTicks <= 0)
         finalSlTicks = DefaultSLTicks;

      if (GlobalUseRiskReward)
      {
         double rr = GlobalRiskReward;
         if (rr < 0.2)
            rr = 0.2;
         finalTpTicks = (int)MathMax(1.0, MathCeil(finalSlTicks * rr));
      }
      else
      {
         int globalTpTicks = PipsToTicks(tradeSymbol, GlobalTPPips, tickSize);
         if (globalTpTicks > 0)
            finalTpTicks = globalTpTicks;
      }

      if (finalTpTicks <= 0)
         finalTpTicks = DefaultTP2Ticks;

      double tp1Frac = GlobalTP1Fraction;
      if (tp1Frac <= 0.05 || tp1Frac >= 1.0)
         tp1Frac = 0.5;
      finalTp1Ticks = (int)MathMax(1.0, MathFloor(finalTpTicks * tp1Frac));
      if (finalTp1Ticks >= finalTpTicks)
         finalTp1Ticks = MathMax(1, finalTpTicks - 1);
      if (!UseTP1TP2)
         finalTp1Ticks = finalTpTicks;
   }
   else
   {
      if (slPercent > 0.0)
      {
         int pctTicks = TicksFromPercent(price, slPercent, tickSize);
         if (pctTicks > 0)
            finalSlTicks = pctTicks;
      }

      if (tp1Percent > 0.0)
      {
         int pctTicks = TicksFromPercent(price, tp1Percent, tickSize);
         if (pctTicks > 0)
            finalTp1Ticks = pctTicks;
      }

      if (tp2Percent > 0.0)
      {
         int pctTicks = TicksFromPercent(price, tp2Percent, tickSize);
         if (pctTicks > 0)
            finalTpTicks = pctTicks;
      }
   }

   if (finalSlTicks <= 0)
      finalSlTicks = DefaultSLTicks;
   if (finalTp1Ticks <= 0)
      finalTp1Ticks = DefaultTP1Ticks;
   if (finalTpTicks <= 0)
      finalTpTicks = DefaultTP2Ticks;
   if (finalTpTicks < finalTp1Ticks)
      finalTpTicks = finalTp1Ticks;

   double effectiveRisk = riskPercent > 0.0 ? riskPercent : cfg.riskPercent;
   double volume = ResolvePositionVolume(tradeSymbol, finalSlTicks, riskPercent, cfg);

   if (sideUpper == "BUY")
   {
      sl = NormalizeDouble(price - finalSlTicks * tickSize, digits);
      tp1Price = NormalizeDouble(price + finalTp1Ticks * tickSize, digits);
      tp = NormalizeDouble(price + finalTpTicks * tickSize, digits);
   }
   else
   {
      sl = NormalizeDouble(price + finalSlTicks * tickSize, digits);
      tp1Price = NormalizeDouble(price - finalTp1Ticks * tickSize, digits);
      tp = NormalizeDouble(price - finalTpTicks * tickSize, digits);
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
         TryApplyPostFillStops(tradeSymbol, sideUpper, finalSlTicks, finalTpTicks);
   }

   if (ok)
   {
      double entryFilled = g_trade.ResultPrice();
      if (entryFilled <= 0.0)
         entryFilled = price;

      if (finalTp1Ticks > 0)
      {
         if (sideUpper == "BUY")
            tp1Price = NormalizeDouble(entryFilled + finalTp1Ticks * tickSize, digits);
         else
            tp1Price = NormalizeDouble(entryFilled - finalTp1Ticks * tickSize, digits);
      }

      ulong positionTicket = ResolvePositionTicket(tradeSymbol, sideUpper, cfg.magic, signalComment);
      if (UseTP1TP2 && finalTp1Ticks > 0 && tp1Price > 0.0)
         RegisterManagedPosition(signalId, positionTicket, strategyId, tradeSymbol, sideUpper, tp1Price, cfg.magic, signalComment);

      PrintFormat("%s placed %s id=%d symbol=%s lots=%.2f sl=%.5f tp1=%.5f tp2=%.5f magic=%I64d ticket=%I64u comment=%s ret=%u %s",
                  strategyId,
                  sideUpper,
                  signalId,
                  tradeSymbol,
                  volume,
                  sl,
                  tp1Price,
                  tp,
                  cfg.magic,
                  positionTicket,
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
   if (sid == "S1" || sid == "S2" || sid == "S3" ||
       sid == "S4" || sid == "S5" || sid == "S6" ||
       sid == "S7" || sid == "S8" || sid == "S9" ||
       sid == "S10" || sid == "S11" || sid == "S12" || sid == "S13")
      return sid;

   string c = ToUpperSafe(payloadComment);
   // Match longer IDs first to avoid "S10/S11/S12/S13" being read as "S1".
   if (StringFind(c, "S13_") >= 0 || c == "S13")
      return "S13";
   if (StringFind(c, "S12_") >= 0 || c == "S12")
      return "S12";
   if (StringFind(c, "S11_") >= 0 || c == "S11")
      return "S11";
   if (StringFind(c, "S10_") >= 0 || c == "S10")
      return "S10";
   if (StringFind(c, "S9_") >= 0 || c == "S9")
      return "S9";
   if (StringFind(c, "S8_") >= 0 || c == "S8")
      return "S8";
   if (StringFind(c, "S7_") >= 0 || c == "S7")
      return "S7";
   if (StringFind(c, "S6_") >= 0 || c == "S6")
      return "S6";
   if (StringFind(c, "S5_") >= 0 || c == "S5")
      return "S5";
   if (StringFind(c, "S4_") >= 0 || c == "S4")
      return "S4";
   if (StringFind(c, "S3_") >= 0 || c == "S3")
      return "S3";
   if (StringFind(c, "S2_") >= 0 || c == "S2")
      return "S2";
   if (StringFind(c, "S1_") >= 0 || c == "S1")
      return "S1";

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

   int signalId = -1;
   if (!JsonTryGetInt(body, "id", signalId))
      return;

   // Relay restart can reset ids to a lower value; resync local cursor automatically.
   if (signalId < g_last_id)
   {
      int resyncId = signalId;
      if (updated && signalId > 0)
         resyncId = signalId - 1;

      if (resyncId < 0)
         resyncId = 0;

      PrintFormat("Relay id rollback detected: local_last_id=%d relay_id=%d updated=%s -> resync_local_id=%d",
                  g_last_id,
                  signalId,
                  (updated ? "true" : "false"),
                  resyncId);

      g_last_id = resyncId;
      SaveLastSignalId();
   }

   if (!updated)
      return;

   if (signalId <= g_last_id)
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
   double slPercent = 0.0;
   double tp1Percent = 0.0;
   double tp2Percent = 0.0;

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
   JsonTryGetDouble(body, "sl_percent", slPercent);
   JsonTryGetDouble(body, "tp1_percent", tp1Percent);
   JsonTryGetDouble(body, "tp2_percent", tp2Percent);

   side = ToUpperSafe(side);
   source = ToUpperSafe(source);

   string strategyId = "";
   if (UseSignalDirectionOnly)
   {
      strategyId = NormalizeStrategyId(DirectionOnlyStrategyId);
      if (strategyId == "")
         strategyId = "S1";
   }
   else
   {
      strategyId = InferStrategyId(payloadStrategyId, payloadComment);
      if (strategyId == "")
      {
         PrintFormat("Signal id=%d ignored: missing/invalid strategy_id payload_strategy_id=%s payload_comment=%s",
                     signalId,
                     payloadStrategyId,
                     payloadComment);
         AdvanceLastId(signalId);
         return;
      }
   }

   if (StrictQuantowerGoldOnly)
   {
      if (source != "QUANTOWER")
      {
         PrintFormat("Signal id=%d ignored: source=%s (expected quantower)", signalId, source);
         AdvanceLastId(signalId);
         return;
      }

      if (!IsGoldSymbolLike(sourceSymbol))
      {
         PrintFormat("Signal id=%d ignored: src_symbol=%s is not recognized as gold symbol",
                     signalId, sourceSymbol);
         AdvanceLastId(signalId);
         return;
      }

      if (!IsRelaySymbolMatch(sourceSymbol))
      {
         PrintFormat("Signal id=%d ignored: src_symbol=%s does not match RelayFuturesSymbol/root=%s",
                     signalId, sourceSymbol, RelayFuturesSymbol);
         AdvanceLastId(signalId);
         return;
      }
   }

   if (IgnoreNonRelayFuturesSymbol &&
       RelayFuturesSymbol != "" &&
       sourceSymbol != "" &&
       !IsRelaySymbolMatch(sourceSymbol))
   {
      PrintFormat("Signal id=%d ignored: src_symbol=%s does not match RelayFuturesSymbol/root=%s",
                  signalId, sourceSymbol, RelayFuturesSymbol);
      AdvanceLastId(signalId);
      return;
   }

   StrategyConfig cfg;
   if (!GetStrategyConfig(strategyId, cfg))
   {
      PrintFormat("Signal id=%d ignored: strategy config not found for %s", signalId, strategyId);
      AdvanceLastId(signalId);
      return;
   }

   if (!cfg.enabled && !UseSignalDirectionOnly)
   {
      PrintFormat("Signal id=%d ignored: %s disabled", signalId, strategyId);
      AdvanceLastId(signalId);
      return;
   }

   datetime nowTs = TimeCurrent();
   datetime lastExec = GetLastExec(strategyId);
   if (lastExec > 0 && (nowTs - lastExec) < CooldownSeconds)
   {
      PrintFormat("Signal id=%d ignored: %s cooldown active (%d sec)", signalId, strategyId, (int)(nowTs - lastExec));
      AdvanceLastId(signalId);
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

   if (UseSignalDirectionOnly)
   {
      PrintFormat("Signal received id=%d strategy=%s side=%s src_symbol=%s trade_symbol=%s mode=direction_only sl_pips=%.1f tp_mode=%s rr=%.2f tp_pips=%.1f lot_mode=%d fixed_lots=%.2f risk_pct=%.2f comment=%s",
                  signalId,
                  strategyId,
                  side,
                  sourceSymbol,
                  tradeSymbol,
                  GlobalSLPips,
                  (GlobalUseRiskReward ? "RR" : "PIPS"),
                  GlobalRiskReward,
                  GlobalTPPips,
                  GlobalLotMode,
                  GlobalFixedLots,
                  GlobalRiskPercent,
                  finalComment);
   }
   else
   {
      PrintFormat("Signal received id=%d strategy=%s side=%s src_symbol=%s trade_symbol=%s sl=%d(%.3f%%) tp1=%d(%.3f%%) tp2=%d(%.3f%%) risk=%.2f comment=%s",
                  signalId,
                  strategyId,
                  side,
                  sourceSymbol,
                  tradeSymbol,
                  slTicks,
                  slPercent,
                  tp1Ticks,
                  tp1Percent,
                  tp2Ticks,
                  tp2Percent,
                  effectiveRisk,
                  finalComment);
   }

   if (DryRunOnly)
   {
      PrintFormat("DryRunOnly=true -> order not sent for signal id=%d", signalId);
      AdvanceLastId(signalId);
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
                                 slPercent,
                                 tp1Percent,
                                 tp2Percent,
                                 effectiveRisk,
                                 finalComment);

   AdvanceLastId(signalId);
   SetLastExec(strategyId, nowTs);

   if (!ok)
      PrintFormat("Signal id=%d handled with trade failure (debounced to avoid duplicates).", signalId);
}

int OnInit()
{
   if (!IsSecretConfigured(RelaySecret))
   {
      Print("RelaySecret is missing/placeholder. Set a real shared secret in EA inputs.");
      return INIT_PARAMETERS_INCORRECT;
   }

   LoadLastSignalId();
   EventSetTimer(PollSeconds);
   g_trade.SetAsyncMode(false);

   for (int i = 0; i < MAX_MANAGED; i++)
      g_managed[i].active = false;

   int enabledCount = 0;
   if (EnableS1) enabledCount++;
   if (EnableS2) enabledCount++;
   if (EnableS3) enabledCount++;
   if (EnableS4) enabledCount++;
   if (EnableS5) enabledCount++;
   if (EnableS6) enabledCount++;
   if (EnableS7) enabledCount++;
   if (EnableS8) enabledCount++;
   if (EnableS9) enabledCount++;
   if (EnableS10) enabledCount++;
   if (EnableS11) enabledCount++;
   if (EnableS12) enabledCount++;
   if (EnableS13) enabledCount++;

   PrintFormat("Relay poller started. chart_symbol=%s use_chart_symbol=%s strict_gold_only=%s enabled_strategies=%d tp1tp2=%s direction_only=%s lot_mode=%d trailing=%s",
               _Symbol,
               (UseChartSymbolForExecution ? "true" : "false"),
               (StrictQuantowerGoldOnly ? "true" : "false"),
               enabledCount,
               (UseTP1TP2 ? "true" : "false"),
               (UseSignalDirectionOnly ? "true" : "false"),
               GlobalLotMode,
               (EnableGlobalTrailingSL ? "true" : "false"));
   return INIT_SUCCEEDED;
}

void OnDeinit(const int reason)
{
   SaveLastSignalId();
   EventKillTimer();
}

void OnTimer()
{
   string url = StringFormat("%s/signal?id=%d&auth=%s", RelayBaseUrl, g_last_id, UrlEncode(RelaySecret));

   char requestBody[];
   char responseBody[];
   string responseHeaders;

   ResetLastError();
   int status = WebRequest("GET", url, "", 5000, requestBody, responseBody, responseHeaders);
   if (status == -1)
   {
      int err = GetLastError();
      string msg = StringFormat("WebRequest failed err=%d", err);
      Print(msg);
      TrackRelayPollFailure(msg);
      ProcessManagedPositions();
      ProcessTrailingStops();
      return;
   }

   string body = CharArrayToString(responseBody, 0, -1, CP_UTF8);

   if (status != 200)
   {
      string msg = StringFormat("Relay HTTP %d: %s", status, body);
      Print(msg);
      TrackRelayPollFailure(msg);
      ProcessManagedPositions();
      ProcessTrailingStops();
      return;
   }

   ResetRelayPollFailureState();
   ProcessSignalPayload(body);
   ProcessManagedPositions();
   ProcessTrailingStops();
}
