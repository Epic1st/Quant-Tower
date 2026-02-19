# Step 18 - Live Quantower-Format Signal Test Executed

Date: 2026-02-19

## Action performed
- Posted a new relay signal with strict Quantower Gold fields:
  - `source=quantower`
  - `symbol=/GCJ26:XCEC`
  - `side=BUY`
  - `comment=manual_test`

## Relay result
- Relay accepted the signal as:
  - `id=2`

## MT5 log verification
- EA log confirms strict filter and chart-symbol execution worked:
  - `Signal received id=2 side=BUY src_symbol=/GCJ26:XCEC trade_symbol=GOLD.FUTURE ...`
- EA attempted order on attached chart symbol (`GOLD.FUTURE`).
- Broker returned `invalid stops` (`ret=10016`) for this test due SL/TP distance constraints.

## Conclusion
- Quantower-format gold signal path is working end-to-end:
  - Relay received -> MT5 EA accepted -> trade command sent.
- Remaining issue is broker stop-level constraints, not routing/filtering.
