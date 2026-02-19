param(
    [Parameter(Mandatory = $true)]
    [string]$RelaySecret,

    [string]$RelayRoot = 'C:\relay',
    [string]$ServiceName = 'relay',
    [int]$Port = 8000
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RelaySecret)) {
    throw 'RelaySecret cannot be empty.'
}

$relayPySource = Join-Path $PSScriptRoot 'relay.py'
$relayPyTarget = Join-Path $RelayRoot 'relay.py'
$venvPath = Join-Path $RelayRoot 'venv'
$venvPython = Join-Path $venvPath 'Scripts\python.exe'
$venvUvicorn = Join-Path $venvPath 'Scripts\uvicorn.exe'
$logDir = Join-Path $RelayRoot 'logs'

New-Item -ItemType Directory -Path $RelayRoot -Force | Out-Null
New-Item -ItemType Directory -Path $logDir -Force | Out-Null

$pythonSelector = '-3'
$pyList = & py -0p 2>$null
if ($LASTEXITCODE -eq 0) {
    if ($pyList -match '\s-3\.11') {
        $pythonSelector = '-3.11'
    } elseif ($pyList -match '\s-3\.12') {
        $pythonSelector = '-3.12'
    }
}

if (-not (Test-Path $venvPython)) {
    & py $pythonSelector -m venv $venvPath
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to create virtual environment.'
    }
}

& $venvPython -m pip install --upgrade pip
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to upgrade pip.'
}

& $venvPython -m pip install fastapi uvicorn
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to install FastAPI/Uvicorn.'
}

if (Test-Path $relayPySource) {
    Copy-Item -Path $relayPySource -Destination $relayPyTarget -Force
}

if (-not (Test-Path $relayPyTarget)) {
    throw "relay.py not found at $relayPyTarget"
}

if (-not (Get-Command nssm -ErrorAction SilentlyContinue)) {
    & winget install -e --id NSSM.NSSM --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to install NSSM via winget.'
    }
}

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    & nssm install $ServiceName $venvUvicorn "relay:app --host 127.0.0.1 --port $Port --no-access-log" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to install relay service.'
    }
}

& nssm set $ServiceName Application $venvUvicorn | Out-Null
& nssm set $ServiceName AppParameters "relay:app --host 127.0.0.1 --port $Port --no-access-log" | Out-Null
& nssm set $ServiceName AppDirectory $RelayRoot | Out-Null
& nssm set $ServiceName AppEnvironmentExtra "RELAY_SECRET=$RelaySecret" | Out-Null
& nssm set $ServiceName AppStdout (Join-Path $logDir 'relay.out.log') | Out-Null
& nssm set $ServiceName AppStderr (Join-Path $logDir 'relay.err.log') | Out-Null
& nssm set $ServiceName AppRotateFiles 1 | Out-Null
& nssm set $ServiceName AppRotateOnline 1 | Out-Null
& nssm set $ServiceName AppRotateBytes 10485760 | Out-Null
& nssm set $ServiceName Start SERVICE_AUTO_START | Out-Null

try {
    & nssm stop $ServiceName | Out-Null
} catch {
}

Start-Sleep -Milliseconds 500
& nssm start $ServiceName | Out-Null
Start-Sleep -Seconds 2

$svc = Get-Service -Name $ServiceName -ErrorAction Stop
if ($svc.Status -ne 'Running') {
    throw "Service '$ServiceName' is not running."
}

Write-Output "Service '$ServiceName' is running with AUTO_START on 127.0.0.1:$Port."
