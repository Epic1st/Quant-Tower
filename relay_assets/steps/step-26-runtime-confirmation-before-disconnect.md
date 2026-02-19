# Step 26 - Runtime Confirmation Before Disconnect

Date: 2026-02-19

## Runtime Checks
- Relay service `relay`: `RUNNING`
- MT5 process `terminal64`: running
- Quantower process: not detected at check time

## Execution Confirmation
From MT5 Experts log (`MQL5\\Logs\\20260219.log`):
- `id=5` S1 received and executed
- `id=6` S2 received and executed
- `id=7` S3 received and executed
- TP1 management event observed (partial close)

## Important
- Closing RDP window (disconnect) is fine.
- Do **NOT** shut down VPS.
- Do **NOT** log off session.
- For automatic Quantower-generated signals, Quantower strategy must be running continuously.
