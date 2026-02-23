using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;

namespace RelaySignalStrategies;

public sealed class MultiStrategySignalEngine : Strategy
{
    [InputParameter("SymbolName", 0)]
    public string SymbolName = "/GCJ26:XCEC";

    [InputParameter("RelayUrl", 1)]
    public string RelayUrl = "http://127.0.0.1:8000/signal";

    [InputParameter("RelaySecret", 2)]
    public string RelaySecret = string.Empty;

    [InputParameter("TradeSymbolForMT5", 3)]
    public string TradeSymbolForMT5 = "XAUUSD";

    [InputParameter("EnableAutoMode", 4)]
    public bool EnableAutoMode = true;

    [InputParameter("EnableS1", 5)]
    public bool EnableS1 = true;

    [InputParameter("RiskS1 (%)", 6, 0.01, 10.0, 0.01, 2)]
    public double RiskS1 = 0.25;

    [InputParameter("CommentS1", 7)]
    public string CommentS1 = "S1_ABSORB";

    [InputParameter("EnableS2", 8)]
    public bool EnableS2 = true;

    [InputParameter("RiskS2 (%)", 9, 0.01, 10.0, 0.01, 2)]
    public double RiskS2 = 0.25;

    [InputParameter("CommentS2", 10)]
    public string CommentS2 = "S2_VACUUM";

    [InputParameter("EnableS3", 11)]
    public bool EnableS3 = true;

    [InputParameter("RiskS3 (%)", 12, 0.01, 10.0, 0.01, 2)]
    public double RiskS3 = 0.25;

    [InputParameter("CommentS3", 13)]
    public string CommentS3 = "S3_RETEST";

    [InputParameter("EnableS4", 140)]
    public bool EnableS4 = true;

    [InputParameter("RiskS4 (%)", 141, 0.01, 10.0, 0.01, 2)]
    public double RiskS4 = 0.25;

    [InputParameter("CommentS4", 142)]
    public string CommentS4 = "S4_ORB_RET";

    [InputParameter("EnableS5", 143)]
    public bool EnableS5 = true;

    [InputParameter("RiskS5 (%)", 144, 0.01, 10.0, 0.01, 2)]
    public double RiskS5 = 0.25;

    [InputParameter("CommentS5", 145)]
    public string CommentS5 = "S5_FA_REV";

    [InputParameter("EnableS6", 146)]
    public bool EnableS6 = true;

    [InputParameter("RiskS6 (%)", 147, 0.01, 10.0, 0.01, 2)]
    public double RiskS6 = 0.25;

    [InputParameter("CommentS6", 148)]
    public string CommentS6 = "S6_LVN_ROT";

    [InputParameter("EnableS7", 149)]
    public bool EnableS7 = true;

    [InputParameter("RiskS7 (%)", 150, 0.01, 10.0, 0.01, 2)]
    public double RiskS7 = 0.25;

    [InputParameter("CommentS7", 151)]
    public string CommentS7 = "S7_POC_CONT";

    [InputParameter("EnableS8", 152)]
    public bool EnableS8 = true;

    [InputParameter("RiskS8 (%)", 153, 0.01, 10.0, 0.01, 2)]
    public double RiskS8 = 0.25;

    [InputParameter("CommentS8", 154)]
    public string CommentS8 = "S8_VWAP_MR";

    [InputParameter("EnableS9", 155)]
    public bool EnableS9 = true;

    [InputParameter("RiskS9 (%)", 156, 0.01, 10.0, 0.01, 2)]
    public double RiskS9 = 0.25;

    [InputParameter("CommentS9", 157)]
    public string CommentS9 = "S9_VWAP_TF";

    [InputParameter("EnableS10", 158)]
    public bool EnableS10 = true;

    [InputParameter("RiskS10 (%)", 159, 0.01, 10.0, 0.01, 2)]
    public double RiskS10 = 0.25;

    [InputParameter("CommentS10", 160)]
    public string CommentS10 = "S10_ICE_BRK";

    [InputParameter("EnableS11", 161)]
    public bool EnableS11 = true;

    [InputParameter("RiskS11 (%)", 162, 0.01, 10.0, 0.01, 2)]
    public double RiskS11 = 0.25;

    [InputParameter("CommentS11", 163)]
    public string CommentS11 = "S11_SWEEP_RECL";

    [InputParameter("EnableS12", 164)]
    public bool EnableS12 = true;

    [InputParameter("RiskS12 (%)", 165, 0.01, 10.0, 0.01, 2)]
    public double RiskS12 = 0.25;

    [InputParameter("CommentS12", 166)]
    public string CommentS12 = "S12_DELTA_DIV";

    [InputParameter("EnableS13", 167)]
    public bool EnableS13 = true;

    [InputParameter("RiskS13 (%)", 168, 0.01, 10.0, 0.01, 2)]
    public double RiskS13 = 0.25;

    [InputParameter("CommentS13", 169)]
    public string CommentS13 = "S13_MTF_PULL";

    [InputParameter("orbBars", 170, 5, 180, 1, 0)]
    public int orbBars = 15;

    [InputParameter("cooldownSeconds", 14, 1, 3600, 1, 0)]
    public int cooldownSeconds = 20;

    [InputParameter("lookbackMinutes", 15, 10, 240, 1, 0)]
    public int lookbackMinutes = 15;

    [InputParameter("domLevels", 16, 1, 20, 1, 0)]
    public int domLevels = 10;

    [InputParameter("wallSizeThreshold", 17, 1, 100000, 1, 0)]
    public int wallSizeThreshold = 80;

    [InputParameter("wallPersistenceSeconds", 18, 1, 120, 1, 0)]
    public int wallPersistenceSeconds = 6;

    [InputParameter("supportBufferTicks", 19, 0, 100, 1, 0)]
    public int supportBufferTicks = 2;

    [InputParameter("breakTicks", 20, 0, 100, 1, 0)]
    public int breakTicks = 2;

    [InputParameter("confirmTicks", 21, 0, 100, 1, 0)]
    public int confirmTicks = 2;

    [InputParameter("noProgressSeconds", 22, 1, 120, 1, 0)]
    public int noProgressSeconds = 8;

    [InputParameter("aggressionWindowSeconds", 23, 1, 120, 1, 0)]
    public int aggressionWindowSeconds = 10;

    [InputParameter("aggressionThreshold", 24, 1, 1000, 1, 0)]
    public int aggressionThreshold = 12;

    [InputParameter("pullWindowSeconds", 25, 1, 60, 1, 0)]
    public int pullWindowSeconds = 4;

    [InputParameter("pullPercentThreshold", 26, 0.05, 0.95, 0.01, 2)]
    public double pullPercentThreshold = 0.25;

    [InputParameter("compressionMaxTicks", 27, 1, 300, 1, 0)]
    public int compressionMaxTicks = 16;

    [InputParameter("deltaFlipThreshold", 28, 1, 500, 1, 0)]
    public int deltaFlipThreshold = 10;

    [InputParameter("microNoiseBars", 29, 2, 60, 1, 0)]
    public int microNoiseBars = 8;

    [InputParameter("minDynamicSlTicks", 30, 1, 10000, 1, 0)]
    public int minDynamicSlTicks = 20;

    [InputParameter("maxDynamicSlTicks", 31, 2, 20000, 1, 0)]
    public int maxDynamicSlTicks = 250;

    [InputParameter("minRR_TP1", 32, 0.5, 5.0, 0.1, 2)]
    public double minRR_TP1 = 1.0;

    [InputParameter("minRR_TP2", 33, 1.0, 8.0, 0.1, 2)]
    public double minRR_TP2 = 2.0;

    [InputParameter("ManualTriggerS1", 34)]
    public bool ManualTriggerS1 = false;

    [InputParameter("ManualSideS1 (BUY/SELL)", 35)]
    public string ManualSideS1 = "BUY";

    [InputParameter("ManualTriggerS2", 36)]
    public bool ManualTriggerS2 = false;

    [InputParameter("ManualSideS2 (BUY/SELL)", 37)]
    public string ManualSideS2 = "BUY";

    [InputParameter("ManualTriggerS3", 38)]
    public bool ManualTriggerS3 = false;

    [InputParameter("ManualSideS3 (BUY/SELL)", 39)]
    public string ManualSideS3 = "BUY";

    [InputParameter("ManualTriggerAny", 40)]
    public bool ManualTriggerAny = false;

    [InputParameter("ManualStrategyId (S1..S13)", 41)]
    public string ManualStrategyId = "S1";

    [InputParameter("ManualSideAny (BUY/SELL)", 42)]
    public string ManualSideAny = "BUY";

    [InputParameter("ManualCommentAny", 43)]
    public string ManualCommentAny = "manual_test";

    [InputParameter("EnableIndicatorFallback", 171)]
    public bool EnableIndicatorFallback = true;

    [InputParameter("IndicatorFallbackOnlyWhenOrderflowCold", 172)]
    public bool IndicatorFallbackOnlyWhenOrderflowCold = false;

    [InputParameter("IndicatorFastEmaPeriod", 173, 2, 200, 1, 0)]
    public int IndicatorFastEmaPeriod = 9;

    [InputParameter("IndicatorSlowEmaPeriod", 174, 3, 400, 1, 0)]
    public int IndicatorSlowEmaPeriod = 21;

    [InputParameter("IndicatorRsiPeriod", 175, 2, 100, 1, 0)]
    public int IndicatorRsiPeriod = 9;

    [InputParameter("IndicatorRsiBuyThreshold", 176, 50, 90, 1, 0)]
    public int IndicatorRsiBuyThreshold = 52;

    [InputParameter("IndicatorRsiSellThreshold", 177, 10, 50, 1, 0)]
    public int IndicatorRsiSellThreshold = 48;

    [InputParameter("IndicatorAtrPeriod", 178, 2, 100, 1, 0)]
    public int IndicatorAtrPeriod = 14;

    [InputParameter("IndicatorAtrStopMultiplier", 179, 0.5, 5.0, 0.1, 2)]
    public double IndicatorAtrStopMultiplier = 1.2;

    [InputParameter("IndicatorTp1RR", 180, 0.5, 5.0, 0.1, 2)]
    public double IndicatorTp1RR = 1.2;

    [InputParameter("IndicatorTp2RR", 181, 1.0, 8.0, 0.1, 2)]
    public double IndicatorTp2RR = 2.4;

    [InputParameter("IndicatorBbPeriod", 182, 5, 200, 1, 0)]
    public int IndicatorBbPeriod = 20;

    [InputParameter("IndicatorBbDeviation", 183, 0.5, 4.0, 0.1, 2)]
    public double IndicatorBbDeviation = 2.0;

    [InputParameter("IndicatorMinBandWidthTicks", 184, 1, 500, 1, 0)]
    public int IndicatorMinBandWidthTicks = 3;

    [InputParameter("IndicatorMaxSpreadAtrFraction", 185, 0.05, 1.0, 0.05, 2)]
    public double IndicatorMaxSpreadAtrFraction = 0.60;

    [InputParameter("UsePercentTargetsForGold", 186)]
    public bool UsePercentTargetsForGold = true;

    [InputParameter("SlPercentOfGoldPrice", 187, 0.05, 5.0, 0.01, 2)]
    public double SlPercentOfGoldPrice = 0.30;

    [InputParameter("Tp1PercentOfGoldPrice", 188, 0.05, 10.0, 0.01, 2)]
    public double Tp1PercentOfGoldPrice = 0.60;

    [InputParameter("Tp2PercentOfGoldPrice", 189, 0.05, 20.0, 0.01, 2)]
    public double Tp2PercentOfGoldPrice = 1.20;

    [InputParameter("EnableFrequentTestSignals", 190)]
    public bool EnableFrequentTestSignals = false;

    [InputParameter("FrequentTestStrategyId (S1..S13)", 191)]
    public string FrequentTestStrategyId = "S1";

    [InputParameter("FrequentTestBarsInterval", 192, 1, 30, 1, 0)]
    public int FrequentTestBarsInterval = 1;

    [InputParameter("FrequentTestCooldownSeconds", 193, 1, 300, 1, 0)]
    public int FrequentTestCooldownSeconds = 1;

    [InputParameter("EnableRelayFailureAlert", 194)]
    public bool EnableRelayFailureAlert = true;

    [InputParameter("RelayFailureAlertThreshold", 195, 1, 100, 1, 0)]
    public int RelayFailureAlertThreshold = 5;

    private Symbol? _symbol;
    private HistoricalData? _history;
    private HttpClient? _http;

    private readonly Queue<AggressionSample> _aggr = new();
    private readonly Queue<PriceSample> _prices = new();
    private readonly Queue<DomSample> _dom = new();

    private readonly Dictionary<string, RuntimeState> _states = new(StringComparer.OrdinalIgnoreCase)
    {
        { "S1", new RuntimeState() },
        { "S2", new RuntimeState() },
        { "S3", new RuntimeState() },
        { "S4", new RuntimeState() },
        { "S5", new RuntimeState() },
        { "S6", new RuntimeState() },
        { "S7", new RuntimeState() },
        { "S8", new RuntimeState() },
        { "S9", new RuntimeState() },
        { "S10", new RuntimeState() },
        { "S11", new RuntimeState() },
        { "S12", new RuntimeState() },
        { "S13", new RuntimeState() }
    };

