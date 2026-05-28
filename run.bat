@echo off
setlocal

set "PROJECT_DIR=%~dp0"
set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"
set "BINDIR=%PROJECT_DIR%TripleDetection.App\bin\Debug\net48"
set "LIBDIR=%PROJECT_DIR%TripleDetection.App\libs"

echo ============================================
echo Triple Detection - Build and Launch
echo ============================================
echo.

echo [1/4] Restoring NuGet packages...
"%DOTNET%" restore "%PROJECT_DIR%TripleDetection.sln"
if %ERRORLEVEL% neq 0 (
    echo Restore failed!
    pause
    exit /b 1
)

echo.
echo [2/4] Building Debug...
"%MSBUILD%" "%PROJECT_DIR%TripleDetection.sln" /p:Configuration=Debug /t:Rebuild /v:m
if %ERRORLEVEL% neq 0 (
    echo.
    echo BUILD FAILED!
    pause
    exit /b 1
)

echo.
echo [3/4] Copying libs to output...
xcopy /y /q "%LIBDIR%\*.dll" "%BINDIR%\" >nul 2>&1
echo Libs copied.

echo.
echo [4/4] Launching application...
start "" "%BINDIR%\TripleDetection.App.exe"

echo Done! Application should be starting...
