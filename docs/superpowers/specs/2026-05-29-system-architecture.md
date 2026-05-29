# Triple-Detection 系统架构文档

> **文档版本:** v1.0
> **更新日期:** 2026-05-29
> **状态:** 生效中

---

## 1. 项目概述

Triple-Detection 是一个基于 WPF 的视觉检测系统，集成 VisionMaster SDK 实现自动化产品检测。

### 1.1 技术栈

| 层级 | 技术 |
|-----|-----|
| 表现层 | WPF (.NET Framework 4.8), C# 8.0, MVVM 模式 |
| 业务层 | VisionMaster SDK (VM.PlatformSDKCS, VM.Core, VMControls) |
| 数据层 | Entity Framework 6 + SQLite |
| 存储 | SQLite 持久化 / JSON 初始化数据 / 内存 (开发) |

### 1.2 项目结构

```
TripleDetection.sln
├── TripleDetection.App/           # WPF 表现层 (.NET Framework 4.8, x64)
├── TripleDetection.Services/       # 业务逻辑层
├── TripleDetection.Data/           # 数据访问层 (EF6 + SQLite)
└── TripleDetection.App.Tests/      # 单元测试
```

### 1.3 关键文件索引

| 类别 | 文件 | 说明 |
|-----|-----|-----|
| 应用入口 | [MainWindow.xaml.cs](TripleDetection.App/MainWindow.xaml.cs) | 主窗口，VM 资源释放，导航管理 |
| 数据库 | [SqliteDbContext.cs](TripleDetection.Data/Repositories/Sqlite/SqliteDbContext.cs) | EF6 DbContext |
| 事务 | [SqliteUnitOfWork.cs](TripleDetection.Data/Repositories/Sqlite/SqliteUnitOfWork.cs) | 事务管理 (Begin/Commit/Rollback) |
| 工厂 | [SqliteRepositoryFactory.cs](TripleDetection.Data/Repositories/Sqlite/SqliteRepositoryFactory.cs) | Repository 工厂 |
| VM集成 | [VmIntegrationService.cs](TripleDetection.App/Services/VmIntegrationService.cs) | VisionMaster SDK 封装 |
| 业务服务 | [Services.cs](TripleDetection.Services/Services.cs) | ProductService, TaskService, AuditLogService |
| 用户服务 | [UserService.cs](TripleDetection.Services/UserService.cs) | 用户认证管理 |

---

