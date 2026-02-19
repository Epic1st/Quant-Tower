# Step 20 - Dynamic SL/TP + Order-Flow Upgrade + Live Validation

Date: 2026-02-19

## Quantower strategy upgrades
- Updated strategy source:
  - `D:\Trading-Quantower\relay_assets\quantower_algo\RelaySignal_GoldAbsorption.cs`
  - `D:\Trading-Quantower\relay_assets\RelaySignal_GoldAbsorption.cs`
- Added dynamic risk/target engine driven by order-flow proxies:
  - DOM top-N depth imbalance (`domLevels`, `domImbalanceThreshold`)
  - Aggression count over short window
  - Large print detection (`largePrintMultiplier`, `largePrintsThreshold`)
  - Absorption proxy near level (`absorptionPrintsThreshold`)
  - Volatility-aware stop sizing (`volatilityLookbackBars`, `volatilityStopMultiplier`)
- Dynamic payload fields now include:
  - `sl_ticks` (dynamic)
  - `tp_ticks` (dynamic)
  - plus diagnostics (`confidence`, `dom_score`, etc.)

## Build/deploy (Quantower)
- Build result: `0 errors, 0 warnings`
- DLL deployed:
  - `C:\Quantower\TradingPlatform\v1.145.16\bin\Scripts\Strategies\RelaySignal_GoldAbsorption.dll`

## MT5 EA upgrades (execution safety)
- Updated:
  - `D:\Trading-Quantower\relay_assets\MT5_RelayPoller.mq5`
- Added:
  - broker stop-level adjustment before send
  - retry without SL/TP on invalid stops
  - optional post-fill SL/TP placement attempts (`TrySetStopsAfterFill`)
- Build result: `0 errors, 0 warnings`

## Live test evidence (Quantower-format relay signals)
- Sent signal `id=3`:
  - `source=quantower`, `symbol=/GCJ26:XCEC`, `sl_ticks=65`, `tp_ticks=160`
- MT5 log:
  - `Signal received id=3 ... trade_symbol=GOLD.FUTURE ...`
  - `Order success id=3 ... sl=5014.73000 tp=5017.00000 ...`

- Sent signal `id=4`:
  - `source=quantower`, `symbol=/GCJ26:XCEC`, `side=SELL`, `sl_ticks=72`, `tp_ticks=180`
- MT5 log:
  - `Signal received id=4 ... trade_symbol=GOLD.FUTURE ...`
  - `Order success id=4 ... sl=5019.66000 tp=5017.14000 ...`

## Conclusion
- End-to-end path works with strict Quantower Gold-only filtering.
- Orders now execute on chart symbol and include valid SL/TP in live tests.
