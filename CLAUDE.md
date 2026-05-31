# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Triple-Detection is an Apache 2.0 licensed visual inspection system built with WPF + VisionMaster SDK, organized under a **DDD four-layer architecture** within a single .NET 8 project.

## Architecture

```
TripleDetection/
├── Domain/                    # 领域层 — Entities, Enums, Repository interfaces
│   ├── Entities/             ← BaseEntity, User, Product, ProdTask, DetectionRecord, AuditLog, SystemConfig
│   ├── Entities/Queries/     ← PagedQuery, IPagedResult<T>, *Query (Product/Task/User/AuditLog/DetectionRecord)
│   ├── Enums/                ← ValidType, ProductStatus, TaskStatus
│   └── Repositories/         ← IRepository<T>, IUnitOfWork, IRepositoryFactory, IDbConnectionFactory
├── Application/               # 应用层 — Business services
│   ├── Services/             ← UserService, ProductService, TaskService, AuditLogService, DetectionRecordService, PasswordHashService, LoggingService
│   ├── VmServices/           ← VmIntegrationService, ImageStorageService, SettingsSyncService
│   └── SettingsServices/     ← VmSettingsService, CommunicationSettingsService, SystemSettingsService, DeviceControlSettingsService
├── Infrastructure/            # 基础设施层 — Data access (implements Domain repository interfaces)
│   ├── Persistence/          ← TripleDetectionDbContext, SqliteUnitOfWork, DatabaseInitializer, Connection factories
│   ├── Persistence/Configurations/  ← EF Core Fluent API entity configurations
│   ├── Repositories/         ← SqliteRepository<T>, AuditLogRepository, DetectionRecordRepository
│   └── Exceptions/           ← DbException, ValidationException
└── Presentation/             # 表现层 — WPF UI with Prism.DryIoc DI
    ├── ViewModels/           ← LoginViewModel, MainViewModel, Auth/, Production/, Settings/
    ├── Views/               ← LoginWindow, MainWindow, Detection/, Production/, Auth/, Settings/, Audit/, App/
    ├── Events/              ← Prism PubSubEvents (DetectionEvents, NavigationEvents, LogEvents)
    ├── Converters/          ← TabConverters, LoginButtonTextConverter, StringToVisibilityConverter
    ├── Models/              ← CommunicationSettings, VmSettings, SystemSettings, DeviceControlSettings, DetectionResult
    └── Resources/           ← Styles.xaml

Presentation references Application + Domain
Application references Domain interfaces only (no concrete Infrastructure)
Domain has no external dependencies
Infrastructure implements Domain repository interfaces, referenced at runtime via DI
```

**Dependency rules:**
- Domain ← Infrastructure (repository implementations)
- Domain ← Application (service interfaces)
- Application ← Infrastructure (concrete repositories)
- Presentation ← All layers (wires DI container)

## Language Version Constraint

**C# 12 is required.** All `.csproj` files must specify:
```xml
<LangVersion>12.0</LangVersion>
```

Do NOT use C# 13+ features (e.g., alias any-type with `using` statements,Regex#). The project targets .NET 8.

## Tech Stack

- **.NET 8** (`net8.0-windows`, x64)
- **WPF** — Presentation layer UI framework
- **Prism.DryIoc 9.0.537** — MVVM, DI container, navigation, PubSubEvents
- **Entity Framework Core 8.0.11** (SQLite provider) — DbContext + Fluent API
- **Newtonsoft.Json 13.0.3** — JSON serialization for settings
- **VisionMaster SDK v4.2.0** — Machine vision platform SDK

## Key Files

