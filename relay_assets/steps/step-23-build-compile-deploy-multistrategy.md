# Step 23 - Build, Compile, and Deployment

Date: 2026-02-19

## Quantower Build
Command:
```powershell
dotnet build D:\Trading-Quantower\relay_assets\quantower_algo\quantower_algo.csproj -c Release
```
Result:
- `0 errors, 0 warnings`
- Output: `D:\Trading-Quantower\relay_assets\quantower_algo\bin\Release\net8.0\quantower_algo.dll`

Deployed DLL:
- `C:\Quantower\TradingPlatform\v1.145.16\bin\Scripts\Strategies\MultiStrategySignalEngine.dll`

## MT5 Compile
Command:
```powershell
C:\Program Files\Flexy Markets MT5 Terminal\MetaEditor64.exe /compile:'D:\Trading-Quantower\relay_assets\MT5_RelayPoller_MultiStrategy.mq5' /log:'D:\Trading-Quantower\relay_assets\mt5_compile_multistrategy.log'
```
Result:
- `0 errors, 0 warnings`

Compiled artifact:
- `D:\Trading-Quantower\relay_assets\MT5_RelayPoller_MultiStrategy.ex5`

Deployed to MT5 Experts:
- `C:\Users\Administrator\AppData\Roaming\MetaQuotes\Terminal\F2329BDBA94B54F808C78A04BDB4D0B6\MQL5\Experts\MT5_RelayPoller_MultiStrategy.mq5`
- `C:\Users\Administrator\AppData\Roaming\MetaQuotes\Terminal\F2329BDBA94B54F808C78A04BDB4D0B6\MQL5\Experts\MT5_RelayPoller_MultiStrategy.ex5`
