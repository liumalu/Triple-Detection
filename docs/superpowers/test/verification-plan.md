# Triple-Detection 应用验证计划

## 概述

本验证计划用于指导改造后应用的可用性验证，覆盖从编译到运行时各关键节点的检查。

---

## 实现约束

### 架构约束

1. **领域层（Domain）不得依赖基础设施层**
   - `IRepository<T>` 等仓储接口定义在 `Domain/Repositories/` 下
   - 不得在领域层中引入具体查询类型（如 `ProductQuery`、`TaskQuery`）的引用
   - 查询参数通过 `PagedQuery` 基类传递，特定查询类型的分发在 Infrastructure 层内部处理

2. **基础设施层（Infrastructure）内部处理特定查询类型分发**
   - `SqliteRepository.Query(PagedQuery)` 内部通过 `is` 模式匹配分发到具体查询方法
   - 不得将 `Query(TaskQuery)`、`Query(ProductQuery)` 等方法暴露到 `IRepository<T>` 接口
   - 示例：
     ```csharp
     // ✅ 正确：在 Infrastructure 内部处理分发
     public IPagedResult<T> Query(PagedQuery query)
     {
         if (query is ProductQuery pq) return Query(pq);
         if (query is TaskQuery tq) return Query(tq);
         return QueryInternal(query, null!);
     }
     
     // ❌ 错误：在领域层接口中声明特定查询方法
     public interface IRepository<T>
     {
         IPagedResult<T> Query(TaskQuery query); // 侵入领域层
     }
     ```

3. **计算属性不得持久化**
   - UI 绑定用的计算属性（如 `ProdTask.ProductName`）不应存储到数据库
   - 应在 ViewModel 层通过查询结果后填充，避免数据冗余和不一致

---

## 节点 1：构建验证（编译检查）

> 本节点验证项目可完整编译，无编译错误和缺失依赖。所有后续节点依赖编译通过后方可执行。

---

### 1.1 编译执行

#### 执行步骤
1. 确认无其他 MSBuild 进程占用（`taskkill /IM MSBuild.exe /F` 如有残留）
2. 执行完整重新编译：
   ```bash
   cd /d D:\xcm\Triple-Detection
   "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" TripleDetection.csproj -t:Rebuild -p:Configuration=Debug -v:m
   ```
3. 等待编译完成（通常 30 秒 - 2 分钟）
4. 检查输出摘要：`0 Error(s)` 为通过

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| 编译成功 | 执行 msbuild 后 | 输出 `0 Error(s)` | 编译输出 |
| 无致命警告 | 观察编译输出 | 无 `error CS` 或 `error MSB` 开头的行 | 编译输出 |
| 项目引用解析 | 观察编译输出 | 无 `error CS0246`（类型找不到）或 `error CS0000`（引用缺失） | 编译输出 |
| VM SDK 引用 | 观察引用解析 | `VM.Core`、`VM.PlatformSDKCS` 等 DLL 正确加载 | 引用管理器 |
| 输出目录干净 | 编译前后对比 `bin/Debug/` | 无残留旧 DLL（说明完整重新编译） | 文件系统 |

#### 失败处理
- **error CS0246（类型/命名空间找不到）：** 检查是否缺少 `using` 语句，或 NuGet 包未正确还原（`dotnet restore`）
- **error CS0000 / error MSB1025（SDK 引用失败）：** 检查 `Infrastructure/libs/VisionMaster/` 下 DLL 是否存在，是否设置了「复制到输出目录」
- **error MSB3103（程序集绑定重定向）：** 检查 `App.config` 或 `*.csproj` 中 `<BindingRedirect>` 配置是否正确
- **error MSB4086（平台目标不匹配）：** 检查 `*.csproj` 中 `<PlatformTarget>` 是否为 `x64`，当前编译配置是否为 `Debug|x64`
- **警告过多（> 50 条）：** 在 `*.csproj` 中添加 `-nowarn:CS0169,CS0618` 等抑制无害警告

---

### 1.2 编译产物验证

#### 执行步骤
1. 编译成功后，检查以下文件和目录是否存在且有效：
2. 确认 `bin/Debug/net8.0-windows/` 下的输出文件

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| 主程序集存在 | 检查 `bin/Debug/net8.0-windows/TripleDetection.exe` | 文件存在，大小 > 1 MB | 文件系统 |
| Presentation.dll | 检查 `bin/Debug/net8.0-windows/Presentation.dll` | 存在（编译成功标志） | 文件系统 |
| Application.dll | 检查 `bin/Debug/net8.0-windows/Application.dll` | 存在 | 文件系统 |
| Infrastructure.dll | 检查 `bin/Debug/net8.0-windows/Infrastructure.dll` | 存在 | 文件系统 |
| Domain.dll | 检查 `bin/Debug/net8.0-windows/Domain.dll` | 存在 | 文件系统 |
| VM DLL 存在 | 检查 `bin/Debug/net8.0-windows/` 下的 `VM.*.dll`、`iMVS-*.dll` | 所有 VM DLL 均被复制到输出目录 | 文件系统 |
| 配置文件存在 | 检查 `bin/Debug/net8.0-windows/` 下的配置文件 | `appsettings.json`、`log4net.config`（如有） | 文件系统 |

#### 失败处理
- **某层 DLL 缺失：** 检查 `*.csproj` 中该项目的 `ProjectReference` 是否正确指向
- **VM DLL 未被复制：** 检查 `Infrastructure.csproj` 中 `<Content Include="libs\VisionMaster\**\*.dll" CopyToOutputDirectory="PreserveNewest" />` 是否存在
- **exe 存在但双击无法启动：** 检查是否缺少 `appsettings.json` 或其他运行时配置文件

---

### 1.3 构建clean验证（可选）

> 如果编译反复失败，执行一次 Clean 后重新编译。

#### 执行步骤
```bash
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" TripleDetection.csproj -t:Clean -p:Configuration=Debug
# 然后重新编译
```

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| Clean 成功 | 执行 msbuild -t:Clean | `bin/` 和 `obj/` 目录被清空 | 文件系统 |
| Clean 后重新编译 | 执行完整 Rebuild | 编译成功，无「增量编译」导致的残留错误 | 编译输出 |
| 增量编译 vs 完整编译 | 对比 Clean 前后编译时间 | Clean 后首次编译时间显著更长（正常现象） | 编译耗时 |

---

## 节点 2：运行时基础验证

### 目标
确认应用可正常启动，基础组件初始化成功。

### 执行步骤
1. 启动应用程序（Debug 模式，F5 或直接运行）
2. 观察输出窗口（Output）或调试控制台
3. 按序检查以下内容：

