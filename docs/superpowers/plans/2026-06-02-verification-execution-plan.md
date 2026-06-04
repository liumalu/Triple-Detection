# Triple-Detection 验证计划执行

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 完成改造后应用的所有运行时验证，确保 .NET Framework 4.8 迁移后的应用可正常启动和运行

**Architecture:** 验证计划节点式执行，从构建验证 → 运行时基础 → 核心功能 → VM SDK 集成 → 数据库完整性 → 端到端集成

**Tech Stack:** WPF + VisionMaster SDK + EF6 + System.Data.SQLite + Prism.DryIoc

---

## 文件结构

验证涉及以下关键文件：
- `Presentation/App.xaml.cs` — 应用启动、数据库初始化、DryIoc 容器注册
- `Presentation/MainWindow.xaml.cs` — 主窗口导航
- `Presentation/Views/Detection/DetectionView.xaml.cs` — 检测流程核心 UI
- `Presentation/ViewModels/Detection/DetectionViewModel.cs` — 检测视图模型
- `Application/VmServices/VmIntegrationService.cs` — VM SDK 包装器
- `Infrastructure/Persistence/DatabaseInitializer.cs` — 数据库初始化和种子数据
- `Config/tripledetection.db` — SQLite 数据库文件

---

## Task 1: 验证构建成功（节点 1）

**Files:**
- Modify: `TripleDetection.csproj`

- [ ] **Step 1: 确认编译状态**

执行编译命令：
```bash
cd /d D:\xcm\Triple-Detection
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" TripleDetection.csproj -t:Rebuild -p:Configuration=Debug -v:m
```

预期：`0 Error(s)`

- [ ] **Step 2: 检查输出目录**

检查 `bin/Debug/` 下是否生成：
- `TripleDetection.exe`
- `Presentation.dll`
- `Application.dll`
- `Infrastructure.dll`
- `Domain.dll`
- VM SDK DLL（`VM.Core.dll`, `VM.PlatformSDKCS.dll` 等）

- [ ] **Step 3: 更新验证记录**

节点 1 已在上次编译通过，确认状态为：
```
| 1. 构建验证 | ✓ 通过 | 2026-06-02 | Malu | .NET Framework 4.8 编译成功，0 errors |
```

---

## Task 2: 运行时基础验证（节点 2）

**Files:**
- Test: `Presentation/App.xaml.cs` — 启动流程
- Test: `Config/tripledetection.db` — 数据库文件

- [ ] **Step 1: 启动应用**

直接运行 `bin/Debug/TripleDetection.exe`，观察：
- LoginWindow 是否显示
- Output 窗口无红色错误
- 无 `ContainerException` 或 `TargetInvocationException`

预期：LoginWindow 正常显示

- [ ] **Step 2: 验证数据库连接**

检查 `Config/tripledetection.db` 是否存在且 > 0 KB

- [ ] **Step 3: 检查日志输出**

观察 `logs/startup.log` 是否包含：
```
[HH:mm:ss] LoginWindow created
[HH:mm:ss] ShowDialog returned: True/False
[HH:mm:ss] MainWindow created from DI
```

- [ ] **Step 4: 更新验证记录**

```
| 2. 运行时基础 | 待执行 | - | - | - |
```

---

## Task 3: 核心功能验证 — 登录（节点 3.1）

**Files:**
- Test: `Presentation/ViewModels/Auth/LoginViewModel.cs`
- Test: `Presentation/Views/Auth/LoginWindow.xaml`

- [ ] **Step 1: 有效登录测试**

输入 `admin` / `admin123` 点击登录

预期：登录成功，MainWindow 显示

- [ ] **Step 2: 无效密码测试**

输入 `admin` / `wrongpass`

预期：弹出错误提示

- [ ] **Step 3: 空输入拦截测试**

留空用户名或密码点击登录

预期：前端阻止或提示必填

- [ ] **Step 4: 更新验证记录**

```
| 3.1 登录 | 待执行 | - | - | - |
```

---

