# Step 19 - Invalid Stops Mitigation Added

Date: 2026-02-19

## Problem
- Broker returned `ret=10016 invalid stops` for GOLD.FUTURE order.

## EA improvements
- Updated `D:\Trading-Quantower\relay_assets\MT5_RelayPoller.mq5`:
  - Added broker stop-distance adjustment using:
    - `SYMBOL_TRADE_STOPS_LEVEL`
    - `SYMBOL_TRADE_FREEZE_LEVEL`
  - SL/TP are auto-shifted to minimum valid distance before sending order.
  - Added input: `RetryWithoutStopsOnInvalidStops = true` (default).
  - If broker still rejects with invalid stops, EA retries market order with no SL/TP.

## Build + deploy
- Recompiled successfully (`0 errors, 0 warnings`).
- Updated `.mq5` and `.ex5` redeployed to MT5 Experts folder.

## User action required
- Re-attach EA on chart (or restart MT5) so running instance loads the new EX5.
