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

public sealed class RelaySignal_GoldAbsorption : Strategy
{
    [InputParameter("SymbolName", 0)]
    public string SymbolName = "/GCJ26:XCEC";

    [InputParameter("RelayUrl", 1)]
    public string RelayUrl = "http://127.0.0.1:8000/signal";

    [InputParameter("RelaySecret", 2)]
    public string RelaySecret = string.Empty;

    [InputParameter("TradeSymbolForMT5", 3)]
    public string TradeSymbolForMT5 = "XAUUSD";

    [InputParameter("sl_ticks", 4, 1, 100000, 1, 0)]
    public int sl_ticks = 40;

    [InputParameter("tp_ticks", 5, 1, 100000, 1, 0)]
    public int tp_ticks = 80;

    [InputParameter("risk", 6, 0.01, 100.0, 0.01, 2)]
    public double risk = 0.25;

    [InputParameter("cooldownSeconds", 7, 1, 3600, 1, 0)]
    public int cooldownSeconds = 20;

    [InputParameter("lookbackMinutes", 8, 1, 240, 1, 0)]
    public int lookbackMinutes = 10;

    [InputParameter("supportBufferTicks", 9, 0, 100, 1, 0)]
    public int supportBufferTicks = 2;

    [InputParameter("breakTicks", 10, 0, 100, 1, 0)]
    public int breakTicks = 2;

    [InputParameter("confirmTicks", 11, 0, 100, 1, 0)]
    public int confirmTicks = 2;

    [InputParameter("noProgressSeconds", 12, 1, 120, 1, 0)]
    public int noProgressSeconds = 8;

    [InputParameter("aggressionWindowSeconds", 13, 1, 120, 1, 0)]
    public int aggressionWindowSeconds = 10;

    [InputParameter("aggressionThreshold", 14, 1, 10000, 1, 0)]
    public int aggressionThreshold = 12;

    [InputParameter("ManualTrigger", 15)]
    public bool ManualTrigger = false;

    [InputParameter("ManualSide (BUY/SELL)", 16)]
    public string ManualSide = "BUY";

    [InputParameter("EnableAutoMode", 17)]
    public bool EnableAutoMode = true;

    [InputParameter("UseDynamicStops", 18)]
    public bool UseDynamicStops = true;

    [InputParameter("domLevels", 19, 1, 20, 1, 0)]
    public int domLevels = 5;

    [InputParameter("domImbalanceThreshold", 20, 0.51, 0.95, 0.01, 2)]
    public double domImbalanceThreshold = 0.55;

    [InputParameter("largePrintMultiplier", 21, 1.0, 10.0, 0.1, 2)]
    public double largePrintMultiplier = 2.0;

    [InputParameter("largePrintsThreshold", 22, 1, 200, 1, 0)]
    public int largePrintsThreshold = 2;

    [InputParameter("absorptionPrintsThreshold", 23, 1, 200, 1, 0)]
    public int absorptionPrintsThreshold = 4;

    [InputParameter("volatilityLookbackBars", 24, 2, 120, 1, 0)]
    public int volatilityLookbackBars = 8;

    [InputParameter("volatilityStopMultiplier", 25, 0.5, 5.0, 0.1, 2)]
    public double volatilityStopMultiplier = 1.1;

    [InputParameter("minDynamicSlTicks", 26, 1, 10000, 1, 0)]
    public int minDynamicSlTicks = 20;

    [InputParameter("maxDynamicSlTicks", 27, 2, 20000, 1, 0)]
    public int maxDynamicSlTicks = 220;

    [InputParameter("minRewardRisk", 28, 0.5, 5.0, 0.1, 2)]
    public double minRewardRisk = 1.0;

    [InputParameter("maxRewardRisk", 29, 0.6, 10.0, 0.1, 2)]
    public double maxRewardRisk = 2.2;

    [InputParameter("UsePercentTargetsForGold", 30)]
    public bool UsePercentTargetsForGold = true;

    [InputParameter("SlPercentOfGoldPrice", 31, 0.05, 5.0, 0.01, 2)]
    public double SlPercentOfGoldPrice = 0.30;

    [InputParameter("Tp1PercentOfGoldPrice", 32, 0.05, 10.0, 0.01, 2)]
    public double Tp1PercentOfGoldPrice = 0.60;

    [InputParameter("Tp2PercentOfGoldPrice", 33, 0.05, 20.0, 0.01, 2)]
    public double Tp2PercentOfGoldPrice = 1.20;

    [InputParameter("EnableFrequentTestSignals", 34)]
    public bool EnableFrequentTestSignals = false;

    [InputParameter("FrequentTestBarsInterval", 35, 1, 30, 1, 0)]
    public int FrequentTestBarsInterval = 1;

    [InputParameter("FrequentTestCooldownSeconds", 36, 1, 300, 1, 0)]
    public int FrequentTestCooldownSeconds = 1;

    [InputParameter("EnableRelayFailureAlert", 37)]
    public bool EnableRelayFailureAlert = true;

    [InputParameter("RelayFailureAlertThreshold", 38, 1, 100, 1, 0)]
    public int RelayFailureAlertThreshold = 5;

    private Symbol? _symbol;
    private HistoricalData? _history;
    private HttpClient? _httpClient;

