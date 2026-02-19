# Step 17 - Ignored Signal With Empty Source Is Expected

Date: 2026-02-19

## Observed log
- `Signal id=1 ignored: source= (expected quantower)`

## Cause
- Relay still had an older test signal (`id=1`) created before strict filters.
- That payload did not contain `source:"quantower"` and/or did not use symbol `/GCJ26:XCEC`.
- EA correctly ignored it due to strict checks.

## Behavior
- EA sets `g_last_id=1` after ignoring, so it will not repeat this stale signal.
- Next valid Quantower Gold signal (`id=2+`) will be processed.

## Next action
- Trigger manual signal from Quantower strategy now.
- Expected in MT5 logs:
  - `Signal received id=... side=... src_symbol=/GCJ26:XCEC trade_symbol=GOLD.FUTURE`
  - followed by order success/failure result.
