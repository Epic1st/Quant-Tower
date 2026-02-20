# Step 30 - Signal Flow Health Check (User Report: "signals not coming")

Date: 2026-02-19
Workspace: `D:\Trading-Quantower`

## What was checked

1. Relay service status
- Service `relay` was `Running` (`Automatic`).
- `GET http://127.0.0.1:8000/health` returned HTTP 200.

2. Runtime processes
- MT5 terminal process was running.
- Relay (`uvicorn`) process was running.
- Quantower starter processes were running.

3. MT5 Expert log state
- Initial symptom: no new relay-poller processing lines after ~09:58.
- This indicated the EA timer loop was not active for current chart session.

## Actions taken

1. Sent controlled live test signal via relay
- Posted `id=27` (S1 BUY)
- MT5 consumed and executed successfully:
  - `Signal received id=27 ...`
  - `S1 placed BUY id=27 ... ret=10009`

2. Performed clean MT5 restart
- Restarted terminal process to clear stalled chart/EA state.

3. Re-validated after restart
- Posted `id=28` (S2 SELL)
- MT5 reloaded `MT5_RelayPoller_MultiStrategy` and consumed signal:
  - `Relay poller started ... (GOLD.FUTURE,H1)`
  - `Signal received id=28 ...`
  - `S2 placed SELL id=28 ... ret=10009`

## Conclusion

The pipeline is currently healthy again:
- Relay receives signals.
- MT5 poller receives signals.
- MT5 executes orders with expected strategy/magic/comment.

Recent verified IDs:
- `27` -> executed
- `28` -> executed

## Note

This validation confirms transport and execution path.
It does not prove that Quantower auto logic is firing continuously; it proves that when a valid signal is posted, MT5 executes it correctly.