| Category | File | Purpose |
|----------|------|---------|
| App bootstrap | `Presentation/App.xaml.cs` | Prism DryIoc container setup, database init |
| Main shell | `Presentation/MainWindow.xaml` | Navigation shell |
| Detection | `Presentation/Views/Detection/DetectionView.xaml.cs` | Main detection workflow UI |
| VM integration | `Application/VmServices/VmIntegrationService.cs` | VisionMaster SDK wrapper |
| Task service | `Application/Services/TaskService.cs` | Task workflow (Pending→Approved→Running→Completed) |
| Auth | `Presentation/ViewModels/LoginViewModel.cs` | User authentication |
| Audit log | `Application/Services/AuditLogService.cs` | Operation audit logging |
| DbContext | `Infrastructure/Persistence/TripleDetectionDbContext.cs` | EF Core 8 SQLite context |
| Repositories | `Infrastructure/Repositories/SqliteRepository.cs` | Raw ADO.NET repository (actively used) |

## VisionMaster SDK

- **Installed at:** `C:\Program Files\VisionMaster4.2.0`
- **SDK DLLs:** `C:\Program Files\VisionMaster4.2.0\Development\V4.x\Libraries\win64\C#`
- **Project references:** `VM.Core`, `VM.PlatformSDKCS`, `iMVS-6000PlatformSDKCS`, `GlobalVariableModuleCs`, `VMControls.*`, `Apps.*`, `Frontend.*`, `GateWay.*`, `log4net`
- **Local libs:** `Infrastructure/libs/VisionMaster/` (copied DLLs)

## Database

- **Engine:** SQLite (via EF Core 8 + raw ADO.NET)
- **Location:** `Config/tripledetection.db`
- **Tables:** Users, Products, Tasks, DetectionRecords, AuditLogs, SystemConfigs
- **Pattern:** Soft delete (`IsDeleted`) on all entities
- **Init seed:** admin/admin123, 3 products, 4 tasks

## Build Commands

```bash
# From project root
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" TripleDetection.csproj -t:Rebuild -p:Configuration=Debug
```

## Core Business Flows

### Detection Flow
1. Select Approved task (only `TaskStatus.Approved` tasks shown)
2. Load Product's `.sol` VisionMaster solution file
3. Set global variables: `BN` (batch number), `Mfg` (manufacturing date), `EXP` (expiration date)
4. Run single detection or enable continuous run
5. On `OnWorkStatusEvent` callback with `nWorkStatus==0 && nProcessID==10000`:
   - Read `GetOutputString()` — comma-separated: `IsOK, BatchNumber, ProductionDate, ExpirationDate`
   - Update UI with OK/NG/pass rate
   - Save `DetectionRecord` asynchronously
   - Publish `DetectionResultEvent` via Prism `IEventAggregator`

### Task Workflow
```
Pending (待审批) → Approved (已审批) → Running (运行中) → Completed (已完成)
```
Tasks must be Approved before appearing in DetectionView task selector.

### User Authentication
1. Login form → `UserService.Authenticate(username, password)`
2. Verify enabled + not locked
3. Password: SHA256 salted hash (legacy plain text migration on login)
4. `SessionManager.SetCurrentUser(user)` stores session statically

## Key Entity Relationships

```
User (1) ───< AuditLog (many)
Product (1) ───< ProdTask (many) ───< DetectionRecord (many)
Product (1) ───< DetectionRecord (many)
```
Note: No FK constraints at DB level — ProductId/TaskId columns exist but no EF Fluent API FK configuration.

## Important Notes

1. **Dual repository layer:** EF Core `TripleDetectionDbContext` coexists with raw ADO `SqliteRepository<T>`. The raw ADO repository has specialized query methods and is the actively used implementation for paged queries and exports.
2. **No IUnitOfWork on DbContext:** `SqliteUnitOfWork` implements `IUnitOfWork`, not `TripleDetectionDbContext`.
3. **Soft delete everywhere:** All `Delete()` calls set `IsDeleted=true`, filtered in all queries.
4. **No navigation properties:** Entities reference each other by ID only — no EF Core navigation properties configured.
5. **VisionMaster SDK loaded at runtime:** Assembly resolver handles `AppDomain.CurrentDomain_AssemblyResolve` for VM DLLs.
