# Step 05 - MT5 + Quantower Integration Artifacts Saved

Date: 2026-02-19

## What was prepared
- MT5 EA polling template created:
  - `D:\Trading-Quantower\relay_assets\MT5_RelayPoller.mq5`
- Relay source copy saved:
  - `D:\Trading-Quantower\relay_assets\relay.py`

## Quantower local relay target
- POST URL: `http://127.0.0.1:8000/signal`
- Required header: `X-Auth: <RELAY_SECRET>`

## MT5 WebRequest setting required
- Add `http://127.0.0.1:8000` to:
  - MT5 -> Tools -> Options -> Expert Advisors -> Allow WebRequest