## 2. 系统架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                        TripleDetection.App                       │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │  Views (XAML)                                               │ │
│  │  DashboardView | DetectionView | ProductListView | Tasks   │ │
│  │  LogsView | SettingsView | UserManagementView              │ │
│  ├─────────────────────────────────────────────────────────────┤ │
│  │  ViewModels                                                 │ │
│  │  MainViewModel | ProductListViewModel | TaskListViewModel   │ │
│  │  UserManagementViewModel | ...                             │ │
│  ├─────────────────────────────────────────────────────────────┤ │
│  │  Services                                                   │ │
│  │  VmIntegrationService  - VisionMaster SDK 封装               │ │
│  │  TabManager           - 视图生命周期管理                      │ │
│  │  LoggingService       - 文件日志                            │ │
│  │  ImageStorageService  - 图像存储                            │ │
│  │  Settings/*Service    - 配置管理                            │ │
│  └─────────────────────────────────────────────────────────────┘ │
└───────────────────────────────┬─────────────────────────────────┘
                               │ References
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                     TripleDetection.Services                     │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────┐  │
│  │ UserService     │ │ ProductService  │ │ TaskService         │  │
│  │ - 登录认证      │ │ - CRUD          │ │ - CRUD              │  │
│  │ - 用户管理      │ │ - 分页过滤      │ │ - 审批工作流         │  │
│  │ - 启用/禁用     │ │                 │ │ - 状态流转           │  │
│  │ - 锁定/解锁     │ │                 │ │                     │  │
│  ├─────────────────┤ ├─────────────────┤ ├─────────────────────┤  │
│  │ AuditLogService │ │ ConfigService   │ │ DataSeeder          │  │
│  │ 操作日志记录    │ │ 键值配置存储     │ │ 测试数据初始化       │  │
│  └─────────────────┘ └─────────────────┘ └─────────────────────┘  │
└───────────────────────────────┬─────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                      TripleDetection.Data                        │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ Entities (BaseEntity: Id, CreateBy, UpdateBy, CreateAt,    │ │
│  │            UpdateAt, IsDeleted)                             │ │
│  │ User (PK:Username) | Product | Task | DetectionRecord      │ │
│  │ SystemConfig | AuditLog                                     │ │
│  ├─────────────────────────────────────────────────────────────┤ │
│  │ Repositories                                                │ │
│  │ Contracts: IRepository<T>, IUserRepository, IUnitOfWork,  │ │
│  │            IRepositoryFactory                               │ │
│  │ Sqlite: SqliteRepository<T>, SqliteUserRepository,          │ │
│  │         SqliteUnitOfWork, SqliteRepositoryFactory          │ │
│  ├─────────────────────────────────────────────────────────────┤ │
│  │ DbContext                                                   │ │
│  │ SqliteDbContext (EF6 + System.Data.SQLite.EF6)             │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

---

## 3. 数据库实体关系

### 3.1 实体ER图

```
┌──────────────┐       ┌──────────────────┐       ┌──────────────────┐
│    User      │       │      Task        │       │  DetectionRecord │
│ (PK:Username)│       │    (PK: Id)      │       │    (PK: Id)      │
├──────────────┤       ├──────────────────┤       ├──────────────────┤
│ Username     │◄──────│ ProductId (FK)   │◄──────│ TaskId (FK)      │
│ RealName     │       │ Name             │       │ Result           │
│ Password     │       │ Status           │       │ Confidence       │
│ Role         │       │ BatchNumber      │       │ CodeInfo         │
│ IsEnabled    │       │ ProductionDate  │       │ ImagePath        │
│ IsLocked     │       │ ExpirationDate  │       │ DetectionTime    │
│ LastLoginAt  │       │ CreatedBy       │       └──────────────────┘
└──────────────┘       │ ReviewedBy       │
       │                │ ReviewedAt      │
       │                └────────┬─────────┘
       │                         │
       │                         │
       ▼                         ▼
┌──────────────┐       ┌──────────────────┐       ┌──────────────────┐
│  AuditLog    │       │     Product      │       │  SystemConfig    │
│  (PK: Id)     │       │    (PK: Id)      │       │   (PK: Id)       │
├──────────────┤       ├──────────────────┤       ├──────────────────┤
│ UserId (FK)  │       │ Code             │       │ Category         │
│ Action       │       │ Name             │       │ Key              │
│ Details      │       │ Description      │       │ Value            │
│ IpAddress    │       │ SolFilePath      │       │ Description      │
│ CreateAt     │       │ ValidType        │       └──────────────────┘
└──────────────┘       │ ValidPeriod      │
                       │ Status           │
                       └──────────────────┘
```

### 3.2 实体规格

| 实体 | 主键 | 说明 |
|-----|-----|-----|
| **User** | Username (string, 不自增) | 用户账号，PK 非自增 |
| **Product** | Id (int, 自增) | 产品，关联 .sol 方案文件 |
| **Task** | Id (int, 自增) | 任务，属于产品，状态流转 |
| **DetectionRecord** | Id (int, 自增) | 检测记录，属任务 |
| **SystemConfig** | Id (int, 自增) | 系统配置，键值对 |
| **AuditLog** | Id (int, 自增) | 操作审计日志 |

---

## 4. 核心业务功能

### 4.1 检测执行 (Detection)

**流程：**
```
选择产品 → 加载 .sol 方案 → 设置全局变量(BN/Mfg/EXP)
    → 单次运行(手动) 或 连续运行(自动)
    → 回调解析结果 → 显示 OK/NG
```

**关键类：** `VmIntegrationService`
- `LoadSolution(solPath)` - 加载视觉方案
- `SetGlobalVariable(name, value)` - 设置全局变量
- `_procedure.Run()` - 单次运行
- `_procedure.ContinuousRunEnable` - 连续运行
- 回调: `VmSolution.OnWorkStatusEvent`

### 4.2 任务工作流 (Task)

**状态流转：**
```
Pending (待审批) → Approved (已审批) → Running (运行中) → Completed (已完成)
```

**关键类：** `TaskService`
- `Create`, `Update`, `Delete`, `GetAll`, `GetById`
- `Approve(taskId, reviewer)` - 审批
- `Start(taskId)` - 开始运行
- `Complete(taskId)` - 完成

### 4.3 用户认证 (User)

**流程：**
```
登录(用户名+密码) → UserService.Authenticate
    → 验证密码/账号状态(启用/锁定)
    → 记录最后登录时间 → 返回结果
```

**关键类：** `UserService`
- `Authenticate(username, password)`
- `Enable`, `Disable`, `Lock`, `Unlock`
- `Create`, `Update`, `Delete`

---

## 5. UI 导航结构

### 5.1 主窗口布局

```
┌──────────────────────────────────────────────────────────────────┐
│ [Logo] Triple Detection          [🔔] [用户名 ▼] [Logout]        │
├────────┬─────────────────────────────────────────────────────────┤
│        │                                                         │
│   N    │                                                         │
│   A    │                    主内容区域                            │
│   V    │                                                         │
│        │                                                         │
│   R    │                                                         │
│   A    │                                                         │
│   I    │                                                         │
│   L    │                                                         │
│        │                                                         │
│  [▤]   │                                                         │
├────────┴─────────────────────────────────────────────────────────┤
│ 状态栏: 时间 | 当前用户 | 状态信息                               │
└──────────────────────────────────────────────────────────────────┘
```

### 5.2 导航项

| 视图 | 路由 | 功能 |
|-----|-----|-----|
| Dashboard | /dashboard | 统计概览 |
| Detection | /detection | 检测执行 (主流程) |
| Products | /products | 产品管理 CRUD |
| Tasks | /tasks | 任务管理 + 审批 |
| Logs | /logs | 操作日志查看 |
| Settings | /settings | 系统配置 |
| UserManagement | /users | 用户权限管理 |

---

## 6. Repository 工厂架构

### 6.1 接口契约

```csharp
public enum DatabaseProviderType { InMemory, Sqlite, MySql, PostgreSql, SqlServer }

public interface IRepositoryFactory
{
    IUnitOfWork CreateUnitOfWork();
    IRepository<T> CreateRepository<T>() where T : BaseEntity;
    IUserRepository CreateUserRepository();
    DatabaseProviderType ProviderType { get; }
}

public interface IUnitOfWork : IDisposable
{
    void BeginTransaction();
    void Commit();
    void Rollback();
    IRepository<T> GetRepository<T>() where T : BaseEntity;
    IUserRepository GetUserRepository();
    int SaveChanges();
    bool IsInTransaction { get; }
}
```

### 6.2 切换数据库

切换到 MySQL/PostgreSQL/SQLServer 只需：
1. 创建 `Repositories/MySql/` 目录
2. 实现 `MySqlDbContext`, `MySqlRepository<T>`, `MySqlUserRepository`, `MySqlUnitOfWork`, `MySqlRepositoryFactory`
3. 修改 `DatabaseConfig.cs` 中的 Factory 实例化

**服务层无需修改。**

---

## 7. VisionMaster SDK 集成

### 7.1 程序集解析

`MainWindow.CurrentDomain_AssemblyResolve` 动态加载：
- 本地 `libs/` 目录优先
- 其次 VM 安装目录: `C:\Program Files\VisionMaster4.2.0\Development\V4.x\...`

### 7.2 VM 资源释放 (Window_Closing)

```csharp
// 1. 停止所有流程的连续运行
procedure.ContinuousRunEnable = false;

// 2. 关闭方案（释放相机连接）
VmSolution.Instance.CloseSolution();

// 3. 注销事件订阅
VmSolution.OnWorkStatusEvent -= callback;
```

### 7.3 全局变量

通过 `GlobalVariableModuleTool.SetGlobalVar(name, value)` 设置：
- `BN` - 批次号
- `Mfg` - 厂商
- `EXP` - 有效期

---

## 8. 数据库初始化

### 8.1 初始化流程

```
DatabaseConfig.Initialize()
    → DatabaseInitializer.Initialize()
    → SqliteDbContext 确保创建
    → SeedInitialData() 从 JSON 读取用户数据
```

### 8.2 配置存储

```csharp
// MainWindow.Window_Loaded
DatabaseConfig.Initialize();

// DatabaseConfig.cs
public static IRepositoryFactory Factory { get; }
```

---

## 9. 验证标准

- [x] 编译通过，0 errors
- [x] 启动应用时自动创建 SQLite 数据库 `Data/tripledetection.db`
- [x] 首次启动从 JSON 导入初始用户数据
- [x] 用户管理 (登录/增删改查) 正常工作
- [x] 产品、任务数据持久化正常
- [x] 关闭/重启应用后数据保持
- [x] VM 资源正确释放
- [ ] 未来切换 MySQL 只需替换 Factory 实现

---

## 10. 相关文档

- [主应用布局设计](./2026-05-25-main-layout-design.md)
- [用户管理重新设计](./2026-05-26-user-management-redesign.md)
- [VM 资源清理设计](./2026-05-28-vm-resource-cleanup-design.md)