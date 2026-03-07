# HSPAS Stop Script
# Usage: .\stop.ps1

$pidFile = Join-Path $PSScriptRoot "hspas.pid"

if (-not (Test-Path $pidFile)) {
    Write-Host "[HSPAS] PID file not found. Service may not be running." -ForegroundColor Yellow
    exit 0
}

$procId = (Get-Content $pidFile).Trim()
$proc = Get-Process -Id $procId -ErrorAction SilentlyContinue

if ($proc) {
    Write-Host "[HSPAS] Stopping service (PID: $procId)..." -ForegroundColor Cyan
    Stop-Process -Id $procId -Force
    $proc | Wait-Process -Timeout 10 -ErrorAction SilentlyContinue
    Write-Host "[HSPAS] Service stopped." -ForegroundColor Green
} else {
    Write-Host "[HSPAS] Process (PID: $procId) no longer exists." -ForegroundColor Yellow
}

Remove-Item $pidFile -Force -ErrorAction SilentlyContinue
