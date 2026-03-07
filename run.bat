@echo off
chcp 65001 >nul
title HSPAS Stock Trading System

set "PROJECT_DIR=%~dp0HSPAS.Api"

echo ============================================
echo   HSPAS Stock Trading System
echo   URL: http://localhost:5117
echo   Press Ctrl+C to stop.
echo ============================================
echo.

cmd /k dotnet run --project "%PROJECT_DIR%" --launch-profile http
