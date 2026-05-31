# DI 架构修复 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让 LoginWindow、MainWindow、DetectionView 通过 DI 构造注入获取 ViewModel，实现完整的构造注入模式

**Architecture:** WPF 中通过构造注入将 ViewModel 注入 View，MainWindow 使用 ContentControl + NavigationService 动态加载 DetectionView，其他 Dialog 窗口统一使用构造注入模式

**Tech Stack:** WPF, Microsoft.Extensions.DI, CommunityToolkit.Mvvm

---

## 文件结构映射

| 文件 | 操作 | 职责 |
|------|------|------|
| `Presentation/Views/LoginWindow.xaml.cs` | 修改 | LoginWindow 构造注入 LoginViewModel |
| `Presentation/MainWindow.xaml` | 修改 | 添加 MainContentRegion ContentControl，移除硬编码 DetectionView |
| `Presentation/MainWindow.xaml.cs` | 修改 | 构造注入 MainViewModel + NavigationService |
| `Presentation/Views/Detection/DetectionView.xaml.cs` | 修改 | 构造注入 MainViewModel，移除手动 new 服务 |

---

### Task 1: LoginWindow 构造注入 LoginViewModel

**Files:**
- Modify: `Presentation/Views/LoginWindow.xaml.cs:16-30`

- [ ] **Step 1: 修改 LoginWindow 构造函数**

```csharp
public LoginWindow(LoginViewModel viewModel)
{
    InitializeComponent();
    DataContext = viewModel;
    _viewModel = viewModel;

    _viewModel.LoginSucceeded += OnLoginSucceeded;
    _viewModel.OnLoginFailed += OnLoginFailed;

    LoadLogo();
    LoadSystemName();

    UsernameTextBox.Focus();
}
```

- [ ] **Step 2: 添加 private field**

在 class 内添加：
```csharp
private readonly LoginViewModel _viewModel;
```

- [ ] **Step 3: 验证编译**

Run: `"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" TripleDetection.csproj -t:Build -p:Configuration=Debug`
Expected: LoginWindow.xaml.cs 无相关错误

- [ ] **Step 4: Commit**

```bash
git add Presentation/Views/LoginWindow.xaml.cs
git commit -m "refactor: inject LoginViewModel via constructor in LoginWindow"
```

---

### Task 2: MainWindow 构造注入 MainViewModel + NavigationService

**Files:**
- Modify: `Presentation/MainWindow.xaml`
- Modify: `Presentation/MainWindow.xaml.cs`

- [ ] **Step 1: 修改 MainWindow.xaml - 添加 ContentControl 区域**

在 `<Grid>` 内，将原来的硬编码 DetectionView 替换为 ContentControl：

```xml
<!-- Center content area -->
<Border Grid.Column="0" Grid.Row="1" Margin="5" BorderBrush="#606060" BorderThickness="1">
    <ContentControl x:Name="MainContentRegion"/>
</Border>
```

移除原来的 `<local:DetectionView x:Name="DetectionViewContent"/>`（如果存在）

- [ ] **Step 2: 修改 MainWindow.xaml.cs - 构造注入**

```csharp
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly NavigationService _navigationService;

    public MainWindow(MainViewModel viewModel, NavigationService navigationService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _navigationService = navigationService;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _navigationService.SetRegion(MainContentRegion);
        _navigationService.RegisterRoute("Detection", typeof(Views.Detection.DetectionView));
        _navigationService.NavigateTo<Views.Detection.DetectionView>("Detection");
    }
}
```

- [ ] **Step 3: 添加 using**

```csharp
using TripleDetection.Presentation.ViewModels.Detection;
using TripleDetection.Presentation.Navigation;
using TripleDetection.Presentation.Views.Detection;
```

- [ ] **Step 4: 验证编译**

Run: `"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" TripleDetection.csproj -t:Build -p:Configuration=Debug`
Expected: MainWindow 相关的 DI 链错误应消失

- [ ] **Step 5: Commit**

```bash
git add Presentation/MainWindow.xaml Presentation/MainWindow.xaml.cs
git commit -m "refactor: inject MainViewModel and NavigationService in MainWindow, add ContentControl region"
```

---

### Task 3: DetectionView 构造注入 MainViewModel

**Files:**
- Modify: `Presentation/Views/Detection/DetectionView.xaml.cs`

- [ ] **Step 1: 修改 DetectionView 构造函数**

保留现有的直接实例化服务（VmIntegrationService 等），但移除手动 new MainViewModel：

```csharp
public partial class DetectionView : UserControl
{
    private readonly LoggingService _logService;
    private readonly MainViewModel _viewModel;
    private readonly VmIntegrationService _vmService;
    // ... 其他 fields

    public DetectionView(MainViewModel viewModel, LoggingService logService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _logService = logService;

        // VmIntegrationService 暂时仍手动构造，但传入已注入的服务
        var detectionRecordService = new DetectionRecordService(
            new SqliteRepositoryFactory(
                new SqliteConnectionFactory(
                    $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tripledetection.db")}")
            ).CreateDetectionRecordRepository()
        );
        _vmService = new VmIntegrationService(null, _logService, detectionRecordService);
        _vmService.OnDetectionResult += VmService_OnDetectionResult;

        LoadTasks();
        SubscribeToLogs();

        _logService.Log("检测页面已加载");
    }
}
```

- [ ] **Step 2: 添加 using**

```csharp
using TripleDetection.Presentation.ViewModels.Detection;
using TripleDetection.Application.VmServices;
using TripleDetection.Application.Services;
using TripleDetection.Infrastructure.Persistence;
using TripleDetection.Infrastructure.Repositories;
```

- [ ] **Step 3: 验证编译**

Run: `"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" TripleDetection.csproj -t:Build -p:Configuration=Debug`
Expected: DetectionView.xaml.cs 构造注入相关错误应减少

- [ ] **Step 4: Commit**

```bash
git add Presentation/Views/Detection/DetectionView.xaml.cs
git commit -m "refactor: inject MainViewModel and LoggingService via constructor in DetectionView"
```

---

### Task 4: 注册 DetectionView 到 DI 容器

**Files:**
- Modify: `Presentation/App.xaml.cs`

- [ ] **Step 1: 在 ConfigureServices 中添加 DetectionView 注册**

找到 ViewModel 注册附近，添加：

```csharp
// Views (transient)
services.AddTransient<LoginWindow>();
services.AddTransient<MainWindow>();
services.AddTransient<Views.Detection.DetectionView>();
```

- [ ] **Step 2: 验证编译**

Run: `"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" TripleDetection.csproj -t:Build -p:Configuration=Debug`
Expected: 无新增错误

- [ ] **Step 3: Commit**

```bash
git add Presentation/App.xaml.cs
git commit -m "feat: register DetectionView in DI container"
```

---

## 自检清单

- [ ] Spec coverage: LoginWindow → MainWindow → DetectionView 核心流程已覆盖
- [ ] Placeholder scan: 无 TBD/TODO，所有步骤包含实际代码
- [ ] Type consistency: MainViewModel、NavigationService、LoginViewModel 等类型名在所有任务中一致

## 执行选择

**Plan complete and saved to `docs/superpowers/plans/2026-05-31-di-architecture-refactor-plan.md`. Two execution options:**

**1. Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

Which approach?