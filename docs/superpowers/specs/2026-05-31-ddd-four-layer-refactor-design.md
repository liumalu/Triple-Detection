# 四层 DDD 架构合并重构设计

**日期：** 2026-05-31
**状态：** 已批准
**目标：** 将 TripleDetection.App + TripleDetection.Services + TripleDetection.Data 三个项目合并为一个 .NET 8 项目，按 DDD 四层结构重组

---

## 1. 目标项目结构

```
TripleDetection/
├── Domain/                    # 领域层
│   ├── Entities/             ← 聚合根、实体（User, Product, ProdTask, DetectionRecord, AuditLog, SystemConfig, BaseEntity）
│   ├── Enums/                ← 值对象枚举（ValidType, ProductStatus, TaskStatus, DatabaseProviderType）
│   ├── Repositories/         ← 仓储接口（IRepository<T>, IUnitOfWork, IRepositoryFactory, IDbConnectionFactory）
│   └── SessionManager.cs
├── Application/               # 应用层（合并了原 Services + App 层业务服务）
│   ├── Services/             ← 业务服务（UserService, AuditLogService, ProductService, TaskService, DetectionRecordService, PasswordHashService）
│   ├── VmServices/           ← VM 集成（VmIntegrationService, ImageStorageService, SettingsSyncService）
│   └── SettingsServices/     ← 设置服务（CommunicationSettingsService, VmSettingsService, SystemSettingsService, DeviceControlSettingsService）
├── Infrastructure/            # 基础设施层
│   ├── Persistence/          ← DbContext、EF Core Configurations、DatabaseInitializer
│   ├── Repositories/         ← 仓储实现（SqliteRepository、AuditLogRepository、DetectionRecordRepository）
│   ├── Persistence/Configurations/  ← EF Entity Configuration（Fluent API）
│   └── Exceptions/           ← 通用异常（DbException, ValidationException）
└── Presentation/             # 表现层（WPF/Prism）
    ├── ViewModels/
    ├── Views/
    ├── Converters/
    ├── Events/
    ├── Models/
    └── Resources/Styles.xaml
```

**依赖规则：**
- Domain ← Infrastructure（实现仓储接口）
- Domain ← Application（调用服务接口）
- Application ← Infrastructure（调用仓储实现）
- Presentation ← 所有层

---

## 2. 各层详细映射

### 2.1 Domain 层

| 原位置 | 新位置 |
|--------|--------|
| `Data/Entities/User.cs` | `Domain/Entities/User.cs` |
| `Data/Entities/Product.cs` | `Domain/Entities/Product.cs` |
| `Data/Entities/ProdTask.cs` | `Domain/Entities/ProdTask.cs` |
| `Data/Entities/DetectionRecord.cs` | `Domain/Entities/DetectionRecord.cs` |
| `Data/Entities/AuditLog.cs` | `Domain/Entities/AuditLog.cs` |
| `Data/Entities/SystemConfig.cs` | `Domain/Entities/SystemConfig.cs` |
| `Data/Entities/Enums.cs` | `Domain/Enums/Enums.cs` |
| `Data/Entities/UserQuery.cs` | `Domain/Entities/Queries/UserQuery.cs` |
| `Data/BaseEntity.cs` | `Domain/Entities/BaseEntity.cs` |
| `Data/Repositories/Contracts/IRepository.cs` | `Domain/Repositories/IRepository.cs` |
| `Data/Repositories/Contracts/IUnitOfWork.cs` | `Domain/Repositories/IUnitOfWork.cs` |
| `Data/Repositories/Contracts/IRepositoryFactory.cs` | `Domain/Repositories/IRepositoryFactory.cs` |
| `Data/IDbConnectionFactory.cs` | `Domain/Repositories/IDbConnectionFactory.cs` |
| `Services/SessionManager.cs` | `Domain/SessionManager.cs` |

### 2.2 Application 层

| 原位置 | 新位置 |
|--------|--------|
| `Services/UserService.cs` | `Application/Services/UserService.cs` |
| `Services/PasswordHashService.cs` | `Application/Services/PasswordHashService.cs` |
| `Services/Audit/AuditLogService.cs` | `Application/Services/AuditLogService.cs` |
| `Services/Data/DetectionRecordService.cs` | `Application/Services/DetectionRecordService.cs` |
| `Services/Production/ProductService.cs` | `Application/Services/ProductService.cs` |
| `Services/Production/TaskService.cs` | `Application/Services/TaskService.cs` |
| `App/Services/Detection/VmIntegrationService.cs` | `Application/VmServices/VmIntegrationService.cs` |
| `App/Services/Detection/ImageStorageService.cs` | `Application/VmServices/ImageStorageService.cs` |
| `App/Services/Settings/SettingsSyncService.cs` | `Application/VmServices/SettingsSyncService.cs` |
| `App/Services/Settings/CommunicationSettingsService.cs` | `Application/SettingsServices/CommunicationSettingsService.cs` |
| `App/Services/Settings/VmSettingsService.cs` | `Application/SettingsServices/VmSettingsService.cs` |
| `App/Services/Settings/SystemSettingsService.cs` | `Application/SettingsServices/SystemSettingsService.cs` |
| `App/Services/Settings/DeviceControlSettingsService.cs` | `Application/SettingsServices/DeviceControlSettingsService.cs` |
| `App/Services/System/LoggingService.cs` | `Application/Services/LoggingService.cs` |