| 检查项 | 预期结果 | 验证位置 |
|--------|----------|----------|
| 应用启动 | LoginWindow 或 MainWindow 正常显示 | 桌面/任务栏 |
| 数据库连接 | `Config/tripledetection.db` 可访问，无 "SQLite Error" | Output 窗口 |
| DI 容器 | DryIoc 容器无 `ContainerException` | Output 窗口 |
| 日志系统 | `LoggingService initialized` 或类似日志 | Output 窗口 |
| VisionMaster DLL | 无 `FileNotFoundException` 关于 VM DLL | Output 窗口 |
| 窗口关闭 | 点击关闭按钮，进程正常退出，无卡死 | 任务管理器 |

### 成功标准
- 登录窗口正常显示
- Output 窗口无红色错误（`Error`、`Exception`）
- 无未捕获的 `TargetInvocationException` 导致应用退出
- 进程正常退出（非崩溃退出）

### 失败处理
- **DI 容器异常：** 检查 `Presentation/App.xaml.cs` 中的 `RegisterTypes` 和 `RegisterSingleton`
- **数据库异常：** 确认 `Config/tripledetection.db` 存在，EF Core 迁移已执行
- **VM DLL 异常：** 检查 `AppDomain.
CurrentDomain_AssemblyResolve` 是否正确处理 `Infrastructure/libs/VisionMaster/` 下的 DLL
- **窗口不显示：** 检查 `App.xaml.cs` 中 `CreateShell()` 是否返回 `LoginWindow` 或 `MainWindow`
- **进程卡死：** 在 VS 中按 Break 调试，查看主线程卡在哪一步

---

## 节点 3：核心功能验证

---

### 3.1 用户认证（登录）

#### 执行步骤
1. 以 `admin` / `admin123` 登录 → 应进入 MainWindow
2. 以 `admin` / 错误密码 登录 → 应提示错误
3. 尝试登录不存在的用户 → 应提示错误
4. 留空用户名或密码点击登录 → 前端应阻止

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| 有效登录 | 输入 `admin` / `admin123` | 登录成功，MainWindow 显示 | 界面切换 |
| 无效密码 | 输入 `admin` / `wrongpass` | 弹出错误提示 | 消息框或输入框下方 |
| 用户不存在 | 输入 `nonexist` / `any` | 弹出「用户名或密码错误」 | 消息框或输入框下方 |
| 空输入拦截 | 留空用户名，点击登录 | 登录按钮禁用或提示必填 | 输入框 |
| 密码明文显示 | 输入密码时观察 | 密码显示为 `••••` | 输入框 |
| 会话保持 | 登录后关闭重开应用 | 应重新要求登录 | 界面 |

#### 失败处理
- 登录后立即闪退 → 检查 `SessionManager.SetCurrentUser()` 是否有空引用
- 错误提示中文乱码 → 检查 `Resources/` 下的字符串资源文件编码
- 登录窗口不消失 → 检查 `NavigationService.RequestNavigate` 是否正确触发

---

### 3.2 主窗口导航

#### 执行步骤
1. 以 admin 登录进入 MainWindow
2. 观察左侧/顶部导航菜单
3. 依次点击各 Tab（检测、任务、产品、用户、设置、审计日志）
4. 退出 admin，用普通用户账号登录观察可访问模块

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| Tab 切换 | 点击「检测」Tab | DetectionView 加载 | 内容区域 |
| Tab 切换 | 点击「产品管理」Tab | ProductListView 加载 | 内容区域 |
| Tab 切换 | 点击「任务管理」Tab | TaskListView 加载 | 内容区域 |
| Tab 切换 | 点击「审计日志」Tab | AuditLogView 加载 | 内容区域 |
| Tab 切换 | 点击「系统设置」Tab | SettingsView 加载 | 内容区域 |
| 权限控制-管理员 | admin 登录 | 所有 Tab 均可见 | 导航菜单 |
| 权限控制-普通用户 | 普通用户登录 | 仅授权 Tab 可见 | 导航菜单（灰显/隐藏） |
| 当前 Tab 高亮 | 点击某 Tab 后 | 该 Tab 高亮或加粗显示 | 导航菜单 |
| 面包屑/标题栏 | 切换 Tab 时 | 标题栏显示当前模块名称 | MainWindow 标题区域 |

#### 失败处理
- Tab 点击无响应 → 检查 `regionManager.RequestNavigate` 是否正确注册
- 视图加载空白 → 检查 View 是否在 DI 容器中注册
- 权限不生效 → 检查 `IAuthorizationService` 和用户角色判断逻辑

---

### 3.3 检测流程（DetectionView）

这是最核心的功能模块，请按顺序执行。

#### 执行步骤
1. 进入检测视图
2. 观察任务下拉框是否只显示「已审批」任务
3. 选择一个任务后点击「加载方案」
4. 填写参数：批号、生产日期、有效期
5. 执行单次检测
6. 执行连续检测（如果设备可用）
7. 观察结果更新和合格率变化

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| 任务下拉框 | 打开检测视图 | 仅列出 `TaskStatus.Approved` 任务 | 下拉框列表 |
| 任务数量 | 打开任务下拉框 | 数量 ≥ 1（否则检查数据库种子数据） | 下拉框 |
| 方案加载 | 选择任务后点击「加载方案」 | 无异常，方案路径日志输出 | Output 窗口 |
| 方案加载 | 加载不存在的方案 | 弹窗提示「方案文件不存在」 | 消息框 |
| 参数-批号 | 输入批号 `TEST001` | 变量 `BN=TEST001` 被设置 | 运行时变量监控 |
| 参数-生产日期 | 选择 `2026-05-31` | 变量 `Mfg=2026-05-31` 被设置 | 运行时变量监控 |
| 参数-有效期 | 选择 `2028-05-31` | 变量 `EXP=2028-05-31` 被设置 | 运行时变量监控 |
| 单次检测 | 点击「单次检测」 | `IsOK` 显示 OK 或 NG，结果区更新 | 结果面板 |
| 连续检测-启动 | 点击「连续检测」 | 按钮变为「停止」，结果实时滚动 | 结果面板 |
| 连续检测-停止 | 点击「停止」 | 检测停止，按钮恢复「连续检测」 | 按钮状态 |
| 合格率 | 执行多次检测后 | 合格率 = OK数 / 总检测数 | 合格率显示区 |
| 状态显示 | 检测进行中 | 状态文字显示「检测中...」 | 状态栏 |
| 状态显示 | 检测空闲时 | 状态文字显示「空闲」 | 状态栏 |
| 结果记录 | 单次检测完成后 | `DetectionRecord` 已保存 | 数据库查询 |
| 实时图表 | 连续检测时 | 折线图/柱状图实时更新 | 图表区域（如有） |

