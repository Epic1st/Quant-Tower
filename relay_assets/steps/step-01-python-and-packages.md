# Step 01 - Python + Dependencies Installed

Date: 2026-02-19

## What was done
- Confirmed Python is available: `Python 3.12.10`.
- Created relay folder: `C:\relay`.
- Created venv: `C:\relay\venv`.
- Installed packages into venv:
  - `fastapi`
  - `uvicorn`

## Commands used
```powershell
New-Item -ItemType Directory -Path C:\relay -Force
py -3.12 -m venv C:\relay\venv
C:\relay\venv\Scripts\python.exe -m pip install --upgrade pip
C:\relay\venv\Scripts\python.exe -m pip install fastapi uvicorn
```

## Result
- Relay runtime prerequisites are installed and ready.
