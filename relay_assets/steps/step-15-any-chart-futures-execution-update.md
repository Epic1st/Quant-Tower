# Step 15 - EA Updated For Any-Chart Futures Execution

Date: 2026-02-19

## What changed
- Updated `D:\Trading-Quantower\relay_assets\MT5_RelayPoller.mq5` to support chart-driven execution.

## New behavior
- `UseChartSymbolForExecution = true` (default)
  - EA always executes on the symbol of the chart it is attached to (`_Symbol`).
- `IgnoreNonRelayFuturesSymbol = true` (default)
  - EA ignores relay signals whose `src_symbol` does not match `RelayFuturesSymbol`.
  - This prevents accidental EURUSD trades when stale/mismatched relay payload exists.

## Existing mapping fallback
- If `UseChartSymbolForExecution = false`, EA falls back to payload/default symbol mapping logic.

## Build + deploy
- Recompiled with MetaEditor CLI.
- Compile result: `0 errors, 0 warnings`.
- Updated files copied to MT5 Experts folder:
  - `MT5_RelayPoller.mq5`
  - `MT5_RelayPoller.ex5`

## What to do in MT5 now
1. Remove EA from chart, then attach it again (or restart MT5) so new EX5 loads.
2. Keep inputs:
   - `UseChartSymbolForExecution = true`
   - `IgnoreNonRelayFuturesSymbol = true`
   - `RelayFuturesSymbol = /GCJ26:XCEC`
3. Attach EA to your broker futures chart (any broker-specific symbol name).
