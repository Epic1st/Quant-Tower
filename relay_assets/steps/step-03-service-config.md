# Step 03 - NSSM Service Configured

Date: 2026-02-19

## What was done
- Installed NSSM from winget (`NSSM.NSSM`).
- Created Windows service: `relay`.
- Service executable: `C:\relay\venv\Scripts\uvicorn.exe`.
- Service arguments: `relay:app --host 127.0.0.1 --port 8000 --no-access-log`.
- Working directory: `C:\relay`.
- Service env var set: `RELAY_SECRET=<RELAY_SECRET>`.
- Startup type set to automatic.

## Logging setup
- Stdout: `C:\relay\logs\relay.out.log`
- Stderr: `C:\relay\logs\relay.err.log`
- Log rotation enabled.
- Access logs disabled to prevent auth secret leakage from query strings.

## Config validation
- `sc qc relay` shows `START_TYPE : 2 AUTO_START`.
- `nssm get relay Application` points to `C:\relay\venv\Scripts\uvicorn.exe`.
- `nssm get relay AppDirectory` is `C:\relay`.

## Saved automation
- `D:\Trading-Quantower\relay_assets\setup_relay_service.ps1`
