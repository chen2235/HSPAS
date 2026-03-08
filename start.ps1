# HSPAS Stock Trading System - Start Script
# Usage: .\start.ps1

# --- 1. Clear browser session & cookie cache ---
Write-Host "[HSPAS] Clearing session and cookie data..." -ForegroundColor Cyan

# Clear ASP.NET temp files (IIS Express / Kestrel session storage)
$tempAspNet = Join-Path $env:LOCALAPPDATA "Temp\Temporary ASP.NET Files"
if (Test-Path $tempAspNet) {
    Remove-Item "$tempAspNet\*" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  - ASP.NET temp files cleared." -ForegroundColor DarkGray
}

# Clear Data Protection keys (resets antiforgery / cookie encryption keys)
$dpKeys = Join-Path $env:LOCALAPPDATA "ASP.NET\DataProtection-Keys"
if (Test-Path $dpKeys) {
    Remove-Item "$dpKeys\*" -Force -ErrorAction SilentlyContinue
    Write-Host "  - Data Protection keys cleared." -ForegroundColor DarkGray
}

# Clear Chrome cookies & session for localhost (optional, only if Chrome is installed)
$chromeCookieDir = Join-Path $env:LOCALAPPDATA "Google\Chrome\User Data\Default"
if (Test-Path $chromeCookieDir) {
    # Only clear if Chrome is not running
    $chromeProc = Get-Process -Name "chrome" -ErrorAction SilentlyContinue
    if (-not $chromeProc) {
        $cookieFiles = @("Cookies", "Cookies-journal")
        foreach ($f in $cookieFiles) {
            $path = Join-Path $chromeCookieDir $f
            if (Test-Path $path) {
                Remove-Item $path -Force -ErrorAction SilentlyContinue
            }
        }
        Write-Host "  - Chrome cookies cleared." -ForegroundColor DarkGray
    } else {
        Write-Host "  - Chrome is running, skipping cookie cleanup." -ForegroundColor Yellow
    }
}

# Clear Edge cookies & session for localhost (optional)
$edgeCookieDir = Join-Path $env:LOCALAPPDATA "Microsoft\Edge\User Data\Default"
if (Test-Path $edgeCookieDir) {
    $edgeProc = Get-Process -Name "msedge" -ErrorAction SilentlyContinue
    if (-not $edgeProc) {
        $cookieFiles = @("Cookies", "Cookies-journal")
        foreach ($f in $cookieFiles) {
            $path = Join-Path $edgeCookieDir $f
            if (Test-Path $path) {
                Remove-Item $path -Force -ErrorAction SilentlyContinue
            }
        }
        Write-Host "  - Edge cookies cleared." -ForegroundColor DarkGray
    } else {
        Write-Host "  - Edge is running, skipping cookie cleanup." -ForegroundColor Yellow
    }
}

Write-Host "[HSPAS] Session/cookie cleanup done." -ForegroundColor Green
Write-Host ""

# --- 2. Stop existing HSPAS process if running ---
$pidFile = Join-Path $PSScriptRoot "hspas.pid"
if (Test-Path $pidFile) {
    $oldPid = (Get-Content $pidFile).Trim()
    $oldProc = Get-Process -Id $oldPid -ErrorAction SilentlyContinue
    if ($oldProc) {
        Write-Host "[HSPAS] Stopping previous instance (PID: $oldPid)..." -ForegroundColor Yellow
        Stop-Process -Id $oldPid -Force
        Start-Sleep -Seconds 2
    }
    Remove-Item $pidFile -Force -ErrorAction SilentlyContinue
}

# --- 3. Start the service ---
$projectDir = Join-Path $PSScriptRoot "HSPAS.Api"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  HSPAS Stock Trading System" -ForegroundColor Cyan
Write-Host "  URL: http://localhost:5117" -ForegroundColor Cyan
Write-Host "  Press Ctrl+C to stop." -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Start dotnet in a new window
$proc = Start-Process -FilePath "dotnet" `
    -ArgumentList "run", "--project", "`"$projectDir`"", "--launch-profile", "http" `
    -PassThru

$proc.Id | Out-File $pidFile -Encoding UTF8

Write-Host "[HSPAS] Service started in new window (PID: $($proc.Id))" -ForegroundColor Green
Write-Host "[HSPAS] Use .\stop.ps1 to stop the service." -ForegroundColor DarkGray
