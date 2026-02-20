# Step 35 - Quantower Manual Trigger Guide (Simple)

## Goal
Send 1 test signal from Quantower to MT5 and confirm MT5 receives it.

## Before You Start
- Keep both apps open:
  - Quantower
  - MT5
- Make sure relay service is running (already done in your setup).
- Internet is not needed for relay path because it is localhost.

## Part A - Manual Trigger from Quantower (easy steps)

1. Open **Quantower**.
2. Go to **Strategy Runner**.
3. Select strategy: **MultiStrategySignalEngine**.
4. Make sure status is **Running** (not Stopped).
5. Open strategy **Parameters**.
6. Set these values exactly:
   - `SymbolName` = `/GCJ26:XCEC`
   - `RelayUrl` = `http://127.0.0.1:8000/signal`
   - `RelaySecret` = your relay secret
   - `EnableS1` = `true`
   - `ManualStrategyId` = `S1`
   - `ManualSideAny` = `BUY`
   - `ManualCommentAny` = `manual_test`
   - `ManualTriggerAny` = `true`
7. Click **Apply / Update**.

Expected behavior:
- It sends exactly one signal.
- `ManualTriggerAny` should go back to `false` automatically.

## Part B - Check in MT5 (must do)

1. Open **MT5**.
2. Go to **Toolbox -> Experts** tab.
3. Look for lines like:
   - `Signal received id=... strategy=S1 ...`
   - `S1 placed BUY id=...`

If you see both lines, test is successful.

## Part C - Optional Relay Check (simple)

Open PowerShell and run:

```powershell
Invoke-WebRequest http://127.0.0.1:8000/health
```

You should get JSON response with `ok=true`.

## Part D - If It Does Not Work

### 1) No signal in MT5 Experts tab
- Check strategy status in Quantower: must be **Running**.
- Check `RelaySecret` in Quantower: must be exact.
- Check symbol is `/GCJ26:XCEC`.

### 2) It says strategy disabled
- Set `EnableS1=true`.

### 3) It says cooldown active
- Wait about 60 seconds, then trigger again.
- Or test another strategy id (example `S2`).

### 4) WebRequest issue in MT5
- In MT5 go to `Tools -> Options -> Expert Advisors`.
- Make sure this URL is allowed:
  - `http://127.0.0.1:8000`

## Quick Repeat Test
To test again quickly:
- Change comment value to a new text (example: `manual_test_2`)
- Set `ManualTriggerAny=true`
- Click **Apply**
- Re-check MT5 Experts tab

---
This is the safest manual test flow for your current setup.
