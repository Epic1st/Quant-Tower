# Step 16 - Strict Quantower Gold-Only Signal Filter

Date: 2026-02-19

## Requirement addressed
- Signals must come from Quantower Gold futures only.

## EA changes
- Updated: `D:\Trading-Quantower\relay_assets\MT5_RelayPoller.mq5`
- Added input:
  - `StrictQuantowerGoldOnly = true` (default)
- New strict checks before any order:
  1. `source` must be `quantower`
  2. `symbol` must match `RelayFuturesSymbol` (default `/GCJ26:XCEC`)
- Non-matching signals are ignored and logged.

## Existing behavior retained
- `UseChartSymbolForExecution = true` keeps execution on attached chart symbol.
- This supports broker-specific futures symbol names while still requiring Quantower gold source signal.

## Build + deploy
- Recompiled successfully (`0 errors, 0 warnings`).
- Updated EX5 redeployed to MT5 Experts folder.

## MT5 inputs to keep
- `RelayFuturesSymbol = /GCJ26:XCEC`
- `StrictQuantowerGoldOnly = true`
- `UseChartSymbolForExecution = true`
- `IgnoreNonRelayFuturesSymbol = true`