    private readonly Queue<AggressionSample> _aggressionSamples = new();
    private readonly Queue<PriceSample> _recentPrices = new();
    private readonly Queue<DomSnapshot> _domSnapshots = new();

    private double _lastBid = double.NaN;
    private double _lastAsk = double.NaN;
    private double _lastTradePrice = double.NaN;

    private double _latestDomBidDepth = double.NaN;
    private double _latestDomAskDepth = double.NaN;
    private double _latestDomImbalance = 0.5;

    private PendingSetup? _pendingBuy;
    private PendingSetup? _pendingSell;

    private DateTime _cooldownUntilUtc = DateTime.MinValue;
    private DateTime _lastAutoSignalBarUtc = DateTime.MinValue;

    private bool _manualTriggerConsumed;
    private readonly object _initGate = new();
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private int _relayFailureCount;
    private Timer? _bootstrapTimer;
    private DateTime _lastBootstrapLogUtc = DateTime.MinValue;
    private bool _subscriptionsActive;
    private bool _isStopping;

    public RelaySignal_GoldAbsorption()
    {
        Name = nameof(RelaySignal_GoldAbsorption);
        Description = "Gold absorption reversal signal relay to local FastAPI service";
    }

    protected override void OnRun()
    {
        _isStopping = false;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(3)
        };

        if (!IsSecretConfigured(RelaySecret))
            this.LogError("RelaySecret is missing or placeholder. Set a real shared secret in strategy settings.");

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

        _httpClient?.Dispose();
        _httpClient = null;

        _aggressionSamples.Clear();
        _recentPrices.Clear();
        _domSnapshots.Clear();
        _pendingBuy = null;
        _pendingSell = null;

