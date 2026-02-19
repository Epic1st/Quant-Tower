# Step 02 - FastAPI Relay Implemented

Date: 2026-02-19

## What was done
- Implemented relay app at:
  - Runtime path: `C:\relay\relay.py`
  - Saved copy: `D:\Trading-Quantower\relay_assets\relay.py`

## Endpoints implemented
- `POST /signal`
  - Requires header: `X-Auth: <RELAY_SECRET>`
  - Stores latest payload in memory
  - Increments `id`
  - Sets UTC timestamp on server
- `GET /signal?id=<last_id>&auth=<RELAY_SECRET>`
  - Returns newest signal only when `id` advanced
- `GET /health`
  - Returns health status and UTC timestamp

## Security behavior
- Relay secret is read only from environment variable `RELAY_SECRET`.
- Unauthorized requests return HTTP `401`.
- Request docs UI is disabled.

## Result
- Relay API logic is implemented and running-ready.
