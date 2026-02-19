# Step 04 - Runtime Verification Completed

Date: 2026-02-19

## What was verified
- Service status: `RUNNING`.
- Health endpoint:
  - `GET http://127.0.0.1:8000/health`
  - Returned HTTP `200`.
- Signal write/read:
  - `POST /signal` with `X-Auth` succeeded.
  - `GET /signal?id=0&auth=...` returned `updated=true` with payload.
- Unauthorized request:
  - Wrong auth returned HTTP `401`.
- Local bind check:
  - `netstat -ano | findstr :8000` shows listener on `127.0.0.1:8000` only.
- Service restart check:
  - Restarted service and re-checked health (`200`).

## Saved automation
- `D:\Trading-Quantower\relay_assets\verify_relay.ps1`