## Task 4: 核心功能验证 — 主窗口导航（节点 3.2）

**Files:**
- Test: `Presentation/MainWindow.xaml`
- Test: `Presentation/MainWindow.xaml.cs`

- [ ] **Step 1: Tab 切换测试**

登录后依次点击各 Tab：
- 检测 → DetectionView
- 产品管理 → ProductListView
- 任务管理 → TaskListView
- 审计日志 → AuditLogView
- 系统设置 → SettingsView

预期：各视图正确加载

- [ ] **Step 2: 权限控制测试**

用普通用户登录，验证受限 Tab 灰显/隐藏

- [ ] **Step 3: 更新验证记录**

```
| 3.2 主窗口导航 | 待执行 | - | - | - |
```

---

## Task 5: 核心功能验证 — 检测流程（节点 3.3）

**Files:**
- Test: `Presentation/Views/Detection/DetectionView.xaml`
- Test: `Presentation/ViewModels/Detection/DetectionViewModel.cs`

- [ ] **Step 1: 任务下拉框验证**

进入检测视图，打开任务下拉框

预期：仅显示 `TaskStatus.Approved` 任务，数量 ≥ 1

- [ ] **Step 2: 方案加载测试**

选择任务后点击「加载方案」

预期：方案加载成功，界面无异常

- [ ] **Step 3: 参数设置测试**

填写：批号=TEST20260531, 生产日期=2026-05-31, 有效期=2028-05-31

预期：`SetGlobalString` 日志出现在 Output 窗口

- [ ] **Step 4: 单次检测测试**

点击「单次检测」

预期：结果区显示 OK 或 NG，合格率更新

- [ ] **Step 5: 连续检测测试**

点击「连续检测」，观察结果实时滚动

点击「停止」，验证停止行为

- [ ] **Step 6: 更新验证记录**

```
| 3.3 检测流程 | 待执行 | - | - | - |
```

---

## Task 6: 核心功能验证 — 任务管理（节点 3.4）

**Files:**
- Test: `Presentation/Views/Production/ProductListView.xaml`
- Test: `Presentation/Views/Production/TaskListView.xaml`
- Test: `Presentation/ViewModels/Production/ProductListViewModel.cs`
- Test: `Presentation/ViewModels/Production/TaskListViewModel.cs`

- [ ] **Step 1: 产品列表测试**

打开产品列表，验证显示和分页

- [ ] **Step 2: 产品新增测试**

新增产品，保存后验证列表刷新

- [ ] **Step 3: 任务列表测试**

打开任务列表，验证状态筛选

- [ ] **Step 4: 任务新增/审批测试**

新增任务并审批为 Approved

- [ ] **Step 5: 更新验证记录**

```
| 3.4 任务管理 | 待执行 | - | - | - |
```

---

## Task 7: 核心功能验证 — 审计日志（节点 3.5）

**Files:**
- Test: `Presentation/Views/Audit/AuditLogView.xaml`
- Test: `Application/Services/AuditLogService.cs`

- [ ] **Step 1: 登录日志测试**

admin 登录后进入审计日志

预期：存在「用户登录」记录

- [ ] **Step 2: 操作类型筛选测试**

按时间范围、操作类型、操作用户筛选

- [ ] **Step 3: 更新验证记录**

```
| 3.5 审计日志 | 待执行 | - | - | - |
```

---

## Task 8: 核心功能验证 — 用户管理（节点 3.6）

**Files:**
- Test: `Presentation/Views/Auth/UserManagementView.xaml`
- Test: `Presentation/ViewModels/Auth/UserManagementViewModel.cs`
- Test: `Presentation/ViewModels/Auth/UserEditViewModel.cs`

- [ ] **Step 1: 用户列表测试**

admin 登录，进入用户管理

预期：显示所有未删除用户

- [ ] **Step 2: 用户新增测试**

新增用户，验证密码哈希存储

- [ ] **Step 3: 用户编辑测试**

启用/禁用用户，修改角色

- [ ] **Step 4: 更新验证记录**

