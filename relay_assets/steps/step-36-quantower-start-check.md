# Step 36 - Quantower Start Check (2026-02-19)

## What was checked
- Quantower process status
- Strategy service state in Quantower logs
- MultiStrategy strategy-specific log file
- Relay latest signal id activity
- MT5 relay receiver recent activity

## Result
- Quantower is running.
- `MultiStrategySignalEngine` is started and state is `Working`.

Evidence from Quantower Serilog:
- `2026-02-19T19:48:31Z` -> `'MultiStrategySignalEngine' was added.`
- `2026-02-19T19:48:46Z` -> `'MultiStrategySignalEngine' state changed to: 'Working'`.

Evidence from strategy log:
- `Started MultiStrategySignalEngine. symbol=/GCJ26:XCEC auto=True`
- `Strategy started...`

## Important observation
- No new `Signal sent ...` or `POST failed ...` entries yet after start.
- Relay `latest_id` remained unchanged during observation window.
- This means strategy is running, but it has not emitted a new signal yet.

## What to do now (manual proof)
1. In Quantower strategy parameters set:
   - `ManualTriggerAny = true`
   - `ManualStrategyId = S1`
   - `ManualSideAny = BUY`
2. Click Apply.
3. Check MT5 Experts tab for:
   - `Signal received id=... strategy=S1 ...`
   - `S1 placed BUY id=...`

This is the fastest way to confirm live end-to-end after strategy start.
