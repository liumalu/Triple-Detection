# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is the Triple-Detection repository (Apache 2.0 licensed). A visual inspection system built with WPF + VisionMaster SDK.

## Architecture

- **TripleDetection.App** - WPF presentation layer (.NET Framework 4.8)
- **TripleDetection.Services** - Business logic layer
- **TripleDetection.Data** - Data access layer (Repository pattern, in-memory for dev, EF Core + SQLite for prod)

## Language Version Constraint

**C# 8.0 is required** for this project. All csproj files must specify:
```xml
<LangVersion>8.0</LangVersion>
```

Do NOT use C# 9.0+ features (e.g., switch expressions, record types, init setters). The project targets .NET Framework 4.8 which ships with the C# 7.3 compiler by default, so LangVersion must be explicitly set to 8.0 in every project file.

## Tech Stack

- WPF (.NET Framework 4.8)
- VisionMaster SDK (VM.PlatformSDKCS, VM.Core, VMControls.Winform.Release)
- InMemoryRepository (dev) → EF Core + SQLite (prod, planned)
- MVVM-like pattern with ViewModels

## Key Files

- `TripleDetection.App/MainWindow.xaml` - Main shell with navigation
- `TripleDetection.App/Views/DetectionView.xaml` - Detection workflow UI
- `TripleDetection.App/Views/DashboardView.xaml` - Dashboard with statistics
- `TripleDetection.App/Services/VmIntegrationService.cs` - VisionMaster SDK wrapper
- `TripleDetection.Data/Repositories/Repository.cs` - Repository implementations
- `TripleDetection.Data/Entities/Entities.cs` - Domain entities

## Build Commands

Use MSBuild from Visual Studio 2022:
```
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" TripleDetection.App.csproj /t:Rebuild /p:Configuration=Debug
```

## VisionMaster SDK

- Installed at: `C:\Program Files\VisionMaster4.2.0`
- SDK DLLs: `C:\Program Files\VisionMaster4.2.0\Development\V4.x\Libraries\win64\C#`
- Default solution file: `D:\xcm\ApplicationDemo\OCRDemoCs\OCRDemoChinese.sol`