#### 失败处理
- 任务下拉框为空 → 检查 `TaskService.GetApprovedTasks()` 和数据库 `Tasks` 表
- 方案加载异常 → 检查 `VmIntegrationService.LoadSolution()` 日志，确认真际方案路径
- 检测结果一直为 NG → 检查 `OnWorkStatusEvent` 回调是否正确解析 `GetOutputString()`
- 合格率不更新 → 检查 `DetectionResultEvent` 是否被正确发布和订阅
- 连续检测卡死 → 检查 `Timer` 是否正确 `Dispose()`，`CancellationToken` 是否传递

---

### 3.4 任务管理（Product/Task）

#### 执行步骤
1. 进入产品列表视图，查看现有产品
2. 新增一个产品，填写名称和描述
3. 保存后验证列表刷新
4. 进入任务列表，筛选不同状态
5. 新增一个任务，选择关联产品
6. 将任务状态从 Pending 改为 Approved
7. 删除一个任务，验证软删除

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| 产品列表加载 | 打开产品列表 | 显示所有未删除产品，分页正常 | 列表网格 |
| 产品新增 | 点击新增，填写信息后保存 | 列表自动刷新，新产品出现 | 列表网格 |
| 产品名称重复 | 新增同名产品 | 前端提示或后端返回错误 | 消息框/输入框 |
| 产品编辑 | 修改产品信息后保存 | 列表中数据已更新 | 列表网格 |
| 产品删除 | 点击删除（软删除） | 产品从列表消失，`IsDeleted=true` | 列表网格 + 数据库 |
| 任务列表加载 | 打开任务列表 | 显示所有任务（含状态筛选） | 列表网格 |
| 任务状态筛选 | 选择状态「全部/待审批/已审批」 | 列表按状态过滤 | 筛选下拉框 |
| 任务新增 | 新增任务，选择关联产品 | 任务创建成功，状态为 Pending | 列表网格 |
| 任务审批 | 将 Pending 改为 Approved | 状态更新，该任务出现在检测视图 | 检测视图任务下拉框 |
| 任务删除 | 点击删除 | `IsDeleted=true`，列表刷新 | 列表网格 |
| 分页-首页/上一页/下一页/末页 | 点击翻页按钮 | 页面数据正确切换 | 分页控件 |
| 分页-每页条数变更 | 更改每页显示数量 | 列表数据量相应变化 | 分页控件 |

#### 失败处理
- 列表不刷新 → 检查 `ObservableCollection` 绑定和 `INotifyPropertyChanged` 实现
- 新增保存失败 → 检查必填字段验证和服务层错误返回
- 任务状态筛选无效 → 检查 `TaskService.GetTasksFiltered(status)` 查询逻辑
- 软删除后仍显示 → 检查 repository 查询是否默认过滤 `IsDeleted=true`

---

### 3.5 审计日志

#### 执行步骤
1. 执行一个有审计日志的操作（如登录、产品新增、任务审批）
2. 进入审计日志视图
3. 验证日志记录是否正确
4. 尝试按时间范围、操作类型、操作用户筛选

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| 登录日志 | admin 登录后进入审计日志 | 存在一条「用户登录」记录 | 审计日志列表 |
| 操作类型 | 执行产品新增 | 记录操作类型为「产品管理」或「新增产品」 | 操作类型列 |
| 操作时间 | 查看最近操作 | 时间戳与实际操作时间一致（误差 < 1分钟） | 时间列 |
| 操作用户 | 查看记录 | 显示执行操作的用户名（admin） | 用户名列 |
| 操作详情 | 查看新增产品的那条记录 | 详情中包含新产品名称或 ID | 详情列 |
| 任务审批日志 | 将任务改为 Approved 状态 | 审计日志记录状态变更前后 | 详情列 |
| 时间范围筛选 | 选择「今天」/「近7天」/「自定义」 | 列表按时间范围过滤 | 筛选区 |
| 操作类型筛选 | 选择「用户登录」筛选 | 仅显示登录操作记录 | 筛选下拉框 |
| 用户筛选 | 选择 admin 筛选 | 仅显示 admin 的操作记录 | 筛选下拉框 |
| 导出功能 | 点击导出（Excel/CSV） | 文件正确下载，包含筛选后数据 | 文件系统 |
| 分页 | 日志超过一页 | 分页正常，翻页数据正确 | 分页控件 |

#### 失败处理
- 登录后无审计日志 → 检查 `LoginViewModel` 是否调用 `AuditLogService.LogAsync()`
- 操作后日志延迟 → 检查 `AuditLogService` 是否异步写入，未阻塞主线程
- 筛选无效 → 检查查询表达式是否正确传递筛选参数
- 导出失败 → 检查文件系统权限或 `ExportService` 异常

---

### 3.6 用户管理（仅管理员）

> 以下测试项需要以 admin 用户登录后执行。

#### 执行步骤
1. 进入用户管理视图（admin 专属）
2. 查看用户列表
3. 新增一个用户
4. 编辑已有用户（启用/禁用、改角色）
5. 删除一个用户（软删除）

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| 用户列表 | 打开用户管理 | 显示所有未删除用户 | 用户列表网格 |
| 用户新增 | 新增用户（填写完整信息） | 用户出现列表中，密码被哈希存储 | 用户列表 + 数据库 |
| 用户编辑-启用 | 禁用用户改为启用 | 用户状态更新，可正常登录 | 用户状态列 |
| 用户编辑-禁用 | 启用用户改为禁用 | 该用户无法登录（提示账户禁用） | 登录界面 |
| 用户编辑-改密码 | 修改用户密码 | 下次登录须用新密码 | 登录界面 |
| 用户编辑-改角色 | 普通用户改为管理员 | 该用户可访问所有 Tab | 导航菜单 |
| 用户删除 | 点击删除 | 用户从列表消失，`IsDeleted=true` | 用户列表 |
| 无权限访问 | 以普通用户访问用户管理 | 界面无此 Tab 或导航被阻止 | 导航菜单 |
| 空密码拦截 | 新增用户时密码留空 | 前端阻止或后端返回验证错误 | 输入框/消息框 |
| 重复用户名 | 新增同名用户 | 前端阻止或后端返回唯一约束错误 | 输入框/消息框 |