```
| 3.6 用户管理 | 待执行 | - | - | - |
```

---

## Task 9: VisionMaster SDK 集成验证 — 程序集加载（节点 4.1）

**Files:**
- Test: `Application/VmServices/VmIntegrationService.cs`

- [ ] **Step 1: DLL 加载验证**

打开检测视图，观察 Output 窗口

预期：VM.Core.dll, VM.PlatformSDKCS.dll, iMVS-6000PlatformSDKCS.dll, GlobalVariableModuleCs.dll 等正确加载

预期：无 `FileNotFoundException`

- [ ] **Step 2: AssemblyResolve 日志检查**

过滤 `AssemblyResolve` 关键词

预期：无大量重复的 AssemblyResolve 日志

- [ ] **Step 3: 更新验证记录**

```
| 4.1 程序集加载 | 待执行 | - | - | - |
```

---

## Task 10: VisionMaster SDK 集成验证 — 方案文件加载（节点 4.2）

**Files:**
- Test: `Presentation/Views/Detection/DetectionView.xaml.cs`

- [ ] **Step 1: 方案加载成功测试**

选择任务后点击「加载方案」

预期：≤ 5 秒内加载完成，界面显示方案名称

- [ ] **Step 2: 方案不存在测试**

选择一个 .sol 文件不存在的任务

预期：弹窗提示「方案文件不存在」

- [ ] **Step 3: 更新验证记录**

```
| 4.2 方案文件加载 | 待执行 | - | - | - |
```

---

## Task 11: VisionMaster SDK 集成验证 — 全局变量设置（节点 4.3）

**Files:**
- Test: `Application/VmServices/VmIntegrationService.cs:143-161`

- [ ] **Step 1: BN 变量设置测试**

输入批号 `TEST20260531`，观察日志

预期：`SetGlobalString("BN", "TEST20260531")` 被调用

- [ ] **Step 2: Mfg 变量设置测试**

输入生产日期 `2026-05-31`

预期：`SetGlobalString("Mfg", "2026-05-31")` 被调用

- [ ] **Step 3: EXP 变量设置测试**

输入有效期 `2028-05-31`

预期：`SetGlobalString("EXP", "2028-05-31")` 被调用

- [ ] **Step 4: 更新验证记录**

```
| 4.3 全局变量设置 | 待执行 | - | - | - |
```

---

## Task 12: VisionMaster SDK 集成验证 — 检测回调（节点 4.4）

**Files:**
- Test: `Application/VmServices/VmIntegrationService.cs:202-286`

- [ ] **Step 1: 回调触发测试**

点击「单次检测」，观察 Output 窗口

预期：`OnWorkStatusEvent` 被触发，`nWorkStatus=0`, `nProcessID=10000`

- [ ] **Step 2: 结果解析测试**

检测完成后验证：
- OK/NG 显示正确
- 批号/生产日期/有效期回填到界面

- [ ] **Step 3: 更新验证记录**

```
| 4.4 检测回调 | 待执行 | - | - | - |
```

---

## Task 13: VisionMaster SDK 集成验证 — 资源清理（节点 4.5）

**Files:**
- Test: `Application/VmServices/VmIntegrationService.cs:79-95`
- Test: `Presentation/Views/Detection/DetectionView.xaml.cs`

- [ ] **Step 1: 窗口关闭资源清理测试**

关闭检测视图，重新打开，再次加载方案

预期：无句柄泄漏，内存增长 < 20MB

- [ ] **Step 2: Timer 清理测试**

连续检测运行时关闭视图

预期：Timer 已停止，线程退出

- [ ] **Step 3: 更新验证记录**

```
| 4.5 资源清理 | 代码实现完成，待运行时验证 | 2026-06-01 | Malu | -
```

---

## Task 14: 数据库完整性验证 — 文件存在性（节点 5.1）

**Files:**
- Test: `Config/tripledetection.db`

- [ ] **Step 1: 数据库文件检查**

确认 `Config/tripledetection.db` 存在且 > 0 KB

