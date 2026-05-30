# 系统清理与归集设计

> **文档版本:** v1.0
> **更新日期:** 2026-05-30
> **状态:** 设计阶段

---

## 1. 背景与目标

### 现状问题

| 问题 | 影响 |
|-----|------|
| DLL 散落在多处（bin/Debug、各project、VisionMaster SDK、libs） | 难以定位、版本混乱、占用空间 |
| DataSeeder 在应用启动时执行初始化，耦合应用 | 启动变慢、数据不可控、难以调试 |
| 日志目录嵌套复杂（log/SDK/、log/Message/） | 难以查找、清理困难 |
| Views/ViewModels 全堆在根目录 | 修改一个业务影响其他业务，稳定性差 |
| Services/Repository/Entities 无业务边界 | 单体文件（Services.cs/Repository.cs/Entities.cs）修改风险高 |

### 目标

1. **DLL 归集** — 统一到 `libs/` 目录管理
2. **初始化数据脚本化** — 移除 DataSeeder，改用 SQL 脚本在数据库手动执行
3. **日志目录简化** — `bin/Debug/logs/` 与 exe 同级
4. **业务归集** — Views/ViewModels/Services/Repositories/Entities 按业务域拆分

---

## 2. 架构设计

### 2.1 DLL 归集

**目标目录结构：**

```
TripleDetection.App/
├── bin/Debug/
│   ├── libs/                    # 归集的第三方 DLL
│   │   ├── VisionMaster/         # VM SDK DLL
│   │   ├── SQLite/               # SQLite DLL
│   │   └── Other/                # 其他第三方 DLL
│   ├── TripleDetection.App.exe
│   └── logs/                     # 日志目录（见 2.3）
└── libs/                         # 开发时 DLL 源码目录
    ├── VisionMaster/
    └── SQLite/
```

**规则：**
- VisionMaster SDK DLL 全部放入 `libs/VisionMaster/`
- SQLite 相关的 DLL 放入 `libs/SQLite/`
- 其他第三方 DLL 放入 `libs/Other/`
- 各 project 的本地 libs 目录统一引用 `../libs/`（相对路径）

**DLL 清单（从 bin/Debug 当前内容）：**
- VisionMaster SDK: `VM.PlatformSDKCS.dll`, `VM.Core.dll`, `GlobalVariableModuleCs.dll` 等 → `libs/VisionMaster/`
- SQLite: `System.Data.SQLite.dll`, `EntityFramework.dll`, `SQLite.Interop.dll` → `libs/SQLite/`
- 其他: `Newtonsoft.Json.dll`, `Apps.*.dll` 系列 → `libs/Other/`

### 2.2 初始化数据脚本化

**移除内容：**
- 删除 `DataSeeder.cs` 和 `DatabaseInitializer.SeedInitialData()`
- 删除 `TripleDetection.Data` 中所有 `InMemoryRepository` 相关代码

**替代方案：**
- 在 `docs/database/` 目录下存放 SQL 初始化脚本 `init.sql`
- 应用启动时只负责创建表结构（`EnsureDatabaseCreated`），**不**填充初始数据
- 初始数据的填充由 DBA 或运维手动执行 SQL 脚本完成

**SQL 脚本结构：**
```sql
-- docs/database/init.sql
INSERT INTO Users (Username, Password, Role, ...) VALUES ('admin', '...', 'Admin', ...);
INSERT INTO Products (Code, Name, ...) VALUES ('P001', 'OCR检测产品A', ...);
-- 等等
```

### 2.3 日志目录简化

**当前结构：**
```
bin/Debug/
├── log/
│   ├── SDK/
│   │   └── PlatformSDK.log
│   └── Message/
│       └── 2026-05-30.log
└── debug.log
```

**目标结构：**
```
bin/Debug/
├── logs/
│   ├── app.log          # 应用日志（包含 LoggingService 输出）
│   ├── sdk.log          # VM SDK 日志
│   └── audit.log        # 审计日志（可选）
└── TripleDetection.App.exe
```

**清理规则：**
- `logs/` 文件夹大小超过 500MB 时自动清理（删除 30 天前文件）
- 清理逻辑在 `LoggingService` 或 `App.OnStartup()` 中实现

### 2.4 业务归集（核心）

**按业务域拆分，每个业务域包含：**
- `Views/{Domain}/` — XAML 文件
- `ViewModels/{Domain}/` — ViewModel 文件
- `Services/{Domain}/` — 该域的业务服务
- `Repositories/{Domain}/` — 该域的 Repository
- `Entities/{Domain}/` — 该域专用的 Entity（如果有独立 Entity）

