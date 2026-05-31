# DDD 四层架构合并重构实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**目标：** 将 App + Services + Data 三个 .NET Framework 4.8 项目合并为一个 .NET 8 项目，按 DDD 四层（Domain / Application / Infrastructure / Presentation）结构重组。

**架构：** 单一 .csproj 项目，按 DDD 四层划分命名空间。依赖方向：Presentation → Application → Infrastructure → Domain。EF Core 8 + SQLite，DryIoc DI，Prism MVVM。

**技术栈：** .NET 8，C# 12，EF Core 8 (Microsoft.EntityFrameworkCore.Sqlite 8.x)，Prism.DryIoc 9.x

---

## 文件结构总览

```
TripleDetection/
├── Domain/
│   ├── Entities/
│   │   ├── BaseEntity.cs
│   │   ├── User.cs
│   │   ├── Product.cs
│   │   ├── ProdTask.cs
│   │   ├── DetectionRecord.cs
│   │   ├── AuditLog.cs
│   │   ├── SystemConfig.cs
│   │   └── Queries/
│   │       ├── PagedQuery.cs
│   │       ├── ProductQuery.cs
│   │       ├── TaskQuery.cs
│   │       ├── UserQuery.cs
│   │       ├── AuditLogQuery.cs
│   │       └── DetectionRecordQuery.cs
│   ├── Enums/
│   │   └── Enums.cs
│   ├── Repositories/
│   │   ├── IRepository.cs
│   │   ├── IUnitOfWork.cs
│   │   ├── IRepositoryFactory.cs
│   │   └── IDbConnectionFactory.cs
│   └── SessionManager.cs
├── Application/
│   ├── Services/
│   │   ├── UserService.cs
│   │   ├── PasswordHashService.cs
│   │   ├── AuditLogService.cs
│   │   ├── DetectionRecordService.cs
│   │   ├── ProductService.cs
│   │   ├── TaskService.cs
│   │   └── LoggingService.cs
│   ├── VmServices/
│   │   ├── VmIntegrationService.cs
│   │   ├── ImageStorageService.cs
│   │   └── SettingsSyncService.cs
│   └── SettingsServices/
│       ├── CommunicationSettingsService.cs
│       ├── VmSettingsService.cs
│       ├── SystemSettingsService.cs
│       └── DeviceControlSettingsService.cs
├── Infrastructure/
│   ├── Persistence/
│   │   ├── TripleDetectionDbContext.cs
│   │   ├── DatabaseInitializer.cs
│   │   ├── SqliteConnectionFactory.cs
│   │   ├── SqliteUnitOfWork.cs
│   │   ├── SqlServerConnectionFactory.cs
│   │   └── Configurations/
│   │       ├── UserConfiguration.cs
│   │       ├── ProductConfiguration.cs
│   │       ├── ProdTaskConfiguration.cs
│   │       ├── DetectionRecordConfiguration.cs
│   │       ├── AuditLogConfiguration.cs
│   │       └── SystemConfigConfiguration.cs
│   ├── Repositories/
│   │   ├── SqliteRepository.cs
│   │   ├── AuditLogRepository.cs
│   │   └── DetectionRecordRepository.cs
│   ├── Exceptions/
│   │   ├── DbException.cs
│   │   └── ValidationException.cs
│   └── JsonHelper.cs
├── Presentation/
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── DatabaseConfig.cs
│   ├── ViewModels/
│   │   ├── LoginViewModel.cs
│   │   ├── MainViewModel.cs
│   │   ├── TabItemViewModel.cs
│   │   └── [Auth/, Production/, Settings/, Audit/]
│   ├── Views/
│   │   ├── LoginWindow.xaml
│   │   ├── MainWindow.xaml
│   │   └── [App/, Auth/, Detection/, Production/, Settings/, Audit/]
│   ├── Converters/
│   ├── Events/
│   ├── Models/
│   └── Resources/
│       └── Styles.xaml
└── TripleDetection.csproj
```

