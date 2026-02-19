# Step 22 - MT5 Multi-Strategy Poller Implemented

Date: 2026-02-19

## File Added
- `D:\Trading-Quantower\relay_assets\MT5_RelayPoller_MultiStrategy.mq5`

## Implemented Inputs
- Strategy-specific controls:
  - `EnableS1/RiskS1/MagicS1/CommentS1`
  - `EnableS2/RiskS2/MagicS2/CommentS2`
  - `EnableS3/RiskS3/MagicS3/CommentS3`
- Global controls:
  - `TradeSymbol`, `MaxSpreadFilter`, `MaxSlippage`
  - `UseTP1TP2`, `PartialClosePercentAtTP1`, `MoveStopToBEAfterTP1`
  - `CooldownSeconds`

## Execution Logic
- Reads `strategy_id` from relay payload
- Applies strict source/symbol filters (`quantower` + `/GCJ26:XCEC`)
- Per-strategy enable check and cooldown debounce
- Risk lot sizing from risk% + SL ticks:
  - `lots = (Equity * Risk%) / (slTicks * tickValue)`
- Uses per-strategy magic/comment on order placement
- Comment is normalized for tracking (strategy + side + setup)

## TP1/TP2 Management
- Order sends with TP2 as final TP
- Tracks TP1 virtually and on hit:
  - partial close at configured percent
  - optional SL move to BE + 1 tick

## Safety
- Broker stop-level adjustment
- Invalid stops retry without SL/TP + post-fill stop set
- Spread filter before send
