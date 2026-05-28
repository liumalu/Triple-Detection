@echo off
setlocal

set "PROJECT_DIR=%~dp0"
set "CONFIG=Release"
set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"
set "BINDIR=%PROJECT_DIR%TripleDetection.App\bin\%CONFIG%\net48"
set "LIBDIR=%PROJECT_DIR%TripleDetection.App\libs"

echo ============================================
echo Triple Detection - Release Build
echo ============================================
echo.

echo [1/5] Restoring NuGet packages...
"%DOTNET%" restore "%PROJECT_DIR%TripleDetection.sln"
if %ERRORLEVEL% neq 0 (
    echo Restore failed!
    pause
    exit /b 1
)

echo.
echo [2/5] Building Release...
"%MSBUILD%" "%PROJECT_DIR%TripleDetection.sln" /p:Configuration=%CONFIG% /t:Rebuild /v:m
if %ERRORLEVEL% neq 0 (
    echo BUILD FAILED!
    pause
    exit /b 1
)

echo.
echo [3/5] Copying libs directory...
if exist "%LIBDIR%" (
    xcopy /E /I /Y "%LIBDIR%" "%BINDIR%\libs\" >nul 2>&1
    echo Copied libs to %BINDIR%\libs\
) else (
    echo Warning: libs directory not found at %LIBDIR%
)

echo.
echo [4/5] Copying additional DLLs...
xcopy /y /q "%PROJECT_DIR%TripleDetection.App\bin\%CONFIG%\net48\*.dll" "%BINDIR%\" >nul 2>&1
xcopy /y /q "%PROJECT_DIR%TripleDetection.App\bin\%CONFIG%\net48\*.config" "%BINDIR%\" >nul 2>&1
echo Additional files copied.

echo.
echo [5/5] Creating distribution zip...
powershell.exe -Command "Compress-Archive -Path '%BINDIR%\*' -DestinationPath '%PROJECT_DIR%TripleDetection-v1.0-Release.zip' -Force"

if exist "%PROJECT_DIR%TripleDetection-v1.0-Release.zip" (
    echo Done! Output: %PROJECT_DIR%TripleDetection-v1.0-Release.zip
) else (
    echo Warning: Zip file was not created.
)
pause
