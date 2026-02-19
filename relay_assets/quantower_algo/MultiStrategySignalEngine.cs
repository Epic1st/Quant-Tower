using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using TradingPlatform.BusinessLayer;

namespace RelaySignalStrategies;

public sealed class MultiStrategySignalEngine : Strategy
{
    [InputParameter("SymbolName", 0)]
    public string SymbolName = "/GCJ26:XCEC";

    [InputParameter("RelayUrl", 1)]
    public string RelayUrl = "http://127.0.0.1:8000/signal";

    [InputParameter("RelaySecret", 2)]
    public string RelaySecret = "<RELAY_SECRET>";

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

    [InputParameter("cooldownSeconds", 14, 1, 3600, 1, 0)]
    public int cooldownSeconds = 60;

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
    public int aggressionThreshold = 20;

    [InputParameter("pullWindowSeconds", 25, 1, 60, 1, 0)]
    public int pullWindowSeconds = 4;

    [InputParameter("pullPercentThreshold", 26, 0.05, 0.95, 0.01, 2)]
    public double pullPercentThreshold = 0.35;

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
        { "S3", new RuntimeState() }
    };

    private double _lastBid = double.NaN;
    private double _lastAsk = double.NaN;
    private double _lastTrade = double.NaN;

    public MultiStrategySignalEngine()
    {
        Name = nameof(MultiStrategySignalEngine);
        Description = "S1/S2/S3 multi strategy signal relay with dynamic zone SL/TP.";
    }

    protected override void OnRun()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

        _symbol = ResolveSymbol(SymbolName);
        if (_symbol == null)
        {
            this.LogError($"Symbol not found: {SymbolName}");
            return;
        }

        _history = _symbol.GetHistory(Period.MIN1, DateTime.UtcNow.AddMinutes(-Math.Max(lookbackMinutes * 3, 60)), DateTime.UtcNow);

        _symbol.NewQuote += OnSymbolNewQuote;
        _symbol.NewLast += OnSymbolNewLast;
        _symbol.NewLevel2 += OnSymbolNewLevel2;

        if (_history != null)
        {
            _history.NewHistoryItem += OnHistoryChanged;
            _history.HistoryItemUpdated += OnHistoryChanged;
        }

        this.LogInfo($"Started {Name}. symbol={_symbol.Name} auto={EnableAutoMode}");
        TryHandleManualTriggers(DateTime.UtcNow);
    }

    protected override void OnStop()
    {
        if (_symbol != null)
        {
            _symbol.NewQuote -= OnSymbolNewQuote;
            _symbol.NewLast -= OnSymbolNewLast;
            _symbol.NewLevel2 -= OnSymbolNewLevel2;
        }

        if (_history != null)
        {
            _history.NewHistoryItem -= OnHistoryChanged;
            _history.HistoryItemUpdated -= OnHistoryChanged;
            _history.Dispose();
            _history = null;
        }

        _http?.Dispose();
        _http = null;

        _aggr.Clear();
        _prices.Clear();
        _dom.Clear();
        foreach (var st in _states.Values)
            st.Reset();

        this.LogInfo("Stopped");
    }

    protected override void OnSettingsUpdated()
    {
        base.OnSettingsUpdated();

        if (!ManualTriggerS1) _states["S1"].ManualConsumed = false;
        if (!ManualTriggerS2) _states["S2"].ManualConsumed = false;
        if (!ManualTriggerS3) _states["S3"].ManualConsumed = false;

        TryHandleManualTriggers(DateTime.UtcNow);
    }

    private void OnHistoryChanged(object sender, HistoryEventArgs e)
    {
        // Keep history stream warm.
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

    private bool TryBuildContext(DateTime nowUtc, double price, out Context ctx)
    {
        ctx = default;
        if (_history == null || _history.Count <= 0)
            return false;

        var tick = GetTickSize();
        var bars = Math.Min(Math.Max(lookbackMinutes, 10), _history.Count);
        var low = double.MaxValue;
        var high = double.MinValue;
        var swingLow = double.MaxValue;
        var swingHigh = double.MinValue;

        var mids = new List<double>(bars);
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

        if (low == double.MaxValue || high == double.MinValue || mids.Count == 0)
            return false;

        var poc = bins.OrderByDescending(x => x.Value).First().Key * tick;
        var vwap = mids.Average();
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
            low,
            high,
            poc,
            vwap,
            swingLow == double.MaxValue ? low : swingLow,
            swingHigh == double.MinValue ? high : swingHigh,
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

    private bool TrySendSignal(string strategyId, string side, string setupType, DateTime nowUtc, double entryPrice, double anchorLevel, bool bypassGuards)
    {
        if (_http == null || _symbol == null)
            return false;

        if (string.IsNullOrWhiteSpace(RelayUrl) || string.IsNullOrWhiteSpace(RelaySecret))
            return false;

        var st = _states[strategyId];
        var bar = NormalizeMinute(GetBarTimeUtc(nowUtc));
        if (!bypassGuards && !CanAutoSignal(st, bar, nowUtc))
            return false;

        if (!TryBuildContext(nowUtc, entryPrice, out var ctx))
            return false;

        var plan = BuildTradePlan(strategyId, side, setupType, entryPrice, anchorLevel, ctx);
        var risk = GetRiskPercent(strategyId);
        var comment = BuildComment(strategyId, side, setupType);
        var payload = BuildPayloadJson(strategyId, side, risk, comment, plan);

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, RelayUrl);
            req.Headers.Add("X-Auth", RelaySecret);
            req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var resp = _http.Send(req);
            var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!resp.IsSuccessStatusCode)
            {
                this.LogError($"POST failed status={(int)resp.StatusCode} strategy={strategyId} side={side} body={body}");
                return false;
            }

            var id = TryExtractId(body);
            st.CooldownUntilUtc = nowUtc.AddSeconds(Math.Max(1, cooldownSeconds));
            st.LastSignalBarUtc = ctx.BarTimeUtc;
            st.LastSignalUtc = nowUtc;
            this.LogInfo($"Signal sent strategy={strategyId} id={id} side={side} setup={setupType} sl={plan.SlTicks} tp1={plan.Tp1Ticks} tp2={plan.Tp2Ticks}");
            return true;
        }
        catch (Exception ex)
        {
            this.LogError($"POST exception strategy={strategyId} side={side}: {ex.GetType().Name} {ex.Message}");
            return false;
        }
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

        var minTp1 = (int)Math.Ceiling(slTicks * Math.Max(1.0, minRR_TP1));
        var minTp2 = (int)Math.Ceiling(slTicks * Math.Max(minRR_TP2, minRR_TP1 + 0.1));

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

        var confidence = ComputeConfidence(strategyId, side, setupType, ctx);
        return new TradePlan(slTicks, tp1, tp2, confidence);
    }

    private string BuildPayloadJson(string strategyId, string side, double risk, string comment, TradePlan plan)
    {
        var unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return "{" +
               "\"source\":\"quantower\"," +
               $"\"strategy_id\":\"{JsonEscape(strategyId.ToUpperInvariant())}\"," +
               $"\"symbol\":\"{JsonEscape(SymbolName ?? string.Empty)}\"," +
               $"\"side\":\"{JsonEscape(side.ToUpperInvariant())}\"," +
               "\"order_type\":\"MARKET\"," +
               $"\"sl_ticks\":{plan.SlTicks}," +
               $"\"tp1_ticks\":{plan.Tp1Ticks}," +
               $"\"tp2_ticks\":{plan.Tp2Ticks}," +
               $"\"tp_ticks\":{plan.Tp2Ticks}," +
               $"\"risk\":{risk.ToString(CultureInfo.InvariantCulture)}," +
               $"\"trade_symbol\":\"{JsonEscape(TradeSymbolForMT5 ?? string.Empty)}\"," +
               $"\"comment\":\"{JsonEscape(comment)}\"," +
               $"\"confidence\":{plan.Confidence.ToString("F4", CultureInfo.InvariantCulture)}," +
               $"\"ts_client\":{unix}" +
               "}";
    }

    private string BuildComment(string strategyId, string side, string setupType)
    {
        var prefix = strategyId switch
        {
            "S1" => CommentS1,
            "S2" => CommentS2,
            "S3" => CommentS3,
            _ => strategyId
        };

        if (string.IsNullOrWhiteSpace(prefix))
            prefix = strategyId;

        var setup = setupType switch
        {
            "absorption_reversal" => "ABSORB",
            "liquidity_vacuum" => "VACUUM",
            "break_retest" => "RETEST",
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
        if (strategyId == "S1") score += 0.10;
        if (strategyId == "S3") score += 0.05;
        return Clamp01(score);
    }

    private bool CanAutoSignal(RuntimeState state, DateTime barTimeUtc, DateTime nowUtc)
    {
        return nowUtc >= state.CooldownUntilUtc && state.LastSignalBarUtc != barTimeUtc;
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
            if (!IsValid(p) || s < wallSizeThreshold || Math.Abs(p - seedPrice) > tick)
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
        persistenceSeconds = count == 1 ? 0 : (lastTs - first).TotalSeconds;
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
        var keep = Math.Max(Math.Max(aggressionWindowSeconds, noProgressSeconds), wallPersistenceSeconds) + 120;
        var cutoff = nowUtc.AddSeconds(-keep);
        while (_aggr.Count > 0 && _aggr.Peek().TimeUtc < cutoff) _aggr.Dequeue();
        while (_prices.Count > 0 && _prices.Peek().TimeUtc < cutoff) _prices.Dequeue();
        while (_dom.Count > 0 && _dom.Peek().TimeUtc < cutoff) _dom.Dequeue();
    }

    private double GetCurrentPrice()
    {
        if (IsValid(_lastTrade) && _lastTrade > 0) return _lastTrade;
        if (_symbol != null && IsValid(_symbol.Last) && _symbol.Last > 0) return _symbol.Last;
        if (IsValid(_lastBid) && IsValid(_lastAsk) && _lastAsk > _lastBid) return (_lastBid + _lastAsk) * 0.5;
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
        return symbols.FirstOrDefault(s => s.Name != null && s.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static string ParseSide(string side) => string.Equals(side, "SELL", StringComparison.OrdinalIgnoreCase) ? "SELL" : "BUY";
    private double GetTickSize() { var tick = _symbol?.TickSize ?? 0.01; return IsValid(tick) && tick > 0 ? tick : 0.01; }
    private DateTime GetBarTimeUtc(DateTime fallback) { try { return _history != null && _history.Count > 0 ? _history.Time(0).ToUniversalTime() : fallback; } catch { return fallback; } }
    private static DateTime NormalizeMinute(DateTime t) => new(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0, DateTimeKind.Utc);
    private static bool IsValid(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
    private static int Median(List<int> values, int fallback) { if (values == null || values.Count == 0) return fallback; values.Sort(); var m = values.Count / 2; return values.Count % 2 == 1 ? values[m] : (values[m - 1] + values[m]) / 2; }
    private static double MinValid(params double[] values) { var min = double.MaxValue; var ok = false; foreach (var v in values) { if (!IsValid(v)) continue; if (v < min) min = v; ok = true; } return ok ? min : double.NaN; }
    private static double MaxValid(params double[] values) { var max = double.MinValue; var ok = false; foreach (var v in values) { if (!IsValid(v)) continue; if (v > max) max = v; ok = true; } return ok ? max : double.NaN; }
    private static string JsonEscape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");

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
