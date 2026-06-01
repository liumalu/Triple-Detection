@echo off
REM Triple-Detection Build and Run Script
REM Usage: Double-click this file or run from command line

setlocal

set "SCRIPT_DIR=%~dp0"
set "MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe""

echo ================================================
echo Triple-Detection Build and Run
echo ================================================
echo.

REM Step 1: Clean previous build output
echo [1/3] Cleaning previous build...
if exist "%SCRIPT_DIR%bin" (
    rmdir /s /q "%SCRIPT_DIR%bin"
)
if exist "%SCRIPT_DIR%obj" (
    rmdir /s /q "%SCRIPT_DIR%obj"
)
echo [OK] Clean completed
echo.

REM Step 2: Build the project
echo [2/3] Building project (Debug configuration)...
echo.
%MSBUILD% "%SCRIPT_DIR%TripleDetection.csproj" -t:Rebuild -p:Configuration=Debug -v:m
if errorlevel 1 (
    echo.
    echo [ERROR] Build failed! Please check the error messages above.
    pause
    exit /b 1
)
echo.
echo [OK] Build completed successfully
echo.

REM Step 3: Launch the application
set "APP_DIR=%SCRIPT_DIR%bin\Debug\net8.0-windows"
set "CONFIG_DIR=%APP_DIR%\Config"

echo [3/3] Launching application...
echo.

REM Ensure Config directory exists
if not exist "%CONFIG_DIR%" (
    mkdir "%CONFIG_DIR%"
    echo [INFO] Created Config directory
)

REM Launch the application
start "" "%APP_DIR%\TripleDetection.exe"

echo.
echo ================================================
echo Application started successfully!
echo ================================================
echo.
echo To debug, attach Visual Studio to the process:
echo   Debug -> Attach to Process -> Find TripleDetection.exe
echo.
echo Closing this window will NOT stop the application.
echo To stop: Task Manager -> End Task for TripleDetection.exe
echo.

endlocal