        this.LogInfo("Stopped.");
    }

    protected override void OnSettingsUpdated()
    {
        base.OnSettingsUpdated();

        if (!ManualTrigger)
            _manualTriggerConsumed = false;

        if (!_subscriptionsActive)
        {
            TryInitializeSymbolAndSubscriptions(logWaiting: true);
            if (!_subscriptionsActive)
                StartBootstrapTimer();
        }

        TryHandleManualTrigger(DateTime.UtcNow);
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

            var from = DateTime.UtcNow.AddMinutes(-Math.Max(lookbackMinutes * 2, 30));
            _history = _symbol.GetHistory(Period.MIN1, from, DateTime.UtcNow);

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
        this.LogInfo($"Started on symbol={boundSymbol}. RelayUrl={RelayUrl}. AutoMode={EnableAutoMode}. DynamicStops={UseDynamicStops}");
        TryHandleManualTrigger(DateTime.UtcNow);
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
        TryHandleManualTrigger(now);
        if (EnableAutoMode)
        {
            var price = GetCurrentPrice();
            if (IsValidNumber(price) && price > 0)
                TryEvaluateAuto(price, now);
        }
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

        var nowUtc = DateTime.UtcNow;
        CalculateDomDepth(dom, out var bidDepth, out var askDepth);

        var total = bidDepth + askDepth;
        var imbalance = total > 0 ? bidDepth / total : 0.5;

        _latestDomBidDepth = bidDepth;
        _latestDomAskDepth = askDepth;
        _latestDomImbalance = Clamp01(imbalance);

        _domSnapshots.Enqueue(new DomSnapshot(nowUtc, bidDepth, askDepth, _latestDomImbalance));
        TrimQueues(nowUtc);
    }

    private void OnSymbolNewLast(Symbol symbol, Last last)
    {
        if (_symbol == null || symbol.Id != _symbol.Id)
            return;

        var now = DateTime.UtcNow;

        RecordAggression(last, now);
        RecordPrice(last.Price, now);

        TryHandleManualTrigger(now);

        if (EnableAutoMode)
            TryEvaluateAuto(last.Price, now);
    }

    private void TryHandleManualTrigger(DateTime nowUtc)
    {
        if (!ManualTrigger)
        {
            _manualTriggerConsumed = false;
            return;
        }

        if (_manualTriggerConsumed)
            return;

        var side = ParseSide(ManualSide);
        var currentPrice = GetCurrentPrice();
        if (TrySendSignal(side, "manual_test", nowUtc, isManual: true, currentPrice: currentPrice, anchorLevel: currentPrice))
        {
            _manualTriggerConsumed = true;
            ManualTrigger = false;
            this.LogInfo($"Manual trigger consumed. side={side}. Parameter reset to false.");
        }
    }

    private void TryEvaluateAuto(double currentPrice, DateTime nowUtc)
    {
        if (_symbol == null || _history == null)
            return;

        if (nowUtc < _cooldownUntilUtc)
            return;

        if (!TryComputeLevels(out var supportLevel, out var resistanceLevel, out var currentBarTimeUtc))
            return;

        if (_lastAutoSignalBarUtc == currentBarTimeUtc)
            return;

        if (EnableFrequentTestSignals)
        {
            if (TryEvaluateFrequentTestSignals(currentPrice, nowUtc, currentBarTimeUtc, supportLevel, resistanceLevel))
                return;
        }

        var tickSize = GetTickSize();
        var supportBuffer = supportBufferTicks * tickSize;
        var breakDistance = breakTicks * tickSize;
        var confirmDistance = confirmTicks * tickSize;

        var sellAggression = CountAggression(nowUtc, wantSell: true);
        var buyAggression = CountAggression(nowUtc, wantSell: false);

        var minRecent = MinRecentPrice(nowUtc);
        var maxRecent = MaxRecentPrice(nowUtc);

        var nearSupport = currentPrice >= supportLevel - supportBuffer && currentPrice <= supportLevel + supportBuffer;
        var nearResistance = currentPrice >= resistanceLevel - supportBuffer && currentPrice <= resistanceLevel + supportBuffer;

        var noNewLowBeyondBreak = !double.IsNaN(minRecent) && minRecent >= supportLevel - breakDistance;
        var noNewHighBeyondBreak = !double.IsNaN(maxRecent) && maxRecent <= resistanceLevel + breakDistance;

        if (nearSupport && sellAggression >= aggressionThreshold && noNewLowBeyondBreak)
            _pendingBuy = new PendingSetup(supportLevel, nowUtc);

        if (nearResistance && buyAggression >= aggressionThreshold && noNewHighBeyondBreak)
            _pendingSell = new PendingSetup(resistanceLevel, nowUtc);

        ExpirePending(nowUtc);

        if (_pendingBuy != null && currentPrice >= _pendingBuy.Level + confirmDistance)
        {
            if (TrySendSignal("BUY", "absorption_reversal", nowUtc, isManual: false, currentPrice: currentPrice, anchorLevel: _pendingBuy.Level))
            {
                _lastAutoSignalBarUtc = currentBarTimeUtc;
                _pendingBuy = null;
                _pendingSell = null;
            }
        }

        if (_pendingSell != null && currentPrice <= _pendingSell.Level - confirmDistance)
        {
            if (TrySendSignal("SELL", "absorption_reversal", nowUtc, isManual: false, currentPrice: currentPrice, anchorLevel: _pendingSell.Level))
            {
                _lastAutoSignalBarUtc = currentBarTimeUtc;
                _pendingBuy = null;
                _pendingSell = null;
            }
        }
    }

    private bool TryEvaluateFrequentTestSignals(double currentPrice, DateTime nowUtc, DateTime currentBarTimeUtc, double supportLevel, double resistanceLevel)
    {
        var interval = Math.Max(1, FrequentTestBarsInterval);
        if ((currentBarTimeUtc.Minute % interval) != 0)
            return false;

        var midpoint = (supportLevel + resistanceLevel) * 0.5;
        var side = currentPrice >= midpoint ? "BUY" : "SELL";
        var anchor = side == "BUY" ? supportLevel : resistanceLevel;
        if (!IsValidNumber(anchor))
            anchor = currentPrice;

        if (TrySendSignal(side, "test_mode_fast", nowUtc, isManual: false, currentPrice: currentPrice, anchorLevel: anchor))
        {
            _lastAutoSignalBarUtc = currentBarTimeUtc;
            _pendingBuy = null;
            _pendingSell = null;
            _cooldownUntilUtc = nowUtc.AddSeconds(Math.Max(1, FrequentTestCooldownSeconds));
            return true;
        }

        return false;
    }

    private void ExpirePending(DateTime nowUtc)
    {
        var ttlSeconds = Math.Max(noProgressSeconds, aggressionWindowSeconds) + 5;
        var ttl = TimeSpan.FromSeconds(ttlSeconds);

        if (_pendingBuy != null && nowUtc - _pendingBuy.CreatedUtc > ttl)
            _pendingBuy = null;

        if (_pendingSell != null && nowUtc - _pendingSell.CreatedUtc > ttl)
            _pendingSell = null;
    }

    private bool TryComputeLevels(out double supportLevel, out double resistanceLevel, out DateTime currentBarTimeUtc)
    {
        supportLevel = double.NaN;
        resistanceLevel = double.NaN;
        currentBarTimeUtc = NormalizeMinute(DateTime.UtcNow);

        if (_history == null || _history.Count <= 0)
            return false;

        var barsToRead = Math.Min(Math.Max(lookbackMinutes, 1), _history.Count);

        var lows = new List<double>(barsToRead);
        var highs = new List<double>(barsToRead);

        for (var i = 0; i < barsToRead; i++)
        {
            var low = _history.Low(i);
            var high = _history.High(i);

            if (double.IsNaN(low) || double.IsInfinity(low) || double.IsNaN(high) || double.IsInfinity(high))
                continue;

            lows.Add(low);
            highs.Add(high);
        }

        if (lows.Count == 0 || highs.Count == 0)
            return false;

        TryComputeRobustLevels(lows, highs, GetTickSize(), out supportLevel, out resistanceLevel);
        if (!IsValidNumber(supportLevel) || !IsValidNumber(resistanceLevel) || resistanceLevel <= supportLevel)
            return false;

        try
        {
            currentBarTimeUtc = NormalizeMinute(_history.Time(0).ToUniversalTime());
        }
        catch
        {
            currentBarTimeUtc = NormalizeMinute(DateTime.UtcNow);
        }

        return true;
    }

    private void RecordAggression(Last last, DateTime nowUtc)
    {
        var isSell = false;
        var isBuy = false;

        if (last.AggressorFlag == AggressorFlag.Sell)
            isSell = true;
        else if (last.AggressorFlag == AggressorFlag.Buy)
            isBuy = true;
        else if (IsValidNumber(_lastBid) && last.Price <= _lastBid)
            isSell = true;
        else if (IsValidNumber(_lastAsk) && last.Price >= _lastAsk)
            isBuy = true;
        else if (last.TickDirection == TickDirection.Down)
            isSell = true;
        else if (last.TickDirection == TickDirection.Up)
            isBuy = true;
        else if (IsValidNumber(_lastTradePrice) && last.Price < _lastTradePrice)
            isSell = true;
        else if (IsValidNumber(_lastTradePrice) && last.Price > _lastTradePrice)
            isBuy = true;

        _lastTradePrice = last.Price;

        if (!isSell && !isBuy)
            return;

        _aggressionSamples.Enqueue(new AggressionSample(nowUtc, isSell, isBuy, last.Price, Math.Max(last.Size, 0)));
        TrimQueues(nowUtc);
    }

    private void RecordPrice(double price, DateTime nowUtc)
    {
        _recentPrices.Enqueue(new PriceSample(nowUtc, price));
        TrimQueues(nowUtc);
    }

    private void CalculateDomDepth(DOMQuote dom, out double bidDepth, out double askDepth)
    {
        bidDepth = 0;
        askDepth = 0;

        if (dom == null)
            return;

        if (dom.Bids != null)
        {
            var levels = Math.Min(Math.Max(domLevels, 1), dom.Bids.Count);
            for (var i = 0; i < levels; i++)
            {
                var q = dom.Bids[i];
                if (q == null || q.Closed || !IsValidNumber(q.Size) || q.Size <= 0)
                    continue;

                bidDepth += q.Size;
            }
        }

        if (dom.Asks != null)
        {
            var levels = Math.Min(Math.Max(domLevels, 1), dom.Asks.Count);
            for (var i = 0; i < levels; i++)
            {
                var q = dom.Asks[i];
                if (q == null || q.Closed || !IsValidNumber(q.Size) || q.Size <= 0)
                    continue;

                askDepth += q.Size;
            }
        }
    }

    private void TrimQueues(DateTime nowUtc)
    {
        var keepSeconds = Math.Max(aggressionWindowSeconds, noProgressSeconds);
        keepSeconds = Math.Max(keepSeconds, 20) + 15;
        var cutoff = nowUtc.AddSeconds(-keepSeconds);

        while (_aggressionSamples.Count > 0 && _aggressionSamples.Peek().TimeUtc < cutoff)
            _aggressionSamples.Dequeue();

        while (_recentPrices.Count > 0 && _recentPrices.Peek().TimeUtc < cutoff)
            _recentPrices.Dequeue();

        while (_domSnapshots.Count > 0 && _domSnapshots.Peek().TimeUtc < cutoff)
            _domSnapshots.Dequeue();

        // Keep upper bounds to prevent runaway iteration cost on very high tick rates.
        var maxSamples = Math.Max(1500, keepSeconds * 200);
        while (_aggressionSamples.Count > maxSamples)
            _aggressionSamples.Dequeue();
        while (_recentPrices.Count > maxSamples)
            _recentPrices.Dequeue();
        while (_domSnapshots.Count > maxSamples)
            _domSnapshots.Dequeue();
    }

    private int CountAggression(DateTime nowUtc, bool wantSell)
    {
        var cutoff = nowUtc.AddSeconds(-Math.Max(aggressionWindowSeconds, 1));
        var count = 0;

        foreach (var sample in _aggressionSamples)
        {
            if (sample.TimeUtc < cutoff)
                continue;

            if (wantSell && sample.IsSell)
                count++;
            if (!wantSell && sample.IsBuy)
                count++;
        }

        return count;
    }

    private int CountLargePrints(DateTime nowUtc, bool wantSell)
    {
        var cutoff = nowUtc.AddSeconds(-Math.Max(aggressionWindowSeconds, 1));
        var sizes = new List<double>();

        foreach (var sample in _aggressionSamples)
        {
            if (sample.TimeUtc < cutoff)
                continue;

            if (wantSell && !sample.IsSell)
                continue;
            if (!wantSell && !sample.IsBuy)
                continue;

            if (sample.Size > 0)
                sizes.Add(sample.Size);
        }

        if (sizes.Count == 0)
            return 0;

        var avg = sizes.Average();
        var threshold = avg * Math.Max(largePrintMultiplier, 1.0);
        var count = 0;

        foreach (var size in sizes)
        {
            if (size >= threshold)
                count++;
        }

        return count;
    }

    private double ComputeAbsorptionScore(string side, double anchorLevel, DateTime nowUtc)
    {
        if (!IsValidNumber(anchorLevel))
            return 0;

        var cutoff = nowUtc.AddSeconds(-Math.Max(noProgressSeconds, 1));
        var wantSell = string.Equals(side, "BUY", StringComparison.OrdinalIgnoreCase);
        var band = Math.Max(GetTickSize(), supportBufferTicks * GetTickSize());

        var count = 0;
        var minPrice = double.MaxValue;
        var maxPrice = double.MinValue;

        foreach (var sample in _aggressionSamples)
        {
            if (sample.TimeUtc < cutoff)
                continue;

            if (wantSell && !sample.IsSell)
                continue;
            if (!wantSell && !sample.IsBuy)
                continue;

            if (Math.Abs(sample.Price - anchorLevel) > band)
                continue;

            count++;
            if (sample.Price < minPrice)
                minPrice = sample.Price;
            if (sample.Price > maxPrice)
                maxPrice = sample.Price;
        }

        if (count == 0)
            return 0;

        var tickSize = GetTickSize();
        var priceRangeTicks = (maxPrice - minPrice) / tickSize;
        var countScore = Clamp01((double)count / Math.Max(absorptionPrintsThreshold, 1));
        var stableRange = priceRangeTicks <= Math.Max(breakTicks, 1) * 1.5;

        return stableRange ? Math.Min(1.0, countScore + 0.25) : countScore * 0.5;
    }

    private double ComputeDomScore(string side)
    {
        if (_domSnapshots.Count == 0)
            return 0.5;

        var sideIsBuy = string.Equals(side, "BUY", StringComparison.OrdinalIgnoreCase);
        var sideImbalance = sideIsBuy ? _latestDomImbalance : 1.0 - _latestDomImbalance;

        var threshold = Math.Max(0.51, Math.Min(0.95, domImbalanceThreshold));
        var imbalanceScore = Clamp01((sideImbalance - 0.5) / Math.Max(0.01, threshold - 0.5));

        var sideDepth = sideIsBuy ? _latestDomBidDepth : _latestDomAskDepth;
        var oppDepth = sideIsBuy ? _latestDomAskDepth : _latestDomBidDepth;
        var totalDepth = sideDepth + oppDepth;
        var depthDominance = totalDepth > 0 ? sideDepth / totalDepth : 0.5;
        var depthScore = Clamp01((depthDominance - 0.5) / 0.25);

        var shiftScore = 0.5;
        if (_domSnapshots.Count >= 2)
        {
            var arr = _domSnapshots.ToArray();
            var prev = arr[arr.Length - 2];
            var cur = arr[arr.Length - 1];

            var prevSide = sideIsBuy ? prev.BidDepth : prev.AskDepth;
            var curSide = sideIsBuy ? cur.BidDepth : cur.AskDepth;
            var prevOpp = sideIsBuy ? prev.AskDepth : prev.BidDepth;
            var curOpp = sideIsBuy ? cur.AskDepth : cur.BidDepth;

            var sidePct = prevSide > 0 ? (curSide - prevSide) / prevSide : 0;
            var oppPct = prevOpp > 0 ? (prevOpp - curOpp) / prevOpp : 0;
            shiftScore = Clamp01(0.5 + 0.5 * (sidePct + oppPct));
        }

        return Clamp01(0.50 * imbalanceScore + 0.30 * depthScore + 0.20 * shiftScore);
    }

    private int ComputeStructureStopTicks(string side, double currentPrice, double anchorLevel, double tickSize, int fallback)
    {
        if (!IsValidNumber(anchorLevel) || tickSize <= 0)
            return fallback;

        if (string.Equals(side, "BUY", StringComparison.OrdinalIgnoreCase))
        {
            var stopPrice = anchorLevel - Math.Max(breakTicks, 1) * tickSize;
            return Math.Max(1, (int)Math.Ceiling((currentPrice - stopPrice) / tickSize));
        }

        var sellStop = anchorLevel + Math.Max(breakTicks, 1) * tickSize;
        return Math.Max(1, (int)Math.Ceiling((sellStop - currentPrice) / tickSize));
    }

    private int ComputeVolatilityTicks(double tickSize, int fallback)
    {
        if (_history == null || _history.Count <= 0 || tickSize <= 0)
            return fallback;

        var bars = Math.Min(Math.Max(volatilityLookbackBars, 2), _history.Count);
        double sumRanges = 0;
        var valid = 0;

        for (var i = 0; i < bars; i++)
        {
            var high = _history.High(i);
            var low = _history.Low(i);

            if (!IsValidNumber(high) || !IsValidNumber(low) || high < low)
                continue;

            sumRanges += (high - low);
            valid++;
        }

        if (valid == 0)
            return fallback;

        var avgRange = sumRanges / valid;
        var ticks = (int)Math.Ceiling((avgRange / tickSize) * Math.Max(0.5, volatilityStopMultiplier));
        return Math.Max(1, ticks);
    }

    private double MinRecentPrice(DateTime nowUtc)
    {
        var cutoff = nowUtc.AddSeconds(-Math.Max(noProgressSeconds, 1));
        var min = double.MaxValue;
        var found = false;

        foreach (var sample in _recentPrices)
        {
            if (sample.TimeUtc < cutoff)
                continue;

            if (sample.Price < min)
                min = sample.Price;

            found = true;
        }

        return found ? min : double.NaN;
    }

    private double MaxRecentPrice(DateTime nowUtc)
    {
        var cutoff = nowUtc.AddSeconds(-Math.Max(noProgressSeconds, 1));
        var max = double.MinValue;
        var found = false;

        foreach (var sample in _recentPrices)
        {
            if (sample.TimeUtc < cutoff)
                continue;

            if (sample.Price > max)
                max = sample.Price;

            found = true;
        }

        return found ? max : double.NaN;
    }

    private double GetCurrentPrice()
    {
        if (IsValidNumber(_lastTradePrice) && _lastTradePrice > 0)
            return _lastTradePrice;

        if (_symbol != null && IsValidNumber(_symbol.Last) && _symbol.Last > 0)
            return _symbol.Last;

        if (IsValidNumber(_lastBid) && IsValidNumber(_lastAsk) && _lastBid > 0 && _lastAsk > 0)
            return (_lastBid + _lastAsk) * 0.5;

        if (_recentPrices.Count > 0)
        {
            var latest = _recentPrices.Last().Price;
            if (IsValidNumber(latest) && latest > 0)
                return latest;
        }

        if (_history != null && _history.Count > 0)
        {
            var high = _history.High(0);
            var low = _history.Low(0);
            if (IsValidNumber(high) && IsValidNumber(low) && high >= low)
                return (high + low) * 0.5;
        }

        return double.NaN;
    }

    private bool TrySendSignal(string side, string reason, DateTime nowUtc, bool isManual, double currentPrice, double anchorLevel)
    {
        if (_httpClient == null || _symbol == null)
            return false;

        if (string.IsNullOrWhiteSpace(RelayUrl) || !IsSecretConfigured(RelaySecret))
        {
            this.LogError("RelayUrl or RelaySecret is invalid. Signal not sent.");
            return false;
        }

        if (!isManual && nowUtc < _cooldownUntilUtc)
            return false;

        var plan = BuildSignalPlan(side, reason, nowUtc, currentPrice, anchorLevel);
        var payloadJson = BuildPayloadJson(side, reason, plan, currentPrice, GetTickSize());

        _cooldownUntilUtc = nowUtc.AddSeconds(Math.Max(cooldownSeconds, 1));
        _ = PostSignalAsync(side, reason, payloadJson, plan, currentPrice);
        return true;
    }

    private async Task PostSignalAsync(string side, string reason, string payloadJson, SignalPlan plan, double currentPrice)
    {
        if (_httpClient == null || string.IsNullOrWhiteSpace(RelayUrl) || !IsSecretConfigured(RelaySecret))
            return;

        await _sendGate.WaitAsync().ConfigureAwait(false);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, RelayUrl);
            request.Headers.Add("X-Auth", RelaySecret);
            request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                this.LogError($"Signal POST failed. status={(int)response.StatusCode} side={side} reason={reason} body={body}");
                TrackRelayFailure();
                return;
            }

            if (_relayFailureCount > 0)
                this.LogInfo($"Relay post recovered after {_relayFailureCount} consecutive failures.");
            _relayFailureCount = 0;

            var id = TryExtractInt(body, "id", -1);
            var slPct = TicksToPercent(plan.SlTicks, GetTickSize(), currentPrice);
            var tpPct = TicksToPercent(plan.TpTicks, GetTickSize(), currentPrice);
            this.LogInfo($"Signal sent id={id} side={side} reason={reason} sl_ticks={plan.SlTicks}({slPct:F3}%) tp_ticks={plan.TpTicks}({tpPct:F3}%) conf={plan.Confidence:F2} dom={plan.DomScore:F2}");
        }
        catch (Exception ex)
        {
            this.LogError($"Signal POST exception side={side} reason={reason}. {ex.GetType().Name}: {ex.Message}");
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

    private SignalPlan BuildSignalPlan(string side, string reason, DateTime nowUtc, double currentPrice, double anchorLevel)
    {
        var baseSl = Math.Max(sl_ticks, 1);
        var baseTp = Math.Max(tp_ticks, 1);

        var wantSellAggression = string.Equals(side, "BUY", StringComparison.OrdinalIgnoreCase);
        var aggressionCount = CountAggression(nowUtc, wantSellAggression);
        var largePrintCount = CountLargePrints(nowUtc, wantSellAggression);
        var absorptionScore = ComputeAbsorptionScore(side, anchorLevel, nowUtc);
        var domScore = ComputeDomScore(side);

        var aggressionScore = Clamp01((double)aggressionCount / Math.Max(aggressionThreshold, 1));
        var largeScore = Clamp01((double)largePrintCount / Math.Max(largePrintsThreshold, 1));
        var confidence = Clamp01(0.35 * aggressionScore + 0.20 * largeScore + 0.25 * absorptionScore + 0.20 * domScore);

        if (!UseDynamicStops || !IsValidNumber(currentPrice) || currentPrice <= 0)
            return new SignalPlan(baseSl, baseTp, confidence, domScore, aggressionCount, largePrintCount, absorptionScore);

        var tickSize = GetTickSize();
        var structureTicks = ComputeStructureStopTicks(side, currentPrice, anchorLevel, tickSize, baseSl);
        var volatilityTicks = ComputeVolatilityTicks(tickSize, baseSl);

        var domRiskAdjust = domScore switch
        {
            >= 0.70 => 0.90,
            <= 0.30 => 1.20,
            _ => 1.00
        };

        var minSl = Math.Max(1, Math.Min(minDynamicSlTicks, maxDynamicSlTicks));
        var maxSl = Math.Max(minSl, Math.Max(minDynamicSlTicks, maxDynamicSlTicks));

        var dynamicSl = (int)Math.Ceiling(Math.Max(baseSl, Math.Max(structureTicks, volatilityTicks)) * domRiskAdjust);
        dynamicSl = Clamp(dynamicSl, minSl, maxSl);

        var rrMin = Math.Min(minRewardRisk, maxRewardRisk);
        var rrMax = Math.Max(minRewardRisk, maxRewardRisk);
        var rr = rrMin + (rrMax - rrMin) * confidence;

        if (string.Equals(reason, "manual_test", StringComparison.OrdinalIgnoreCase))
            rr = Math.Max(rr, 1.5);

        var dynamicTp = (int)Math.Ceiling(dynamicSl * rr);
        dynamicTp = Math.Max(dynamicTp, (int)Math.Ceiling(dynamicSl * rrMin));

        if (UsePercentTargetsForGold && IsValidNumber(currentPrice) && currentPrice > 0)
        {
            // Adaptive percent mode: percent distances remain gold-relative while tracking live structure.
            var tickForPct = GetTickSize();
            var baseSlPct = Math.Max(0.05, SlPercentOfGoldPrice);
            var baseTp1Pct = Math.Max(baseSlPct + 0.01, Tp1PercentOfGoldPrice);
            var baseTp2Pct = Math.Max(baseTp1Pct + 0.01, Tp2PercentOfGoldPrice);

            var minSlPct = baseSlPct * 0.55;
            var maxSlPct = baseSlPct * 2.50;
            var minTpPct = baseTp1Pct * 0.55;
            var maxTpPct = baseTp2Pct * 2.80;

            var slDynamicPct = TicksToPercent(dynamicSl, tickForPct, currentPrice);
            if (slDynamicPct <= 0)
                slDynamicPct = baseSlPct;

            var volFactor = ClampDouble(slDynamicPct / baseSlPct, 0.65, 1.75);
            slDynamicPct = ClampDouble(slDynamicPct * volFactor, minSlPct, maxSlPct);

            var tpDynamicPct = TicksToPercent(dynamicTp, tickForPct, currentPrice);
            if (tpDynamicPct <= 0)
                tpDynamicPct = baseTp2Pct * volFactor;

            var rrPctFloor = slDynamicPct * rrMin;
            tpDynamicPct = ClampDouble(tpDynamicPct, Math.Max(minTpPct, rrPctFloor), maxTpPct);

            var slPctTicks = PercentToTicks(currentPrice, slDynamicPct, tickForPct);
            var tpPctTicks = PercentToTicks(currentPrice, tpDynamicPct, tickForPct);

            if (slPctTicks > 0)
                dynamicSl = Clamp(slPctTicks, minSl, maxSl);

            var rrFloor = (int)Math.Ceiling(dynamicSl * rrMin);
            if (tpPctTicks > 0)
                dynamicTp = tpPctTicks;

            dynamicTp = Math.Max(dynamicTp, rrFloor);
        }

        return new SignalPlan(dynamicSl, dynamicTp, confidence, domScore, aggressionCount, largePrintCount, absorptionScore);
    }

    private string BuildPayloadJson(string side, string comment, SignalPlan plan, double entryPrice, double tickSize)
    {
        var unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var safeTick = IsValidNumber(tickSize) && tickSize > 0 ? tickSize : GetTickSize();
        var safeEntry = IsValidNumber(entryPrice) && entryPrice > 0 ? entryPrice : GetCurrentPrice();
        var tp1Ticks = Math.Max(1, (int)Math.Ceiling(plan.TpTicks * 0.5));
        var tp2Ticks = Math.Max(tp1Ticks + 1, plan.TpTicks);
        var slPercent = TicksToPercent(plan.SlTicks, safeTick, safeEntry);
        var tp1Percent = TicksToPercent(tp1Ticks, safeTick, safeEntry);
        var tp2Percent = TicksToPercent(tp2Ticks, safeTick, safeEntry);
        var distanceMode = UsePercentTargetsForGold ? "percent" : "ticks";
        var payload = new Dictionary<string, object?>
        {
            ["source"] = "quantower",
            ["strategy_id"] = "S1",
            ["symbol"] = SymbolName ?? string.Empty,
            ["side"] = (side ?? string.Empty).ToUpperInvariant(),
            ["order_type"] = "MARKET",
            ["sl_ticks"] = plan.SlTicks,
            ["tp1_ticks"] = tp1Ticks,
            ["tp2_ticks"] = tp2Ticks,
            ["tp_ticks"] = plan.TpTicks,
            ["sl_percent"] = slPercent,
            ["tp1_percent"] = tp1Percent,
            ["tp2_percent"] = tp2Percent,
            ["distance_mode"] = distanceMode,
            ["entry_price"] = safeEntry,
            ["risk"] = risk,
            ["trade_symbol"] = TradeSymbolForMT5 ?? string.Empty,
            ["comment"] = comment ?? string.Empty,
            ["confidence"] = plan.Confidence,
            ["dom_score"] = plan.DomScore,
            ["aggression_count"] = plan.AggressionCount,
            ["large_prints"] = plan.LargePrintCount,
            ["absorption_score"] = plan.AbsorptionScore,
            ["ts_client"] = unix
        };
        return JsonSerializer.Serialize(payload);
    }

    private static int TryExtractInt(string json, string key, int fallback)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
            return fallback;

        var marker = $"\"{key}\":";
        var p = json.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (p < 0)
            return fallback;

        p += marker.Length;
        while (p < json.Length && char.IsWhiteSpace(json[p]))
            p++;

        var e = p;
        if (e < json.Length && (json[e] == '-' || json[e] == '+'))
            e++;

        while (e < json.Length && char.IsDigit(json[e]))
            e++;

        if (e <= p)
            return fallback;

        var text = json.Substring(p, e - p);
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private Symbol? ResolveSymbol(string symbolName)
    {
        if (string.IsNullOrWhiteSpace(symbolName))
            return null;

        var symbols = Core.Instance.Symbols;
        if (symbols == null || symbols.Length == 0)
            return null;

        var exact = symbols.FirstOrDefault(s => string.Equals(s.Name, symbolName, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
            return exact;

        var contains = symbols.FirstOrDefault(s => s.Name != null && s.Name.IndexOf(symbolName, StringComparison.OrdinalIgnoreCase) >= 0);
        if (contains != null)
            return contains;

        var parts = symbolName.Split(':');
        var left = parts.Length > 0 ? parts[0].Trim() : symbolName.Trim();
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

    private string ParseSide(string manualSide)
    {
        if (string.Equals(manualSide, "SELL", StringComparison.OrdinalIgnoreCase))
            return "SELL";

        return "BUY";
    }

    private double GetTickSize()
    {
        if (_symbol == null)
            return 0.01;

        var tick = _symbol.TickSize;
        if (!IsValidNumber(tick) || tick <= 0)
            return 0.01;

        return tick;
    }

    private static bool IsValidNumber(double value)
        => !double.IsNaN(value) && !double.IsInfinity(value);

    private static bool IsSecretConfigured(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return false;
        return !string.Equals(secret.Trim(), "<RELAY_SECRET>", StringComparison.OrdinalIgnoreCase);
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

        support = swingLows.Count >= 2 ? Quantile(swingLows, 0.5) : Quantile(lows, 0.2);
        resistance = swingHighs.Count >= 2 ? Quantile(swingHighs, 0.5) : Quantile(highs, 0.8);

        if (tick > 0 && IsValidNumber(support) && IsValidNumber(resistance) && resistance - support < 4 * tick)
            resistance = support + 4 * tick;
    }

    private static int PercentToTicks(double entryPrice, double percent, double tickSize)
    {
        if (!IsValidNumber(entryPrice) || entryPrice <= 0 ||
            !IsValidNumber(percent) || percent <= 0 ||
            !IsValidNumber(tickSize) || tickSize <= 0)
            return 0;

        var distance = entryPrice * (percent / 100.0);
        if (!IsValidNumber(distance) || distance <= 0)
            return 0;

        return Math.Max(1, (int)Math.Ceiling(distance / tickSize));
    }

    private static double TicksToPercent(int ticks, double tickSize, double entryPrice)
    {
        if (ticks <= 0 ||
            !IsValidNumber(tickSize) || tickSize <= 0 ||
            !IsValidNumber(entryPrice) || entryPrice <= 0)
            return 0.0;

        var distance = ticks * tickSize;
        return distance / entryPrice * 100.0;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }

    private static double ClampDouble(double value, double min, double max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }

    private static double Clamp01(double value)
    {
        if (value < 0)
            return 0;
        if (value > 1)
            return 1;
        return value;
    }

    private static double Quantile(List<double> values, double q)
    {
        if (values == null || values.Count == 0)
            return double.NaN;
        var sorted = values.Where(IsValidNumber).OrderBy(x => x).ToList();
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

    private static DateTime NormalizeMinute(DateTime t)
        => new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0, DateTimeKind.Utc);

    private sealed class PendingSetup
    {
        public PendingSetup(double level, DateTime createdUtc)
        {
            Level = level;
            CreatedUtc = createdUtc;
        }

        public double Level { get; }
        public DateTime CreatedUtc { get; }
    }

    private readonly struct SignalPlan
    {
        public SignalPlan(int slTicks, int tpTicks, double confidence, double domScore, int aggressionCount, int largePrintCount, double absorptionScore)
        {
            SlTicks = slTicks;
            TpTicks = tpTicks;
            Confidence = confidence;
            DomScore = domScore;
            AggressionCount = aggressionCount;
            LargePrintCount = largePrintCount;
            AbsorptionScore = absorptionScore;
        }

        public int SlTicks { get; }
        public int TpTicks { get; }
        public double Confidence { get; }
        public double DomScore { get; }
        public int AggressionCount { get; }
        public int LargePrintCount { get; }
        public double AbsorptionScore { get; }
    }

    private readonly struct AggressionSample
    {
        public AggressionSample(DateTime timeUtc, bool isSell, bool isBuy, double price, double size)
        {
            TimeUtc = timeUtc;
            IsSell = isSell;
            IsBuy = isBuy;
            Price = price;
            Size = size;
        }

        public DateTime TimeUtc { get; }
        public bool IsSell { get; }
        public bool IsBuy { get; }
        public double Price { get; }
        public double Size { get; }
    }

    private readonly struct PriceSample
    {
        public PriceSample(DateTime timeUtc, double price)
        {
            TimeUtc = timeUtc;
            Price = price;
        }

        public DateTime TimeUtc { get; }
        public double Price { get; }
    }

    private readonly struct DomSnapshot
    {
        public DomSnapshot(DateTime timeUtc, double bidDepth, double askDepth, double imbalance)
        {
            TimeUtc = timeUtc;
            BidDepth = bidDepth;
            AskDepth = askDepth;
            Imbalance = imbalance;
        }

        public DateTime TimeUtc { get; }
        public double BidDepth { get; }
        public double AskDepth { get; }
        public double Imbalance { get; }
    }
}
