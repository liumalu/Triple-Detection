# 系统清理与归集实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 系统清理（DLL归集、移除DataSeeder、简化日志） + 按业务域归集（Views/ViewModels/Services/Repositories/Entities 重构）

**Architecture:**
- Phase 1-3: 基础设施清理（DLL、日志、初始化数据）— 低风险，文件操作为主
- Phase 4: 业务归集 — 高风险，涉及命名空间重构和跨文件引用更新

**Tech Stack:** .NET Framework 4.8, WPF, Entity Framework 6 + SQLite, VisionMaster SDK

---

## 文件结构映射

### 当前状态 vs 目标状态

| 层级 | 当前 | 目标 |
|------|------|------|
| DLL | `bin/Debug/` 散落 + `libs/` 部分归集 | `bin/Debug/libs/` + `bin/Debug/x86/x64/`（SQLite） |
| 初始化 | `DataSeeder.cs` 启动时执行 | 删除，`docs/database/init.sql` 替代 |
| 日志 | `log/SDK/` + `log/Message/` 嵌套 | `logs/` 与 exe 同级 |
| Entities | `Entities.cs` 单体（~300行） | `Entities/{Domain}/` 每实体独立文件 |
| Services | `Services.cs` 单体 | `Services/{Domain}/` 每服务独立文件 |
| Repositories | `Repository.cs` 含 InMemory | `InMemory` 删除，`SqliteRepository.cs` 移入 Infrastructure |
| Views | `Views/` 平铺 | `Views/{Domain}/` 按业务域归集 |
| ViewModels | `ViewModels/` 平铺 | `ViewModels/{Domain}/` 按业务域归集 |

### 业务域划分

| 域 | Views | ViewModels | Services | Repositories |
|---|-------|-----------|---------|-------------|
| `Auth/` | LoginView, UserManagementView, UserEditWindow | UserManagementViewModel, UserEditViewModel | UserService | UserRepository |
| `Production/` | ProductListView, ProductEditWindow, TaskListView, TaskEditWindow | ProductListViewModel, ProductEditViewModel, TaskListViewModel, TaskEditViewModel | ProductService, TaskService | ProductRepository, ProdTaskRepository |
| `Detection/` | DetectionView, DetectionHistoryView | MainViewModel, DetectionHistoryViewModel | VmIntegrationService, DetectionRecordService, ImageStorageService | DetectionRecordRepository |
| `Audit/` | AuditLogView | AuditLogViewModel | AuditLogService | AuditLogRepository |
| `System/` | SettingsView, LogsView, DashboardView | SettingsShellViewModel | SettingsService, ConfigService, LoggingService, TabManager | SystemConfigRepository |

### 共享基础设施（非业务域）

| 文件 | 位置 | 说明 |
|------|------|------|
| `BaseEntity.cs` | `Entities/` | 所有实体的基类 |
| `IRepository.cs` | `Repositories/Infrastructure/` | 通用仓储接口 |
| `SqliteRepository.cs` | `Repositories/Infrastructure/` | SQLite 通用实现 |
| `SqliteDbContext.cs` | `Repositories/Infrastructure/` | 唯一的 DbContext |
| `SqliteRepositoryFactory.cs` | `Repositories/Infrastructure/` | 工厂类 |
| `SessionManager.cs` | `Services/` | 用户上下文（共享） |
| `TabConverters.cs` | `Converters/` | 转换器 |

---

## Phase 1: DLL 归集

**目标:** 将 `bin/Debug/` 下散落的 DLL 归集到 `libs/` 子目录。

**当前 `bin/Debug/` 关键 DLL（需移动）:**
- VisionMaster SDK DLL（VM.Core.dll, VM.PlatformSDKCS.dll 等）→ `libs/VisionMaster/`
- SQLite DLL（System.Data.SQLite.dll, EntityFramework.dll, SQLite.Interop.dll）→ `libs/SQLite/`
- 其他第三方 DLL（OpenCvSharp, MVDCore, log4net, Newtonsoft.Json 等）→ `libs/Other/`

**当前 `libs/` 已有 27 个 DLL（已归集），保持不变。**

**规则:**
- `bin/Debug/x86/` 和 `bin/Debug/x64/` 保留（SQLite.Interop.dll 特殊，x86/x64 架构分开）
- 各 `.csproj` 的 HintPath 引用路径需同步更新

### DLL 归集任务

