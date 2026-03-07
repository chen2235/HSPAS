@echo off
chcp 65001 >nul
title HSPAS Stop

echo [HSPAS] Stopping all HSPAS services...
taskkill /IM dotnet.exe /F >nul 2>&1

if %errorlevel% equ 0 (
    echo [HSPAS] Service stopped.
) else (
    echo [HSPAS] No running service found.
)

pause