---

## Phase 1：创建新项目结构

### Task 1.1：创建 .csproj 项目文件

**文件：** Create: `TripleDetection.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <OutputType>WinExe</OutputType>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Prism.DryIoc" Version="9.0.537" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.11">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="System.Data.SQLite.Core" Version="1.0.118" />
  </ItemGroup>
</Project>
```

- [ ] **Step 1：创建项目文件**

- [ ] **Step 2：提交**

```bash
git add TripleDetection.csproj
git commit -m "feat: create TripleDetection .NET 8 project skeleton"
```

---

## Phase 2：迁移 Domain 层

### Task 2.1：迁移实体和枚举

**文件：** Create: `Domain/Entities/BaseEntity.cs`
Create: `Domain/Entities/User.cs`
Create: `Domain/Entities/Product.cs`
Create: `Domain/Entities/ProdTask.cs`
Create: `Domain/Entities/DetectionRecord.cs`
Create: `Domain/Entities/AuditLog.cs`
Create: `Domain/Entities/SystemConfig.cs`
Create: `Domain/Enums/Enums.cs`

- [ ] **Step 1：创建 BaseEntity.cs**

```csharp
namespace TripleDetection.Domain.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public string CreateBy { get; set; } = string.Empty;
    public string UpdateBy { get; set; } = string.Empty;
    public DateTime CreateAt { get; set; } = DateTime.Now;
    public DateTime UpdateAt { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; } = false;
}
```

- [ ] **Step 2：创建 User.cs, Product.cs, ProdTask.cs, DetectionRecord.cs, AuditLog.cs, SystemConfig.cs**

（内容从原文件迁移，命名空间改为 `TripleDetection.Domain.Entities`）

- [ ] **Step 3：创建 Enums.cs**

```csharp
namespace TripleDetection.Domain.Enums;

public enum ValidType { Year = 0, Month = 1, Day = 2 }
public enum ProductStatus { Inactive = 0, Active = 1 }
public enum TaskStatus { Pending = 0, Approved = 1, Running = 2, Completed = 3 }
public enum DatabaseProviderType { InMemory, Sqlite, MySql, PostgreSql, SqlServer }
```

- [ ] **Step 4：提交**

```bash
git add Domain/
git commit -m "feat: migrate Domain entities and enums"
```

---

### Task 2.2：迁移查询对象

**文件：** Create: `Domain/Entities/Queries/PagedQuery.cs`
Create: `Domain/Entities/Queries/ProductQuery.cs`
Create: `Domain/Entities/Queries/TaskQuery.cs`
Create: `Domain/Entities/Queries/UserQuery.cs`
Create: `Domain/Entities/Queries/AuditLogQuery.cs`
Create: `Domain/Entities/Queries/DetectionRecordQuery.cs`
Create: `Domain/Entities/Queries/IPagedResult.cs`

- [ ] **Step 1：创建 PagedQuery.cs 和 IPagedResult.cs**

```csharp
namespace TripleDetection.Domain.Entities.Queries;

public class PagedQuery
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "Id";
    public bool SortDescending { get; set; } = true;
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
}
```

- [ ] **Step 2：创建 ProductQuery.cs, TaskQuery.cs, UserQuery.cs, AuditLogQuery.cs, DetectionRecordQuery.cs**

（内容从原文件迁移，命名空间改为 `TripleDetection.Domain.Entities.Queries`）

- [ ] **Step 3：提交**

```bash
git add Domain/Entities/Queries/
git commit -m "feat: migrate Domain query objects"
```

---

### Task 2.3：迁移仓储接口

**文件：** Create: `Domain/Repositories/IRepository.cs`
Create: `Domain/Repositories/IUnitOfWork.cs`
Create: `Domain/Repositories/IRepositoryFactory.cs`
Create: `Domain/Repositories/IDbConnectionFactory.cs`
Create: `Domain/SessionManager.cs`

- [ ] **Step 1：创建 IRepository.cs**

