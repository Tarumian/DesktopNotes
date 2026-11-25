@echo off
setlocal
cd /d "%~dp0"

echo Building MSIX Package for Hushak...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_msix.ps1" %*

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] MSIX packaging failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [SUCCESS] MSIX package is located in bin\Package\
pause
