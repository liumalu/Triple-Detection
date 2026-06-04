# Triple-Detection 审计功能增强设计文档

**日期:** 2026-06-04
**版本:** 1.0
**状态:** 已批准

---

## 1. 概述

### 1.1 目标

为 Triple-Detection 系统增加完整的审计功能，满足以下三个目标：
- **合规审计** — 满足行业法规要求（如 ISO 13485 医疗器械质量管理体系），需要完整的操作追溯能力
- **生产质量分析** — 通过检测记录统计合格率、趋势分析，帮助优化生产工艺
- **系统运维监控** — 监控用户行为、检测系统稳定性，及时发现异常

### 1.2 范围

- 系统业务操作日志（用户登录、产品管理、任务审批等）
- 检测任务执行记录（OK/NG、批次号、耗时等）
- 两者的统计分析（趋势图、分布图、汇总报表）

---

## 2. 架构设计

### 2.1 架构概览

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation 层                          │
│  AuditLogView  │  StatisticsView  │  DashboardView (增强) │
└────────┬───────────────┬─────────────────────┬─────────────┘
         │               │                     │
         ▼               ▼                     ▼
┌─────────────────────────────────────────────────────────────┐
│                   Application 层                            │
│  AuditLogService │ DetectionRecordService │ StatisticsService (NEW) │
└────────┬───────────────┬─────────────────────┬─────────────┘
         │               │                     │
         ▼               ▼                     ▼
┌─────────────────────────────────────────────────────────────┤
│                    Domain 层                                │
│  AuditLog (增强) │ DetectionRecord (增强) │ 统计结果模型   │
└─────────────────────────────────────────────────────────────┘
         │               │                     │
         ▼               ▼                     ▼
┌─────────────────────────────────────────────────────────────┤
│                 Infrastructure 层                           │
│  AuditLogRepository │ DetectionRecordRepository │ 查询优化 │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 StatisticsService 职责

- 封装所有统计分析逻辑（操作日志统计 + 检测记录统计）
- 提供统计查询接口，供 Presentation 层 ViewModel 调用
- 不涉及数据写入（写入由 AuditLogService / DetectionRecordService 负责）

### 2.3 设计原则

- 写操作（Log）保持现状，复用现有服务
- 读操作（Query/Statistics）由 StatisticsService 统一提供
- 符合 DDD 四层架构：Application 层不直接依赖 Infrastructure

---

## 3. 数据模型

### 3.1 操作日志规范（AuditLog）

#### Action 枚举值

| 分类 | Action 值 | 说明 |
|------|-----------|------|
| **认证** | `LOGIN` | 用户登录成功 |
| | `LOGOUT` | 用户登出 |
| | `LOGIN_FAILED` | 登录失败 |
| **产品管理** | `PRODUCT_CREATE` | 新建产品 |
| | `PRODUCT_UPDATE` | 更新产品 |
| | `PRODUCT_DELETE` | 删除产品 |
| **任务管理** | `TASK_CREATE` | 新建任务 |
| | `TASK_UPDATE` | 更新任务 |
| | `TASK_APPROVE` | 审批任务 |
| | `TASK_START` | 启动任务 |
| | `TASK_COMPLETE` | 完成任务 |
| **检测执行** | `DETECTION_RUN` | 执行单次检测 |
| | `DETECTION_CONTINUOUS_START` | 启动连续检测 |
| | `DETECTION_CONTINUOUS_STOP` | 停止连续检测 |
| **系统设置** | `SETTINGS_UPDATE` | 更新系统设置 |
| **审计查询** | `AUDIT_LOG_QUERY` | 查询审计日志 |
| **导出** | `EXPORT_AUDIT_LOG` | 导出审计日志 |
| | `EXPORT_DETECTION_RECORD` | 导出检测记录 |

#### ObjectType 枚举值

| ObjectType | 说明 |
|------------|------|
| `User` | 用户对象 |
| `Product` | 产品对象 |
| `ProdTask` | 生产任务对象 |
| `Detection` | 检测执行记录 |
| `SystemConfig` | 系统配置 |
| `AuditLog` | 审计日志本身 |

#### Details JSON 结构规范

```json
// 登录
{ "ip": "192.168.1.100", "browser": "WPF-App" }

// 任务审批
{ "taskId": 1, "taskName": "OCR检测任务", "fromStatus": "Pending", "toStatus": "Approved" }

// 检测执行
{ "taskId": 1, "result": "OK", "batchNumber": "BN-2025-001", "elapsedMs": 1250 }

// 导出
{ "format": "Excel", "recordCount": 150, "filter": { "dateRange": "2025-06-01~2025-06-30" } }
```