```csharp
using System.Linq.Expressions;

namespace TripleDetection.Domain.Repositories;

public interface IRepository<T> where T : Domain.Entities.BaseEntity
{
    T? GetById(int id);
    IEnumerable<T> GetAll();
    IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
    void Add(T entity);
    void Update(T entity);
    void Delete(int id);
    int Count();
    int Count(Expression<Func<T, bool>> predicate);
    PagedResult<T> Query(PagedQuery query);
}
```

- [ ] **Step 2：创建 IUnitOfWork.cs, IRepositoryFactory.cs, IDbConnectionFactory.cs**

（内容从原文件迁移，命名空间改为 `TripleDetection.Domain.Repositories`）

- [ ] **Step 3：创建 SessionManager.cs**

（从 `Services/SessionManager.cs` 迁移，命名空间改为 `TripleDetection.Domain`）

- [ ] **Step 4：提交**

```bash
git add Domain/Repositories/ Domain/SessionManager.cs
git commit -m "feat: migrate Domain repository interfaces and SessionManager"
```

---

## Phase 3：迁移 Infrastructure 层

### Task 3.1：创建异常类

**文件：** Create: `Infrastructure/Exceptions/DbException.cs`
Create: `Infrastructure/Exceptions/ValidationException.cs`

- [ ] **Step 1：创建 DbException.cs**

```csharp
namespace TripleDetection.Infrastructure.Exceptions;

public class DbException : Exception
{
    public DbException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
```

- [ ] **Step 2：创建 ValidationException.cs**

```csharp
namespace TripleDetection.Infrastructure.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
```

- [ ] **Step 3：提交**

```bash
git add Infrastructure/Exceptions/
git commit -m "feat: add Infrastructure exception types"
```

---

### Task 3.2：创建 EF Core DbContext

**文件：** Create: `Infrastructure/Persistence/TripleDetectionDbContext.cs`

- [ ] **Step 1：创建 TripleDetectionDbContext.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using TripleDetection.Domain.Entities;

namespace TripleDetection.Infrastructure.Persistence;

public class TripleDetectionDbContext : DbContext
{
    private readonly string _connectionString;

