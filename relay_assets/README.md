# Relay Setup Bundle

This folder contains all relay setup artifacts and step logs.

## Files
- `relay.py` - FastAPI relay app source.
- `setup_relay_service.ps1` - End-to-end setup script for Python venv, dependencies, and NSSM service.
- `verify_relay.ps1` - Health/auth/signal verification script.
- `MT5_RelayPoller.mq5` - MT5 relay poller with symbol mapping and trade execution.
- `MT5_RelayPoller.ex5` - Compiled MT5 relay poller.
- `MT5_RelayPoller_MultiStrategy.mq5` - Multi-strategy MT5 relay poller (S1/S2/S3).
- `MT5_RelayPoller_MultiStrategy.ex5` - Compiled multi-strategy MT5 relay poller.
- `RelaySignal_GoldAbsorption.cs` - Top-level Quantower strategy source deliverable.
- `MultiStrategySignalEngine.cs` - Top-level multi-strategy Quantower source deliverable.
- `quantower_algo/RelaySignal_GoldAbsorption.cs` - Quantower signal strategy source.
- `quantower_algo/MultiStrategySignalEngine.cs` - Quantower multi-strategy engine source.
- `quantower_algo/bin/Release/net8.0/quantower_algo.dll` - Compiled Quantower strategy assembly.

## Step logs
- `steps/step-01-python-and-packages.md`
- `steps/step-02-relay-api.md`
- `steps/step-03-service-config.md`
- `steps/step-04-verification.md`
- `steps/step-05-integration-assets.md`
- `steps/step-06-workspace-script-validation.md`
- `steps/step-07-completion-summary.md`
- `steps/step-08-quantower-mt5-integration-implementation.md`
- `steps/step-09-end-to-end-test-procedure.md`
- `steps/step-10-deliverables-complete.md`
- `steps/step-11-deliverable-filename-alignment.md`
- `steps/step-12-handoff-summary.md`
- `steps/step-13-step-07-file-verification.md`
- `steps/step-14-why-eurusd-traded.md`
- `steps/step-15-any-chart-futures-execution-update.md`
- `steps/step-16-strict-quantower-gold-only-filter.md`
- `steps/step-17-ignored-empty-source-expected.md`
- `steps/step-18-live-signal-test-executed.md`
- `steps/step-19-invalid-stops-mitigation.md`
- `steps/step-20-dynamic-stops-orderflow-live-validation.md`
- `steps/step-21-quantower-multistrategy-engine.md`
- `steps/step-22-mt5-multistrategy-poller.md`
- `steps/step-23-build-compile-deploy-multistrategy.md`
- `steps/step-24-multistrategy-test-procedure.md`
- `steps/step-25-live-multistrategy-verification.md`
- `steps/step-26-runtime-confirmation-before-disconnect.md`
- `steps/step-27-user-live-confirmation.md`
- `steps/step-28-ten-more-strategies-catalog.md`

## Run setup again
```powershell
cd D:\Trading-Quantower\relay_assets
.\setup_relay_service.ps1 -RelaySecret "<RELAY_SECRET>"
```

## Verify
```powershell
cd D:\Trading-Quantower\relay_assets
.\verify_relay.ps1 -RelaySecret "<RELAY_SECRET>"
```