- [ ] **Task 1: 创建 libs 子目录**
  - 创建: `TripleDetection.App/bin/Debug/libs/VisionMaster/`
  - 创建: `TripleDetection.App/bin/Debug/libs/SQLite/`
  - 创建: `TripleDetection.App/bin/Debug/libs/Other/`
  - 创建: `TripleDetection.App/bin/Debug/libs/Apps/`
  - 创建: `TripleDetection.App/bin/Debug/libs/GateWay/`
  - 创建: `TripleDetection.App/bin/Debug/libs/Frontend/`
  - 创建: `TripleDetection.App/bin/Debug/libs/ICSharpCode/`
  - 创建: `TripleDetection.App/bin/Debug/libs/VMControls/`

- [ ] **Task 2: 移动 DLL 到 libs 子目录**
  - 移动: 所有 VisionMaster SDK DLL → `libs/VisionMaster/`
  - 移动: `System.Data.SQLite.dll`, `System.Data.SQLite.EF6.dll` → `libs/SQLite/`
  - 移动: `EntityFramework.dll`, `EntityFramework.SqlServer.dll` → `libs/SQLite/`
  - 移动: `OpenCvSharp*.dll`, `MVDCore.Net.dll`, `MVDImage.Net.dll` → `libs/Other/`
  - 移动: `Apps.*.dll` → `libs/Apps/`
  - 移动: `GateWay*.dll` → `libs/GateWay/`
  - 移动: `Frontend*.dll` → `libs/Frontend/`
  - 移动: `ICSharpCode.*.dll` → `libs/ICSharpCode/`
  - 移动: `VMControls.*.dll` → `libs/VMControls/`

- [ ] **Task 3: 更新 HintPath 引用**
  - 修改: `TripleDetection.App.csproj` 中所有 DLL 的 HintPath，从绝对路径改为 `..\libs\...`

---

## Phase 2: 初始化数据脚本化

**目标:** 移除 DataSeeder，应用启动不再填充初始数据。

- [ ] **Task 4: 创建 SQL 初始化脚本**
  - 创建: `docs/database/init.sql`
  - 内容: 包含 Users、Products、ProdTasks 初始数据的 INSERT 语句

- [ ] **Task 5: 删除 DataSeeder.cs**
  - 删除: `TripleDetection.Services/DataSeeder.cs`
  - 修改: `TripleDetection.Services/Services.cs` — 移除 `DataSeeder` 引用

- [ ] **Task 6: 移除 InMemoryRepository**
  - 删除: `TripleDetection.Data/Repositories/Repository.cs` 中的 `InMemoryRepository<T>` 实现
  - 删除: `TripleDetection.Data/Repositories/Repository.cs` 中的相关 using
  - 检查: `IRepository<T>` 接口是否仍在 Repository.cs，如是则拆分到独立文件 `Repositories/Infrastructure/IRepository.cs`
  - 修改: 所有引用 `InMemoryRepository` 的地方（`SqliteRepositoryFactory.cs` 中无引用，Services 中可能有）

---

## Phase 3: 日志目录简化

**目标:** 日志统一输出到 `bin/Debug/logs/`。

- [ ] **Task 7: 统一日志目录**
  - 修改: `TripleDetection.App/Services/LoggingService.cs` — `Log` 目录改为 `logs`
  - 修改: `TripleDetection.App/Services/VmIntegrationService.cs` — 日志路径同步更新
  - 规则: `logs/app.log`（应用日志），`logs/sdk.log`（SDK 日志）
  - 删除: 旧的 `log/` 目录结构

- [ ] **Task 8: 添加日志清理逻辑**
  - 修改: `LoggingService` 或 `App.OnStartup()` — 启动时清理 30 天前日志文件

---

## Phase 4: 业务归集（高风险）

### Task 9: 拆分 Entities.cs

- [ ] 拆分 `Entities/Entities.cs` 为独立文件：
  - 创建: `Entities/User.cs` — User 实体
  - 创建: `Entities/Product.cs` — Product 实体
  - 创建: `Entities/ProdTask.cs` — ProdTask 实体（重命名自 Task）
  - 创建: `Entities/DetectionRecord.cs` — DetectionRecord 实体
  - 创建: `Entities/SystemConfig.cs` — SystemConfig 实体
  - 创建: `Entities/AuditLog.cs` — AuditLog 实体
  - 创建: `Entities/Enums.cs` — 所有枚举（TaskStatus, ProductStatus, ValidType 等）