    private double _lastBid = double.NaN;
    private double _lastAsk = double.NaN;
    private double _lastTrade = double.NaN;
    private bool _manualAnyConsumed;
    private readonly object _initGate = new();
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private int _relayFailureCount;
    private Timer? _bootstrapTimer;
    private DateTime _lastBootstrapLogUtc = DateTime.MinValue;
    private bool _subscriptionsActive;
    private bool _isStopping;

    public MultiStrategySignalEngine()
    {
        Name = nameof(MultiStrategySignalEngine);
        Description = "S1-S13 multi strategy signal relay with dynamic zone SL/TP.";
    }

    protected override void OnRun()
    {
        _isStopping = false;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

        if (!IsSecretConfigured(RelaySecret))
            this.LogError("RelaySecret is missing. Set RelaySecret in strategy settings.");

        TryInitializeSymbolAndSubscriptions(logWaiting: true);
        if (!_subscriptionsActive)
            StartBootstrapTimer();
    }

    protected override void OnStop()
    {
        _isStopping = true;
        StopBootstrapTimer();

        lock (_initGate)
        {
            if (_symbol != null)
            {
                _symbol.NewQuote -= OnSymbolNewQuote;
                _symbol.NewLast -= OnSymbolNewLast;
                _symbol.NewLevel2 -= OnSymbolNewLevel2;
                _symbol = null;
            }

            if (_history != null)
            {
                _history.NewHistoryItem -= OnHistoryChanged;
                _history.HistoryItemUpdated -= OnHistoryChanged;
                _history.Dispose();
                _history = null;
            }

            _subscriptionsActive = false;
        }

        _http?.Dispose();
        _http = null;

        _aggr.Clear();
        _prices.Clear();
        _dom.Clear();
        foreach (var st in _states.Values)
            st.Reset();
        _manualAnyConsumed = false;

        this.LogInfo("Stopped");
    }

    protected override void OnSettingsUpdated()
    {
        base.OnSettingsUpdated();

        if (!ManualTriggerS1) _states["S1"].ManualConsumed = false;
        if (!ManualTriggerS2) _states["S2"].ManualConsumed = false;
        if (!ManualTriggerS3) _states["S3"].ManualConsumed = false;
        if (!ManualTriggerAny) _manualAnyConsumed = false;

        if (!_subscriptionsActive)
        {
            TryInitializeSymbolAndSubscriptions(logWaiting: true);
            if (!_subscriptionsActive)
                StartBootstrapTimer();
        }

        TryHandleManualTriggers(DateTime.UtcNow);
    }

    private void TryInitializeSymbolAndSubscriptions(bool logWaiting)
    {
        var initializedNow = false;
        var boundSymbol = string.Empty;

        lock (_initGate)
        {
            if (_subscriptionsActive || _isStopping)
                return;

            var resolved = ResolveSymbol(SymbolName);
            if (resolved == null)
            {
                if (logWaiting && DateTime.UtcNow - _lastBootstrapLogUtc >= TimeSpan.FromSeconds(20))
                {
                    this.LogInfo($"Symbol '{SymbolName}' not available yet; waiting for connection/data.");
                    _lastBootstrapLogUtc = DateTime.UtcNow;
                }

                return;
            }

            _symbol = resolved;
            boundSymbol = _symbol.Name;
            _history = _symbol.GetHistory(Period.MIN1, DateTime.UtcNow.AddMinutes(-Math.Max(lookbackMinutes * 3, 60)), DateTime.UtcNow);

            _symbol.NewQuote += OnSymbolNewQuote;
            _symbol.NewLast += OnSymbolNewLast;
            _symbol.NewLevel2 += OnSymbolNewLevel2;

            if (_history != null)
            {
                _history.NewHistoryItem += OnHistoryChanged;
                _history.HistoryItemUpdated += OnHistoryChanged;
            }

            _subscriptionsActive = true;
            initializedNow = true;
        }

        if (!initializedNow)
            return;

        StopBootstrapTimer();
        this.LogInfo($"Started {Name}. symbol={boundSymbol} auto={EnableAutoMode}");
        TryHandleManualTriggers(DateTime.UtcNow);
    }

