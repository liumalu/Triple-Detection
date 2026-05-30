@echo off
cd /d d:\xcm\Triple-Detection\TripleDetection.App
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" TripleDetection.App.csproj /t:Rebuild /p:Configuration=Debug /v:minimal
echo Exit code: %ERRORLEVEL%