#### 失败处理
- 密码明文存储 → 检查 `PasswordHashService` 实现，密码必须 SHA256 哈希
- 权限绕过 → 检查 `[Authorize]` 或 `IAuthorizationService` 在 ViewModel 层是否正确
- 删除后仍可登录 → 检查 `UserService.Authenticate()` 是否过滤 `IsDeleted=true`

---

## 节点 4：VisionMaster SDK 集成验证

> 本节点专门验证 VisionMaster SDK 的程序集加载、方案调用和回调解析。所有步骤均在检测视图中执行（前提：节点 2 和节点 3.1 已通过）。

---

### 4.1 程序集（Assembly）加载验证

#### 执行步骤
1. 启动应用，以 admin 登录，点击检测执行,进入检测执行页面，加载方案，查看检测视图
2. 打开检测视图（此时 VisionMaster SDK 被首次调用）
3. 打开 VS **输出窗口**，切换到「调试」选项卡
4. 过滤关键词 `AssemblyLoad`、`FileNotFoundException`、`VM.`、`iMVS`

#### 验证表格

| 检查项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| VM.Core 加载 | 打开检测视图 | `VM.Core.dll` 已加载，无 `FileNotFoundException` | 输出窗口 |
| VM.PlatformSDKCS 加载 | 打开检测视图 | `VM.PlatformSDKCS.dll` 已加载 | 输出窗口 |
| iMVS-6000PlatformSDKCS 加载 | 打开检测视图 | `iMVS-6000PlatformSDKCS.dll` 已加载 | 输出窗口 |
| GlobalVariableModuleCs 加载 | 打开检测视图 | `GlobalVariableModuleCs.dll` 已加载 | 输出窗口 |
| Apps.* 系列加载 | 打开检测视图 | 多个 `Apps.*.dll` 已加载 | 输出窗口 |
| 无 FileNotFoundException | 观察输出窗口 | 全程无红色 FileNotFoundException | 输出窗口 |
| AssemblyResolve 未触发 | 过滤 AssemblyResolve | 无大量重复的 AssemblyResolve 日志（说明路径正确） | 输出窗口 |

#### 失败处理
- **大量 `AssemblyResolve` 日志：** `AppDomain.CurrentDomain_AssemblyResolve` 未正确处理或 DLL 路径配置错误
- **某个 VM DLL 未加载：** 检查 `Infrastructure/libs/VisionMaster/` 下是否缺少该 DLL，或是否被正确复制到输出目录
- **所有 VM DLL 均未加载：** 检查 `VmIntegrationService` 构造函数中是否正确设置了 `AssemblyResolve` 事件处理器

---

### 4.2 方案文件（.sol）加载验证

#### 执行步骤
1. 在检测视图的任务下拉框选择一个「已审批」任务
2. 点击「加载方案」按钮
3. 观察：
   - 是否有异常弹出
   - Output 窗口是否有方案路径日志
   - 界面上的方案名称/路径是否更新

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| 方案加载成功 | 选择任务后点击「加载方案」 | 无异常，方案名显示在界面上 | 检测视图界面 |
| 方案路径日志 | 加载成功后 | Output 窗口输出类似 `Solution loaded: D:\...\abc.sol` | Output 窗口 |
| 方案不存在 | 选择一个 .sol 文件不存在的任务 | 弹出错误提示「方案文件不存在」 | 消息框 |
| 方案损坏/无效 | 选择一个内容损坏的 .sol 文件 | 弹出错误提示「方案加载失败」 | 消息框 |
| 多次加载切换 | 先加载方案A，再加载方案B | 方案A被正确卸载，方案B加载成功 | 界面状态 |
| 加载时 UI 冻结 | 点击「加载方案」 | 界面短暂阻塞后恢复（非长时间卡死） | 界面响应 |
| 方案名称显示 | 加载成功后 | 界面上显示方案文件名称（非完整路径） | 方案名称 Label |

#### 失败处理
- **点击后无反应：** 检查「加载方案」按钮的 `ICommand` 是否正确绑定，ViewModel 方法是否被调用
- **异常弹出但方案仍加载：** 所有异常应被 catch 后记录日志，不应弹窗给用户（除非致命）
- **方案路径找不到：** 检查 `Product` 实体中 `SolutionPath` 字段值，确认相对于 `Environment.CurrentDirectory` 的路径是否正确

---

### 4.3 全局变量（Global Variables）设置验证

#### 执行步骤
1. 方案加载成功后（4.2 通过）
2. 在检测视图的变量设置区填写：
   - 批号（BN）：`TEST20260531`
   - 生产日期（Mfg）：`2026-05-31`
   - 有效期（EXP）：`2028-05-31`
3. 观察 Output 窗口或调用日志，验证变量是否被正确写入 VM

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| BN 变量设置 | 输入批号后触发设置 | `SetGlobalString("BN", "TEST20260531")` 被调用 | Output 窗口或日志 |
| Mfg 变量设置 | 输入生产日期后触发设置 | `SetGlobalString("Mfg", "2026-05-31")` 被调用 | 日志 |
| EXP 变量设置 | 输入有效期后触发设置 | `SetGlobalString("EXP", "2028-05-31")` 被调用 | 日志 |
| 变量为空时 | 清空批号，点击设置 | 应阻止设置或提示变量名不能为空 | 消息框或输入框 |
| 变量特殊字符 | 批号输入 `TEST-ABC_123` | 变量正确设置，无编码异常 | 日志 |
| 设置顺序 | 同时设置多个变量 | BN → Mfg → EXP 顺序调用（非并发） | 日志时间戳 |

#### 失败处理
- **`SetGlobalString` 未被调用：** 检查 `VmIntegrationService.SetGlobalString()` 是否被 ViewModel 调用
- **变量名大小写：** VM SDK 变量名严格区分大小写，确认代码中变量名与方案中一致（`BN`、`Mfg`、`EXP`）
- **日期格式：** 确认传入格式为 `yyyy-MM-dd`，与 VisionMaster 方案中日期格式一致

---

### 4.4 检测回调（OnWorkStatusEvent）验证

