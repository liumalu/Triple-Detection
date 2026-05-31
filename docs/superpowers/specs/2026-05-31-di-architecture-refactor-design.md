# DI 架构修复设计

**日期**: 2026-05-31
**状态**: 已批准

## 目标

修复 Triple-Detection 项目中从 Prism.DryIoc 迁移到 Microsoft.Extensions.DI 过程中遗留的架构不一致问题，让所有 View 通过 DI 容器获取 ViewModel，实现完整的构造注入模式。

## 问题描述

项目处于双重架构混合状态：
- App.xaml.cs 已配置 Microsoft.Extensions.DI 容器
- 部分 View/ViewModel 已迁移到 CommunityToolkit.Mvvm
- 但大量 View 仍通过 `x:Name` 在 code-behind 手动 `new ViewModel()`，而 ViewModel 构造函数需要 DI 服务
- 导致 140+ 个编译错误，无法构建

## 修复范围

### 核心流程（优先修复）
1. **LoginWindow** → **MainWindow** → **DetectionView** 登录-主页-检测主流程

### Dialog 窗口（后续修复）
- TaskListView、TaskEditWindow
- ProductListView、ProductEditWindow
- UserManagementView、UserEditWindow
- SettingsView、SettingsShellViewModel

## 设计方案

### 1. LoginWindow（入口）

**现状**: 在 App.xaml.cs 中已注册 DI，ViewModel 构造需要 IUserService

**修复**:
- LoginWindow 构造注入 `LoginViewModel`
- 移除 code-behind 的手动 new

```csharp
public LoginWindow(LoginViewModel viewModel)
{
    InitializeComponent();
    DataContext = viewModel;
    viewModel.LoginSucceeded += OnLoginSucceeded;
    viewModel.OnLoginFailed += OnLoginFailed;
}
```

### 2. MainWindow（主容器）

**现状**: XAML 直接实例化 DetectionView，code-behind 手动构造服务

**修复**:
- MainWindow 构造注入 `MainViewModel` 和 `NavigationService`
- DetectionView 改为 `ContentControl` 区域模式
- MainWindow 在 Loaded 时设置 NavigationService 的 Region

```csharp
public MainWindow(MainViewModel viewModel, NavigationService navigationService)
{
    InitializeComponent();
    DataContext = viewModel;
    _navigationService = navigationService;
}

private void Window_Loaded(object sender, RoutedEventArgs e)
{
    _navigationService.SetRegion(MainContentRegion);
    _navigationService.RegisterRoute("Detection", typeof(DetectionView));
    _navigationService.NavigateTo<DetectionView>("Detection");
}
```

XAML 改动：
- 移除 XAML 中硬编码的 `<local:DetectionView x:Name="DetectionViewContent"/>`
- 改为 `<ContentControl x:Name="MainContentRegion"/>`

### 3. DetectionView（检测核心）

**现状**: UserControl，code-behind 手动 new LoggingService、MainViewModel、VmIntegrationService 等

**修复**:
- DetectionView 构造注入 `MainViewModel` 和 `LoggingService`
- 移除 code-behind 中所有手动 new 的服务
- View 与 ViewModel 通过数据绑定交互

```csharp
public DetectionView(MainViewModel viewModel, LoggingService logService, VmIntegrationService vmService)
{
    InitializeComponent();
    DataContext = viewModel;
    _logService = logService;
    _vmService = vmService;
    _vmService.OnDetectionResult += VmService_OnDetectionResult;
    LoadTasks();
    SubscribeToLogs();
}
```

**注意**: VmIntegrationService 等视觉算法相关服务较复杂，暂时保留直接实例化，但通过 DI 获取 LoggingService 等基础服务。

### 4. Dialog 窗口模式

所有编辑窗口（TaskEditWindow、ProductEditWindow、UserEditWindow 等）采用统一模式：

```csharp
public TaskEditWindow(TaskEditViewModel viewModel)
{
    InitializeComponent();
    DataContext = viewModel;
    viewModel.RequestClose += (s, result) =>
    {
        DialogResult = result;
        Close();
    };
}
```

调用时：
```csharp
var viewModel = App.Services.GetRequiredService<TaskEditViewModel>();
var window = new TaskEditWindow(viewModel) { Owner = this };
if (window.ShowDialog() == true)
{
    Refresh();
}
```

## 架构图

```
App.xaml.cs (DI Container)
│
├── LoginWindow (DI) ──────────→ LoginViewModel (DI) ──────────→ IUserService
│
├── MainWindow (DI) ───────────→ MainViewModel (DI)
│    │
│    └── ContentControl (MainContentRegion)
│         └── DetectionView (DI) ───────────→ MainViewModel (DI)
│                                        ├── LoggingService (DI)
│                                        └── VmIntegrationService (手动)
│
└── TaskEditWindow (DI) ────────→ TaskEditViewModel (DI)
     ProductEditWindow (DI) ────→ ProductEditViewModel (DI)
     UserEditWindow (DI) ────────→ UserEditViewModel (DI)
```

## DI 容器配置（App.xaml.cs）

现有配置已完整：

```csharp
// Infrastructure
services.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory(connectionString));
services.AddSingleton<IRepositoryFactory>(sp => new SqliteRepositoryFactory(...));
services.AddTransient(typeof(IRepository<>), typeof(SqliteRepository<>));

// Services
services.AddSingleton(new LoggingService(logPath));
services.AddSingleton(new ImageStorageService(okDir, ngDir));
services.AddSingleton<VmIntegrationService>();

// Application Services
services.AddTransient<IUserService, UserService>();
services.AddTransient<IProductService, ProductService>();
services.AddTransient<ITaskService, TaskService>();

// ViewModels
services.AddTransient<LoginViewModel>();
services.AddTransient<MainViewModel>();
services.AddTransient<TaskListViewModel>();
// ... 其他 ViewModel

// Views
services.AddTransient<LoginWindow>();
services.AddTransient<MainWindow>();
services.AddTransient<DetectionView>();
```

## 关键设计决策

| 决策 | 选择 | 原因 |
|------|------|------|
| LoginWindow | 构造注入 ViewModel | 已有 DI 注册，改动最小 |
| MainWindow | ContentControl 区域 + NavigationService | 支持动态切换_detection 内容区域 |
| DetectionView | 构造注入 ViewModel | UserControl 支持构造注入 |
| VmIntegrationService | 暂时保留手动实例化 | 视觉算法 SDK 初始化复杂，后续优化 |
| Dialog windows | 构造注入 ViewModel | 统一模式，ShowDialog 仍通过 DI Resolve |

## 数据绑定约定

DetectionView 中移除 code-behind 逻辑后，原有交互通过绑定实现：

| 原 code-behind 逻辑 | 迁移方式 |
|--------------------|---------|
| `_logService.Log(...)` | ViewModel 方法，绑定 Command |
| `_okCount` / `_ngCount` | ViewModel 属性，绑定显示 |
| `lstDetectionLogs.Items.Insert(...)` | ViewModel ObservableCollection，绑定 ItemsSource |

## 后续优化项

1. **VmIntegrationService DI 化** — 将 ImageStorageService、LoggingService 通过 DI 注入
2. **其他 View/ViewModel 修复** — TaskListView、ProductListView 等
3. **ViewModelLocator** — 如需更灵活的 View-ViewModel 映射，可引入 ViewModelLocator