**标准业务域：**

| 业务域 | Views | ViewModels | Services | Repositories |
|--------|-------|-----------|---------|-------------|
| `System/` | SettingsView, LogsView | SettingsViewModel | SettingsService, ConfigService | SystemConfigRepository |
| `Auth/` | LoginView, UserManagementView | LoginViewModel, UserManagementViewModel | UserService | UserRepository |
| `Production/` | ProductListView, ProductEditWindow, TaskListView, TaskEditWindow | ProductListViewModel, ProductEditViewModel, TaskListViewModel, TaskEditViewModel | ProductService, TaskService | ProductRepository, ProdTaskRepository |
| `Detection/` | DetectionView, DetectionHistoryView | MainViewModel | VmIntegrationService, ImageStorageService, DetectionRecordService | DetectionRecordRepository |
| `Audit/` | AuditLogView | AuditLogViewModel | AuditLogService | AuditLogRepository |

**跨域共享：**
- `Entities/` 根目录放置共享基础实体（`BaseEntity.cs`, `ValidType.cs`, `TaskStatus.cs`, `ProductStatus.cs`）
- `Repositories/Infrastructure/` 放置通用接口和基类（`IRepository.cs`, `SqliteRepository.cs`）
- `Services/` 根目录放置跨域共享服务（`SessionManager.cs`, `LoggingService.cs`）

**需要从单体文件拆分的内容：**
1. `Services.cs` → 拆分为 `Auth/UserService.cs` + `Production/ProductService.cs` + `Production/TaskService.cs` + `Audit/AuditLogService.cs`
2. `Repository.cs` → `InMemoryRepository` 移除（不再需要），通用 `SqliteRepository<T>` 移入 `Infrastructure/`
3. `Entities.cs` → 拆分为 `User.cs` + `Product.cs` + `ProdTask.cs` + `DetectionRecord.cs` + `SystemConfig.cs` + `AuditLog.cs` + 共享枚举文件

---

## 3. 实施步骤

### Phase 1: DLL 归集（低风险）
1. 创建 `libs/VisionMaster/`、`libs/SQLite/`、`libs/Other/` 文件夹
2. 移动 DLL 到对应子文件夹
3. 更新各 `.csproj` 的 HintPath 引用
4. 删除不再需要的本地 libs 目录

### Phase 2: 初始化数据脚本化（低风险）
1. 创建 `docs/database/init.sql`
2. 删除 `DataSeeder.cs`
3. 删除 `DatabaseInitializer.SeedInitialData()` 调用
4. 删除 `InMemoryRepository.cs`

### Phase 3: 日志目录简化（低风险）
1. 统一 `LoggingService` 输出到 `bin/Debug/logs/app.log`
2. 添加启动时日志清理逻辑

### Phase 4: 业务归集（高风险，需精细执行）
1. 创建各业务域文件夹结构
2. 从 `Entities.cs` 拆分出各 Entity 独立文件
3. 从 `Services.cs` 拆分出各 Service 独立文件
4. 移动 Views 和 ViewModels 到对应业务域文件夹
5. 更新所有 `using` 引用和命名空间
6. 更新 `TripleDetection.App.csproj` 的文件包含

---

## 4. 关键文件变更清单

| 操作 | 文件 |
|------|------|
| 拆分 | `Entities.cs` → `User.cs`, `Product.cs`, `ProdTask.cs`, `DetectionRecord.cs`, `SystemConfig.cs`, `AuditLog.cs` |
| 拆分 | `Services.cs` → `UserService.cs`, `ProductService.cs`, `TaskService.cs`, `AuditLogService.cs` |
| 移动 | 所有 View XAML/CS → `Views/{Domain}/` |
| 移动 | 所有 ViewModel CS → `ViewModels/{Domain}/` |
| 新建 | `docs/database/init.sql` |
| 删除 | `DataSeeder.cs` |
| 删除 | `InMemoryRepository.cs`（含 `Repository.cs` 中的 `InMemoryRepository` 实现） |
| 重构 | `LoggingService` → 输出到 `logs/` 目录 |

---

## 5. 验证标准

- [ ] 编译通过，0 errors
- [ ] 应用启动正常，不卡死
- [ ] DLL 全部在 `libs/` 子目录下，无重复
- [ ] 产品管理和任务管理的增删改查正常工作
- [ ] 日志文件写入 `bin/Debug/logs/` 目录
- [ ] Views/ViewModels 按业务域归集，无交叉影响
- [ ] 命名空间全部更新为 `TripleDetection.{Domain}`

---

## 6. 相关文档

- [系统架构文档](./2026-05-29-system-architecture.md)