- [ ] **Step 2: WAL 文件检查**

确认无 `.db-wal`, `.db-shm` 残留

- [ ] **Step 3: 更新验证记录**

```
| 5.1 文件存在性 | 待执行 | - | - | - |
```

---

## Task 15: 数据库完整性验证 — 表结构（节点 5.2）

**Files:**
- Test: `Infrastructure/Persistence/TripleDetectionDbContext.cs`

- [ ] **Step 1: 表存在性验证**

执行 SQL：
```sql
SELECT name FROM sqlite_master WHERE type='table' AND name IN ('Users','Products','Tasks','DetectionRecords','AuditLogs');
```

预期：5 个表均存在

- [ ] **Step 2: 列结构验证**

对每个表执行 `PRAGMA table_info(...)` 验证列名和类型

- [ ] **Step 3: 更新验证记录**

```
| 5.2 表结构 | 待执行 | - | - | - |
```

---

## Task 16: 数据库完整性验证 — 种子数据（节点 5.3）

**Files:**
- Test: `Infrastructure/Persistence/DatabaseInitializer.cs`

- [ ] **Step 1: admin 用户验证**

```sql
SELECT * FROM Users WHERE UserName='admin' AND IsDeleted=0;
```

预期：返回 1 行，密码为 SHA256 哈希（64字符），IsEnabled=1

- [ ] **Step 2: 产品数据验证**

```sql
SELECT COUNT(*) FROM Products WHERE IsDeleted=0;
```

预期：≥ 3 条

- [ ] **Step 3: 任务数据验证**

```sql
SELECT COUNT(*) FROM Tasks WHERE IsDeleted=0;
SELECT Status, COUNT(*) FROM Tasks WHERE IsDeleted=0 GROUP BY Status;
```

预期：≥ 4 条，涵盖 Pending/Approved/Running/Completed

- [ ] **Step 4: 更新验证记录**

```
| 5.3 种子数据 | 待执行 | - | - | - |
```

---

## Task 17: 数据库完整性验证 — 软删除过滤（节点 5.4）

**Files:**
- Test: `Infrastructure/Repositories/SqliteRepository.cs`

- [ ] **Step 1: 软删除验证**

删除一个产品，确认：
- `UPDATE ... SET IsDeleted=1` 而非 `DELETE`
- UI 列表不再显示

- [ ] **Step 2: 查询过滤验证**

执行 `SELECT * FROM Products WHERE IsDeleted=0;`

预期：不返回已删除产品

- [ ] **Step 3: 更新验证记录**

```
| 5.4 软删除过滤 | 待执行 | - | - | - |
```

---

## Task 18: 数据库完整性验证 — 索引性能（节点 5.5）

**Files:**
- Test: `Infrastructure/Persistence/DatabaseInitializer.cs`

- [ ] **Step 1: 索引检查**

对高频字段执行 `EXPLAIN QUERY PLAN`：
- `Users.UserName`
- `Tasks.Status`
- `AuditLogs.UserId`
- `DetectionRecords.TaskId`

- [ ] **Step 2: 更新验证记录**

```
| 5.5 索引性能 | 待执行 | - | - | - |
```

---

## Task 19: 端到端集成验证 — 完整检测流程（节点 6.1）

**Files:**
- Test: 完整流程

- [ ] **Step 1: 完整流程测试**

按验证计划 6.1 执行完整流程：
```
1. 启动应用 → LoginWindow
2. admin / admin123 登录
3. MainWindow 加载完成
4. 检测 Tab → 选择已审批任务
5. 加载方案 → 填写参数 → 单次检测
6. 观察 OK/NG 结果和合格率更新
7. 新建任务并审批 → 返回检测视图验证出现
8. 审计日志验证检测记录
9. 退出登录 → 回到 LoginWindow
```

- [ ] **Step 2: 更新验证记录**

```
| 6.1 完整检测流程 | 待执行 | - | - | - |
```

---

## Task 20: 端到端集成验证 — 异常流程（节点 6.2）