#### 执行步骤
1. 全局变量设置完成后（4.3 通过）
2. 点击「单次检测」按钮
3. 等待检测完成（约 1-3 秒）
4. 观察：
   - 检测结果（OK/NG）是否更新
   - Output 窗口中回调日志
   - 批号/生产日期/有效期是否回填到界面

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| 回调触发 | 点击「单次检测」 | `OnWorkStatusEvent` 被触发（至少一次） | Output 窗口日志 |
| 回调 nWorkStatus==0 | 检测完成且结果 OK | `nWorkStatus == 0` 进入结果解析分支 | 代码断点或日志 |
| 回调 nWorkStatus==1 | 检测完成且结果 NG | `nWorkStatus == 1` 进入结果解析分支 | 代码断点或日志 |
| 结果解析-格式正确 | 检测成功后 | `GetOutputString()` 返回逗号分隔字符串 | 代码断点 |
| 结果解析-IsOK | 回调解析 | 解析第一个值正确映射到 UI 的 OK/NG 显示 | 结果面板 |
| 结果解析-批号 | 回调解析 | 解析第二个值正确回填「批号」输入框 | 批号输入框 |
| 结果解析-生产日期 | 回调解析 | 解析第三个值正确回填「生产日期」输入框 | 日期输入框 |
| 结果解析-有效期 | 回调解析 | 解析第四个值正确回填「有效期」输入框 | 日期输入框 |
| 回调 nProcessID==10000 | 单次检测完成 | `nProcessID == 10000` 时才解析结果（非 10001 等其他值） | 代码断点 |
| 连续检测回调 | 点击「连续检测」运行多次 | 每次检测均触发回调，结果实时累加 | 结果面板 |
| 回调解析异常 | `GetOutputString()` 返回格式错误 | 不应崩溃，日志记录异常，当前结果保持不变 | Output 窗口 |
| 回调超时 | 检测超过 10 秒无响应 | 应有超时处理，不永久阻塞 UI | 界面响应 |

#### 失败处理
- **回调从未触发：** 检查 `VmIntegrationService` 是否正确注册了 `OnWorkStatusEvent` 事件
- **结果始终为 NG：** 可能是方案内部判断问题，或 `GetOutputString()` 返回值与解析代码不匹配
- **UI 不更新：** 检查 `OnWorkStatusEvent` 回调中是否正确使用了 `Application.Current.Dispatcher.Invoke`
- **nProcessID 判断错误：** 确认代码判断的是 `nProcessID == 10000`（单次检测固定值），而非其他魔法数字
- **内存泄漏（连续检测）：** 检查 `Timer` 是否在停止时正确 `Dispose()`，事件处理器是否重复订阅

---

### 4.5 VisionMaster SDK 资源清理验证

#### 执行步骤
1. 加载方案后执行一次检测（4.4 通过）
2. 关闭检测视图（导航到其他 Tab）
3. 重新打开检测视图，再次加载方案执行检测
4. 重复步骤 2-3 三次，观察内存和句柄变化

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| 窗口关闭-方案卸载 | 关闭检测视图 | 前一个方案被正确卸载，无句柄泄漏 | 任务管理器 |
| 窗口关闭-Timer 停止 | 关闭检测视图（如正在连续检测） | 连续检测 Timer 已停止，线程退出 | 代码断点/日志 |
| 重新加载 | 重新打开视图再次加载 | 新方案正常加载，旧方案资源已释放 | 内存/操作正常 |
| 内存增长 | 打开→关闭检测视图 10 次 | 内存增长 < 20MB（无内存泄漏） | 任务管理器 |

#### 失败处理
- **句柄泄漏：** 检查 `VmIntegrationService.Dispose()` 是否被调用，或是否实现了 `IDisposable`
- **Timer 泄漏：** 连续检测的 `Timer` 在停止或视图卸载时必须 `Dispose()`
- **内存持续增长：** 使用 dotMemory 或ANTS Profiler 定位未被回收的 VM SDK 对象

---

## 节点 5：数据库完整性验证

> 本节点验证数据库文件、表结构、种子数据是否与 EF Core 模型和业务预期一致。可使用 **DB Browser for SQLite**、**Azure Data Studio** 或 `sqlite3` CLI 执行查询。

---

### 5.1 数据库文件存在性验证

#### 执行步骤
1. 确认应用未运行（无进程锁定数据库）
2. 检查 `Config/tripledetection.db` 文件存在
3. 确认文件大小 > 0 KB

#### 验证表格

| 检查项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| 数据库文件存在 | 检查 `Config/` 目录 | `tripledetection.db` 存在 | 文件系统 |
| 文件非空 | 查看文件大小 | 文件大小 > 0 KB | 文件系统 |
| 文件可读 | 以只读方式打开 | 可正常读取，无「数据库已锁定」 | SQLite 工具 |
| 无 WAL/ journal 冲突 | 确认无 `.db-wal`、`.db-shm` 残留 | 无残留文件（正常关闭的标志） | 文件系统 |

#### 失败处理
- **文件不存在：** 检查 `DatabaseInitializer` 是否在 `App.xaml.cs` 启动时被调用
- **文件为 0 KB：** 数据库未正确初始化，可能是权限问题或磁盘已满
- **文件被锁定：** 有应用进程未正确退出，结束相关进程后重试

---

### 5.2 表结构验证

#### 执行步骤
1. 连接 `tripledetection.db`
2. 执行以下 SQL 查看所有表
3. 对每个表执行 `PRAGMA table_info(...)` 确认列名和类型

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| Users 表存在 | 执行 `SELECT name FROM sqlite_master WHERE type='table' AND name='Users';` | 返回 1 行 | SQL 结果 |
| Users.Id | `PRAGMA table_info(Users);` | 存在 `Id` INTEGER PRIMARY KEY | 列列表 |
| Users.UserName | 同上 | 存在 `UserName` TEXT NOT NULL UNIQUE | 列列表 |
| Users.PasswordHash | 同上 | 存在 `PasswordHash` TEXT NOT NULL | 列列表 |
| Users.IsDeleted | 同上 | 存在 `IsDeleted` INTEGER (0/1) | 列列表 |
| Users.IsEnabled | 同上 | 存在 `IsEnabled` INTEGER (0/1) | 列列表 |
| Users.Role | 同上 | 存在 `Role` TEXT | 列列表 |
| Products 表存在 | 同上 | Products 表存在 | SQL 结果 |
| Products.ProductCode | `PRAGMA table_info(Products);` | 存在 `ProductCode` TEXT NOT NULL | 列列表 |
| Products.SolutionPath | 同上 | 存在 `SolutionPath` TEXT | 列列表 |
| Tasks 表存在 | 同上 | Tasks 表存在 | SQL 结果 |
| Tasks.Status | `PRAGMA table_info(Tasks);` | 存在 `Status` INTEGER (对应枚举 int) | 列列表 |
| Tasks.ProductId | 同上 | 存在 `ProductId` INTEGER | 列列表 |
| Tasks.IsDeleted | 同上 | 存在 `IsDeleted` INTEGER | 列列表 |
| DetectionRecords 表存在 | 同上 | DetectionRecords 表存在 | SQL 结果 |
| DetectionRecords.IsOK | 同上 | 存在 `IsOK` INTEGER | 列列表 |
| DetectionRecords.BatchNumber | 同上 | 存在 `BatchNumber` TEXT | 列列表 |
| AuditLogs 表存在 | 同上 | AuditLogs 表存在 | SQL 结果 |
| AuditLogs.OperationType | 同上 | 存在 `OperationType` TEXT | 列列表 |
| AuditLogs.UserId | 同上 | 存在 `UserId` INTEGER | 列列表 |
| 外键约束（无） | 检查所有表 | 确认无 FK 约束（与架构文档一致） | PRAGMA foreign_key_list |

