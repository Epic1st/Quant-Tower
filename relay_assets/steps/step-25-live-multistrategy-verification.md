# Step 25 - Live Multi-Strategy Signal Verification (S1/S2/S3)

Date: 2026-02-19

## Action Performed
- Sent 3 live Quantower-format relay payloads to local relay endpoint:
  - `strategy_id=S1`, side `BUY`, `sl_ticks=140`, `tp1_ticks=140`, `tp2_ticks=280`, `risk=0.05`
  - `strategy_id=S2`, side `SELL`, `sl_ticks=160`, `tp1_ticks=160`, `tp2_ticks=320`, `risk=0.05`
  - `strategy_id=S3`, side `BUY`, `sl_ticks=180`, `tp1_ticks=180`, `tp2_ticks=360`, `risk=0.05`

## Relay IDs
- S1 -> `id=5`
- S2 -> `id=6`
- S3 -> `id=7`

## MT5 Experts Log Evidence
File:
- `C:\Users\Administrator\AppData\Roaming\MetaQuotes\Terminal\F2329BDBA94B54F808C78A04BDB4D0B6\MQL5\Logs\20260219.log`

Observed lines:
- `Signal received id=5 strategy=S1 ...`
- `S1 placed BUY id=5 ... lots=0.80 ... magic=260201 comment=S1_ABSORB_BUY_PIPE_TEST ...`
- `Signal received id=6 strategy=S2 ...`
- `S2 placed SELL id=6 ... lots=0.70 ... magic=260202 comment=S2_VACUUM_SELL_PIPE_TEST ...`
- `Signal received id=7 strategy=S3 ...`
- `S3 placed BUY id=7 ... lots=0.62 ... magic=260203 comment=S3_RETEST_BUY_PIPE_TEST ...`
- `TP1 partial close strategy=S2 signal=6 ... closed=0.35`

## Conclusion
- End-to-end relay -> MT5 execution path is working for all 3 strategies.
- Strategy tracking (strategy_id + magic + comment) is confirmed in logs.
- TP1 management logic is active (partial close observed).
