# 审计日志系统实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 构建双日志架构 — 检测执行日志（DetectionRecord）高频持久化 + 业务审计日志（AuditLog）显式记录查询导出，支持 FDA 21 CFR Part 11 审计追踪

**Architecture:**
- **DetectionRecord** — VmIntegrationService 内部回调保存，异常不阻塞检测流程，写入 SQLite
- **AuditLog** — 各服务方法显式调用 `_auditLog.Log()`，通过 IAuditLogRepository 持久化，支持分页查询和全量导出
- **SessionManager** — 静态单例提供当前用户上下文（默认 system），登录实现后替换为真实用户

**Tech Stack:** WPF (.NET Framework 4.8), C# 8.0, EF Core + SQLite, MVVM

---

## Context

FDA 21 CFR Part 11 要求药企系统具备严格的审计追踪日志。当前系统存在以下问题：
1. `AuditLog` 实体存在但 in-memory only，从未被调用
2. `DetectionRecord` 实体存在但 DetectionView 中未持久化（结果仅存内存 UI 列表）
3. 配置变更、用户管理、任务审批操作均无审计记录
4. 日志为平面文本文件，无结构化字段，无法查询

本计划实现：
- **DetectionRecord** — 每次检测结果持久化，VmIntegrationService 内部保存，异常吞噬
- **AuditLog** — 所有业务操作（登录/登出/CRUD/审批/配置变更）显式记录，支持查询和导出
- **SessionManager** — 用户上下文骨架（默认 system 用户）

---

## 关键文件

| 文件 | 作用 |
|-----|-----|
| `TripleDetection.Data/Entities/Entities.cs` | 实体定义（含现有 DetectionRecord、AuditLog） |
| `TripleDetection.Data/BaseEntity.cs` | 抽象基类 |
| `TripleDetection.Data/Repositories/Repository.cs` | 通用仓储接口 IPagedResult、PagedQuery、查询条件类 |
| `TripleDetection.Data/Repositories/Sqlite/SqliteDbContext.cs` | EF DbContext |
| `TripleDetection.Data/Repositories/Sqlite/SqliteRepository.cs` | SQLite 仓储基类 |
| `TripleDetection.Data/Repositories/Configuration/DetectionRecordConfiguration.cs` | DetectionRecord EF 配置 |
| `TripleDetection.App/Services/VmIntegrationService.cs` | 检测服务，DetectionRecord 保存点 |
| `TripleDetection.App/Services/LoggingService.cs` | 现有 LoggingService（文本日志，非审计） |
| `TripleDetection.App/Views/DetectionView.xaml.cs` | 检测 UI |

---

## 文件结构（变更）

```
TripleDetection.Data/
  Entities/Entities.cs                    [修改] DetectionRecord 增加 BatchNumber/ProductId/ElapsedMs/IsOK；AuditLog 继承 BaseEntity
  BaseEntity.cs                          [不变]

  Repositories/
    Repository.cs                        [修改] 增加 AuditLogQuery 和 DetectionRecordQuery
    Sqlite/
      SqliteDbContext.cs                [修改] 增加 AuditLog DbSet
      SqliteRepository.cs               [修改] 增加 Query(AuditLogQuery) 和 Query(DetectionRecordQuery)
    Configuration/
      AuditLogConfiguration.cs          [新建] AuditLog EF 配置
      DetectionRecordConfiguration.cs   [修改] 增加新字段配置
    AuditLogRepository.cs               [新建] IAuditLogRepository 接口 + 实现
    DetectionRecordRepository.cs        [新建] IDetectionRecordRepository 接口 + 实现

TripleDetection.Services/
  Services.cs                            [修改] AuditLogService 改造为持久化；各服务注入审计
  SessionManager.cs                     [新建] 静态用户上下文

TripleDetection.App/
  Services/
    VmIntegrationService.cs             [修改] 注入 DetectionRecordService，保存时异常吞噬
    LoggingService.cs                   [不变]
  Views/
    AuditLogView.xaml                   [新建] 审计日志查询 UI
    AuditLogView.xaml.cs
    DetectionHistoryView.xaml           [新建] 检测历史记录 UI
    DetectionHistoryView.xaml.cs
```

---

## 实现步骤

### Task 1: DetectionRecord 实体增强

**Files:**
- Modify: `TripleDetection.Data/Entities/Entities.cs:72-84`
- Modify: `TripleDetection.Data/Repositories/Configuration/DetectionRecordConfiguration.cs`

- [ ] **Step 1: 修改 DetectionRecord 实体**
- [ ] **Step 2: 更新 EF 配置**
- [ ] **Step 3: 编译验证**
- [ ] **Step 4: Git 提交**

---

### Task 2: AuditLog 实体改造 + Repository

**Files:**
- Modify: `TripleDetection.Data/Entities/Entities.cs:100-111`
- Create: `TripleDetection.Data/Repositories/Configuration/AuditLogConfiguration.cs`
- Create: `TripleDetection.Data/Repositories/AuditLogRepository.cs`
- Modify: `TripleDetection.Data/Repositories/Sqlite/SqliteDbContext.cs`
- Modify: `TripleDetection.Data/Repositories/Repository.cs`