#### 失败处理
- **表不存在：** EF Core 迁移未执行，或 `DatabaseInitializer` 未在 `App.xaml.cs` 启动时被调用
- **列缺失/类型不匹配：** 检查 `TripleDetectionDbContext` 中 Fluent API 配置与实际表结构不符
- **外键约束存在但不应有：** 架构文档明确无 FK，检查 DbContext 是否错误配置了 `HasForeignKey`

---

### 5.3 种子数据完整性验证

> 种子数据由 `DatabaseInitializer` 在首次运行时自动创建。验证前请确认数据库是从未填充过数据的新建库。

#### 执行步骤
1. 删除现有 `Config/tripledetection.db`（如需重新初始化）
2. 启动应用一次，触发数据库初始化
3. 连接数据库执行以下查询

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| admin 用户存在 | `SELECT * FROM Users WHERE UserName='admin' AND IsDeleted=0;` | 返回 1 行 | SQL 结果 |
| admin 密码已哈希 | 检查 `PasswordHash` 字段 | 不等于明文 `admin123`，长度 64（SHA256 十六进制） | 字段值 |
| admin 用户已启用 | `SELECT IsEnabled FROM Users WHERE UserName='admin';` | `IsEnabled = 1` | 字段值 |
| admin 角色 | `SELECT Role FROM Users WHERE UserName='admin';` | 角色为 `Admin` 或 `Administrator` | 字段值 |
| 用户数量 | `SELECT COUNT(*) FROM Users WHERE IsDeleted=0;` | ≥ 1（含 admin） | 数值 |
| 产品数量 | `SELECT COUNT(*) FROM Products WHERE IsDeleted=0;` | ≥ 3 | 数值 |
| 产品数据有效 | `SELECT COUNT(*) FROM Products WHERE IsDeleted=0 AND ProductCode IS NOT NULL AND ProductCode <> '';` | 与上条一致（无 null/空 code） | 数值 |
| 产品方案路径有效 | `SELECT SolutionPath FROM Products WHERE IsDeleted=0 LIMIT 1;` | 非空，路径指向存在的 `.sol` 文件 | 文件系统 |
| 任务数量 | `SELECT COUNT(*) FROM Tasks WHERE IsDeleted=0;` | ≥ 4 | 数值 |
| 任务状态分布 | `SELECT Status, COUNT(*) FROM Tasks WHERE IsDeleted=0 GROUP BY Status;` | 包含 Pending(0)、Approved(1)、Running(2)、Completed(3) 四种状态 | SQL 结果 |
| 任务关联有效产品 | `SELECT COUNT(*) FROM Tasks t WHERE t.IsDeleted=0 AND t.ProductId IN (SELECT Id FROM Products WHERE IsDeleted=0);` | 等于任务总数（每个任务引用有效产品） | 数值 |
| AuditLogs 表可写 | 执行 `INSERT INTO AuditLogs (UserId, OperationType, OperationTime, Details) VALUES (1, 'Test', datetime('now'), 'test');` 然后 `ROLLBACK;` | 无错误 | SQL 结果 |
| DetectionRecords 表可写 | 同上测试（插入后 ROLLBACK） | 无错误 | SQL 结果 |

#### 失败处理
- **密码为明文：** `DatabaseInitializer` 在创建 admin 用户时未使用 `PasswordHashService`，遗留明文数据有安全风险
- **任务关联产品孤立：** FK 缺失导致，孤立任务可正常创建但无法在检测视图加载方案（`Product.SolutionPath` 为 null）
- **状态枚举值不匹配：** `TaskStatus` 枚举从 0 开始还是从 1 开始，与数据库 `Status` 列数值需一致

---

### 5.4 软删除与查询过滤验证

#### 执行步骤
1. 执行软删除操作（在 UI 中删除一个产品或任务）
2. 用 SQL 确认 `IsDeleted` 被设为 `1`（非物理删除）
3. 在 UI 中确认该记录不再出现在列表中

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| 软删除-SET | 删除一个产品 | `UPDATE Products SET IsDeleted=1 WHERE Id=X`（非 DELETE 语句） | SQL Profiler / 日志 |
| 软删除-隐藏 | UI 产品列表 | 已删除产品不再显示 | 产品列表界面 |
| 软删除-可恢复 | 数据库直接改回 `IsDeleted=0` | 产品重新出现在列表 | 产品列表 + SQL |
| 查询过滤-Users | `SELECT * FROM Users WHERE IsDeleted=0;` | 不返回已删除用户 | SQL 结果 |
| 查询过滤-Products | 同上 | 不返回已删除产品 | SQL 结果 |
| 查询过滤-Tasks | 同上 | 不返回已删除任务 | SQL 结果 |
| 查询过滤-AuditLogs | `SELECT * FROM AuditLogs;` | AuditLogs 不过滤（操作日志需完整保留） | SQL 结果 |

#### 失败处理
- **物理删除而非软删除：** Repository 的 `Delete()` 方法使用了 `DELETE FROM` 而非 `UPDATE ... SET IsDeleted=1`
- **UI 仍显示已删除项：** 查询未在 WHERE 中加入 `IsDeleted=0` 条件
- **软删除后重新打开视图又出现：** EF Core 缓存问题，切换视图时未刷新 `ObservableCollection`

---

### 5.5 索引与性能验证（如适用）

#### 执行步骤
1. 对高频查询字段执行 `EXPLAIN QUERY PLAN`
2. 确认关键字段有适当索引

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| Users.UserName 索引 | `EXPLAIN QUERY PLAN SELECT * FROM Users WHERE UserName='admin';` | 使用 `USING INDEX` 或 `USING COVERING INDEX` | SQL 输出 |
| Tasks.Status 索引 | `EXPLAIN QUERY PLAN SELECT * FROM Tasks WHERE Status=1 AND IsDeleted=0;` | 使用索引（Approved 状态查询频繁） | SQL 输出 |
| AuditLogs.UserId 索引 | `EXPLAIN QUERY PLAN SELECT * FROM AuditLogs WHERE UserId=1 ORDER BY OperationTime DESC;` | 使用索引 | SQL 输出 |
| DetectionRecords.TaskId 索引 | `EXPLAIN QUERY PLAN SELECT * FROM DetectionRecords WHERE TaskId=1;` | 使用索引（查询历史记录） | SQL 输出 |