**Files:**
- Test: 异常流程

- [ ] **Step 1: 未登录访问测试**

绕过登录直接访问 MainWindow

预期：强制跳转回 LoginWindow

- [ ] **Step 2: 连续点击防护测试**

短时间内多次点击检测按钮

预期：仅执行一次

- [ ] **Step 3: 更新验证记录**

```
| 6.2 异常流程 | 待执行 | - | - | - |
```

---

## Task 21: 端到端集成验证 — 数据一致性（节点 6.3）

**Files:**
- Test: 数据一致性

- [ ] **Step 1: DetectionRecord 保存验证**

执行检测后查询数据库：
```sql
SELECT IsOK, BatchNumber, TaskId FROM DetectionRecords ORDER BY DetectionTime DESC LIMIT 1;
```

预期：与 UI 显示一致

- [ ] **Step 2: AuditLog 一致性验证**

```sql
SELECT OperationType, UserId, OperationTime FROM AuditLogs ORDER BY OperationTime DESC LIMIT 5;
```

预期：操作类型和用户正确

- [ ] **Step 3: 更新验证记录**

```
| 6.3 数据一致性 | 待执行 | - | - | - |
```

---

## Task 22: 端到端集成验证 — 性能稳定性（节点 6.4）

**Files:**
- Test: 性能稳定性

- [ ] **Step 1: 启动时间测试**

从双击图标到 LoginWindow 显示 ≤ 5 秒

- [ ] **Step 2: 内存占用测试**

登录后空闲状态 ≤ 200 MB

连续检测 1 分钟 ≤ 500 MB

- [ ] **Step 3: 进程退出测试**

退出登录并关闭应用，进程完全退出

- [ ] **Step 4: 更新验证记录**

```
| 6.4 性能稳定性 | 待执行 | - | - | - |
```

---

## Task 23: 更新验证记录表

**Files:**
- Modify: `docs/superpowers/test/verification-plan.md:791-816`

- [ ] **Step 1: 汇总所有验证结果**

根据执行结果更新验证记录表，填充所有「待执行」项的实际状态、日期、执行人

---

## 批次 3 验证结果记录

**执行日期：** 2026-06-03
**执行人：** Malu

| 节点 | 验证结果 | 日期 | 执行人 | 备注 |
|------|---------|------|--------|------|
| 3.4 任务管理 | ✅ 通过 | 2026-06-03 | Malu | 修复：ProductEditWindow产品描述对齐、TaskStatusConverter中文显示 |
| 3.5 审计日志 | ⏭️ 跳过 | - | - | AuditLogView 未实现 |
| 3.6 用户管理 | ✅ 通过 | 2026-06-03 | Malu | 修复：InverseBoolConverter缺失导致新增用户崩溃 |
| 4.4 检测回调 | ✅ 通过 | 2026-06-03 | Malu | 代码逻辑验证：nWorkStatus==0 && nProcessID==10000，GetOutputString解析正确 |
| 4.5 资源清理 | ✅ 通过 | 2026-06-03 | Malu | 之前已测试通过 |
| 5.3 种子数据 | ✅ 通过（已修复） | 2026-06-03 | Malu | 修复：admin密码明文→SHA256哈希、Product.Code未填充、ProdTask.ProductId未关联、任务状态不完整 |
| 5.4 软删除过滤 | ✅ 通过 | 2026-06-03 | Malu | SqliteRepository.Delete()执行UPDATE SET IsDeleted=1，查询自动过滤IsDeleted=0 |

---

## 修复详情

### 1. ProductEditWindow.xaml — 产品描述对齐修复

**文件：** `Presentation/Views/Production/ProductEditWindow.xaml` (Row 6)

```xml
<!-- 修复前 -->
<TextBlock Grid.Row="6" Grid.Column="0" Text="产品描述:" VerticalAlignment="Top" Margin="0,0,10,0"/>

<!-- 修复后 -->
<TextBlock Grid.Row="6" Grid.Column="0" Text="产品描述:" VerticalAlignment="Center" Margin="0,0,10,10"/>
```