### 2.3 Infrastructure 层

| 原位置 | 新位置 |
|--------|--------|
| `Data/Repositories/Sqlite/SqliteRepository.cs` | `Infrastructure/Repositories/SqliteRepository.cs` |
| `Data/Repositories/Sqlite/SqliteUnitOfWork.cs` | `Infrastructure/Persistence/SqliteUnitOfWork.cs` |
| `Data/Repositories/AuditLogRepository.cs` | `Infrastructure/Repositories/AuditLogRepository.cs` |
| `Data/Repositories/DetectionRecordRepository.cs` | `Infrastructure/Repositories/DetectionRecordRepository.cs` |
| `Data/ConnectionFactories/SqliteConnectionFactory.cs` | `Infrastructure/Persistence/SqliteConnectionFactory.cs` |
| `Data/ConnectionFactories/SqlServerConnectionFactory.cs` | `Infrastructure/Persistence/SqlServerConnectionFactory.cs` |
| `Data/Repositories/Configuration/*.cs` | `Infrastructure/Persistence/Configurations/` |
| `Data/DatabaseInitializer.cs` | `Infrastructure/Persistence/DatabaseInitializer.cs` |
| `Data/JsonHelper.cs` | `Infrastructure/JsonHelper.cs` |
| 新增 | `Infrastructure/Exceptions/DbException.cs` |
| 新增 | `Infrastructure/Exceptions/ValidationException.cs` |

### 2.4 Presentation 层

| 原位置 | 新位置 |
|--------|--------|
| `App/ViewModels/` | `Presentation/ViewModels/` |
| `App/Views/` | `Presentation/Views/` |
| `App/Converters/` | `Presentation/Converters/` |
| `App/Events/` | `Presentation/Events/` |
| `App/Models/` | `Presentation/Models/` |
| `App/Resources/Styles.xaml` | `Presentation/Resources/Styles.xaml` |
| `App/App.xaml` | `Presentation/App.xaml` |
| `App/MainWindow.xaml` | `Presentation/MainWindow.xaml` |
| `App/DatabaseConfig.cs` | `Presentation/DatabaseConfig.cs` |

---

## 3. 异常定义

```csharp
// Infrastructure/Exceptions/DbException.cs
public class DbException : Exception
{
    public DbException(string message, Exception innerException = null)
        : base(message, innerException) { }
}

// Infrastructure/Exceptions/ValidationException.cs
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
```

---

## 4. 错误处理策略

- **Domain 层**：不处理异常，仅抛业务异常（`ValidationException` 等）。
- **Application 层**：服务方法内 try-catch，记录审计日志后重新抛出或返回 `Result<T>`。
- **Infrastructure 层**：数据库异常统一封装为 `DbException`，避免数据库实现泄露到上层。
- **Presentation 层**：ViewModel 通过 `try-catch` + `ErrorMessage` 属性反馈，View 通过 Binding 显示。

---

## 5. 关键重构项

### 5.1 解决历史遗留问题

| 问题 | 解决方案 |
|------|----------|
| `SqliteDbContext` 不存在但被引用 | 删除该引用，实现真正的 EF Core 8 DbContext |
| Views 绕开 DI 直接 `new Service()` | 统一改为构造函数注入 ViewModel |
| `DetectionView` 自建 `SqliteRepositoryFactory` | 迁移到 DI 容器统一管理 |
| `AuditLogView` / `DetectionHistoryView` 直连 SQLite | 改用 `IRepository<T>` 接口查询 |

### 5.2 EF Core 8 迁移

- 现有 EF 配置类（`ProductConfiguration` 等）迁移为 EF Core 8 Fluent API 配置
- `SqliteRepository<T>` 的 raw ADO.NET 实现替换为 `DbContext + LINQ`
- `DatabaseInitializer` 迁移为 EF Core 8 `EnsureCreated()` / Migrations

---

## 6. 迁移步骤

```
Phase 1 — 创建新项目结构（不破坏现有代码）
  └─ 创建 TripleDetection.csproj（.NET 8），按四层创建空目录

Phase 2 — 迁移 Domain 层
  └─ 迁移 Entities、Enums、所有 Repository 接口

Phase 3 — 迁移 Infrastructure 层
  └─ 实现 EF Core 8 DbContext，迁移 Repositories，实现异常类

Phase 4 — 迁移 Application 层
  └─ 迁移所有 Services（合并 App.Services + Services）

Phase 5 — 迁移 Presentation 层
  └─ 迁移 ViewModels、Views、Resources、App.xaml.cs

Phase 6 — 清理
  └─ 删除旧项目文件，更新 solution 文件引用
```

每步均可独立验证，不会有长时间不可运行状态。

---

## 7. 技术约束

- **目标框架：** .NET 8
- **C# 版本：** C# 12（LangVersion 12.0）
- **ORM：** Entity Framework Core 8 + SQLite（Microsoft.EntityFrameworkCore.Sqlite 8.x）
- **DI 容器：** DryIoc（Prism.DryIoc）
- **MVVM：** Prism 9.x（Prism.DryIoc for .NET 8）