- [ ] **Step 1: 改造 AuditLog 实体继承 BaseEntity**
- [ ] **Step 2: 创建 AuditLogQuery 查询条件类**
- [ ] **Step 3: 创建 AuditLogConfiguration**
- [ ] **Step 4: 创建 IAuditLogRepository 接口**
- [ ] **Step 5: 创建 AuditLogRepository 实现**
- [ ] **Step 6: 更新 SqliteDbContext 添加 AuditLog DbSet**
- [ ] **Step 7: 更新 SqliteRepository 增加 Query 重载**
- [ ] **Step 8: 编译验证**
- [ ] **Step 9: Git 提交**

---

### Task 3: SessionManager 用户上下文

**Files:**
- Create: `TripleDetection.Services/SessionManager.cs`

- [ ] **Step 1: 创建 SessionManager.cs**
- [ ] **Step 2: 编译验证**
- [ ] **Step 3: Git 提交**

---

### Task 4: AuditLogService 改造为持久化

**Files:**
- Modify: `TripleDetection.Services/Services.cs`

- [ ] **Step 1: 改造 AuditLogService**
- [ ] **Step 2: 编译验证**
- [ ] **Step 3: Git 提交**

---

### Task 5: 各服务注入 AuditLog 审计

**Files:**
- Modify: `TripleDetection.Services/Services.cs`

- [ ] **Step 1: 在 UserService 方法中添加审计调用**
- [ ] **Step 2: 在 TaskService 方法中添加审计调用**
- [ ] **Step 3: 在 ProductService 方法中添加审计调用**
- [ ] **Step 4: 在 ConfigService 中添加审计调用**
- [ ] **Step 5: 编译验证**
- [ ] **Step 6: Git 提交**

---

### Task 6: VmIntegrationService 保存 DetectionRecord

**Files:**
- Modify: `TripleDetection.App/Services/VmIntegrationService.cs`

- [ ] **Step 1: 注入 DetectionRecordRepository 并保存记录**
- [ ] **Step 2: 编译验证**
- [ ] **Step 3: Git 提交**

---

### Task 7: AuditLogView 审计日志查询 UI

**Files:**
- Create: `TripleDetection.App/Views/AuditLogView.xaml`
- Create: `TripleDetection.App/Views/AuditLogView.xaml.cs`

- [ ] **Step 1: 创建 AuditLogView.xaml**
- [ ] **Step 2: 创建 AuditLogView.xaml.cs**
- [ ] **Step 3: 将 AuditLogView 添加到主导航**
- [ ] **Step 4: 编译验证**
- [ ] **Step 5: Git 提交**

---

### Task 8: DetectionHistoryView 检测历史记录 UI

**Files:**
- Create: `TripleDetection.App/Views/DetectionHistoryView.xaml`
- Create: `TripleDetection.App/Views/DetectionHistoryView.xaml.cs`

- [ ] **Step 1: 创建 DetectionHistoryView.xaml**
- [ ] **Step 2: 创建 DetectionHistoryView.xaml.cs**
- [ ] **Step 3: 编译验证**
- [ ] **Step 4: Git 提交**

---

### Task 9: RepositoryFactory 注册新仓储

**Files:**
- Modify: `TripleDetection.Data/Repositories/Sqlite/SqliteRepositoryFactory.cs`
- Create: `TripleDetection.Data/Repositories/DetectionRecordRepository.cs`
- Modify: `TripleDetection.Services/Services.cs`

- [ ] **Step 1: 注册 IAuditLogRepository 和 IDetectionRecordRepository**
- [ ] **Step 2: 创建 DetectionRecordRepository 实现**
- [ ] **Step 3: 创建 DetectionRecordService**
- [ ] **Step 4: 编译验证**
- [ ] **Step 5: Git 提交**

---

## 验证标准

- [ ] 编译通过，0 errors
- [ ] DetectionRecord 持久化：检测运行后，数据库中 DetectionRecords 表有新记录
- [ ] AuditLog 持久化：用户管理/任务管理操作后，AuditLogs 表有新记录
- [ ] AuditLogView 查询：按时间范围/用户/操作类型/关键词筛选正常
- [ ] AuditLog 导出：CSV 包含所有满足条件的数据（非仅当前页）
- [ ] DetectionHistoryView 展示历史检测记录，分页正常
- [ ] VmIntegrationService 异常时检测流程不中断
- [ ] SessionManager 默认 system 用户

---

## Task 依赖图

```
Task 1 (DetectionRecord 实体增强)
    ↓
Task 2 (AuditLog Repository + Query) ← 需 Task 1 完成
    ↓
Task 3 (SessionManager)              ← 独立
    ↓
Task 4 (AuditLogService 改造)        ← 需 Task 2
    ↓
Task 5 (各服务审计注入)              ← 需 Task 4
    ↓
Task 6 (VmIntegrationService 保存)  ← 需 Task 1, 9
    ↓
Task 7 (AuditLogView)                ← 需 Task 4
    ↓
Task 8 (DetectionHistoryView)        ← 需 Task 1, 9
    ↓
Task 9 (RepositoryFactory 注册)      ← 需 Task 2
```