# Quantower Beginner's Guide — Complete Step-by-Step

> **You need:** Quantower installed, MT5 open, relay service running (already set up for you).

---

## PART 1: Open Quantower & Find Your Strategy

### Step 1 — Launch Quantower
- Double-click the **Quantower** shortcut on your desktop (or find it in Start Menu).
- Wait for it to fully load (may take 30-60 seconds).

### Step 2 — Open the Strategy Runner
- Look at the **top menu bar** in Quantower.
- Click **"Algo"** (or it may say **"Strategy Runner"**).
- If you don't see it:
  - Click the **hamburger menu** (☰) at the top-left.
  - Look for **"Strategy Runner"** or **"Strategy Manager"** in the dropdown.
  - Click it to open the panel.

### Step 3 — Find Your Strategy
- In the Strategy Runner panel, you should see a list of strategies.
- Look for **"MultiStrategySignalEngine"** — this is your main strategy.
- It should show status: **Running** (green) or **Working**.

> **If you don't see it listed:** 
> 1. Click the **"+"** button or **"Add Strategy"**.
> 2. In the dropdown/search box, look for **"MultiStrategySignalEngine"**.
> 3. Select it.

### Step 4 — Check the Status
- If the strategy says **Stopped** or **Not Running**:
  - Right-click on it → click **"Start"** or **"Run"**.
  - Wait 5-10 seconds.
  - It should change to **Running** / **Working**.

---

## PART 2: View & Verify Strategy Settings

### Step 5 — Open Strategy Parameters
- Click on **"MultiStrategySignalEngine"** in the list.
- Look for a **"Settings"** or **"Parameters"** button/tab.
  - Some versions: double-click the strategy name.
  - Some versions: click the **gear icon** (⚙️) next to it.
  - Some versions: right-click → **"Properties"** or **"Settings"**.
- A panel will open showing all configuration parameters.

### Step 6 — Verify These Critical Settings

Check that these values are correct (scroll through the parameter list):

| Parameter | Expected Value | What It Does |
|-----------|---------------|--------------|
| **SymbolName** | `/GCJ26:XCEC` | The gold futures contract to watch |
| **RelayUrl** | `http://127.0.0.1:8000/signal` | Where signals are sent |
| **RelaySecret** | *(your secret — should NOT be empty)* | Password for the relay |
| **TradeSymbolForMT5** | `XAUUSD` | What MT5 trades |
| **EnableAutoMode** | `true` | Allows automatic signal detection |
| **EnableS1** | `true` | Strategy 1 (absorption reversal) is on |

> **If RelaySecret is empty:** You need to enter the same secret that your relay service uses. Check with the person who set it up, or look in the NSSM service configuration.

---

## PART 3: Send a Test Signal (Manual Trigger)

This is how you force-send one test signal to verify the whole pipeline works.

### Step 7 — Set Manual Trigger Parameters
In the strategy parameters panel, scroll down and find these fields. Change them to:

| Parameter | Set To | 
|-----------|--------|
| **ManualTriggerAny** | `true` |
| **ManualStrategyId** | `S1` |
| **ManualSideAny** | `BUY` |

### Step 8 — Apply the Settings
- Click **"Apply"** or **"Update"** or **"Save"** (button at the bottom of the parameters panel).
- **What happens immediately:**
  - Quantower sends ONE test signal to the relay.
  - `ManualTriggerAny` automatically switches back to `false`.
  - You should see a log message in Quantower's output.

### Step 9 — Check Quantower Log Output
- Look for a **"Log"** or **"Output"** tab in Quantower (usually at the bottom).
- You should see something like:
  ```
  Signal sent strategy=S1 id=15 side=BUY setup=manual_test ...
  ```
- If you see this, **Quantower successfully sent the signal!** ✅

---

## PART 4: Confirm MT5 Received It

### Step 10 — Switch to MT5
- Open your **MetaTrader 5** window.

### Step 11 — Open the Experts Tab
- At the bottom of MT5, you'll see tabs: **Trade**, **History**, **News**, etc.
- Click the **"Experts"** tab (or **"Journal"** tab).
- This shows all EA (Expert Advisor) messages.

### Step 12 — Look for These Messages
You should see recent entries like:
```
Signal received id=15 strategy=S1 side=BUY ...
```
and:
```
Executed BUY 0.01 XAUUSD ...
```

**If you see both lines → The entire pipeline works!** 🎉

---

## PART 5: Troubleshooting

### ❌ Problem: Strategy is not listed in Quantower
**Fix:** The DLL might not be deployed.
1. Open File Explorer.
2. Go to: `C:\Quantower\TradingPlatform\v1.145.16\bin\Scripts\Strategies\`
3. You should see `MultiStrategySignalEngine.dll`.
4. If missing, copy it from: `D:\Trading-Quantower\relay_assets\quantower_algo\bin\Release\net8.0\quantower_algo.dll`
5. Restart Quantower.

### ❌ Problem: Strategy says "Error" or won't start
**Fix:** The symbol might not be available.
1. Make sure you're connected to your data feed in Quantower.
2. Check that `/GCJ26:XCEC` is a valid symbol on your connection.
3. If the contract has rolled (expired), you need to update `SymbolName` to the new contract (e.g., `/GCM26:XCEC` for June).

### ❌ Problem: Signal sent but MT5 shows nothing
**Fix:** Check MT5 EA is running and relay URL is allowed.
1. In MT5: check the chart has the EA attached (smiley face icon in top-right of chart).
2. Go to MT5 → **Tools** → **Options** → **Expert Advisors** tab.
3. Make sure **"Allow WebRequest for listed URL"** is checked.
4. Add `http://127.0.0.1:8000` to the allowed URL list if not already there.
5. Click OK and restart the EA.

### ❌ Problem: "WebRequest failed" in MT5 Experts tab
**Fix:** The relay service isn't running.
1. Open PowerShell (search "PowerShell" in Start Menu).
2. Type: `curl.exe http://127.0.0.1:8000/health` and press Enter.
3. If it says "Connection refused" → the relay is down.
4. To restart it: open PowerShell as Administrator and run:
   ```
   nssm restart QuantowerRelay
   ```

### ❌ Problem: "Cooldown active" when triggering manually
**Fix:** Wait 60 seconds and try again, or use a different strategy ID:
- Change `ManualStrategyId` to `S2` instead of `S1`.
- Set `ManualTriggerAny = true` again and Apply.

---

## Quick Reference Card

| Task | Where | How |
|------|-------|-----|
| Open Strategy Runner | Quantower top menu | Click **Algo** or **Strategy Runner** |
| Start strategy | Strategy Runner | Right-click → **Start** |
| Change settings | Strategy Runner | Click strategy → **Settings/Parameters** |
| Send test signal | Parameters panel | Set `ManualTriggerAny=true` → Apply |
| See signal log | Quantower | **Log/Output** tab at bottom |
| See MT5 trades | MT5 | **Experts** tab at bottom |
| Check relay | PowerShell | `curl.exe http://127.0.0.1:8000/health` |

---

## How It All Works (Simple Version)

```
YOU set ManualTrigger=true in Quantower
         ↓
Quantower sends signal to relay (localhost:8000)
         ↓
MT5 EA polls relay every 1 second
         ↓
MT5 sees new signal → places trade on XAUUSD
```

In **auto mode** (normal operation), you don't need to do anything — Quantower watches the gold market and automatically detects trade setups. When it finds one, it sends a signal to MT5 through the relay, and MT5 executes the trade automatically.
