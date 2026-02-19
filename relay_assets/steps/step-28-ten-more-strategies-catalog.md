# Step 28 - 10 Additional Quantower Strategy Ideas

Date: 2026-02-19

## Goal
Provide 10 production-ready strategy concepts that fit current relay -> MT5 pipeline and can be tracked by strategy_id/magic/comment.

## Shared payload format
All should send:
- `strategy_id`
- `side`
- `sl_ticks`
- `tp1_ticks`
- `tp2_ticks`
- `risk`
- `comment`

## S4 - Opening Range Breakout + Retest (ORB_RET)
- Entry: break 5m/15m opening range, then retest hold with positive delta in break direction.
- SL: behind retest swing + adaptive buffer.
- TP1/TP2: 1R, then 2R or next session level.
- Tag: `S4_ORB_RET_BUY/SELL`.

## S5 - Failed Auction Reversal (FA_REV)
- Entry: auction above VAH/ below VAL fails; price re-enters value with absorption.
- SL: outside failed auction extreme.
- TP1/TP2: POC first, opposite value edge second.
- Tag: `S5_FA_REV_BUY/SELL`.

## S6 - LVN Rejection Rotation (LVN_ROT)
- Entry: touch LVN, rejection footprint, return toward acceptance.
- SL: beyond LVN rejection tail.
- TP1/TP2: nearest HVN/POC then far HVN.
- Tag: `S6_LVN_ROT_BUY/SELL`.

## S7 - POC Reclaim Trend Continuation (POC_CONT)
- Entry: pullback into session POC in trend, imbalance confirms continuation.
- SL: below/above pullback swing.
- TP1/TP2: prior impulse high/low then extension.
- Tag: `S7_POC_CONT_BUY/SELL`.

## S8 - VWAP Band Mean Reversion (VWAP_MR)
- Entry: stretch to outer VWAP band with exhaustion + delta divergence.
- SL: beyond excursion extreme.
- TP1/TP2: VWAP core then opposite band.
- Tag: `S8_VWAP_MR_BUY/SELL`.

## S9 - VWAP Pullback Trend Follow (VWAP_TF)
- Entry: trend day, pullback to VWAP/inner band, order flow resumes trend.
- SL: below/above pullback pivot.
- TP1/TP2: prior trend extreme then measured extension.
- Tag: `S9_VWAP_TF_BUY/SELL`.

## S10 - Iceberg Absorption Breakout (ICE_BRK)
- Entry: repeated prints at level with hidden absorption, then breakout.
- SL: behind iceberg zone.
- TP1/TP2: 1R then next liquidity wall.
- Tag: `S10_ICE_BRK_BUY/SELL`.

## S11 - Liquidity Sweep + Reclaim (SWEEP_RECL)
- Entry: stop-run above prior high/below prior low, immediate reclaim with strong opposite delta.
- SL: sweep wick extreme + buffer.
- TP1/TP2: return to mid-range then opposite range edge.
- Tag: `S11_SWEEP_RECL_BUY/SELL`.

## S12 - Delta Divergence at Key Level (DELTA_DIV)
- Entry: price retests level with weaker aggressive volume (divergence) and reversal confirmation.
- SL: beyond level failure point.
- TP1/TP2: 1R then next profile node.
- Tag: `S12_DELTA_DIV_BUY/SELL`.

## S13 - Multi-Timeframe Trend Pullback (MTF_PULL)
- Entry: higher timeframe bias + lower timeframe pullback completion + tape confirmation.
- SL: structure invalidation on trigger timeframe.
- TP1/TP2: prior swing then trend projection.
- Tag: `S13_MTF_PULL_BUY/SELL`.

## Priority to implement first
1. S4 ORB Retest
2. S5 Failed Auction Reversal
3. S11 Sweep + Reclaim
4. S8 VWAP Mean Reversion

## Why these 4 first
- Strongly structured and testable.
- Need only bars + VWAP + DOM/tape proxies already available.
- Good diversity: breakout, reversal, reversion.