### 3.2 混合存储模式

为平衡查询性能与灵活性，采用混合存储模式：

| 字段类型 | 存储方式 | 原因 |
|----------|----------|------|
| `Action`, `ObjectType`, `ObjectId`, `CreateAt` | 独立列 + 索引 | 高频查询条件 |
| `FromStatus`, `ToStatus`, `RelatedRecordId` | 独立列 | 常见统计分析维度 |
| `Details` | JSON | 扩展信息、前端展示、特殊分析 |

### 3.3 数据库 Schema 变更

#### AuditLog 表新增字段

```sql
ALTER TABLE AuditLogs ADD COLUMN FromStatus TEXT;
ALTER TABLE AuditLogs ADD COLUMN ToStatus TEXT;
ALTER TABLE AuditLogs ADD COLUMN RelatedRecordId INTEGER;

CREATE INDEX idx_auditlogs_action_date ON AuditLogs(Action, CreateAt);
CREATE INDEX idx_auditlogs_user_date ON AuditLogs(UserId, CreateAt);
CREATE INDEX idx_auditlogs_object ON AuditLogs(ObjectType, ObjectId);
```

#### DetectionRecord 表新增字段

```sql
ALTER TABLE DetectionRecords ADD COLUMN TaskName TEXT;
ALTER TABLE DetectionRecords ADD COLUMN ProductName TEXT;
ALTER TABLE DetectionRecords ADD COLUMN ProductCode TEXT;

CREATE INDEX idx_detectionrecords_date ON DetectionRecords(DetectionTime);
CREATE INDEX idx_detectionrecords_task_date ON DetectionRecords(TaskId, DetectionTime);
```

---

## 4. 统计分析接口

### 4.1 IStatisticsService 接口

```csharp
public interface IStatisticsService
{
    // 操作日志统计
    UserActivityStatistics GetUserActivityStats(int userId, DateTime startDate, DateTime endDate);
    IEnumerable<ActionDistribution> GetActionDistribution(DateTime startDate, DateTime endDate);
    IEnumerable<DailyOperationTrend> GetDailyOperationTrend(DateTime startDate, DateTime endDate);
    TaskStatusTransitionStatistics GetTaskStatusTransitions(DateTime startDate, DateTime endDate);

    // 检测记录统计
    DailyDetectionSummary GetDailyDetectionSummary(DateTime date);
    PassRateStatistics GetPassRateStatistics(DateTime startDate, DateTime endDate, int? taskId = null);
    IEnumerable<DailyPassRateTrend> GetDailyPassRateTrend(DateTime startDate, DateTime endDate, int? taskId = null);
    IEnumerable<ProductDetectionStatistics> GetProductStatistics(DateTime startDate, DateTime endDate);
    DetectionTimeStatistics GetDetectionTimeStatistics(DateTime startDate, DateTime endDate, int? taskId = null);
}
```

### 4.2 统计结果模型

```csharp
public class UserActivityStatistics
{
    public int UserId { get; set; }
    public string UserName { get; set; }
    public int TotalOperations { get; set; }
    public int LoginCount { get; set; }
    public int TaskOperations { get; set; }
    public int DetectionOperations { get; set; }
    public DateTime LastActivityAt { get; set; }
}

public class ActionDistribution
{
    public string Action { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class DailyOperationTrend
{
    public DateTime Date { get; set; }
    public int TotalCount { get; set; }
    public Dictionary<string, int> ActionBreakdown { get; set; }
}

public class TaskStatusTransitionStatistics
{
    public int TotalTransitions { get; set; }
    public Dictionary<string, int> TransitionCounts { get; set; }
    public string MostCommonTransition { get; set; }
}

public class DailyDetectionSummary
{
    public DateTime Date { get; set; }
    public int TotalDetections { get; set; }
    public int OkCount { get; set; }
    public int NgCount { get; set; }
    public double PassRate { get; set; }
    public double AverageElapsedMs { get; set; }
}

public class PassRateStatistics
{
    public int TotalCount { get; set; }
    public int OkCount { get; set; }
    public int NgCount { get; set; }
    public double PassRate { get; set; }
    public double MinPassRate { get; set; }
    public double MaxPassRate { get; set; }
}

public class DailyPassRateTrend
{
    public DateTime Date { get; set; }
    public int Total { get; set; }
    public int Ok { get; set; }
    public int Ng { get; set; }
    public double PassRate { get; set; }
}

public class ProductDetectionStatistics
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public string ProductCode { get; set; }
    public int TotalDetections { get; set; }
    public int OkCount { get; set; }
    public int NgCount { get; set; }
    public double PassRate { get; set; }
}

public class DetectionTimeStatistics
{
    public double AverageElapsedMs { get; set; }
    public double MinElapsedMs { get; set; }
    public double MaxElapsedMs { get; set; }
    public Dictionary<string, double> ByTaskType { get; set; }
}
```

