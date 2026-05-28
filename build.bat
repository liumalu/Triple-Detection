@echo off
setlocal

set "PROJECT_DIR=%~dp0"
set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"

echo ============================================
echo Triple Detection - Build Script
echo ============================================
echo.

echo [1/3] Restoring NuGet packages...
"%DOTNET%" restore "%PROJECT_DIR%TripleDetection.sln"
if %ERRORLEVEL% neq 0 (
    echo Restore failed!
    pause
    exit /b 1
)

echo.
echo [2/3] Building Debug...
"%MSBUILD%" "%PROJECT_DIR%TripleDetection.sln" /p:Configuration=Debug /t:Rebuild /v:m
if %ERRORLEVEL% neq 0 (
    echo.
    echo BUILD FAILED!
    pause
    exit /b 1
)

echo.
echo [3/3] Copying libs to output...
xcopy /y /q "%PROJECT_DIR%TripleDetection.App\libs\*.dll" "%PROJECT_DIR%TripleDetection.App\bin\Debug\net48\" >nul 2>&1
if exist "%PROJECT_DIR%TripleDetection.App\bin\Debug\net48\VM.PlatformSDKCS.dll" (
    echo Libs copied.
) else (
    echo Warning: libs may not have been copied correctly.
)

echo.
echo Build completed successfully!
echo Output: %PROJECT_DIR%TripleDetection.App\bin\Debug\net48\TripleDetection.App.exe
pause
