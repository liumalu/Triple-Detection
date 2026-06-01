@echo off
REM Triple-Detection Start Script
REM Usage: Double-click this file or run from command line

setlocal

REM Get the directory where this script is located
set "SCRIPT_DIR=%~dp0"
set "APP_DIR=%SCRIPT_DIR%bin\Debug\net8.0-windows"

REM Check if the application exists
if not exist "%APP_DIR%\TripleDetection.exe" (
    echo [ERROR] Application not found: %APP_DIR%\TripleDetection.exe
    echo Please build the project first using:
    echo   msbuild TripleDetection.csproj -t:Rebuild -p:Configuration=Debug
    pause
    exit /b 1
)

REM Check if Config directory exists
if not exist "%APP_DIR%\Config" (
    echo [INFO] Config directory not found, creating...
    mkdir "%APP_DIR%\Config"
)

echo [INFO] Starting TripleDetection application...
echo [INFO] App directory: %APP_DIR%
echo [INFO] Config directory: %APP_DIR%\Config
echo.

REM Launch the application
start "" "%APP_DIR%\TripleDetection.exe"

echo [INFO] Application started. Closing this window will not stop the application.
echo To stop the application, use Task Manager.

endlocal