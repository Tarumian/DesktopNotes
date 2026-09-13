@echo off
chcp 65001 >nul
echo 1. Publishing DesktopNotes standalone...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish\standalone
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Dotnet publish failed.
    pause
    exit /b %ERRORLEVEL%
)

echo 2. Compiling Inno Setup installer...
"%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" DesktopNotes.iss
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Inno Setup compilation failed.
    pause
    exit /b %ERRORLEVEL%
)

echo SUCCESS: publish\installer\DesktopNotes_Setup_v1.0.exe created!
pause