    private void StartBootstrapTimer()
    {
        if (_bootstrapTimer != null || _isStopping)
            return;

        _bootstrapTimer = new Timer(_ =>
        {
            if (_isStopping)
                return;

            TryInitializeSymbolAndSubscriptions(logWaiting: true);
            if (_subscriptionsActive)
                StopBootstrapTimer();
        }, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
    }

    private void StopBootstrapTimer()
    {
        var timer = _bootstrapTimer;
        _bootstrapTimer = null;
        timer?.Dispose();
    }

    private void OnHistoryChanged(object sender, HistoryEventArgs e)
    {
        var now = DateTime.UtcNow;
        TryHandleManualTriggers(now);
        if (EnableAutoMode)
            TryEvaluateAuto(now);
    }

    private void OnSymbolNewQuote(Symbol symbol, Quote quote)
    {
        if (_symbol == null || symbol.Id != _symbol.Id)
            return;

        _lastBid = quote.Bid;
        _lastAsk = quote.Ask;
    }

    private void OnSymbolNewLevel2(Symbol symbol, Level2Quote level2, DOMQuote dom)
    {
        if (_symbol == null || symbol.Id != _symbol.Id || dom == null)
            return;

        CalculateDomStats(dom, out var bidDepth, out var askDepth, out var bidWallPrice, out var bidWallSize, out var askWallPrice, out var askWallSize);

        var now = DateTime.UtcNow;
        var total = bidDepth + askDepth;
        var imbalance = total > 0 ? bidDepth / total : 0.5;
        _dom.Enqueue(new DomSample(now, bidDepth, askDepth, Clamp01(imbalance), bidWallPrice, bidWallSize, askWallPrice, askWallSize));
        TrimQueues(now);
    }

    private void OnSymbolNewLast(Symbol symbol, Last last)
    {
        if (_symbol == null || symbol.Id != _symbol.Id)
            return;

        var now = DateTime.UtcNow;
        RecordAggression(last, now);
        _prices.Enqueue(new PriceSample(now, last.Price));
        TrimQueues(now);

        TryHandleManualTriggers(now);

        if (EnableAutoMode)
            TryEvaluateAuto(now);
    }

    private void TryHandleManualTriggers(DateTime nowUtc)
    {
        TryHandleManual("S1", EnableS1, ref ManualTriggerS1, ManualSideS1, nowUtc);
        TryHandleManual("S2", EnableS2, ref ManualTriggerS2, ManualSideS2, nowUtc);
        TryHandleManual("S3", EnableS3, ref ManualTriggerS3, ManualSideS3, nowUtc);
        TryHandleManualAny(nowUtc);
    }

    private void TryHandleManual(string strategyId, bool enabled, ref bool trigger, string sideRaw, DateTime nowUtc)
    {
        var state = _states[strategyId];
        if (!enabled)
            return;

        if (!trigger)
        {
            state.ManualConsumed = false;
            return;
        }

        if (state.ManualConsumed)
            return;

        var side = ParseSide(sideRaw);
        var price = GetCurrentPrice();
        if (!IsValid(price) || price <= 0)
            return;

        if (TrySendSignal(strategyId, side, "manual_test", nowUtc, price, price, bypassGuards: true))
        {
            state.ManualConsumed = true;
            trigger = false;
            this.LogInfo($"Manual trigger consumed strategy={strategyId} side={side}");
        }
    }

    private void TryHandleManualAny(DateTime nowUtc)
    {
        if (!ManualTriggerAny)
        {
            _manualAnyConsumed = false;
            return;
        }

        if (_manualAnyConsumed)
            return;

        var strategyId = NormalizeStrategyIdInput(ManualStrategyId);
        if (string.IsNullOrEmpty(strategyId))
        {
            _manualAnyConsumed = true;
            ManualTriggerAny = false;
            this.LogError($"ManualTriggerAny ignored: invalid ManualStrategyId='{ManualStrategyId}'. Expected S1..S13.");
            return;
        }

        if (!IsStrategyEnabled(strategyId))
        {
            _manualAnyConsumed = true;
            ManualTriggerAny = false;
            this.LogInfo($"ManualTriggerAny ignored: {strategyId} is disabled.");
            return;
        }

        var side = ParseSide(ManualSideAny);
        var price = GetCurrentPrice();
        if (!IsValid(price) || price <= 0)
            return;

        var setup = string.IsNullOrWhiteSpace(ManualCommentAny) ? "manual_test" : ManualCommentAny.Trim();
        if (TrySendSignal(strategyId, side, setup, nowUtc, price, price, bypassGuards: true))
        {
            _manualAnyConsumed = true;
            ManualTriggerAny = false;
            this.LogInfo($"Manual ANY trigger consumed strategy={strategyId} side={side} setup={setup}");
        }
    }

    private static string NormalizeStrategyIdInput(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var t = raw.Trim().ToUpperInvariant();
        if (t.StartsWith("S", StringComparison.Ordinal))
            t = t.Substring(1);

        if (!int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            return string.Empty;

        return n >= 1 && n <= 13 ? $"S{n}" : string.Empty;
    }

    private bool IsStrategyEnabled(string strategyId)
    {
        return strategyId switch
        {
            "S1" => EnableS1,
            "S2" => EnableS2,
            "S3" => EnableS3,
            "S4" => EnableS4,
            "S5" => EnableS5,
            "S6" => EnableS6,
            "S7" => EnableS7,
            "S8" => EnableS8,
            "S9" => EnableS9,
            "S10" => EnableS10,
            "S11" => EnableS11,
            "S12" => EnableS12,
            "S13" => EnableS13,
            _ => false
        };
    }

    private void TryEvaluateAuto(DateTime nowUtc)
    {
        var price = GetCurrentPrice();
        if (!IsValid(price) || price <= 0)
            return;

        if (!TryBuildContext(nowUtc, price, out var ctx))
            return;

        if (EnableS1)
            EvaluateS1(nowUtc, ctx);
        if (EnableS2)
            EvaluateS2(nowUtc, ctx);
        if (EnableS3)
            EvaluateS3(nowUtc, ctx);
        if (EnableS4)
            EvaluateS4(nowUtc, ctx);
        if (EnableS5)
            EvaluateS5(nowUtc, ctx);
        if (EnableS6)
            EvaluateS6(nowUtc, ctx);
        if (EnableS7)
            EvaluateS7(nowUtc, ctx);
        if (EnableS8)
            EvaluateS8(nowUtc, ctx);
        if (EnableS9)
            EvaluateS9(nowUtc, ctx);
        if (EnableS10)
            EvaluateS10(nowUtc, ctx);
        if (EnableS11)
            EvaluateS11(nowUtc, ctx);
        if (EnableS12)
            EvaluateS12(nowUtc, ctx);
        if (EnableS13)
            EvaluateS13(nowUtc, ctx);

        if (EnableIndicatorFallback)
            EvaluateIndicatorFallback(nowUtc, ctx);

        if (EnableFrequentTestSignals)
            EvaluateFrequentTestSignals(nowUtc, ctx);
    }

    private void EvaluateS1(DateTime nowUtc, Context ctx)
    {
        var st = _states["S1"];
        if (!CanAutoSignal(st, ctx.BarTimeUtc, nowUtc))
            return;

        var buffer = supportBufferTicks * ctx.TickSize;
        var confirm = confirmTicks * ctx.TickSize;

        var hasBidWall = IsValid(ctx.BidWallPrice) && ctx.BidWallPersistenceSeconds >= wallPersistenceSeconds;
        var hasAskWall = IsValid(ctx.AskWallPrice) && ctx.AskWallPersistenceSeconds >= wallPersistenceSeconds;

        if (!st.PendingActive)
        {
            if (hasBidWall)
            {
                var anchor = Math.Min(ctx.Support, ctx.BidWallPrice);
                var near = ctx.Price >= anchor - buffer && ctx.Price <= anchor + buffer;
                var execution = ctx.SellAggression >= aggressionThreshold && HasNoProgress(true, anchor, nowUtc, ctx.TickSize) && ComputeAbsorptionScore(true, anchor, nowUtc, ctx.TickSize) >= 0.45;
                if (near && execution)
                    st.SetPending("BUY", anchor, "absorption_reversal", nowUtc);
            }

            if (hasAskWall)
            {
                var anchor = Math.Max(ctx.Resistance, ctx.AskWallPrice);
                var near = ctx.Price >= anchor - buffer && ctx.Price <= anchor + buffer;
                var execution = ctx.BuyAggression >= aggressionThreshold && HasNoProgress(false, anchor, nowUtc, ctx.TickSize) && ComputeAbsorptionScore(false, anchor, nowUtc, ctx.TickSize) >= 0.45;
                if (near && execution)
                    st.SetPending("SELL", anchor, "absorption_reversal", nowUtc);
            }
        }

        if (!st.PendingActive)
            return;

        if (nowUtc - st.PendingAtUtc > TimeSpan.FromSeconds(Math.Max(noProgressSeconds, aggressionWindowSeconds) + 6))
        {
            st.PendingActive = false;
            return;
        }

        if (st.PendingSide == "BUY" && ctx.Price >= st.PendingLevel + confirm && TrySendSignal("S1", "BUY", st.PendingSetup, nowUtc, ctx.Price, st.PendingLevel, false))
            st.PendingActive = false;

        if (st.PendingSide == "SELL" && ctx.Price <= st.PendingLevel - confirm && TrySendSignal("S1", "SELL", st.PendingSetup, nowUtc, ctx.Price, st.PendingLevel, false))
            st.PendingActive = false;
    }

    private void EvaluateS2(DateTime nowUtc, Context ctx)
    {
        var st = _states["S2"];
        if (!CanAutoSignal(st, ctx.BarTimeUtc, nowUtc))
            return;

        var buffer = supportBufferTicks * ctx.TickSize;
        var confirm = confirmTicks * ctx.TickSize;

        if (!st.PendingActive)
        {
            var compressed = ctx.CompressionTicks <= compressionMaxTicks;

            var nearResistance = ctx.Price >= ctx.Resistance - buffer && ctx.Price <= ctx.Resistance + buffer;
            var buyExecution = ctx.BuyPullScore >= 0.70 && ctx.DomImbalance >= 0.55 && ctx.DeltaNow >= deltaFlipThreshold;
            if (compressed && nearResistance && buyExecution)
                st.SetPending("BUY", ctx.Resistance, "liquidity_vacuum", nowUtc);

            var nearSupport = ctx.Price >= ctx.Support - buffer && ctx.Price <= ctx.Support + buffer;
            var sellExecution = ctx.SellPullScore >= 0.70 && ctx.DomImbalance <= 0.45 && ctx.DeltaNow <= -deltaFlipThreshold;
            if (compressed && nearSupport && sellExecution)
                st.SetPending("SELL", ctx.Support, "liquidity_vacuum", nowUtc);
        }

        if (!st.PendingActive)
            return;

        if (nowUtc - st.PendingAtUtc > TimeSpan.FromSeconds(Math.Max(10, pullWindowSeconds * 3)))
        {
            st.PendingActive = false;
            return;
        }

        if (st.PendingSide == "BUY" && ctx.Price >= st.PendingLevel + confirm && TrySendSignal("S2", "BUY", st.PendingSetup, nowUtc, ctx.Price, st.PendingLevel, false))
            st.PendingActive = false;

        if (st.PendingSide == "SELL" && ctx.Price <= st.PendingLevel - confirm && TrySendSignal("S2", "SELL", st.PendingSetup, nowUtc, ctx.Price, st.PendingLevel, false))
            st.PendingActive = false;
    }

    private void EvaluateS3(DateTime nowUtc, Context ctx)
    {
        var st = _states["S3"];
        if (!CanAutoSignal(st, ctx.BarTimeUtc, nowUtc))
            return;

        var breakDistance = confirmTicks * ctx.TickSize;
        var retestBand = supportBufferTicks * ctx.TickSize;

        if (!st.RetestActive)
        {
            if (ctx.Price >= ctx.Resistance + breakDistance && ctx.DeltaNow >= deltaFlipThreshold)
                st.SetRetest("BUY", ctx.Resistance, nowUtc);
            else if (ctx.Price <= ctx.Support - breakDistance && ctx.DeltaNow <= -deltaFlipThreshold)
                st.SetRetest("SELL", ctx.Support, nowUtc);
        }

        if (!st.RetestActive)
            return;

        if (nowUtc - st.RetestAtUtc > TimeSpan.FromSeconds(Math.Max(40, cooldownSeconds)))
        {
            st.RetestActive = false;
            return;
        }

        var inBand = ctx.Price >= st.RetestLevel - retestBand && ctx.Price <= st.RetestLevel + retestBand;
        if (!inBand)
            return;

        if (st.RetestSide == "BUY")
        {
            var deltaFlip = ctx.DeltaNow >= Math.Max(2, deltaFlipThreshold / 2) || (ctx.DeltaPrev < 0 && ctx.DeltaNow > 0);
            if (deltaFlip && HasNoProgress(true, st.RetestLevel, nowUtc, ctx.TickSize) && TrySendSignal("S3", "BUY", "break_retest", nowUtc, ctx.Price, st.RetestLevel, false))
                st.RetestActive = false;
        }
        else
        {
            var deltaFlip = ctx.DeltaNow <= -Math.Max(2, deltaFlipThreshold / 2) || (ctx.DeltaPrev > 0 && ctx.DeltaNow < 0);
            if (deltaFlip && HasNoProgress(false, st.RetestLevel, nowUtc, ctx.TickSize) && TrySendSignal("S3", "SELL", "break_retest", nowUtc, ctx.Price, st.RetestLevel, false))
                st.RetestActive = false;
        }
    }

    private void EvaluateS4(DateTime nowUtc, Context ctx)
    {
        var st = _states["S4"];
        if (!CanAutoSignal(st, ctx.BarTimeUtc, nowUtc))
            return;
        if (!TryComputeOrbLevels(out var orbHigh, out var orbLow))
            return;

        var buffer = supportBufferTicks * ctx.TickSize;
        var confirm = confirmTicks * ctx.TickSize;

        if (!st.PendingActive)
        {
            if (ctx.Price >= orbHigh + confirm && ctx.DeltaNow >= Math.Max(2, deltaFlipThreshold / 2))
                st.SetPending("BUY", orbHigh, "orb_retest", nowUtc);
            else if (ctx.Price <= orbLow - confirm && ctx.DeltaNow <= -Math.Max(2, deltaFlipThreshold / 2))
                st.SetPending("SELL", orbLow, "orb_retest", nowUtc);
        }

        if (!st.PendingActive)
            return;
        if (nowUtc - st.PendingAtUtc > TimeSpan.FromSeconds(45))
        {
            st.PendingActive = false;
            return;
        }

        var nearRetest = Math.Abs(ctx.Price - st.PendingLevel) <= buffer * 1.5;
        if (!nearRetest)
            return;

        if (st.PendingSide == "BUY" && ctx.DeltaNow > 0 && TrySendSignal("S4", "BUY", st.PendingSetup, nowUtc, ctx.Price, st.PendingLevel, false))
            st.PendingActive = false;
        if (st.PendingSide == "SELL" && ctx.DeltaNow < 0 && TrySendSignal("S4", "SELL", st.PendingSetup, nowUtc, ctx.Price, st.PendingLevel, false))
            st.PendingActive = false;
    }

    private void EvaluateS5(DateTime nowUtc, Context ctx)
    {
        var st = _states["S5"];
        if (!CanAutoSignal(st, ctx.BarTimeUtc, nowUtc))
            return;
        if (!TryGetRecentRange(Math.Max(aggressionWindowSeconds * 2, 10), out var recentLow, out var recentHigh))
            return;

        var breakDistance = Math.Max(1, breakTicks) * ctx.TickSize;
        var confirm = confirmTicks * ctx.TickSize;

        var longFailAuction = recentLow <= ctx.Support - breakDistance && ctx.Price >= ctx.Support + confirm && ctx.DeltaNow > 0;
        var shortFailAuction = recentHigh >= ctx.Resistance + breakDistance && ctx.Price <= ctx.Resistance - confirm && ctx.DeltaNow < 0;

        if (longFailAuction)
            TrySendSignal("S5", "BUY", "failed_auction", nowUtc, ctx.Price, ctx.Support, false);
        else if (shortFailAuction)
            TrySendSignal("S5", "SELL", "failed_auction", nowUtc, ctx.Price, ctx.Resistance, false);
    }

    private void EvaluateS6(DateTime nowUtc, Context ctx)
    {
        var st = _states["S6"];
        if (!CanAutoSignal(st, ctx.BarTimeUtc, nowUtc))
            return;

        var buffer = supportBufferTicks * ctx.TickSize;
        var lvnLower = (ctx.Support + ctx.Poc) * 0.5;
        var lvnUpper = (ctx.Resistance + ctx.Poc) * 0.5;

        var buyCond = Math.Abs(ctx.Price - lvnLower) <= buffer * 2.0 && ctx.Price < ctx.Poc && ctx.BuyAggression >= ctx.SellAggression && ctx.DeltaNow >= -1;
        var sellCond = Math.Abs(ctx.Price - lvnUpper) <= buffer * 2.0 && ctx.Price > ctx.Poc && ctx.SellAggression >= ctx.BuyAggression && ctx.DeltaNow <= 1;

        if (buyCond)
            TrySendSignal("S6", "BUY", "lvn_rotation", nowUtc, ctx.Price, lvnLower, false);
        else if (sellCond)
            TrySendSignal("S6", "SELL", "lvn_rotation", nowUtc, ctx.Price, lvnUpper, false);
    }

    private void EvaluateS7(DateTime nowUtc, Context ctx)
    {
        var st = _states["S7"];
        if (!CanAutoSignal(st, ctx.BarTimeUtc, nowUtc))
            return;

        var nearPoc = Math.Abs(ctx.Price - ctx.Poc) <= supportBufferTicks * ctx.TickSize * 2.0;
        if (!nearPoc)
            return;

        var buyCond = ctx.Price >= ctx.Vwap && ctx.DeltaNow > 0 && ctx.DomImbalance >= 0.52;
        var sellCond = ctx.Price <= ctx.Vwap && ctx.DeltaNow < 0 && ctx.DomImbalance <= 0.48;

        if (buyCond)
            TrySendSignal("S7", "BUY", "poc_continuation", nowUtc, ctx.Price, ctx.Poc, false);
        else if (sellCond)
            TrySendSignal("S7", "SELL", "poc_continuation", nowUtc, ctx.Price, ctx.Poc, false);
    }

    private void EvaluateS8(DateTime nowUtc, Context ctx)
    {
        var st = _states["S8"];
        if (!CanAutoSignal(st, ctx.BarTimeUtc, nowUtc))
            return;

        var thresholdTicks = Math.Max(8, compressionMaxTicks / 2);
        var deviationTicks = (ctx.Price - ctx.Vwap) / ctx.TickSize;

        var buyCond = deviationTicks <= -thresholdTicks && ctx.DeltaNow > ctx.DeltaPrev;
        var sellCond = deviationTicks >= thresholdTicks && ctx.DeltaNow < ctx.DeltaPrev;

        if (buyCond)
            TrySendSignal("S8", "BUY", "vwap_mean_revert", nowUtc, ctx.Price, ctx.Vwap, false);
        else if (sellCond)
            TrySendSignal("S8", "SELL", "vwap_mean_revert", nowUtc, ctx.Price, ctx.Vwap, false);
    }

    private void EvaluateS9(DateTime nowUtc, Context ctx)
    {
        var st = _states["S9"];
        if (!CanAutoSignal(st, ctx.BarTimeUtc, nowUtc))
            return;
        if (!TryGetRecentRange(Math.Max(noProgressSeconds * 3, 10), out var recentLow, out var recentHigh))
            return;

        var buffer = supportBufferTicks * ctx.TickSize;
        var confirm = confirmTicks * ctx.TickSize;

        var buyCond = ctx.Price >= ctx.Vwap + confirm && recentLow <= ctx.Vwap + buffer && ctx.DeltaNow > 0 && ctx.DomImbalance >= 0.5;
        var sellCond = ctx.Price <= ctx.Vwap - confirm && recentHigh >= ctx.Vwap - buffer && ctx.DeltaNow < 0 && ctx.DomImbalance <= 0.5;

        if (buyCond)
            TrySendSignal("S9", "BUY", "vwap_trend_follow", nowUtc, ctx.Price, ctx.Vwap, false);
        else if (sellCond)
            TrySendSignal("S9", "SELL", "vwap_trend_follow", nowUtc, ctx.Price, ctx.Vwap, false);
    }

    private void EvaluateS10(DateTime nowUtc, Context ctx)
    {
        var st = _states["S10"];
        if (!CanAutoSignal(st, ctx.BarTimeUtc, nowUtc))
            return;

        var confirm = confirmTicks * ctx.TickSize;
        var hasBidWall = IsValid(ctx.BidWallPrice) && ctx.BidWallPersistenceSeconds >= wallPersistenceSeconds;
        var hasAskWall = IsValid(ctx.AskWallPrice) && ctx.AskWallPersistenceSeconds >= wallPersistenceSeconds;

        var buyCond = hasAskWall && ctx.Price >= ctx.AskWallPrice + confirm && ctx.BuyAggression >= Math.Max(3, aggressionThreshold / 2) && ctx.BuyPullScore >= 0.4;
        var sellCond = hasBidWall && ctx.Price <= ctx.BidWallPrice - confirm && ctx.SellAggression >= Math.Max(3, aggressionThreshold / 2) && ctx.SellPullScore >= 0.4;

        if (buyCond)
            TrySendSignal("S10", "BUY", "iceberg_breakout", nowUtc, ctx.Price, ctx.AskWallPrice, false);
        else if (sellCond)
            TrySendSignal("S10", "SELL", "iceberg_breakout", nowUtc, ctx.Price, ctx.BidWallPrice, false);
    }

    private void EvaluateS11(DateTime nowUtc, Context ctx)
    {
        var st = _states["S11"];
        if (!CanAutoSignal(st, ctx.BarTimeUtc, nowUtc))
            return;
        if (!TryGetRecentRange(Math.Max(aggressionWindowSeconds * 2, 10), out var recentLow, out var recentHigh))
            return;

        var breakDistance = Math.Max(1, breakTicks) * ctx.TickSize;
        var confirm = confirmTicks * ctx.TickSize;

        var buyCond = recentLow <= ctx.Support - breakDistance && ctx.Price >= ctx.Support + confirm && ctx.DeltaNow > 0 && ctx.DeltaPrev < 0;
        var sellCond = recentHigh >= ctx.Resistance + breakDistance && ctx.Price <= ctx.Resistance - confirm && ctx.DeltaNow < 0 && ctx.DeltaPrev > 0;

        if (buyCond)
            TrySendSignal("S11", "BUY", "sweep_reclaim", nowUtc, ctx.Price, ctx.Support, false);
        else if (sellCond)
            TrySendSignal("S11", "SELL", "sweep_reclaim", nowUtc, ctx.Price, ctx.Resistance, false);
    }

    private void EvaluateS12(DateTime nowUtc, Context ctx)
    {
        var st = _states["S12"];
        if (!CanAutoSignal(st, ctx.BarTimeUtc, nowUtc))
            return;

        var buffer = supportBufferTicks * ctx.TickSize;
        var nearSupport = ctx.Price >= ctx.Support - buffer && ctx.Price <= ctx.Support + buffer;
        var nearResistance = ctx.Price >= ctx.Resistance - buffer && ctx.Price <= ctx.Resistance + buffer;

        var buyCond = nearSupport && ctx.SellAggression >= ctx.BuyAggression && ctx.DeltaNow >= ctx.DeltaPrev + 1;
        var sellCond = nearResistance && ctx.BuyAggression >= ctx.SellAggression && ctx.DeltaNow <= ctx.DeltaPrev - 1;

        if (buyCond)
            TrySendSignal("S12", "BUY", "delta_divergence", nowUtc, ctx.Price, ctx.Support, false);
        else if (sellCond)
            TrySendSignal("S12", "SELL", "delta_divergence", nowUtc, ctx.Price, ctx.Resistance, false);
    }

    private void EvaluateS13(DateTime nowUtc, Context ctx)
    {
        var st = _states["S13"];
        if (!CanAutoSignal(st, ctx.BarTimeUtc, nowUtc))
            return;

        var trendBias = ComputeTrendBias();
        var buffer = supportBufferTicks * ctx.TickSize;

        var buyZone = ctx.Price <= ctx.Vwap + buffer * 2.0 && ctx.Price >= ctx.Support - buffer;
        var sellZone = ctx.Price >= ctx.Vwap - buffer * 2.0 && ctx.Price <= ctx.Resistance + buffer;

        var buyCond = trendBias > 0 && buyZone && ctx.DeltaNow >= 0 && ctx.DomImbalance >= 0.5;
        var sellCond = trendBias < 0 && sellZone && ctx.DeltaNow <= 0 && ctx.DomImbalance <= 0.5;

        if (buyCond)
            TrySendSignal("S13", "BUY", "mtf_pullback", nowUtc, ctx.Price, ctx.Vwap, false);
        else if (sellCond)
            TrySendSignal("S13", "SELL", "mtf_pullback", nowUtc, ctx.Price, ctx.Vwap, false);
    }

    private void EvaluateIndicatorFallback(DateTime nowUtc, Context ctx)
    {
        if (!EnableS1)
            return;

        var st = _states["S1"];
        if (!CanAutoSignal(st, ctx.BarTimeUtc, nowUtc))
            return;

        var orderflowCold = _aggr.Count < Math.Max(6, aggressionThreshold / 2) || _dom.Count < 2;
        if (IndicatorFallbackOnlyWhenOrderflowCold && !orderflowCold)
            return;

        if (!TryBuildIndicatorSnapshot(ctx, out var ind))
            return;

        var spreadLimit = Math.Max(1.0, ind.AtrTicks * Math.Max(0.05, IndicatorMaxSpreadAtrFraction));
        if (ctx.SpreadTicks > spreadLimit)
            return;

        var tick = Math.Max(ctx.TickSize, 0.0001);
        var bandWidthTicks = (ind.BandUpper - ind.BandLower) / tick;
        if (bandWidthTicks < Math.Max(1, IndicatorMinBandWidthTicks))
            return;

        var crossUp = ind.FastNow > ind.SlowNow && ind.FastPrev <= ind.SlowPrev;
        var crossDown = ind.FastNow < ind.SlowNow && ind.FastPrev >= ind.SlowPrev;

        var buyCond = (crossUp || ind.FastNow > ind.SlowNow) &&
                      ind.RsiNow >= IndicatorRsiBuyThreshold &&
                      ctx.Price >= ind.Basis;

        var sellCond = (crossDown || ind.FastNow < ind.SlowNow) &&
                       ind.RsiNow <= IndicatorRsiSellThreshold &&
                       ctx.Price <= ind.Basis;

        if (buyCond && !sellCond)
        {
            var anchor = MinValid(ind.SlowNow, ind.BandLower, ctx.Support);
            if (!IsValid(anchor))
                anchor = ctx.Support;
            TrySendSignal("S1", "BUY", "indicator_stack", nowUtc, ctx.Price, anchor, false);
        }
        else if (sellCond && !buyCond)
        {
            var anchor = MaxValid(ind.SlowNow, ind.BandUpper, ctx.Resistance);
            if (!IsValid(anchor))
                anchor = ctx.Resistance;
            TrySendSignal("S1", "SELL", "indicator_stack", nowUtc, ctx.Price, anchor, false);
        }
    }

    private void EvaluateFrequentTestSignals(DateTime nowUtc, Context ctx)
    {
        var strategyId = NormalizeStrategyIdInput(FrequentTestStrategyId);
        if (string.IsNullOrEmpty(strategyId) || !IsStrategyEnabled(strategyId))
            return;

        if (!_states.TryGetValue(strategyId, out var st))
            return;

        if (st.LastSignalBarUtc == ctx.BarTimeUtc)
            return;

        if (nowUtc < st.CooldownUntilUtc)
            return;

        var interval = Math.Max(1, FrequentTestBarsInterval);
        if ((ctx.BarTimeUtc.Minute % interval) != 0)
            return;

        var side = ctx.Price >= ctx.Vwap ? "BUY" : "SELL";
        var anchor = side == "BUY" ? MinValid(ctx.Support, ctx.Poc, ctx.Vwap) : MaxValid(ctx.Resistance, ctx.Poc, ctx.Vwap);
        if (!IsValid(anchor))
            anchor = ctx.Price;

        if (TrySendSignal(strategyId, side, "test_mode_fast", nowUtc, ctx.Price, anchor, bypassGuards: true))
            st.CooldownUntilUtc = nowUtc.AddSeconds(Math.Max(1, FrequentTestCooldownSeconds));
    }

    private bool TryBuildIndicatorSnapshot(Context ctx, out IndicatorSnapshot snapshot)
    {
        snapshot = default;

        var barsNeeded = Math.Max(Math.Max(IndicatorSlowEmaPeriod + 5, IndicatorRsiPeriod + 5), IndicatorBbPeriod + 5);
        barsNeeded = Math.Max(barsNeeded, IndicatorAtrPeriod + 5);

        if (!TryGetBarSeries(barsNeeded, out var mids, out var highs, out var lows))
            return false;

        if (!TryComputeEma(mids, Math.Max(2, IndicatorFastEmaPeriod), out var fastNow, out var fastPrev))
            return false;
        if (!TryComputeEma(mids, Math.Max(3, IndicatorSlowEmaPeriod), out var slowNow, out var slowPrev))
            return false;
        if (!TryComputeRsi(mids, Math.Max(2, IndicatorRsiPeriod), out var rsiNow, out _))
            return false;
        if (!TryComputeBollinger(mids, Math.Max(5, IndicatorBbPeriod), Math.Max(0.5, IndicatorBbDeviation), out var basis, out var upper, out var lower))
            return false;

        var tickSize = Math.Max(ctx.TickSize, GetTickSize());
        if (!TryComputeAtrTicks(highs, lows, mids, Math.Max(2, IndicatorAtrPeriod), tickSize, out var atrTicks))
            atrTicks = Math.Max(1, ctx.MicroNoiseTicks);

        snapshot = new IndicatorSnapshot(fastNow, fastPrev, slowNow, slowPrev, rsiNow, basis, upper, lower, atrTicks);
        return true;
    }

    private bool TryGetBarSeries(int bars, out List<double> mids, out List<double> highs, out List<double> lows)
    {
        mids = new List<double>();
        highs = new List<double>();
        lows = new List<double>();

        if (_history == null || _history.Count <= 0)
            return TryGetSyntheticSeriesFromPrices(Math.Max(3, bars), out mids, out highs, out lows);

        var count = Math.Min(Math.Max(3, bars), _history.Count);
        for (var i = count - 1; i >= 0; i--)
        {
            var high = _history.High(i);
            var low = _history.Low(i);
            if (!IsValid(high) || !IsValid(low) || high < low)
                continue;

            highs.Add(high);
            lows.Add(low);
            mids.Add((high + low) * 0.5);
        }

        if (mids.Count >= 3)
            return true;

        return TryGetSyntheticSeriesFromPrices(Math.Max(3, bars), out mids, out highs, out lows);
    }

    private bool TryGetSyntheticSeriesFromPrices(int bars, out List<double> mids, out List<double> highs, out List<double> lows)
    {
        mids = new List<double>();
        highs = new List<double>();
        lows = new List<double>();

        if (_prices.Count < 3)
            return false;

        var take = Math.Min(Math.Max(3, bars), _prices.Count);
        var start = _prices.Count - take;
        var index = 0;
        foreach (var sample in _prices)
        {
            if (index++ < start)
                continue;

            var p = sample.Price;
            if (!IsValid(p) || p <= 0)
                continue;

            highs.Add(p);
            lows.Add(p);
            mids.Add(p);
        }

        return mids.Count >= 3;
    }

    private static bool TryComputeEma(IReadOnlyList<double> values, int period, out double current, out double previous)
    {
        current = double.NaN;
        previous = double.NaN;
        if (values == null || period < 2 || values.Count < period + 1)
            return false;

        var alpha = 2.0 / (period + 1.0);
        var ema = values[0];
        var prev = ema;

        for (var i = 1; i < values.Count; i++)
        {
            prev = ema;
            ema = (alpha * values[i]) + ((1.0 - alpha) * ema);
        }

        current = ema;
        previous = prev;
        return IsValid(current) && IsValid(previous);
    }

    private static bool TryComputeRsi(IReadOnlyList<double> values, int period, out double current, out double previous)
    {
        current = 50.0;
        previous = 50.0;
        if (values == null || period < 2 || values.Count < period + 2)
            return false;

        double gains = 0;
        double losses = 0;
        for (var i = 1; i <= period; i++)
        {
            var delta = values[i] - values[i - 1];
            if (delta > 0)
                gains += delta;
            else
                losses -= delta;
        }

        var avgGain = gains / period;
        var avgLoss = losses / period;

        previous = ToRsi(avgGain, avgLoss);
        current = previous;

        for (var i = period + 1; i < values.Count; i++)
        {
            var delta = values[i] - values[i - 1];
            var gain = delta > 0 ? delta : 0;
            var loss = delta < 0 ? -delta : 0;

            avgGain = ((avgGain * (period - 1)) + gain) / period;
            avgLoss = ((avgLoss * (period - 1)) + loss) / period;

            var rsi = ToRsi(avgGain, avgLoss);
            if (i == values.Count - 2)
                previous = rsi;
            current = rsi;
        }

        return IsValid(current) && IsValid(previous);
    }

    private static double ToRsi(double avgGain, double avgLoss)
    {
        if (avgLoss <= 0)
            return avgGain <= 0 ? 50.0 : 100.0;

        var rs = avgGain / avgLoss;
        return 100.0 - (100.0 / (1.0 + rs));
    }

    private static bool TryComputeBollinger(IReadOnlyList<double> values, int period, double deviation, out double basis, out double upper, out double lower)
    {
        basis = double.NaN;
        upper = double.NaN;
        lower = double.NaN;

        if (values == null || period < 2 || values.Count < period)
            return false;

        var start = values.Count - period;
        double sum = 0;
        for (var i = start; i < values.Count; i++)
            sum += values[i];

        basis = sum / period;

        double variance = 0;
        for (var i = start; i < values.Count; i++)
        {
            var d = values[i] - basis;
            variance += d * d;
        }

        var stdDev = Math.Sqrt(variance / period);
        upper = basis + deviation * stdDev;
        lower = basis - deviation * stdDev;

        return IsValid(basis) && IsValid(upper) && IsValid(lower);
    }

    private static bool TryComputeAtrTicks(IReadOnlyList<double> highs, IReadOnlyList<double> lows, IReadOnlyList<double> mids, int period, double tickSize, out double atrTicks)
    {
        atrTicks = 0;
        if (highs == null || lows == null || mids == null || period < 2 || tickSize <= 0)
            return false;

        var count = Math.Min(highs.Count, Math.Min(lows.Count, mids.Count));
        if (count < period + 1)
            return false;

        var trs = new List<double>(count - 1);
        for (var i = 1; i < count; i++)
        {
            var high = highs[i];
            var low = lows[i];
            var prevClose = mids[i - 1];

            var tr = Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));
            trs.Add(Math.Max(0, tr));
        }

        if (trs.Count < period)
            return false;

        var start = trs.Count - period;
        double atr = 0;
        for (var i = start; i < trs.Count; i++)
            atr += trs[i];
        atr /= period;

        atrTicks = atr / tickSize;
        return atrTicks > 0 && IsValid(atrTicks);
    }

