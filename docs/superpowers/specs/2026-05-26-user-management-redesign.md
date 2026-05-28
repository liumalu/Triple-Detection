# 用户管理模块重构设计方案

> **日期:** 2026-05-26
> **状态:** 已批准，待实现

## 背景

用户管理模块经过多次调试发现以下问题：
1. `SimpleJsonHelper` 的自定义 JSON 解析器有 bug（数组元素反序列化失败）
2. 数据持久化配置文件（`Config/users.json`）初始为空，每次构建覆盖了 bin/Debug 的数据
3. 现有架构无仓储模式、无依赖注入，数据流不清晰
4. ViewModel 和 View 代码耦合，参考 ProductListView 模式重构

## 目标

参考 `ProductListView` 的架构模式，重构用户管理模块：
- JSON 文件持久化（复用 SimpleJsonHelper）
- 仓储模式（Repository Pattern）
- 分页功能
- MVVM 架构

## 架构

```
UserManagementView (XAML)
         ↓ 数据绑定
UserManagementViewModel
         ↓ 调用
UserService (业务服务层)
         ↓ 调用
UserRepository (数据访问层，仓储模式)
         ↓ 持久化
Config/users.json (SimpleJsonHelper)
```

## 新建文件清单

| 文件路径 | 职责 |
|---------|------|
| `TripleDetection.Data/Repositories/IUserRepository.cs` | 仓储接口，定义泛型 CRUD + `Find()` + `Count()` |
| `TripleDetection.Data/Repositories/UserRepository.cs` | 基于 JSON + SimpleJsonHelper 的实现 |
| `TripleDetection.Data/Entities/UserQuery.cs` | 查询条件：`Username`(Contains), `Role`(exact), `StatusText`(exact), 继承 `PagedQuery` |
| `TripleDetection.App/ViewModels/UserManagementViewModel.cs` | 重构：搜索/分页/CRUD，`SearchCommand`, `ResetCommand`, `OpenEditWindowCommand` |
| `TripleDetection.App/Views/UserEditWindow.xaml` | 重构自 UserEditDialog，改用标准 Window + MVVM 绑定 |
| `TripleDetection.App/Views/UserEditWindow.xaml.cs` | Code-behind，Owner=MainWindow，DialogResult 处理 |
| `TripleDetection.App/ViewModels/UserEditViewModel.cs` | 编辑窗口 ViewModel，属性：`Username`, `RealName`, `Password`, `Role`, `IsEnabled` |
| `TripleDetection.App/Views/UserManagementView.xaml` | 重构视图，对齐 ProductListView 布局 + 分页控件 |
| `TripleDetection.App/Views/UserManagementView.xaml.cs` | Code-behind，按钮事件路由到 ViewModel |

## 删除文件清单

| 文件 | 原因 |
|------|------|
| `Services/Settings/UserService.cs` | 功能迁移到 UserRepository |
| `ViewModels/UserManagementViewModel.cs` (旧) | 被新文件替换 |
| `Views/UserEditDialog.xaml` | 被 UserEditWindow 替换 |
| `Views/UserEditDialog.xaml.cs` | 被 UserEditWindow 替换 |
| `Views/UserManagementView.xaml` (旧) | 被新文件替换 |
| `Views/UserManagementView.xaml.cs` (旧) | 被新文件替换 |

## 关键设计

### 1. UserQuery (查询条件)

```csharp
public class UserQuery : PagedQuery
{
    public string Username { get; set; }      // Contains 模糊匹配
    public string Role { get; set; }           // Exact 精确匹配
    public string StatusText { get; set; }     // Exact 精确匹配
}
```

### 2. IUserRepository 接口

```csharp
public interface IUserRepository
{
    IEnumerable<User> GetAll();
    IEnumerable<User> Find(Expression<Func<User, bool>> predicate);
    void Add(User entity);
    void Update(User entity);
    void Delete(string username);
    User GetByUsername(string username);
    int Count();
    int Count(Expression<Func<User, bool>> predicate);
    PagedResult<User> Query(UserQuery query);
}
```

### 3. UserRepository 实现

- 内部持有 `_users` 列表（内存缓存）
- `EnsureLoaded()` 在首次访问时加载 JSON
- `Save()` 在每次修改后保存 JSON
- `Query(UserQuery)` 支持分页 + 过滤

### 4. UserEditViewModel

```csharp
public class UserEditViewModel
{
    public User User { get; set; }
    public bool IsEdit { get; }  // true=编辑，false=新增
    public ObservableCollection<string> Roles { get; }  // Admin/Supervisor/Operator/Viewer
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
}
```

### 5. 分页控件

与 ProductListView 完全一致：
- 首页 / 上一页 / 下一页 / 末页 按钮
- 当前页 / 总页数 显示 (`当前页/总页数`)
- 每页条数（默认 20，可配置）

## 数据流

```
用户点击"查询" → ViewModel.Search() → UserService.Query() → UserRepository.Query()
                                                      ↓
JSON file ← SimpleJsonHelper.Save ← UserRepository.Save ← UserService
                                                      ↓
ObservableCollection<User> ← PagedResult<User> ← UserRepository.Query()
```

## 验证标准

1. 应用启动后，用户列表显示 3 条初始数据（admin/supervisor/operator）
2. 点击"新增用户"弹出编辑窗口，填写后保存，列表刷新显示新用户
3. 点击"编辑"弹出编辑窗口，修改后保存，列表刷新显示更新后数据
4. 点击"删除"弹出确认框，确认后用户从列表移除
5. 启用/禁用/锁定/解锁按钮操作后，状态立即更新并保存到 JSON
6. 查询条件（用户名/角色/状态）可正确筛选用户
7. 分页导航（首页/上一页/下一页/末页）正常工作
8. 关闭应用再启动，数据从 JSON 正确加载

## 技术约束

- **C# 8.0**（LangVersion=8.0，.NET Framework 4.8）
- 不使用 C# 9.0+ 特性（record types, switch expressions 等）
- 参考现有 ProductListView 的代码风格和架构
- 不修改检测、仪表盘、日志等其他模块