---

## 5. UI 设计

### 5.1 导航结构

```
MainWindow 侧边栏
├── 首页（Dashboard）
├── 检测管理
├── 产品管理
├── 任务管理
├── 审计统计
│   ├── 操作日志      → AuditLogView
│   ├── 检测记录      → DetectionHistoryView（已有）
│   └── 统计分析      → StatisticsView (NEW)
├── 系统设置
└── 用户管理
```

### 5.2 AuditLogView 布局

- 顶部：查询条件（日期范围、用户、操作类型、关键字）
- 中部：操作类型分布（饼图）+ 每日操作趋势（折线图）
- 底部：日志列表（DataGrid 分页展示）

### 5.3 StatisticsView 布局

- 顶部：时间范围筛选 + 刷新按钮
- 概览区：合格率、检测耗时统计卡片
- 图表区：合格率趋势（折线图）、产品维度统计（柱状图）

### 5.4 DashboardView 增强

- 使用真实的 `StatisticsService` 数据
- 今日 OK/NG/合格率/待审核 统计卡片
- 最近 7 天合格率趋势图
- 最近检测记录列表

---

## 6. 审计日志记录点

| 模块 | 文件 | 记录操作 |
|------|------|----------|
| 登录 | `LoginViewModel.cs` | `LOGIN`, `LOGIN_FAILED`, `LOGOUT` |
| 产品 | `ProductEditViewModel.cs` | `PRODUCT_CREATE`, `PRODUCT_UPDATE`, `PRODUCT_DELETE` |
| 任务 | `TaskEditViewModel.cs` | `TASK_CREATE`, `TASK_UPDATE`, `TASK_APPROVE`, `TASK_START`, `TASK_COMPLETE` |
| 检测 | `DetectionView.xaml.cs` | `DETECTION_RUN`, `DETECTION_CONTINUOUS_START`, `DETECTION_CONTINUOUS_STOP` |
| 设置 | `*SettingsViewModel.cs` | `SETTINGS_UPDATE` |
| 导出 | 各 Export 方法 | `EXPORT_AUDIT_LOG`, `EXPORT_DETECTION_RECORD` |

---

## 7. 实施计划

### Phase 1: 数据库 & 基础设施（1-2天）
- [ ] 修改 `AuditLog` 实体，新增 `FromStatus`, `ToStatus`, `RelatedRecordId` 字段
- [ ] 修改 `DetectionRecord` 实体，新增 `TaskName`, `ProductName`, `ProductCode` 字段
- [ ] 更新 `DatabaseInitializer.cs`，包含新字段和索引
- [ ] 更新 EF Fluent API 配置（如果有使用）

### Phase 2: 应用层服务（1-2天）
- [ ] 新增 `StatisticsService` + `IStatisticsService`
- [ ] 新增所有统计结果模型（`*Statistics`, `*Trend` 类）
- [ ] 在现有 Service 中添加审计日志记录点

### Phase 3: 展示层 UI（2-3天）
- [ ] 完整实现 `AuditLogView.xaml`
- [ ] 新增 `StatisticsView.xaml`
- [ ] 增强 `DashboardView`
- [ ] 新增 `StatisticsViewModel`

### Phase 4: 集成测试 & 修复（1天）
- [ ] 端到端测试完整审计流程
- [ ] 性能测试（大数据量下的统计查询）
- [ ] Bug 修复

**预计总工时：5-8 人天**

---

## 8. 依赖关系

```
Phase 1 (DB) 
    ↓
Phase 2 (App Services) ← 需要 Phase 1 完成
    ↓
Phase 3 (UI) ← 需要 Phase 2 完成
    ↓
Phase 4 (Testing)
```

---

## 9. 风险与注意事项

1. **JSON Details 查询**：通过混合存储模式解决，常见统计字段使用独立列
2. **大数据量性能**：通过新增索引优化，避免全表扫描
3. **向后兼容**：ALTER TABLE ADD COLUMN 在 SQLite 中是向后兼容的
4. **现有代码影响**：Phase 2 的日志记录点需要在现有代码中添加调用，需仔细测试