    private bool TryComputeOrbLevels(out double orbHigh, out double orbLow)
    {
        orbHigh = double.NaN;
        orbLow = double.NaN;
        if (_history == null || _history.Count <= 0)
            return false;

        var bars = Math.Min(Math.Max(lookbackMinutes, orbBars + 5), _history.Count);
        var useBars = Math.Min(Math.Max(orbBars, 5), bars);
        var start = Math.Max(0, bars - useBars);

        var hi = double.MinValue;
        var lo = double.MaxValue;
        for (var i = start; i < bars; i++)
        {
            var h = _history.High(i);
            var l = _history.Low(i);
            if (!IsValid(h) || !IsValid(l) || h < l)
                continue;

            hi = Math.Max(hi, h);
            lo = Math.Min(lo, l);
        }

        if (hi == double.MinValue || lo == double.MaxValue)
            return false;

        orbHigh = hi;
        orbLow = lo;
        return true;
    }

    private bool TryGetRecentRange(int seconds, out double minPrice, out double maxPrice)
    {
        minPrice = double.MaxValue;
        maxPrice = double.MinValue;
        var cutoff = DateTime.UtcNow.AddSeconds(-Math.Max(1, seconds));
        var found = false;

        foreach (var p in _prices)
        {
            if (p.TimeUtc < cutoff)
                continue;
            minPrice = Math.Min(minPrice, p.Price);
            maxPrice = Math.Max(maxPrice, p.Price);
            found = true;
        }

        if (!found)
        {
            minPrice = double.NaN;
            maxPrice = double.NaN;
            return false;
        }

        return true;
    }