---

## 节点 6：集成验证（端到端）

> 本节点验证完整的业务流，覆盖从登录到退出的全链路数据流动。所有子步骤依赖前置节点通过方可执行。

---

### 6.1 完整检测流程（主场景）

#### 执行步骤
按以下顺序执行，中间任何一步失败则终止，记录失败节点后修复。

```
1. 启动应用，显示 LoginWindow
2. 输入 admin / admin123，点击登录
3. MainWindow 加载完成，观察所有 Tab 是否显示
4. 点击「检测」Tab，进入 DetectionView
5. 打开任务下拉框，选择一个「已审批」任务
6. 点击「加载方案」，等待方案加载完成（≤ 5 秒）
7. 填写参数：批号=TEST20260531, 生产日期=2026-05-31, 有效期=2028-05-31
8. 点击「单次检测」，等待结果（≤ 10 秒）
9. 观察结果：OK 或 NG 亮起，合格率数字更新
10. 打开任务管理，新建一个 Pending 任务并审批为 Approved
11. 返回检测视图，新审批的任务出现在下拉框
12. 进入审计日志视图，确认检测操作已被记录
13. 点击右上角「退出登录」，回到 LoginWindow
```

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| 登录跳转 | admin 登录 | LoginWindow 关闭，MainWindow 显示 | 界面切换 |
| Tab 全部可见 | MainWindow 加载完成 | 检测/产品/任务/用户/设置/审计日志 Tab 均可见 | MainWindow |
| 任务下拉框有值 | 进入检测视图 | 至少 1 个 Approved 任务可选 | 下拉框列表 |
| 方案加载完成 | 点击「加载方案」后 ≤ 5 秒 | 界面显示方案名称，无异常弹窗 | 检测视图 |
| 参数设置触发 | 填写批号/日期后 | `SetGlobalString` 日志出现（Output 窗口） | Output 窗口 |
| 单次检测完成 | 点击「单次检测」后 ≤ 10 秒 | 结果区显示 OK 或 NG | 结果面板 |
| 合格率更新 | 检测完成后 | 合格率 = 1/1 = 100%（单次检测） | 合格率显示 |
| 结果记录持久化 | 检测完成后 | `DetectionRecords` 表新增一条记录 | SQL 查询 |
| 新任务出现在检测视图 | 审批新任务后返回检测视图 | 新任务出现在下拉框 | 下拉框列表 |
| 审计日志有记录 | 检测完成后进入审计日志 | 检测操作被记录，操作类型正确 | 审计日志列表 |
| 退出登录 | 点击「退出登录」 | MainWindow 关闭，LoginWindow 显示 | 界面切换 |
| 退出后 Session 失效 | 退出后不登录直接访问 MainWindow | 被重定向回登录窗口 | 界面 |

#### 失败处理
- **步骤 2 失败（登录无反应）：** 检查 `LoginViewModel.LoginCommand` 执行路径，`SessionManager.SetCurrentUser()` 是否异常
- **步骤 5 失败（下拉框为空）：** 确认至少有一个 `TaskStatus.Approved` 任务，参考节点 5.3 排查
- **步骤 6 失败（方案加载超时）：** 检查 VM SDK 是否正确初始化，方案路径是否可访问，参考节点 4.2
- **步骤 9 失败（结果不更新）：** 检查 `OnWorkStatusEvent` 回调是否触发，参考节点 4.4
- **步骤 10-11 失败（新任务不出现）：** 检查 `TaskService.Approve()` 是否正确持久化，ObservableCollection 是否刷新
- **步骤 12 失败（审计日志无记录）：** 检查 `DetectionViewModel` 是否在检测完成后调用 `AuditLogService.LogAsync()`
- **步骤 13 失败（退出后能直接访问）：** 检查 MainWindow 加载时是否验证了 `SessionManager.CurrentUser != null`

---

### 6.2 异常流程与边界场景

#### 执行步骤
以下场景模拟用户在非正常情况下的操作，验证应用的容错和提示能力。

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| 未登录直接访问检测视图 | 手动构造 URL 或绕过登录 | 强制跳转回 LoginWindow | 界面 |
| 会话超时 | 登录后 30 分钟无操作 | Session 过期，弹出超时提示并跳转登录 | 消息框/界面 |
| 并发登录同一账号 | 在另一台设备用同一账号登录 | 原设备被踢出，提示「账号在其他地方登录」 | 消息框 |
| 连续快速点击检测 | 短时间内（< 1 秒）多次点击 | 仅执行一次检测（防重复提交） | 结果面板 |
| 检测中切换 Tab | 连续检测运行时切换到产品视图 | 检测继续运行（后台执行） | 结果面板/另一视图 |
| 方案加载中再次点击 | 加载方案未完成时再次点击「加载方案」 | 忽略第二次点击或提示「加载中」 | 消息框/按钮状态 |
| 网络/相机断连 | 检测时 VM 连接断开 | 弹出错误提示，状态显示「连接断开」 | 消息框/状态栏 |
| 批量删除后刷新 | 删除 10 个任务后立即刷新页面 | 列表正确刷新，无遗漏或重复 | 列表网格 |
| 数据库文件被删除 | 运行中手动删除 `tripledetection.db` | 应用检测到并提示数据库错误，不崩溃 | 消息框 |
| 窗口最小化时检测完成 | 最小化窗口后执行检测 | 系统托盘或任务栏闪烁提示，切换后结果已更新 | 任务栏/窗口 |

#### 失败处理
- **绕过登录可访问：** 检查 MainWindow 加载时是否验证了 `SessionManager.CurrentUser`，并实现了 `IConfirmedNavigation` 或类似守卫
- **连续点击未拦截：** 检测按钮在执行中应 `IsEnabled = false`，防止重复提交
- **数据库断开应用崩溃：** 所有数据库操作应有 try-catch，`SqliteRepository` 异常不应直接暴露给 UI

---

### 6.3 数据一致性端到端验证

