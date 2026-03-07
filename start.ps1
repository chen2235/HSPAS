# HSPAS Start Script
# Usage: .\start.ps1

$ErrorActionPreference = "Stop"
$projectDir = Join-Path $PSScriptRoot "HSPAS.Api"
$pidFile = Join-Path $PSScriptRoot "hspas.pid"

# Check if already running
if (Test-Path $pidFile) {
    $existingPid = Get-Content $pidFile
    $proc = Get-Process -Id $existingPid -ErrorAction SilentlyContinue
    if ($proc) {
        Write-Host "[HSPAS] Service already running (PID: $existingPid). Run .\stop.ps1 first." -ForegroundColor Yellow
        exit 0
    }
    Remove-Item $pidFile -Force
}

Write-Host "[HSPAS] Starting HSPAS service..." -ForegroundColor Cyan

# Start dotnet run in background
$process = Start-Process -FilePath "dotnet" `
    -ArgumentList "run", "--project", $projectDir, "--launch-profile", "http" `
    -PassThru -NoNewWindow

# Save PID
$process.Id | Out-File -FilePath $pidFile -Encoding UTF8 -NoNewline

Write-Host "[HSPAS] Service started (PID: $($process.Id))" -ForegroundColor Green
Write-Host "[HSPAS] Open browser: http://localhost:5117" -ForegroundColor Green
Write-Host "[HSPAS] Stop service: .\stop.ps1" -ForegroundColor Gray
