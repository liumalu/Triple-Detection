@echo off
setlocal

set "PROJECT_DIR=%~dp0"
set "CONFIG=Release"
set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
set "BINDIR=%PROJECT_DIR%TripleDetection.App\bin\%CONFIG%"
set "LIBDIR=%PROJECT_DIR%TripleDetection.App\libs"

echo ============================================
echo Triple Detection - Release Build
echo ============================================
echo.

echo [1/3] Building Release...
powershell.exe -Command "& '%MSBUILD%' '%PROJECT_DIR%TripleDetection.App\TripleDetection.App.csproj' /p:Configuration=%CONFIG% /t:Rebuild /v:m"
if %ERRORLEVEL% neq 0 (
    echo BUILD FAILED!
    pause
    exit /b 1
)

echo.
echo [2/3] Copying libs...
xcopy /y /q "%LIBDIR%\*.dll" "%BINDIR%\" >nul 2>&1

echo.
echo [3/3] Creating distribution zip...
powershell.exe -Command "Compress-Archive -Path '%BINDIR%\*' -DestinationPath '%PROJECT_DIR%TripleDetection-v1.0-Release.zip' -Force"

echo Done! Output: %PROJECT_DIR%TripleDetection-v1.0-Release.zip
pause