#### 执行步骤
在完整流程（6.1）执行完毕后，执行以下 SQL 查询验证数据一致性。

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| DetectionRecord 正确保存 | 检测完成后查询 | `IsOK` 值与 UI 显示一致，`BatchNumber` = `TEST20260531` | SQL 结果 |
| DetectionRecord 关联正确 | 同上 | `TaskId` 指向执行检测时选中的任务 | SQL 结果 |
| AuditLog 操作类型 | 查询检测后的审计日志 | `OperationType` 包含「检测」或「Detection」 | SQL 结果 |
| AuditLog 操作用户 | 同上 | `UserId` 指向 admin（ID=1） | SQL 结果 |
| AuditLog 时间合理性 | 同上 | `OperationTime` 在检测执行时间前后 ± 1 分钟 | SQL 结果 |
| 任务状态未被异常修改 | 审批一个新任务并完成检测后 | 任务状态仍为 Approved（检测不改变任务状态） | SQL 结果 |
| 产品引用计数（如有） | 新增产品/任务后 | 产品 `TaskCount` 或类似字段正确递增 | SQL 结果 |

#### 失败处理
- **DetectionRecord 与 UI 不一致：** `DetectionRecordService.SaveAsync()` 保存的值来源是否为 `OnWorkStatusEvent` 回调解析结果
- **AuditLog 用户 ID 错误：** `AuditLogService` 调用时是否从 `SessionManager.CurrentUser?.Id` 获取用户 ID（空用户可能导致 NULL 或 0）
- **任务状态被意外修改：** 检测代码中是否有代码路径错误地调用了 `TaskService.UpdateStatus()`

---

### 6.4 性能和稳定性快速验证

#### 执行步骤
在节点 6.1 主流程通过后，执行以下快速稳定性检查。

#### 验证表格

| 测试项 | 操作 | 预期结果 | 验证位置 |
|--------|------|----------|----------|
| 冷启动时间 | 从双击图标到 LoginWindow 显示 | ≤ 5 秒（无 VM SDK 加载的纯 UI 启动） | 秒表 |
| 登录响应时间 | 点击登录按钮到 MainWindow 显示 | ≤ 2 秒 | 秒表 |
| 检测视图切换时间 | 从其他 Tab 切换到检测视图 | ≤ 1 秒 | 秒表 |
| 内存占用-空闲 | 登录后停留 MainWindow，不操作 | ≤ 200 MB | 任务管理器 |
| 内存占用-检测中 | 连续检测运行 1 分钟 | ≤ 500 MB（无内存泄漏） | 任务管理器 |
| CPU 占用-空闲 | 登录后无操作 | ≤ 5%（无后台轮询浪费） | 任务管理器 |
| 进程数 | 应用运行后 | 进程数为 1（主进程，无多余子进程泄漏） | 任务管理器 |
| 退出后进程终止 | 退出登录并关闭应用 | 进程完全退出（无残留） | 任务管理器 |

#### 失败处理
- **冷启动 > 5 秒：** 检查 `App.xaml.cs` 中 `InitializeComponent()` 之前是否有同步数据库操作
- **内存持续增长：** 使用 dotMemory 定位，常见原因：`ObservableCollection` 不断追加未清理、事件处理器重复订阅未取消
- **多进程残留：** 检查是否有 `Process.Start()` 创建的子进程未正确 `Close()`

---

## 验证结果记录

| 节点 | 状态 | 日期 | 执行人 | 备注 |
|------|------|------|--------|------|
| 1. 构建验证 | ✓ 通过 | 2026-05-31 | Malu | 修复了 Main 入口点和 Styles.xaml 路径问题 |
| 2. 运行时基础 | 代码修复完成，待运行时验证 | 2026-05-31 | Malu | 修复：数据库路径从 Data/ 改为 Config/（与 CLAUDE.md 一致）；修复：SeedInitialData 中 Tasks INSERT 语句移除了不存在的 IsEnabled 列 |
| 3.1 登录 | 代码修复完成，待运行时验证 | 2026-05-31 | Malu | LoginViewModel 逻辑正常；LoginWindow 需运行时验证 |
| 3.2 主窗口导航 | 代码修复完成，待运行时验证 | 2026-05-31 | Malu | MainWindow.xaml.cs 已注册 Detection 路由；其他视图路由需运行时验证 |
| 3.3 检测流程 | 代码修复完成，待运行时验证 | 2026-05-31 | Malu | 修复：CmbTask_SelectionChanged 添加 selectedIndex 边界检查（防止 -1 索引）；DetectionView 已注册到 DI |
| 3.4 任务管理 | 代码修复完成，待运行时验证 | 2026-05-31 | Malu | 修复：TaskListViewModel 和 TaskEditViewModel 中硬编码 "admin" 改为 SessionManager.CurrentUserName |
| 3.5 审计日志 | 代码修复完成，待运行时验证 | 2026-05-31 | Malu | 修复：AuditLogView.xaml.cs 数据库路径从 Data/ 改为 Config/ |
| 3.6 用户管理 | 代码修复完成，待运行时验证 | 2026-05-31 | Malu | 修复：UserManagementViewModel 和 UserEditViewModel 中硬编码 "admin" 改为 SessionManager.CurrentUserName |
| 4.1 程序集加载 | 待执行 | - | - | - |
| 4.2 方案文件加载 | 待执行 | - | - | - |
| 4.3 全局变量设置 | 待执行 | - | - | - |
| 4.4 检测回调 | 待执行 | - | - | - |
| 4.5 资源清理 | 代码实现完成，待运行时验证 | 2026-06-01 | Malu | 实现：DetectionView Unloaded 时停止连续检测；Dispose 时完整清理 VmRenderControl 和事件订阅；VmIntegrationService.Cleanup() 关闭 VmSolution.Instance.CloseSolution() |
| 5.1 文件存在性 | 待执行 | - | - | - |
| 5.2 表结构 | 待执行 | - | - | - |
| 5.3 种子数据 | 待执行 | - | - | - |
| 5.4 软删除过滤 | 待执行 | - | - | - |
| 5.5 索引性能 | 待执行 | - | - | - |
| 6.1 完整检测流程 | 待执行 | - | - | - |
| 6.2 异常流程 | 待执行 | - | - | - |
| 6.3 数据一致性 | 待执行 | - | - | - |
| 6.4 性能稳定性 | 待执行 | - | - | - |

---

## 附录：常用调试命令

```bash
# 查看 git 状态
git status

# 查看修改文件
git diff --name-only

# 编译项目
msbuild TripleDetection.csproj -t:Rebuild -p:Configuration=Debug

# 检查 SQLite 数据库
sqlite3 Config/tripledetection.db "SELECT * FROM Users;"
```

---

*文档生成日期：2026-05-31*