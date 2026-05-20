@echo off
setlocal

set "PROJECT_DIR=%~dp0"
set "CONFIG=Debug"
set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
set "BINDIR=%PROJECT_DIR%TripleDetection.App\bin\%CONFIG%"
set "LIBDIR=%PROJECT_DIR%TripleDetection.App\libs"

echo ============================================
echo Triple Detection - Build and Launch
echo ============================================
echo.

echo [1/3] Building project...
echo.

powershell.exe -Command "& '%MSBUILD%' '%PROJECT_DIR%TripleDetection.App\TripleDetection.App.csproj' /p:Configuration=%CONFIG% /t:Rebuild /v:m"
if %ERRORLEVEL% neq 0 (
    echo.
    echo BUILD FAILED!
    pause
    exit /b 1
)

echo.
echo [2/3] Copying libs...
echo.

xcopy /y /q "%LIBDIR%\*.dll" "%BINDIR%\" >nul 2>&1

echo.
echo [3/3] Launching application...
echo.

start "" "%BINDIR%\TripleDetection.App.exe"

echo Done!