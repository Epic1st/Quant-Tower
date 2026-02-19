# Step 24 - Multi-Strategy Test Procedure

Date: 2026-02-19

## Goal
Validate one manual signal each for S1/S2/S3 and confirm MT5 receives strategy_id, applies correct magic/comment, and executes.

## 1) MT5 setup
- Attach `MT5_RelayPoller_MultiStrategy` to your target chart.
- In MT5 WebRequest allow list add:
  - `http://127.0.0.1:8000`
- Enable Algo Trading.

## 2) Quantower setup
- Load strategy `MultiStrategySignalEngine` in Strategy Runner on symbol `/GCJ26:XCEC`.
- Keep `EnableAutoMode=false` for initial manual plumbing tests.

## 3) Manual trigger tests
- Trigger S1: set `ManualTriggerS1=true` (side BUY or SELL)
- Trigger S2: set `ManualTriggerS2=true`
- Trigger S3: set `ManualTriggerS3=true`

Expected payload fields include:
- `strategy_id`: S1/S2/S3
- `comment`: strategy-tagged comment
- `sl_ticks`, `tp1_ticks`, `tp2_ticks`

## 4) Verify relay state
PowerShell:
```powershell
Invoke-WebRequest 'http://127.0.0.1:8000/signal?id=0&auth=<RELAY_SECRET>' -UseBasicParsing
```
Check `strategy_id`, `comment`, and tick fields.

## 5) Verify MT5 logs
In Experts tab, expect lines like:
- `Signal received id=... strategy=S1 ...`
- `S1 placed BUY ... magic=260201 comment=S1_ABSORB_...`
- `TP1 partial close ...` (if price reaches TP1)

## 6) Move to auto mode
- Set `EnableAutoMode=true` in Quantower strategy.
- Keep cooldown and filters active; tune thresholds gradually.
