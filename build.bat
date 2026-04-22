@echo off
cd /d "%~dp0"
echo ============================================
echo   Diktat-Tool.exe bauen (C# / .NET 8)
echo ============================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo FEHLER: .NET SDK nicht gefunden!
    echo Bitte installieren: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo .NET Version:
dotnet --version
echo.

echo [1/2] Pakete laden und bauen...
dotnet publish -c Release -r win-x64 --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true ^
  -o dist

if errorlevel 1 (
    echo.
    echo FEHLER beim Bauen!
    pause
    exit /b 1
)

echo.
echo [2/2] Ergebnis:
if exist "dist\Diktat-Tool.exe" (
    for %%I in ("dist\Diktat-Tool.exe") do echo    Diktat-Tool.exe — %%~zI Bytes
    echo.
    echo ============================================
    echo   FERTIG!
    echo.
    echo   dist\Diktat-Tool.exe ist bereit.
    echo   Einfach weitergeben — kein .NET noetig!
    echo   Jeder gibt seinen API Key beim ersten
    echo   Start selbst ein.
    echo ============================================
) else (
    echo FEHLER: exe nicht gefunden
)

pause
