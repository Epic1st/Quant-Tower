# Step 14 - Why EURUSD Traded Instead of Gold

Date: 2026-02-19

## What happened
- MT5 log shows incoming signal payload had:
  - `src_symbol=EURUSD`
  - `trade_symbol=EURUSD`
- EA executed the symbol from payload mapping logic.
- Chart symbol (`GOLD.FUTURE`) does not force execution symbol in this EA.

## Why
- Relay had/received a EURUSD signal (likely from earlier manual relay test payload).
- EA started from `id=0`, so it picked up the first available signal in relay memory.

## Fix now
1. In EA inputs, set:
   - `RelayFuturesSymbol=/GCJ26:XCEC`
   - `DefaultTradeSymbol=XAUUSD` (or your broker gold symbol)
2. In Quantower strategy inputs, set:
   - `SymbolName=/GCJ26:XCEC`
   - `TradeSymbolForMT5=XAUUSD`
3. Send a new manual signal from Quantower (or POST) with gold payload fields.
4. Verify MT5 log shows source symbol `/GCJ26:XCEC` and trade symbol `XAUUSD`.

## Safety recommendation
- Add strict symbol filter in EA so non-gold payloads are ignored.