    private double ComputeTrendBias()
    {
        if (_history == null || _history.Count < 12)
            return 0;

        var bars = Math.Min(_history.Count, Math.Max(lookbackMinutes, 12));
        var chunk = Math.Max(4, bars / 4);

        double recentSum = 0;
        double olderSum = 0;
        var recentCount = 0;
        var olderCount = 0;

        for (var i = 0; i < bars; i++)
        {
            var h = _history.High(i);
            var l = _history.Low(i);
            if (!IsValid(h) || !IsValid(l) || h < l)
                continue;

            var mid = (h + l) * 0.5;
            if (i < chunk)
            {
                recentSum += mid;
                recentCount++;
            }
            else if (i < chunk * 2)
            {
                olderSum += mid;
                olderCount++;
            }
        }

        if (recentCount == 0 || olderCount == 0)
            return 0;

        return (recentSum / recentCount) - (olderSum / olderCount);
    }

    private bool TryBuildContext(DateTime nowUtc, double price, out Context ctx)
    {
        ctx = default;

        if (_history != null && _history.Count > 0)
        {
            var tick = GetTickSize();
            var bars = Math.Min(Math.Max(lookbackMinutes, 10), _history.Count);
            var low = double.MaxValue;
            var high = double.MinValue;
            var swingLow = double.MaxValue;
            var swingHigh = double.MinValue;

            var mids = new List<double>(bars);
            var lows = new List<double>(bars);
            var highs = new List<double>(bars);
            var bins = new Dictionary<int, int>();
            var ranges = new List<int>();

            for (var i = 0; i < bars; i++)
            {
                var barLow = _history.Low(i);
                var barHigh = _history.High(i);
                if (!IsValid(barLow) || !IsValid(barHigh) || barHigh < barLow)
                    continue;

                low = Math.Min(low, barLow);
                high = Math.Max(high, barHigh);
                lows.Add(barLow);
                highs.Add(barHigh);

                if (i < 5)
                {
                    swingLow = Math.Min(swingLow, barLow);
                    swingHigh = Math.Max(swingHigh, barHigh);
                }

                var mid = (barLow + barHigh) * 0.5;
                mids.Add(mid);
                var bin = (int)Math.Round(mid / tick);
                if (!bins.TryAdd(bin, 1))
                    bins[bin]++;

                if (i < Math.Min(microNoiseBars, bars))
                {
                    var rt = (int)Math.Ceiling((barHigh - barLow) / tick);
                    if (rt > 0)
                        ranges.Add(rt);
                }
            }

            if (low != double.MaxValue && high != double.MinValue && mids.Count > 0)
            {
                var support = low;
                var resistance = high;
                TryComputeRobustLevels(lows, highs, tick, out support, out resistance);

                var poc = bins.OrderByDescending(x => x.Value).First().Key * tick;
                var vwap = mids.Average();
                if (TryComputeTapeProfile(nowUtc, tick, out var tapePoc, out var tapeVwap))
                {
                    if (IsValid(tapePoc))
                        poc = tapePoc;
                    if (IsValid(tapeVwap))
                        vwap = tapeVwap;
                }
                var noiseTicks = Median(ranges, Math.Max(2, breakTicks));
                var compression = ComputeCompressionTicks(nowUtc, pullWindowSeconds);

                var domImb = _dom.Count > 0 ? _dom.Last().Imbalance : 0.5;
                FindPersistentWall(true, nowUtc, tick, out var bidWallPrice, out _, out var bidPersist);
                FindPersistentWall(false, nowUtc, tick, out var askWallPrice, out _, out var askPersist);

                var spreadTicks = 0.0;
                if (IsValid(_lastBid) && IsValid(_lastAsk) && _lastAsk > _lastBid)
                    spreadTicks = (_lastAsk - _lastBid) / tick;

                ctx = new Context(
                    NormalizeMinute(GetBarTimeUtc(nowUtc)),
                    price,
                    tick,
                    support,
                    resistance,
                    poc,
                    vwap,
                    swingLow == double.MaxValue ? support : swingLow,
                    swingHigh == double.MinValue ? resistance : swingHigh,
                    noiseTicks,
                    bidWallPrice,
                    bidPersist,
                    askWallPrice,
                    askPersist,
                    domImb,
                    CountAggression(nowUtc, true),
                    CountAggression(nowUtc, false),
                    ComputeDelta(nowUtc, aggressionWindowSeconds, 0),
                    ComputeDelta(nowUtc, aggressionWindowSeconds * 2, aggressionWindowSeconds),
                    ComputePullScore(nowUtc, true),
                    ComputePullScore(nowUtc, false),
                    compression,
                    spreadTicks
                );

                return true;
            }
        }

        return TryBuildContextFromRecentPrices(nowUtc, price, out ctx);
    }

    private bool TryBuildContextFromRecentPrices(DateTime nowUtc, double price, out Context ctx)
    {
        ctx = default;
        if (_prices.Count < 3)
            return false;

        var tick = GetTickSize();
        var cutoff = nowUtc.AddMinutes(-Math.Max(5, lookbackMinutes));
        var samples = _prices.Where(p => p.TimeUtc >= cutoff).ToList();
        if (samples.Count < 3)
            samples = _prices.ToList();
        if (samples.Count < 3)
            return false;

        var low = samples.Min(p => p.Price);
        var high = samples.Max(p => p.Price);
        if (!IsValid(low) || !IsValid(high) || high < low)
            return false;

        var support = low;
        var resistance = high;
        TryComputeRobustLevels(samples.Select(p => p.Price).ToList(), samples.Select(p => p.Price).ToList(), tick, out support, out resistance);

        var swingCount = Math.Min(5, samples.Count);
        var swing = samples.Skip(samples.Count - swingCount).ToList();
        var swingLow = swing.Min(p => p.Price);
        var swingHigh = swing.Max(p => p.Price);

        var mids = samples.Select(p => p.Price).ToList();
        var bins = new Dictionary<int, int>();
        foreach (var mid in mids)
        {
            var bin = (int)Math.Round(mid / tick);
            if (!bins.TryAdd(bin, 1))
                bins[bin]++;
        }

        var ranges = new List<int>();
        var rangeStart = Math.Max(1, mids.Count - Math.Max(2, microNoiseBars));
        for (var i = rangeStart; i < mids.Count; i++)
        {
            var rt = (int)Math.Ceiling(Math.Abs(mids[i] - mids[i - 1]) / tick);
            if (rt > 0)
                ranges.Add(rt);
        }

        var poc = bins.OrderByDescending(x => x.Value).First().Key * tick;
        var vwap = mids.Average();
        if (TryComputeTapeProfile(nowUtc, tick, out var tapePoc, out var tapeVwap))
        {
            if (IsValid(tapePoc))
                poc = tapePoc;
            if (IsValid(tapeVwap))
                vwap = tapeVwap;
        }
        var noiseTicks = Median(ranges, Math.Max(2, breakTicks));
        var compression = ComputeCompressionTicks(nowUtc, pullWindowSeconds);

        var domImb = _dom.Count > 0 ? _dom.Last().Imbalance : 0.5;
        FindPersistentWall(true, nowUtc, tick, out var bidWallPrice, out _, out var bidPersist);
        FindPersistentWall(false, nowUtc, tick, out var askWallPrice, out _, out var askPersist);

        var spreadTicks = 0.0;
        if (IsValid(_lastBid) && IsValid(_lastAsk) && _lastAsk > _lastBid)
            spreadTicks = (_lastAsk - _lastBid) / tick;

        ctx = new Context(
            NormalizeMinute(nowUtc),
            price,
            tick,
            support,
            resistance,
            poc,
            vwap,
            swingLow,
            swingHigh,
            noiseTicks,
            bidWallPrice,
            bidPersist,
            askWallPrice,
            askPersist,
            domImb,
            CountAggression(nowUtc, true),
            CountAggression(nowUtc, false),
            ComputeDelta(nowUtc, aggressionWindowSeconds, 0),
            ComputeDelta(nowUtc, aggressionWindowSeconds * 2, aggressionWindowSeconds),
            ComputePullScore(nowUtc, true),
            ComputePullScore(nowUtc, false),
            compression,
            spreadTicks
        );

        return true;
    }

    private bool TryBuildEmergencyContext(DateTime nowUtc, double price, out Context ctx)
    {
        ctx = default;
        if (!IsValid(price) || price <= 0)
            return false;

        var tick = GetTickSize();
        var noiseTicks = Math.Max(2, breakTicks);
        var offset = Math.Max(6, noiseTicks * 2) * tick;
        var support = price - offset;
        var resistance = price + offset;

        var spreadTicks = 0.0;
        if (IsValid(_lastBid) && IsValid(_lastAsk) && _lastAsk > _lastBid)
            spreadTicks = (_lastAsk - _lastBid) / tick;

        var domImb = _dom.Count > 0 ? _dom.Last().Imbalance : 0.5;
        var compression = ComputeCompressionTicks(nowUtc, pullWindowSeconds);

        ctx = new Context(
            NormalizeMinute(nowUtc),
            price,
            tick,
            support,
            resistance,
            price,
            price,
            support,
            resistance,
            noiseTicks,
            double.NaN,
            0,
            double.NaN,
            0,
            domImb,
            CountAggression(nowUtc, true),
            CountAggression(nowUtc, false),
            ComputeDelta(nowUtc, aggressionWindowSeconds, 0),
            ComputeDelta(nowUtc, aggressionWindowSeconds * 2, aggressionWindowSeconds),
            ComputePullScore(nowUtc, true),
            ComputePullScore(nowUtc, false),
            compression,
            spreadTicks
        );

        return true;
    }

    private bool TrySendSignal(string strategyId, string side, string setupType, DateTime nowUtc, double entryPrice, double anchorLevel, bool bypassGuards)
    {
        if (_http == null || _symbol == null)
            return false;

        if (string.IsNullOrWhiteSpace(RelayUrl) || !IsSecretConfigured(RelaySecret))
            return false;

        var st = _states[strategyId];
        var bar = NormalizeMinute(GetBarTimeUtc(nowUtc));
        if (!bypassGuards && !CanAutoSignal(st, bar, nowUtc))
            return false;

        Context ctx;
        if (!TryBuildContext(nowUtc, entryPrice, out ctx))
        {
            if (bypassGuards)
            {
                if (!TryBuildEmergencyContext(nowUtc, entryPrice, out ctx))
                    return false;
            }
            else
            {
                return false;
            }
        }

        var plan = BuildTradePlan(strategyId, side, setupType, entryPrice, anchorLevel, ctx);
        var risk = GetRiskPercent(strategyId);
        var comment = BuildComment(strategyId, side, setupType);
        var payload = BuildPayloadJson(strategyId, side, risk, comment, plan, entryPrice, ctx.TickSize);

        st.CooldownUntilUtc = nowUtc.AddSeconds(Math.Max(1, cooldownSeconds));
        st.LastSignalBarUtc = ctx.BarTimeUtc;
        st.LastSignalUtc = nowUtc;

        _ = PostSignalAsync(strategyId, side, setupType, payload, plan, entryPrice, ctx.TickSize);
        return true;
    }

