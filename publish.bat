@echo off
echo ========================================
echo    AlMuhasib - Build & Publish
echo ========================================
echo.

cd /d "%~dp0"

echo [1/2] Cleaning previous publish...
if exist "src\AlMuhasib.UI\bin\publish" rmdir /s /q "src\AlMuhasib.UI\bin\publish"

echo [2/2] Publishing single-file executable...
dotnet publish src\AlMuhasib.UI\AlMuhasib.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o "publish"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Publish failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo    Publish completed successfully!
echo    Output: publish\AlMuhasib.exe
echo ========================================
echo.
pause
