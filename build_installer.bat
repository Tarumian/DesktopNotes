@echo off
chcp 65001 >nul
echo 1. Publishing Hushak standalone...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish\standalone
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Dotnet publish failed.
    pause
    exit /b %ERRORLEVEL%
)

echo 2. Compiling Inno Setup installer...
"%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" Hushak.iss
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Inno Setup compilation failed.
    pause
    exit /b %ERRORLEVEL%
)

echo 3. Packaging tarumian.am web release...
powershell -ExecutionPolicy Bypass -File "%~dp0scripts\package_web_release.ps1"
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Web release packaging failed.
    pause
    exit /b %ERRORLEVEL%
)

echo 4. Updating and validating winget manifests...
powershell -ExecutionPolicy Bypass -File "%~dp0scripts\update_winget_manifest.ps1"
if %ERRORLEVEL% NEQ 0 (
    echo [WARNING] Winget manifest update or validation failed.
)

echo SUCCESS: Installer, tarumian.am package, and winget manifests generated!
pause