    private async Task PostSignalAsync(string strategyId, string side, string setupType, string payload, TradePlan plan, double entryPrice, double tickSize)
    {
        if (_http == null || string.IsNullOrWhiteSpace(RelayUrl) || !IsSecretConfigured(RelaySecret))
            return;

        await _sendGate.WaitAsync().ConfigureAwait(false);
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, RelayUrl);
            req.Headers.Add("X-Auth", RelaySecret);
            req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                this.LogError($"POST failed status={(int)resp.StatusCode} strategy={strategyId} side={side} body={body}");
                TrackRelayFailure();
                return;
            }

            if (_relayFailureCount > 0)
                this.LogInfo($"Relay post recovered after {_relayFailureCount} consecutive failures.");
            _relayFailureCount = 0;

            var id = TryExtractId(body);
            var slPct = TicksToPercent(plan.SlTicks, tickSize, entryPrice);
            var tp1Pct = TicksToPercent(plan.Tp1Ticks, tickSize, entryPrice);
            var tp2Pct = TicksToPercent(plan.Tp2Ticks, tickSize, entryPrice);
            this.LogInfo($"Signal sent strategy={strategyId} id={id} side={side} setup={setupType} sl={plan.SlTicks}({slPct:F3}%) tp1={plan.Tp1Ticks}({tp1Pct:F3}%) tp2={plan.Tp2Ticks}({tp2Pct:F3}%)");
        }
        catch (Exception ex)
        {
            this.LogError($"POST exception strategy={strategyId} side={side}: {ex.GetType().Name} {ex.Message}");
            TrackRelayFailure();
        }
        finally
        {
            _sendGate.Release();
        }
    }

    private void TrackRelayFailure()
    {
        _relayFailureCount++;
        if (!EnableRelayFailureAlert)
            return;

        var threshold = Math.Max(1, RelayFailureAlertThreshold);
        if (_relayFailureCount == threshold || (_relayFailureCount > threshold && _relayFailureCount % threshold == 0))
            this.LogError($"Relay connectivity warning: consecutive post failures={_relayFailureCount}");
    }

    private TradePlan BuildTradePlan(string strategyId, string side, string setupType, double entryPrice, double anchorLevel, Context ctx)
    {
        var spreadTicks = Math.Max(1, (int)Math.Ceiling(ctx.SpreadTicks));
        var bufferTicks = Math.Max(2, Math.Max(spreadTicks * 2, ctx.MicroNoiseTicks));

        var invalid = side == "BUY"
            ? MinValid(anchorLevel, ctx.SwingLow, ctx.Support, ctx.BidWallPrice)
            : MaxValid(anchorLevel, ctx.SwingHigh, ctx.Resistance, ctx.AskWallPrice);

        if (!IsValid(invalid))
            invalid = side == "BUY" ? ctx.Support : ctx.Resistance;

        var stopPrice = side == "BUY" ? invalid - bufferTicks * ctx.TickSize : invalid + bufferTicks * ctx.TickSize;

        if (side == "BUY" && stopPrice >= entryPrice)
            stopPrice = entryPrice - Math.Max(8, bufferTicks * 2) * ctx.TickSize;
        if (side == "SELL" && stopPrice <= entryPrice)
            stopPrice = entryPrice + Math.Max(8, bufferTicks * 2) * ctx.TickSize;

        var slTicks = side == "BUY"
            ? (int)Math.Ceiling((entryPrice - stopPrice) / ctx.TickSize)
            : (int)Math.Ceiling((stopPrice - entryPrice) / ctx.TickSize);

        slTicks = Clamp(slTicks, Math.Min(minDynamicSlTicks, maxDynamicSlTicks), Math.Max(minDynamicSlTicks, maxDynamicSlTicks));

        if (string.Equals(setupType, "indicator_stack", StringComparison.OrdinalIgnoreCase))
        {
            if (TryBuildIndicatorSnapshot(ctx, out var indPlan))
            {
                var atrBased = (int)Math.Ceiling(indPlan.AtrTicks * Math.Max(0.5, IndicatorAtrStopMultiplier));
                if (atrBased > 0)
                    slTicks = Clamp(Math.Max(slTicks, atrBased), Math.Min(minDynamicSlTicks, maxDynamicSlTicks), Math.Max(minDynamicSlTicks, maxDynamicSlTicks));
            }
        }

        var minTp1 = (int)Math.Ceiling(slTicks * Math.Max(1.0, minRR_TP1));
        var minTp2 = (int)Math.Ceiling(slTicks * Math.Max(minRR_TP2, minRR_TP1 + 0.1));

        if (string.Equals(setupType, "indicator_stack", StringComparison.OrdinalIgnoreCase))
        {
            minTp1 = Math.Max(minTp1, (int)Math.Ceiling(slTicks * Math.Max(0.8, IndicatorTp1RR)));
            minTp2 = Math.Max(minTp2, (int)Math.Ceiling(slTicks * Math.Max(IndicatorTp2RR, IndicatorTp1RR + 0.1)));
        }

        var levels = new List<double> { ctx.Poc, ctx.Vwap, ctx.Support, ctx.Resistance, ctx.BidWallPrice, ctx.AskWallPrice };
        var candidates = side == "BUY"
            ? levels.Where(v => IsValid(v) && v > entryPrice).Distinct().OrderBy(v => v).ToList()
            : levels.Where(v => IsValid(v) && v < entryPrice).Distinct().OrderByDescending(v => v).ToList();

        var tp1 = candidates.Count > 0
            ? (side == "BUY" ? (int)Math.Ceiling((candidates[0] - entryPrice) / ctx.TickSize) : (int)Math.Ceiling((entryPrice - candidates[0]) / ctx.TickSize))
            : 0;
        var tp2 = candidates.Count > 1
            ? (side == "BUY" ? (int)Math.Ceiling((candidates[1] - entryPrice) / ctx.TickSize) : (int)Math.Ceiling((entryPrice - candidates[1]) / ctx.TickSize))
            : tp1;

        tp1 = Math.Max(tp1, minTp1);
        tp2 = Math.Max(tp2, Math.Max(minTp2, tp1 + 1));

        if (strategyId == "S2")
        {
            var measured = (int)Math.Ceiling((ctx.Resistance - ctx.Support) / ctx.TickSize * 1.3);
            tp2 = Math.Max(tp2, measured);
        }

        if (UsePercentTargetsForGold && IsValid(entryPrice) && entryPrice > 0 && ctx.TickSize > 0)
        {
            // Adaptive percent mode: keep percent-based distances but let them breathe with structure/volatility.
            var baseSlPct = Math.Max(0.05, SlPercentOfGoldPrice);
            var baseTp1Pct = Math.Max(baseSlPct + 0.01, Tp1PercentOfGoldPrice);
            var baseTp2Pct = Math.Max(baseTp1Pct + 0.01, Tp2PercentOfGoldPrice);

            var minSlPct = baseSlPct * 0.55;
            var maxSlPct = baseSlPct * 2.50;
            var minTp1Pct = baseTp1Pct * 0.55;
            var maxTp1Pct = baseTp1Pct * 2.50;
            var minTp2Pct = baseTp2Pct * 0.55;
            var maxTp2Pct = baseTp2Pct * 2.80;

            var slDynamicPct = TicksToPercent(slTicks, ctx.TickSize, entryPrice);
            if (slDynamicPct <= 0)
                slDynamicPct = baseSlPct;

            var volFactor = 1.0;
            if (TryBuildIndicatorSnapshot(ctx, out var indPlan) && indPlan.AtrTicks > 0)
            {
                var atrPct = TicksToPercent((int)Math.Ceiling(indPlan.AtrTicks), ctx.TickSize, entryPrice);
                if (atrPct > 0)
                    volFactor = ClampDouble(atrPct / baseSlPct, 0.65, 1.75);
            }
            else
            {
                volFactor = ClampDouble(slDynamicPct / baseSlPct, 0.65, 1.75);
            }

            slDynamicPct = ClampDouble(slDynamicPct * volFactor, minSlPct, maxSlPct);

            var tp1DynamicPct = TicksToPercent(tp1, ctx.TickSize, entryPrice);
            var tp2DynamicPct = TicksToPercent(tp2, ctx.TickSize, entryPrice);

            if (tp1DynamicPct <= 0)
                tp1DynamicPct = baseTp1Pct * volFactor;
            if (tp2DynamicPct <= 0)
                tp2DynamicPct = baseTp2Pct * volFactor;

            var rrPct1 = slDynamicPct * Math.Max(1.0, minRR_TP1);
            var rrPct2 = slDynamicPct * Math.Max(minRR_TP2, minRR_TP1 + 0.1);

            tp1DynamicPct = ClampDouble(tp1DynamicPct, Math.Max(minTp1Pct, rrPct1), maxTp1Pct);
            tp2DynamicPct = ClampDouble(tp2DynamicPct, Math.Max(Math.Max(minTp2Pct, rrPct2), tp1DynamicPct + 0.01), maxTp2Pct);

            var slPctTicks = PercentToTicks(entryPrice, slDynamicPct, ctx.TickSize);
            var tp1PctTicks = PercentToTicks(entryPrice, tp1DynamicPct, ctx.TickSize);
            var tp2PctTicks = PercentToTicks(entryPrice, tp2DynamicPct, ctx.TickSize);

            var slMinTicks = Math.Min(minDynamicSlTicks, maxDynamicSlTicks);
            var slMaxTicks = Math.Max(minDynamicSlTicks, maxDynamicSlTicks);
            if (slPctTicks > 0)
                slTicks = Clamp(slPctTicks, slMinTicks, slMaxTicks);

            var rrFloor1 = (int)Math.Ceiling(slTicks * Math.Max(1.0, minRR_TP1));
            var rrFloor2 = (int)Math.Ceiling(slTicks * Math.Max(minRR_TP2, minRR_TP1 + 0.1));

            if (tp1PctTicks > 0)
                tp1 = tp1PctTicks;
            if (tp2PctTicks > 0)
                tp2 = tp2PctTicks;

            tp1 = Math.Max(tp1, rrFloor1);
            tp2 = Math.Max(tp2, Math.Max(rrFloor2, tp1 + 1));
        }

        var confidence = ComputeConfidence(strategyId, side, setupType, ctx);
        return new TradePlan(slTicks, tp1, tp2, confidence);
    }

    private string BuildPayloadJson(string strategyId, string side, double risk, string comment, TradePlan plan, double entryPrice, double tickSize)
    {
        var unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var safeTick = IsValid(tickSize) && tickSize > 0 ? tickSize : GetTickSize();
        var safeEntry = IsValid(entryPrice) && entryPrice > 0 ? entryPrice : GetCurrentPrice();
        var slPercent = TicksToPercent(plan.SlTicks, safeTick, safeEntry);
        var tp1Percent = TicksToPercent(plan.Tp1Ticks, safeTick, safeEntry);
        var tp2Percent = TicksToPercent(plan.Tp2Ticks, safeTick, safeEntry);
        var distanceMode = UsePercentTargetsForGold ? "percent" : "ticks";
        var payload = new Dictionary<string, object?>
        {
            ["source"] = "quantower",
            ["strategy_id"] = strategyId.ToUpperInvariant(),
            ["symbol"] = SymbolName ?? string.Empty,
            ["side"] = side.ToUpperInvariant(),
            ["order_type"] = "MARKET",
            ["sl_ticks"] = plan.SlTicks,
            ["tp1_ticks"] = plan.Tp1Ticks,
            ["tp2_ticks"] = plan.Tp2Ticks,
            ["tp_ticks"] = plan.Tp2Ticks,
            ["sl_percent"] = slPercent,
            ["tp1_percent"] = tp1Percent,
            ["tp2_percent"] = tp2Percent,
            ["distance_mode"] = distanceMode,
            ["entry_price"] = safeEntry,
            ["risk"] = risk,
            ["trade_symbol"] = TradeSymbolForMT5 ?? string.Empty,
            ["comment"] = comment,
            ["confidence"] = plan.Confidence,
            ["ts_client"] = unix
        };
        return JsonSerializer.Serialize(payload);
    }

    private string BuildComment(string strategyId, string side, string setupType)
    {
        var prefix = strategyId switch
        {
            "S1" => CommentS1,
            "S2" => CommentS2,
            "S3" => CommentS3,
            "S4" => CommentS4,
            "S5" => CommentS5,
            "S6" => CommentS6,
            "S7" => CommentS7,
            "S8" => CommentS8,
            "S9" => CommentS9,
            "S10" => CommentS10,
            "S11" => CommentS11,
            "S12" => CommentS12,
            "S13" => CommentS13,
            _ => strategyId
        };

        if (string.IsNullOrWhiteSpace(prefix))
            prefix = strategyId;

        var setup = setupType switch
        {
            "absorption_reversal" => "ABSORB",
            "liquidity_vacuum" => "VACUUM",
            "break_retest" => "RETEST",
            "orb_retest" => "ORB_RET",
            "failed_auction" => "FA_REV",
            "lvn_rotation" => "LVN_ROT",
            "poc_continuation" => "POC_CONT",
            "vwap_mean_revert" => "VWAP_MR",
            "vwap_trend_follow" => "VWAP_TF",
            "iceberg_breakout" => "ICE_BRK",
            "sweep_reclaim" => "SWEEP_RECL",
            "delta_divergence" => "DELTA_DIV",
            "mtf_pullback" => "MTF_PULL",
            "indicator_stack" => "IND_STACK",
            "test_mode_fast" => "TEST_FAST",
            "manual_test" => "MANUAL",
            _ => setupType
        };

        return $"{prefix}_{side.ToUpperInvariant()}_{setup.ToUpperInvariant()}";
    }

    private double GetRiskPercent(string strategyId)
    {
        return strategyId switch
        {
            "S1" => Math.Max(0.01, RiskS1),
            "S2" => Math.Max(0.01, RiskS2),
            "S3" => Math.Max(0.01, RiskS3),
            "S4" => Math.Max(0.01, RiskS4),
            "S5" => Math.Max(0.01, RiskS5),
            "S6" => Math.Max(0.01, RiskS6),
            "S7" => Math.Max(0.01, RiskS7),
            "S8" => Math.Max(0.01, RiskS8),
            "S9" => Math.Max(0.01, RiskS9),
            "S10" => Math.Max(0.01, RiskS10),
            "S11" => Math.Max(0.01, RiskS11),
            "S12" => Math.Max(0.01, RiskS12),
            "S13" => Math.Max(0.01, RiskS13),
            _ => 0.25
        };
    }

    private double ComputeConfidence(string strategyId, string side, string setupType, Context ctx)
    {
        if (string.Equals(setupType, "manual_test", StringComparison.OrdinalIgnoreCase))
            return 0.99;

        var dom = side == "BUY" ? ctx.DomImbalance : 1.0 - ctx.DomImbalance;
        var pull = side == "BUY" ? ctx.BuyPullScore : ctx.SellPullScore;
        var delta = side == "BUY"
            ? Clamp01((double)Math.Max(0, ctx.DeltaNow) / Math.Max(1, deltaFlipThreshold))
            : Clamp01((double)Math.Max(0, -ctx.DeltaNow) / Math.Max(1, deltaFlipThreshold));

        var score = 0.4 * Clamp01((dom - 0.5) / 0.3) + 0.35 * pull + 0.25 * delta;
        if (strategyId == "S1") score *= 1.05;
        if (strategyId == "S3") score *= 1.03;
        return Clamp01(score);
    }

    private bool CanAutoSignal(RuntimeState state, DateTime barTimeUtc, DateTime nowUtc)
    {
        return nowUtc >= state.CooldownUntilUtc && state.LastSignalBarUtc != barTimeUtc;
    }

    private bool TryComputeTapeProfile(DateTime nowUtc, double tick, out double poc, out double vwap)
    {
        poc = double.NaN;
        vwap = double.NaN;
        if (_aggr.Count == 0 || tick <= 0)
            return false;

        var cutoff = nowUtc.AddMinutes(-Math.Max(3, lookbackMinutes));
        var byPrice = new Dictionary<int, double>();
        var weightedSum = 0.0;
        var totalSize = 0.0;

        foreach (var a in _aggr)
        {
            if (a.TimeUtc < cutoff || !IsValid(a.Price) || a.Price <= 0)
                continue;

            var size = IsValid(a.Size) && a.Size > 0 ? a.Size : 1.0;
            totalSize += size;
            weightedSum += a.Price * size;

            var bin = (int)Math.Round(a.Price / tick);
            if (!byPrice.TryAdd(bin, size))
                byPrice[bin] += size;
        }

        if (totalSize <= 0 || byPrice.Count == 0)
            return false;

        vwap = weightedSum / totalSize;
        poc = byPrice.OrderByDescending(x => x.Value).First().Key * tick;
        return true;
    }

    private static void TryComputeRobustLevels(List<double> lows, List<double> highs, double tick, out double support, out double resistance)
    {
        support = lows != null && lows.Count > 0 ? lows.Min() : double.NaN;
        resistance = highs != null && highs.Count > 0 ? highs.Max() : double.NaN;

        if (lows == null || highs == null || lows.Count < 3 || highs.Count < 3)
            return;

        var swingLows = new List<double>();
        var swingHighs = new List<double>();
        var n = Math.Min(lows.Count, highs.Count);
        var span = n >= 12 ? 2 : 1;

        for (var i = span; i < n - span; i++)
        {
            var low = lows[i];
            var high = highs[i];
            var isSwingLow = true;
            var isSwingHigh = true;

            for (var k = 1; k <= span; k++)
            {
                if (!(low < lows[i - k] && low <= lows[i + k]))
                    isSwingLow = false;
                if (!(high > highs[i - k] && high >= highs[i + k]))
                    isSwingHigh = false;
            }

            if (isSwingLow)
                swingLows.Add(low);
            if (isSwingHigh)
                swingHighs.Add(high);
        }

        if (swingLows.Count >= 2)
            support = Quantile(swingLows, 0.5);
        else
            support = Quantile(lows, 0.2);

        if (swingHighs.Count >= 2)
            resistance = Quantile(swingHighs, 0.5);
        else
            resistance = Quantile(highs, 0.8);

        if (tick > 0 && IsValid(support) && IsValid(resistance) && resistance - support < 4 * tick)
            resistance = support + 4 * tick;
    }

    private void CalculateDomStats(DOMQuote dom, out double bidDepth, out double askDepth, out double bidWallPrice, out double bidWallSize, out double askWallPrice, out double askWallSize)
    {
        bidDepth = 0;
        askDepth = 0;
        bidWallPrice = double.NaN;
        askWallPrice = double.NaN;
        bidWallSize = 0;
        askWallSize = 0;

        if (dom.Bids != null)
        {
            var n = Math.Min(Math.Max(domLevels, 1), dom.Bids.Count);
            for (var i = 0; i < n; i++)
            {
                var q = dom.Bids[i];
                if (q == null || q.Closed || !IsValid(q.Size) || q.Size <= 0 || !IsValid(q.Price))
                    continue;
                bidDepth += q.Size;
                if (q.Size > bidWallSize) { bidWallSize = q.Size; bidWallPrice = q.Price; }
            }
        }

        if (dom.Asks != null)
        {
            var n = Math.Min(Math.Max(domLevels, 1), dom.Asks.Count);
            for (var i = 0; i < n; i++)
            {
                var q = dom.Asks[i];
                if (q == null || q.Closed || !IsValid(q.Size) || q.Size <= 0 || !IsValid(q.Price))
                    continue;
                askDepth += q.Size;
                if (q.Size > askWallSize) { askWallSize = q.Size; askWallPrice = q.Price; }
            }
        }
    }

    private void FindPersistentWall(bool wantBid, DateTime nowUtc, double tick, out double wallPrice, out double avgSize, out double persistenceSeconds)
    {
        wallPrice = double.NaN;
        avgSize = 0;
        persistenceSeconds = 0;
        if (_dom.Count == 0)
            return;

        var last = _dom.Last();
        var seedPrice = wantBid ? last.BidWallPrice : last.AskWallPrice;
        if (!IsValid(seedPrice))
            return;

        var cutoff = nowUtc.AddSeconds(-Math.Max(10, wallPersistenceSeconds * 3));
        var recentSizes = new List<double>();
        foreach (var d in _dom)
        {
            if (d.TimeUtc < cutoff)
                continue;
            var s = wantBid ? d.BidWallSize : d.AskWallSize;
            if (IsValid(s) && s > 0)
                recentSizes.Add(s);
        }

        var dynamicThreshold = wallSizeThreshold;
        if (recentSizes.Count >= 6)
            dynamicThreshold = (int)Math.Ceiling(Math.Max(wallSizeThreshold, Quantile(recentSizes, 0.70)));

        var first = DateTime.MinValue;
        var lastTs = DateTime.MinValue;
        var count = 0;
        var sum = 0.0;

        foreach (var d in _dom)
        {
            if (d.TimeUtc < cutoff)
                continue;

            var p = wantBid ? d.BidWallPrice : d.AskWallPrice;
            var s = wantBid ? d.BidWallSize : d.AskWallSize;
            if (!IsValid(p) || s < dynamicThreshold || Math.Abs(p - seedPrice) > tick * 2.0)
                continue;

            if (first == DateTime.MinValue)
                first = d.TimeUtc;
            lastTs = d.TimeUtc;
            count++;
            sum += s;
        }

        if (count == 0)
            return;

        wallPrice = seedPrice;
        avgSize = sum / count;
        persistenceSeconds = count == 1 ? Math.Max(0.25, (nowUtc - first).TotalSeconds) : (lastTs - first).TotalSeconds;
    }

    private int CountAggression(DateTime nowUtc, bool wantBuy)
    {
        var cutoff = nowUtc.AddSeconds(-Math.Max(1, aggressionWindowSeconds));
        var count = 0;
        foreach (var a in _aggr)
        {
            if (a.TimeUtc < cutoff)
                continue;
            if (wantBuy && a.IsBuy) count++;
            if (!wantBuy && a.IsSell) count++;
        }
        return count;
    }

    private int ComputeDelta(DateTime nowUtc, int startSeconds, int endSeconds)
    {
        var from = nowUtc.AddSeconds(-Math.Max(1, startSeconds));
        var to = nowUtc.AddSeconds(-Math.Max(0, endSeconds));
        var delta = 0;
        foreach (var a in _aggr)
        {
            if (a.TimeUtc < from || a.TimeUtc > to)
                continue;
            if (a.IsBuy) delta++;
            if (a.IsSell) delta--;
        }
        return delta;
    }

    private int ComputeCompressionTicks(DateTime nowUtc, int seconds)
    {
        var cutoff = nowUtc.AddSeconds(-Math.Max(1, seconds));
        var min = double.MaxValue;
        var max = double.MinValue;
        var found = false;
        foreach (var p in _prices)
        {
            if (p.TimeUtc < cutoff)
                continue;
            min = Math.Min(min, p.Price);
            max = Math.Max(max, p.Price);
            found = true;
        }
        if (!found)
            return int.MaxValue;
        return Math.Max(1, (int)Math.Ceiling((max - min) / GetTickSize()));
    }

    private double ComputePullScore(DateTime nowUtc, bool wantBuyBreakout)
    {
        if (_dom.Count < 2)
            return 0;

        var current = _dom.Last();
        var cutoff = nowUtc.AddSeconds(-Math.Max(1, pullWindowSeconds));
        DomSample? baseline = null;
        foreach (var d in _dom)
            if (d.TimeUtc <= cutoff)
                baseline = d;
        if (baseline == null)
            baseline = _dom.First();

        var b = baseline.Value;
        if (wantBuyBreakout)
        {
            if (b.AskDepth <= 0)
                return 0;
            var pull = (b.AskDepth - current.AskDepth) / b.AskDepth;
            var imb = Clamp01((current.Imbalance - 0.5) / 0.3);
            return Clamp01((pull / Math.Max(0.05, pullPercentThreshold)) * 0.7 + imb * 0.3);
        }
        else
        {
            if (b.BidDepth <= 0)
                return 0;
            var pull = (b.BidDepth - current.BidDepth) / b.BidDepth;
            var imb = Clamp01((0.5 - current.Imbalance) / 0.3);
            return Clamp01((pull / Math.Max(0.05, pullPercentThreshold)) * 0.7 + imb * 0.3);
        }
    }

    private bool HasNoProgress(bool forLong, double level, DateTime nowUtc, double tick)
    {
        var cutoff = nowUtc.AddSeconds(-Math.Max(1, noProgressSeconds));
        var min = double.MaxValue;
        var max = double.MinValue;
        var found = false;
        foreach (var p in _prices)
        {
            if (p.TimeUtc < cutoff)
                continue;
            min = Math.Min(min, p.Price);
            max = Math.Max(max, p.Price);
            found = true;
        }
        if (!found)
            return false;
        var breakDistance = Math.Max(1, breakTicks) * tick;
        return forLong ? min >= level - breakDistance : max <= level + breakDistance;
    }

    private double ComputeAbsorptionScore(bool forLong, double level, DateTime nowUtc, double tick)
    {
        var cutoff = nowUtc.AddSeconds(-Math.Max(1, noProgressSeconds));
        var band = Math.Max(tick, supportBufferTicks * tick);
        var count = 0;
        var min = double.MaxValue;
        var max = double.MinValue;

        foreach (var a in _aggr)
        {
            if (a.TimeUtc < cutoff)
                continue;
            if (forLong && !a.IsSell) continue;
            if (!forLong && !a.IsBuy) continue;
            if (Math.Abs(a.Price - level) > band) continue;
            count++;
            min = Math.Min(min, a.Price);
            max = Math.Max(max, a.Price);
        }

        if (count == 0)
            return 0;

        var stable = ((max - min) / tick) <= Math.Max(1, breakTicks) * 1.5;
        var act = Clamp01((double)count / Math.Max(1, aggressionThreshold));
        return Clamp01(stable ? act + 0.2 : act * 0.6);
    }

    private void RecordAggression(Last last, DateTime nowUtc)
    {
        var isSell = false;
        var isBuy = false;

        if (last.AggressorFlag == AggressorFlag.Sell) isSell = true;
        else if (last.AggressorFlag == AggressorFlag.Buy) isBuy = true;
        else if (IsValid(_lastBid) && last.Price <= _lastBid) isSell = true;
        else if (IsValid(_lastAsk) && last.Price >= _lastAsk) isBuy = true;
        else if (last.TickDirection == TickDirection.Down) isSell = true;
        else if (last.TickDirection == TickDirection.Up) isBuy = true;
        else if (IsValid(_lastTrade) && last.Price < _lastTrade) isSell = true;
        else if (IsValid(_lastTrade) && last.Price > _lastTrade) isBuy = true;

        _lastTrade = last.Price;
        if (!isSell && !isBuy)
            return;

        _aggr.Enqueue(new AggressionSample(nowUtc, isSell, isBuy, last.Price, Math.Max(0, last.Size)));
    }

    private void TrimQueues(DateTime nowUtc)
    {
        var keep = Math.Max(Math.Max(aggressionWindowSeconds, noProgressSeconds), wallPersistenceSeconds);
        keep = Math.Max(keep, pullWindowSeconds);
        keep = Math.Max(keep, 15);
        keep += 20;
        var cutoff = nowUtc.AddSeconds(-keep);
        while (_aggr.Count > 0 && _aggr.Peek().TimeUtc < cutoff) _aggr.Dequeue();
        while (_prices.Count > 0 && _prices.Peek().TimeUtc < cutoff) _prices.Dequeue();
        while (_dom.Count > 0 && _dom.Peek().TimeUtc < cutoff) _dom.Dequeue();

        // Keep upper bounds to prevent runaway iteration cost on very high tick rates.
        var maxSamples = Math.Max(2000, keep * 200);
        while (_aggr.Count > maxSamples) _aggr.Dequeue();
        while (_prices.Count > maxSamples) _prices.Dequeue();
        while (_dom.Count > maxSamples) _dom.Dequeue();
    }

    private double GetCurrentPrice()
    {
        if (IsValid(_lastTrade) && _lastTrade > 0) return _lastTrade;
        if (_symbol != null && IsValid(_symbol.Last) && _symbol.Last > 0) return _symbol.Last;
        if (IsValid(_lastBid) && IsValid(_lastAsk) && _lastAsk > _lastBid) return (_lastBid + _lastAsk) * 0.5;
        if (_prices.Count > 0)
        {
            var last = _prices.Last().Price;
            if (IsValid(last) && last > 0)
                return last;
        }
        if (_history != null && _history.Count > 0)
        {
            var high = _history.High(0);
            var low = _history.Low(0);
            if (IsValid(high) && IsValid(low) && high >= low)
                return (high + low) * 0.5;
        }
        return double.NaN;
    }

    private static int TryExtractId(string json)
    {
        if (string.IsNullOrEmpty(json)) return -1;
        var marker = "\"id\":";
        var p = json.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (p < 0) return -1;
        p += marker.Length;
        while (p < json.Length && char.IsWhiteSpace(json[p])) p++;
        var e = p;
        if (e < json.Length && (json[e] == '-' || json[e] == '+')) e++;
        while (e < json.Length && char.IsDigit(json[e])) e++;
        if (e <= p) return -1;
        return int.TryParse(json.Substring(p, e - p), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : -1;
    }

    private Symbol? ResolveSymbol(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var symbols = Core.Instance.Symbols;
        if (symbols == null || symbols.Length == 0) return null;

        var exact = symbols.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;

        var contains = symbols.FirstOrDefault(s => s.Name != null && s.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
        if (contains != null) return contains;

        var parts = name.Split(':');
        var left = parts.Length > 0 ? parts[0].Trim() : name.Trim();
        var exchange = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        var root = ExtractFuturesRoot(left);
        if (string.IsNullOrEmpty(root))
            return null;

        var candidates = symbols.Where(s => !string.IsNullOrEmpty(s.Name) && s.Name.StartsWith(root, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(exchange))
            candidates = candidates.Where(s => s.Name.EndsWith($":{exchange}", StringComparison.OrdinalIgnoreCase));

        return candidates.OrderBy(s => (s.Name ?? string.Empty).Length).FirstOrDefault();
    }

    private static string ExtractFuturesRoot(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var s = value.Trim();
        var i = s.Length - 1;
        var digitCount = 0;
        while (i >= 0 && char.IsDigit(s[i]))
        {
            digitCount++;
            i--;
        }

        if (digitCount == 0 || i < 0)
            return string.Empty;

        var month = char.ToUpperInvariant(s[i]);
        const string futuresMonths = "FGHJKMNQUVXZ";
        if (futuresMonths.IndexOf(month) < 0)
            return string.Empty;

        return i > 0 ? s.Substring(0, i) : string.Empty;
    }

    private static string ParseSide(string side) => string.Equals(side, "SELL", StringComparison.OrdinalIgnoreCase) ? "SELL" : "BUY";
    private double GetTickSize() { var tick = _symbol?.TickSize ?? 0.01; return IsValid(tick) && tick > 0 ? tick : 0.01; }
    private DateTime GetBarTimeUtc(DateTime fallback) { try { return _history != null && _history.Count > 0 ? _history.Time(0).ToUniversalTime() : fallback; } catch { return fallback; } }
    private static DateTime NormalizeMinute(DateTime t) => new(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0, DateTimeKind.Utc);
    private static bool IsValid(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    private static bool IsSecretConfigured(string? secret)
    {
        return !string.IsNullOrWhiteSpace(secret);
    }
    private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    private static double ClampDouble(double v, double min, double max) => v < min ? min : v > max ? max : v;
    private static int PercentToTicks(double entryPrice, double percent, double tickSize)
    {
        if (!IsValid(entryPrice) || entryPrice <= 0 || !IsValid(percent) || percent <= 0 || !IsValid(tickSize) || tickSize <= 0)
            return 0;
        var distance = entryPrice * (percent / 100.0);
        if (!IsValid(distance) || distance <= 0)
            return 0;
        return Math.Max(1, (int)Math.Ceiling(distance / tickSize));
    }
    private static double TicksToPercent(int ticks, double tickSize, double entryPrice)
    {
        if (ticks <= 0 || !IsValid(tickSize) || tickSize <= 0 || !IsValid(entryPrice) || entryPrice <= 0)
            return 0.0;
        var distance = ticks * tickSize;
        return distance / entryPrice * 100.0;
    }
    private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
    private static int Median(List<int> values, int fallback) { if (values == null || values.Count == 0) return fallback; values.Sort(); var m = values.Count / 2; return values.Count % 2 == 1 ? values[m] : (values[m - 1] + values[m]) / 2; }
    private static double Quantile(List<double> values, double q)
    {
        if (values == null || values.Count == 0)
            return double.NaN;
        var sorted = values.Where(IsValid).OrderBy(x => x).ToList();
        if (sorted.Count == 0)
            return double.NaN;
        q = ClampDouble(q, 0.0, 1.0);
        var idx = (sorted.Count - 1) * q;
        var lo = (int)Math.Floor(idx);
        var hi = (int)Math.Ceiling(idx);
        if (lo == hi)
            return sorted[lo];
        var w = idx - lo;
        return sorted[lo] * (1.0 - w) + sorted[hi] * w;
    }
    private static double MinValid(params double[] values) { var min = double.MaxValue; var ok = false; foreach (var v in values) { if (!IsValid(v)) continue; if (v < min) min = v; ok = true; } return ok ? min : double.NaN; }
    private static double MaxValid(params double[] values) { var max = double.MinValue; var ok = false; foreach (var v in values) { if (!IsValid(v)) continue; if (v > max) max = v; ok = true; } return ok ? max : double.NaN; }

    private sealed class RuntimeState
    {
        public bool ManualConsumed;
        public DateTime CooldownUntilUtc = DateTime.MinValue;
        public DateTime LastSignalBarUtc = DateTime.MinValue;
        public DateTime LastSignalUtc = DateTime.MinValue;
        public bool PendingActive;
        public string PendingSide = "";
        public string PendingSetup = "";
        public double PendingLevel;
        public DateTime PendingAtUtc = DateTime.MinValue;
        public bool RetestActive;
        public string RetestSide = "";
        public double RetestLevel;
        public DateTime RetestAtUtc = DateTime.MinValue;

        public void SetPending(string side, double level, string setup, DateTime atUtc)
        {
            PendingActive = true;
            PendingSide = side;
            PendingLevel = level;
            PendingSetup = setup;
            PendingAtUtc = atUtc;
        }

        public void SetRetest(string side, double level, DateTime atUtc)
        {
            RetestActive = true;
            RetestSide = side;
            RetestLevel = level;
            RetestAtUtc = atUtc;
        }

        public void Reset()
        {
            ManualConsumed = false;
            CooldownUntilUtc = DateTime.MinValue;
            LastSignalBarUtc = DateTime.MinValue;
            LastSignalUtc = DateTime.MinValue;
            PendingActive = false;
            PendingSide = "";
            PendingSetup = "";
            PendingLevel = 0;
            PendingAtUtc = DateTime.MinValue;
            RetestActive = false;
            RetestSide = "";
            RetestLevel = 0;
            RetestAtUtc = DateTime.MinValue;
        }
    }

    private readonly struct Context
    {
        public Context(DateTime barTimeUtc, double price, double tickSize, double support, double resistance, double poc, double vwap, double swingLow, double swingHigh, int microNoiseTicks, double bidWallPrice, double bidWallPersistenceSeconds, double askWallPrice, double askWallPersistenceSeconds, double domImbalance, int buyAggression, int sellAggression, int deltaNow, int deltaPrev, double buyPullScore, double sellPullScore, int compressionTicks, double spreadTicks)
        {
            BarTimeUtc = barTimeUtc; Price = price; TickSize = tickSize; Support = support; Resistance = resistance; Poc = poc; Vwap = vwap;
            SwingLow = swingLow; SwingHigh = swingHigh; MicroNoiseTicks = microNoiseTicks; BidWallPrice = bidWallPrice; BidWallPersistenceSeconds = bidWallPersistenceSeconds;
            AskWallPrice = askWallPrice; AskWallPersistenceSeconds = askWallPersistenceSeconds; DomImbalance = domImbalance; BuyAggression = buyAggression; SellAggression = sellAggression;
            DeltaNow = deltaNow; DeltaPrev = deltaPrev; BuyPullScore = buyPullScore; SellPullScore = sellPullScore; CompressionTicks = compressionTicks; SpreadTicks = spreadTicks;
        }
        public DateTime BarTimeUtc { get; } public double Price { get; } public double TickSize { get; } public double Support { get; } public double Resistance { get; }
        public double Poc { get; } public double Vwap { get; } public double SwingLow { get; } public double SwingHigh { get; } public int MicroNoiseTicks { get; }
        public double BidWallPrice { get; } public double BidWallPersistenceSeconds { get; } public double AskWallPrice { get; } public double AskWallPersistenceSeconds { get; }
        public double DomImbalance { get; } public int BuyAggression { get; } public int SellAggression { get; } public int DeltaNow { get; } public int DeltaPrev { get; }
        public double BuyPullScore { get; } public double SellPullScore { get; } public int CompressionTicks { get; } public double SpreadTicks { get; }
    }

    private readonly struct IndicatorSnapshot
    {
        public IndicatorSnapshot(double fastNow, double fastPrev, double slowNow, double slowPrev, double rsiNow, double basis, double bandUpper, double bandLower, double atrTicks)
        {
            FastNow = fastNow;
            FastPrev = fastPrev;
            SlowNow = slowNow;
            SlowPrev = slowPrev;
            RsiNow = rsiNow;
            Basis = basis;
            BandUpper = bandUpper;
            BandLower = bandLower;
            AtrTicks = atrTicks;
        }

        public double FastNow { get; }
        public double FastPrev { get; }
        public double SlowNow { get; }
        public double SlowPrev { get; }
        public double RsiNow { get; }
        public double Basis { get; }
        public double BandUpper { get; }
        public double BandLower { get; }
        public double AtrTicks { get; }
    }

    private readonly struct TradePlan
    {
        public TradePlan(int slTicks, int tp1Ticks, int tp2Ticks, double confidence) { SlTicks = slTicks; Tp1Ticks = tp1Ticks; Tp2Ticks = tp2Ticks; Confidence = confidence; }
        public int SlTicks { get; } public int Tp1Ticks { get; } public int Tp2Ticks { get; } public double Confidence { get; }
    }

    private readonly struct AggressionSample
    {
        public AggressionSample(DateTime timeUtc, bool isSell, bool isBuy, double price, double size) { TimeUtc = timeUtc; IsSell = isSell; IsBuy = isBuy; Price = price; Size = size; }
        public DateTime TimeUtc { get; } public bool IsSell { get; } public bool IsBuy { get; } public double Price { get; } public double Size { get; }
    }

    private readonly struct PriceSample
    {
        public PriceSample(DateTime timeUtc, double price) { TimeUtc = timeUtc; Price = price; }
        public DateTime TimeUtc { get; } public double Price { get; }
    }

    private readonly struct DomSample
    {
        public DomSample(DateTime timeUtc, double bidDepth, double askDepth, double imbalance, double bidWallPrice, double bidWallSize, double askWallPrice, double askWallSize)
        { TimeUtc = timeUtc; BidDepth = bidDepth; AskDepth = askDepth; Imbalance = imbalance; BidWallPrice = bidWallPrice; BidWallSize = bidWallSize; AskWallPrice = askWallPrice; AskWallSize = askWallSize; }
        public DateTime TimeUtc { get; } public double BidDepth { get; } public double AskDepth { get; } public double Imbalance { get; }
        public double BidWallPrice { get; } public double BidWallSize { get; } public double AskWallPrice { get; } public double AskWallSize { get; }
    }
}