- [ ] 更新所有 using 引用：项目中有 `using TripleDetection.Data.Entities` 的文件全部更新为具体命名空间

### Task 10: 拆分 Services.cs

- [ ] 拆分 `Services/Services.cs` 为独立文件：
  - 创建: `Services/Auth/UserService.cs`
  - 创建: `Services/Production/ProductService.cs`
  - 创建: `Services/Production/TaskService.cs`
  - 创建: `Services/Audit/AuditLogService.cs`

- [ ] 更新 `using` 命名空间：所有引用 Services.cs 中各服务的文件更新 using 路径

### Task 11: 移动 Views 到业务域文件夹

- [ ] 移动并更新命名空间：
  - Auth: `UserManagementView`, `UserEditWindow`
  - Production: `ProductListView`, `ProductEditWindow`, `TaskListView`, `TaskEditWindow`
  - Detection: `DetectionView`, `DetectionHistoryView`
  - Audit: `AuditLogView`
  - System: `SettingsView`, `LogsView`, `DashboardView`

### Task 12: 移动 ViewModels 到业务域文件夹

- [ ] 移动并更新命名空间：
  - Auth: `UserManagementViewModel`, `UserEditViewModel`
  - Production: `ProductListViewModel`, `ProductEditViewModel`, `TaskListViewModel`, `TaskEditViewModel`
  - Detection: `MainViewModel`, `DetectionHistoryViewModel`
  - Audit: `AuditLogViewModel`
  - System: `SettingsShellViewModel`

### Task 13: 移动 Services 到业务域文件夹

- [ ] 当前 `Services/Settings/` 已存在（CommunicationSettingsService 等），保持现状
- [ ] 移动 `VmIntegrationService.cs`, `ImageStorageService.cs`, `DetectionRecordService.cs` → `Services/Detection/`
- [ ] 移动 `LoggingService.cs`, `TabManager.cs`, `SessionManager.cs` → `Services/System/`
- [ ] 更新所有 using 引用

### Task 14: 清理重复/废弃文件

- [ ] 删除: `Services/UserService.cs`（已被 Auth/UserService.cs 替代）
- [ ] 删除: `Services/SimpleJsonHelper.cs`（不再需要）
- [ ] 删除: `Data/SimpleJsonHelper.cs`（不再需要）
- [ ] 删除: `Models/` 目录下与 Entities 重复的模型文件（需确认哪些确实重复）

### Task 15: 更新 csproj 文件包含

- [ ] 更新 `TripleDetection.App.csproj` — 所有 `<Compile Include="...">` 路径更新为新文件夹结构
- [ ] 更新 `TripleDetection.Services.csproj` — 同上
- [ ] 更新 `TripleDetection.Data.csproj` — 同上

---

## 验证步骤

每个 Phase 完成后执行：

### Phase 1 验证
- [ ] `dotnet build` 通过，0 errors
- [ ] 应用启动正常，VM SDK DLL 加载无报错

### Phase 2 验证
- [ ] `dotnet build` 通过
- [ ] 应用启动不卡死（无 DataSeeder 调用）
- [ ] 产品/任务管理 CRUD 正常（走 SQLite）

### Phase 3 验证
- [ ] 日志文件写入 `bin/Debug/logs/` 目录
- [ ] `logs/` 下有 `app.log` 和 `sdk.log`

### Phase 4 验证
- [ ] `dotnet build` 通过，0 errors
- [ ] 产品管理增删改查正常
- [ ] 任务管理增删改查正常
- [ ] 检测页面加载 VM 方案正常
- [ ] 审计日志正常记录

---

## 执行顺序

1. **Phase 1** (Task 1-3): DLL 归集 — 低风险，基础设施准备
2. **Phase 2** (Task 4-6): 初始化数据脚本化 + 移除 InMemory — 低风险，清理废弃代码
3. **Phase 3** (Task 7-8): 日志目录简化 — 低风险，改善可维护性
4. **Phase 4** (Task 9-15): 业务归集 — 高风险，按 Task 顺序执行，每 Task 后编译验证

---

## 关键风险

1. **命名空间更新** — Phase 4 中最大风险，20+ 文件的 using 引用需要逐个更新
2. **DLL 路径更新** — HintPath 从绝对路径改相对路径，容易遗漏
3. **csproj 文件包含** — WPF 项目文件包含规则复杂，文件移动后需精确更新

**缓解措施:** 每个 Task 后立即编译验证，发现问题立即回退该 Task 的修改。