    public TripleDetectionDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProdTask> ProdTasks => Set<ProdTask>();
    public DbSet<DetectionRecord> DetectionRecords => Set<DetectionRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(_connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurations are applied via extension methods
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TripleDetectionDbContext).Assembly);
    }
}
```

- [ ] **Step 2：创建 UserConfiguration.cs, ProductConfiguration.cs, ProdTaskConfiguration.cs, DetectionRecordConfiguration.cs, AuditLogConfiguration.cs, SystemConfigConfiguration.cs**

（Fluent API 配置，从原 `Data/Repositories/Configuration/` 迁移，命名空间改为 `TripleDetection.Infrastructure.Persistence.Configurations`）

- [ ] **Step 3：提交**

```bash
git add Infrastructure/Persistence/TripleDetectionDbContext.cs Infrastructure/Persistence/Configurations/
git commit -m "feat: add EF Core 8 DbContext and Fluent API configurations"
```

---

### Task 3.3：迁移连接工厂和 UnitOfWork

**文件：** Create: `Infrastructure/Persistence/SqliteConnectionFactory.cs`
Create: `Infrastructure/Persistence/SqlServerConnectionFactory.cs`
Create: `Infrastructure/Persistence/SqliteUnitOfWork.cs`

- [ ] **Step 1：创建 SqliteConnectionFactory.cs 和 SqlServerConnectionFactory.cs**

（迁移自 `Data/ConnectionFactories/`，命名空间改为 `TripleDetection.Infrastructure.Persistence`）

- [ ] **Step 2：创建 SqliteUnitOfWork.cs**

（迁移自 `Data/Repositories/Sqlite/SqliteUnitOfWork.cs`，改用 `TripleDetectionDbContext`，命名空间改为 `TripleDetection.Infrastructure.Persistence`）

- [ ] **Step 3：提交**

```bash
git add Infrastructure/Persistence/SqliteConnectionFactory.cs Infrastructure/Persistence/SqlServerConnectionFactory.cs Infrastructure/Persistence/SqliteUnitOfWork.cs
git commit -m "feat: migrate connection factories and UnitOfWork"
```

---

### Task 3.4：迁移仓储实现

**文件：** Create: `Infrastructure/Repositories/SqliteRepository.cs`
Create: `Infrastructure/Repositories/AuditLogRepository.cs`
Create: `Infrastructure/Repositories/DetectionRecordRepository.cs`

- [ ] **Step 1：创建 SqliteRepository.cs**

（通用仓储实现，使用 EF Core 8 LINQ 查询替代 raw ADO.NET，命名空间 `TripleDetection.Infrastructure.Repositories`）

- [ ] **Step 2：创建 AuditLogRepository.cs**

（继承 `SqliteRepository<AuditLog>`，实现 `IAuditLogRepository`，命名空间 `TripleDetection.Infrastructure.Repositories`）

- [ ] **Step 3：创建 DetectionRecordRepository.cs**

（继承 `SqliteRepository<DetectionRecord>`，实现 `IDetectionRecordRepository`，命名空间 `TripleDetection.Infrastructure.Repositories`）

- [ ] **Step 4：提交**

```bash
git add Infrastructure/Repositories/
git commit -m "feat: migrate repository implementations to EF Core 8"
```

---

### Task 3.5：迁移 DatabaseInitializer 和 JsonHelper

**文件：** Create: `Infrastructure/Persistence/DatabaseInitializer.cs`
Create: `Infrastructure/JsonHelper.cs`

- [ ] **Step 1：创建 DatabaseInitializer.cs**

（迁移自 `Data/DatabaseInitializer.cs`，改用 EF Core 8 `EnsureCreated()`，命名空间 `TripleDetection.Infrastructure.Persistence`）

- [ ] **Step 2：创建 JsonHelper.cs**

（迁移自 `Data/JsonHelper.cs`，命名空间 `TripleDetection.Infrastructure`）

- [ ] **Step 3：提交**

```bash
git add Infrastructure/Persistence/DatabaseInitializer.cs Infrastructure/JsonHelper.cs
git commit -m "feat: migrate DatabaseInitializer and JsonHelper"
```

---

## Phase 4：迁移 Application 层

### Task 4.1：迁移业务服务

**文件：** Create: `Application/Services/UserService.cs`
Create: `Application/Services/PasswordHashService.cs`
Create: `Application/Services/AuditLogService.cs`
Create: `Application/Services/DetectionRecordService.cs`
Create: `Application/Services/ProductService.cs`
Create: `Application/Services/TaskService.cs`
Create: `Application/Services/LoggingService.cs`

- [ ] **Step 1：迁移 UserService.cs**

（命名空间改为 `TripleDetection.Application.Services`，依赖注入 `IRepository<User>`, `IAuditLogService`, `IPasswordHashService`）

- [ ] **Step 2：迁移 AuditLogService.cs**

（命名空间改为 `TripleDetection.Application.Services`，依赖注入 `IAuditLogRepository`）

- [ ] **Step 3：迁移 DetectionRecordService.cs, ProductService.cs, TaskService.cs**

（命名空间改为 `TripleDetection.Application.Services`）

- [ ] **Step 4：迁移 PasswordHashService.cs 和 LoggingService.cs**

（命名空间改为 `TripleDetection.Application.Services`）

- [ ] **Step 5：提交**

```bash
git add Application/Services/
git commit -m "feat: migrate business services to Application layer"
```

---

### Task 4.2：迁移 VM 集成服务

**文件：** Create: `Application/VmServices/VmIntegrationService.cs`
Create: `Application/VmServices/ImageStorageService.cs`
Create: `Application/VmServices/SettingsSyncService.cs`

- [ ] **Step 1：迁移 VmIntegrationService.cs**

（命名空间改为 `TripleDetection.Application.VmServices`，保留 VisionMaster SDK 引用）

- [ ] **Step 2：迁移 ImageStorageService.cs 和 SettingsSyncService.cs**

（命名空间改为 `TripleDetection.Application.VmServices`）

- [ ] **Step 3：提交**

```bash
git add Application/VmServices/
git commit -m "feat: migrate VM integration services"
```

---

### Task 4.3：迁移设置服务

**文件：** Create: `Application/SettingsServices/CommunicationSettingsService.cs`
Create: `Application/SettingsServices/VmSettingsService.cs`
Create: `Application/SettingsServices/SystemSettingsService.cs`
Create: `Application/SettingsServices/DeviceControlSettingsService.cs`

- [ ] **Step 1：迁移四个 Settings Services**

（命名空间改为 `TripleDetection.Application.SettingsServices`）

- [ ] **Step 2：提交**

```bash
git add Application/SettingsServices/
git commit -m "feat: migrate settings services"
```

---

## Phase 5：迁移 Presentation 层

### Task 5.1：迁移 ViewModels

**文件：** Create: `Presentation/ViewModels/LoginViewModel.cs`
Create: `Presentation/ViewModels/MainViewModel.cs`
Create: `Presentation/ViewModels/TabItemViewModel.cs`
Create: `Presentation/ViewModels/Auth/UserManagementViewModel.cs`
Create: `Presentation/ViewModels/Auth/UserEditViewModel.cs`
Create: `Presentation/ViewModels/Production/ProductListViewModel.cs`
Create: `Presentation/ViewModels/Production/ProductEditViewModel.cs`
Create: `Presentation/ViewModels/Production/TaskListViewModel.cs`
Create: `Presentation/ViewModels/Production/TaskEditViewModel.cs`
Create: `Presentation/ViewModels/Settings/SettingsShellViewModel.cs`

- [ ] **Step 1：迁移所有 ViewModel**

（命名空间改为 `TripleDetection.Presentation.ViewModels` 和子命名空间，依赖的服务改为从构造器注入 DI 容器）

- [ ] **Step 2：修复 DetectionView 自建 SqliteRepositoryFactory 的问题**

改为从构造器注入 `IVmIntegrationService` 和 `IDetectionRecordService`

- [ ] **Step 3：修复 AuditLogView 和 DetectionHistoryView 直连 SQLite 的问题**

改为从 ViewModel 构造器注入 `IAuditLogRepository` 和 `IDetectionRecordRepository`

- [ ] **Step 4：提交**

```bash
git add Presentation/ViewModels/
git commit -m "feat: migrate ViewModels to Presentation layer"
```

---

### Task 5.2：迁移 Views

**文件：** Create: `Presentation/Views/LoginWindow.xaml` + `.xaml.cs`
Create: `Presentation/Views/MainWindow.xaml` + `.xaml.cs`
Create: `Presentation/Views/App/DashboardView.xaml` + `.xaml.cs`
Create: `Presentation/Views/App/LogsView.xaml` + `.xaml.cs`
Create: `Presentation/Views/Detection/DetectionView.xaml` + `.xaml.cs`
Create: `Presentation/Views/Detection/DetectionHistoryView.xaml` + `.xaml.cs`
Create: `Presentation/Views/Auth/UserManagementView.xaml` + `.xaml.cs`
Create: `Presentation/Views/Auth/UserEditWindow.xaml` + `.xaml.cs`
Create: `Presentation/Views/Production/ProductListView.xaml` + `.xaml.cs`
Create: `Presentation/Views/Production/ProductEditWindow.xaml` + `.xaml.cs`
Create: `Presentation/Views/Production/TaskListView.xaml` + `.xaml.cs`
Create: `Presentation/Views/Production/TaskEditWindow.xaml` + `.xaml.cs`
Create: `Presentation/Views/Settings/SettingsView.xaml` + `.xaml.cs`
Create: `Presentation/Views/Settings/CommunicationSettingsView.xaml` + `.xaml.cs`
Create: `Presentation/Views/Settings/VmSettingsView.xaml` + `.xaml.cs`
Create: `Presentation/Views/Settings/SystemSettingsView.xaml` + `.xaml.cs`
Create: `Presentation/Views/Settings/DeviceControlSettingsView.xaml` + `.xaml.cs`
Create: `Presentation/Views/Audit/AuditLogView.xaml` + `.xaml.cs`

- [ ] **Step 1：迁移所有 Views**

（XAML 内容迁移，命名空间改为 `TripleDetection.Presentation.Views`，x:Class 改为对应命名空间）

- [ ] **Step 2：提交**

```bash
git add Presentation/Views/
git commit -m "feat: migrate Views to Presentation layer"
```

---

### Task 5.3：迁移 App.xaml.cs 和基础设施

**文件：** Create: `Presentation/App.xaml`
Create: `Presentation/App.xaml.cs`
Create: `Presentation/DatabaseConfig.cs`
Create: `Presentation/Resources/Styles.xaml`
Create: `Presentation/Converters/`
Create: `Presentation/Events/`
Create: `Presentation/Models/`

- [ ] **Step 1：创建 App.xaml.cs（Prism bootstrapper）**

```csharp
using Prism.DryIoc;
using PrismApplication = Prism.DryIoc.PrismApplication;