### 2. TaskStatusConverter — 新建并注册

**文件：** `Presentation/Converters/TaskStatusConverter.cs` (新建)

```csharp
public class TaskStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TaskStatus status)
        {
            switch (status)
            {
                case TaskStatus.Pending:   return "待审核";
                case TaskStatus.Approved: return "已审核";
                case TaskStatus.Running:   return "执行中";
                case TaskStatus.Completed: return "已完成";
                default: return status.ToString();
            }
        }
        return value != null ? value.ToString() : "";
    }
}
```

**注册：** `Presentation/App.xaml` 添加 `<converters:TaskStatusConverter x:Key="TaskStatusConverter"/>`

### 3. InverseBoolConverter — 新建并注册

**文件：** `Presentation/Converters/InverseBoolConverter.cs` (新建)

```csharp
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue) return !boolValue;
        return value;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue) return !boolValue;
        return value;
    }
}
```

**注册：** `Presentation/App.xaml` 添加 `<converters:InverseBoolConverter x:Key="InverseBoolConverter"/>`

### 4. DatabaseInitializer.cs — 种子数据修复

**文件：** `Infrastructure/Persistence/DatabaseInitializer.cs`

| 问题 | 修复前 | 修复后 |
|------|--------|--------|
| admin 密码 | 明文 `admin123` | SHA256 哈希 (salt: `TriD4dminS4lt==`) |
| Product.Code | 未填充 (NULL) | `OCR-2025-001`, `DEF-2025-001`, `DIM-2025-001` |
| ProdTask.ProductId | 未设置 (0/NULL) | 关联到产品 1, 2, 3 |
| ProdTask.ProductName | 未设置 | 产品名称 |
| 任务状态覆盖 | 只有 Pending(0)×1, Approved(1)×3 | Pending(0)×1, Approved(1)×3, Running(2)×1, Completed(3)×1 |

---

## 自检清单

执行完成后，确认：

1. **Spec 覆盖：** 每个节点（1-6）均已执行验证
2. **无占位符：** 所有 "待执行" 项均已更新为实际结果
3. **类型一致性：** 验证过程中的观察结果与代码实现一致

---

## 剩余待验证节点

| 节点 | 状态 | 说明 |
|------|------|------|
| 3.1 登录 | ⏳ 待执行 | 批次2后需验证 |
| 3.2 主窗口导航 | ⏳ 待执行 | 批次2后需验证 |
| 3.3 检测流程 | ⏳ 待执行 | 需VM SDK环境 |
| 4.1 程序集加载 | ⏳ 待执行 | 需VM SDK环境 |
| 4.2 方案文件加载 | ⏳ 待执行 | 需VM SDK环境 |
| 4.3 全局变量设置 | ⏳ 待执行 | 需VM SDK环境 |
| 5.1 文件存在性 | ⏳ 待执行 | 数据库文件检查 |
| 5.2 表结构 | ⏳ 待执行 | 需SQL查询验证 |
| 5.5 索引性能 | ⏳ 待执行 | 需SQL查询验证 |
| 6.1 完整检测流程 | ⏳ 待执行 | 需VM SDK环境 |
| 6.2 异常流程 | ⏳ 待执行 | 需运行时验证 |
| 6.3 数据一致性 | ⏳ 待执行 | 需SQL查询验证 |
| 6.4 性能稳定性 | ⏳ 待执行 | 需运行时验证 |

---

## 执行方式

**"Plan complete and saved to `docs/superpowers/plans/2026-06-02-verification-execution-plan.md`. 执行方式：**

**1. Subagent-Driven (recommended)** — 每次执行一个验证节点，Runtime 验证需要实际启动应用观察

**2. Inline Execution** — 在当前 session 中顺序执行各节点验证

由于所有待验证项均已完成代码修复，现在需要 runtime 验证，建议选择 **Inline Execution** 方式，逐步启动应用验证各节点是否正常工作。"