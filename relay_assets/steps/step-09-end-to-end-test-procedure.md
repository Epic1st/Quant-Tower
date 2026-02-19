# Step 09 - End-to-End Test Procedure (Manual Signal -> Relay -> MT5)

Date: 2026-02-19

## 1) Start prerequisites
- Ensure relay service is running:
```powershell
sc.exe query relay
```
- Expected: `STATE : 4 RUNNING`

- Optional relay health check:
```powershell
Invoke-WebRequest -UseBasicParsing http://127.0.0.1:8000/health
```

## 2) MT5 setup
- In MT5: `Tools -> Options -> Expert Advisors`
  - Enable `Allow algorithmic trading`
  - Add `http://127.0.0.1:8000` to `Allow WebRequest for listed URL`
- In Navigator -> Expert Advisors, attach `MT5_RelayPoller` to a chart.
- EA inputs (recommended for live test):
  - `RelayBaseUrl = http://127.0.0.1:8000`
  - `RelaySecret = <RELAY_SECRET>`
  - `RelayFuturesSymbol = /GCJ26:XCEC`
  - `DefaultTradeSymbol = XAUUSD` (or broker-specific symbol)
  - `DryRunOnly = false` (set `true` if you want logs only)

## 3) Quantower strategy setup
- Ensure file exists:
  - `C:\Quantower\TradingPlatform\v1.145.16\bin\Scripts\Strategies\RelaySignal_GoldAbsorption.dll`
- Restart Quantower (if it was open before deployment).
- Open `Strategy Runner` / `Strategy Manager`.
- Add strategy `RelaySignal_GoldAbsorption`.
- Set parameters:
  - `SymbolName = /GCJ26:XCEC`
  - `RelayUrl = http://127.0.0.1:8000/signal`
  - `RelaySecret = <RELAY_SECRET>`
  - `TradeSymbolForMT5 = XAUUSD`
  - `ManualTrigger = true`
  - `ManualSide = BUY` (or `SELL`)
  - Keep `EnableAutoMode = true` or set `false` for pure manual plumbing test
- Run/start strategy.

## 4) Verify relay updated
- Poll relay latest signal:
```powershell
$auth='<RELAY_SECRET>'
$encoded=[uri]::EscapeDataString($auth)
Invoke-RestMethod "http://127.0.0.1:8000/signal?id=0&auth=$encoded" | ConvertTo-Json -Depth 5
```
- Expected: `updated=true`, payload with side/comment from Quantower.

## 5) Verify MT5 received and acted
- In MT5 `Experts` tab:
  - Expect log line: `Signal received id=... side=...`
  - If `DryRunOnly=false`, expect either:
    - `Order success ...`
    - or `Order failed ... retcode ...` (still confirms plumbing)
- In `Trade` tab:
  - If success, a new market position/order appears on mapped symbol.

## 6) Troubleshooting
- Relay service:
```powershell
sc.exe query relay
Get-Content C:\relay\logs\relay.err.log -Tail 100
```
- Port bind check:
```powershell
netstat -ano | findstr :8000
```
- Auth errors (`401`):
  - Relay secret mismatch between Quantower strategy, MT5 EA, and service env var.
- MT5 WebRequest errors:
  - URL missing in MT5 allowed list.
- Symbol mapping issues:
  - Set EA `DefaultTradeSymbol` to your broker?s real gold symbol (e.g., `XAUUSD.a`, `GOLD`, etc.).