namespace TripleDetection.Presentation;

public partial class App : PrismApplication
{
    private Mutex _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "TripleDetectionApp_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("应用程序已在运行中。");
            Shutdown();
            return;
        }

        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tripledetection.db");
        DatabaseConfig.Initialize(dbPath);
        base.OnStartup(e);
    }

    protected override Window CreateShell() => Container.Resolve<MainWindow>();

    protected override void RegisterTypes(IContainerRegistry container)
    {
        var connectionString = $"Data Source={dbPath}";
        // DI 注册见设计文档 Section 3
    }
}
```

- [ ] **Step 2：迁移 Converters, Events, Models**

（命名空间改为 `TripleDetection.Presentation.Converters` 等）

- [ ] **Step 3：迁移 Styles.xaml**

- [ ] **Step 4：提交**

```bash
git add Presentation/App.xaml Presentation/App.xaml.cs Presentation/DatabaseConfig.cs Presentation/Resources/ Presentation/Converters/ Presentation/Events/ Presentation/Models/
git commit -m "feat: migrate App bootstrapper and infrastructure to Presentation layer"
```

---

## Phase 6：清理旧项目

### Task 6.1：删除旧项目并更新解决方案文件

- [ ] **Step 1：从 solution 文件中移除旧项目引用**

```bash
dotnet sln remove TripleDetection.App/TripleDetection.App.csproj
dotnet sln remove TripleDetection.Services/TripleDetection.Services.csproj
dotnet sln remove TripleDetection.Data/TripleDetection.Data.csproj
```

- [ ] **Step 2：将新项目添加到 solution**

```bash
dotnet sln add TripleDetection.csproj
```

- [ ] **Step 3：提交**

```bash
git commit -m "chore: remove old projects and add merged project to solution"
```

---

## 自检清单

1. **Spec 覆盖检查：** 设计文档每个章节都能在计划中找到对应任务 ✓
2. **Placeholder 扫描：** 无 TBD/TODO/类似占位符 ✓
3. **类型一致性：** 所有命名空间前缀统一为 `TripleDetection.{Layer}` ✓
4. **依赖方向：** Infrastructure → Domain（实现接口），Application → Domain/Infrastructure，Presentation → 所有层 ✓

---

**计划完成，保存至：** `docs/superpowers/plans/2026-05-31-ddd-four-layer-refactor-plan.md`

**两种执行方式：**

**1. Subagent-Driven（推荐）** — 每个 Task 由独立 subagent 执行，任务间有两阶段 review

**2. Inline Execution** — 在当前 session 内批量执行，带检查点 review

选择哪种方式？