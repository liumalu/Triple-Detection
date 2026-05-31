# MVVM Framework Migration Design — Prism 9.x → CommunityToolkit.Mvvm

> **日期:** 2026-05-31
> **状态:** 已批准
> **目标:** 将 Presentation 层从 Prism 9.x 迁移到 CommunityToolkit.Mvvm + Microsoft.Extensions.DependencyInjection，目标框架保持 net8.0-windows

---

## 1. 背景与约束

**问题:** Prism 9.0.537 官方版本无 net8.0-windows 支持（最高 net6.0-windows7.0）。用户明确要求 net8.0-windows 技术选型。

**迁移范围:** Presentation 层（App.xaml.cs、ViewModels、Events）。Application / Infrastructure / Domain 层不受影响。

**不变:**
- 所有 Domain entities、enums、repository 接口
- 所有 Application services
- 所有 Infrastructure repositories、DbContext
- XAML Views（绑定语法不变）
- 数据库 schema

---

## 2. 技术替换映射

### 2.1 NuGet 包变更

| 操作 | 包 | 版本 |
|---|---|---|
| 移除 | `Prism.DryIoc` | 9.0.537 |
| 新增 | `CommunityToolkit.Mvvm` | 8.x (net8.0-windows ✅) |
| 新增 | `Microsoft.Extensions.DependencyInjection` | 8.x |

### 2.2 DI 容器

| Prism 9.x | 迁移后 |
|---|---|
| `PrismApplication` (DryIoc) | `Application` + `IServiceCollection` + `ServiceProvider` |
| `IContainerRegistry` | `IServiceCollection` |
| `container.Register<TInterface, TImpl>(Reuse.Transient)` | `services.AddTransient<TInterface, TImpl>()` |
| `container.Register<T>(Reuse.Singleton)` | `services.AddSingleton<T>()` |
| `container.RegisterInstance<T>(instance)` | `services.AddSingleton<T>(instance)` |
| `container.RegisterTypeForNavigation<T>("name")` | `services.AddTransient<T>()` + 路由字典 |

### 2.3 MVVM 基类

| Prism 9.x | CommunityToolkit.Mvvm |
|---|---|
| `using Prism.Mvvm; BindableBase` | `using CommunityToolkit.Mvvm; ObservableObject` |
| `using Prism.Commands; DelegateCommand` | `using CommunityToolkit.Mvvm; RelayCommand` |
| `using Prism.Commands; DelegateCommand<T>` | `using CommunityToolkit.Mvvm; RelayCommand<T>` |
| `DelegateCommand.Execute()` | `RelayCommand.Execute()` (相同) |
| `.ObservesProperty(() => Prop)` | `.ObserveProperty(nameof(Prop))` |

### 2.4 事件聚合

| Prism 9.x | CommunityToolkit.Mvvm |
|---|---|
| `using Prism.Events; PubSubEvent<T>` | `using CommunityToolkit.Mvvm;` |
| `IEventAggregator` | `IMessenger` (WeakReferenceMessenger.Default) |
| `eventAggregator.GetEvent<TEvent>().Subscribe(h)` | `messenger.Register<TMsg>(recipient, h)` |
| `eventAggregator.GetEvent<TEvent>().Publish(payload)` | `messenger.Send(payload)` 或 `.Publish(payload)` |
| `PubSubEvent<T>` (泛型继承) | 不需要 — 直接定义消息类 |

**注意:** Prism `PubSubEvent<T>` 默认线程安全（跨线程 Publish 自动 marshall 到订阅线程）。`WeakReferenceMessenger` 无此保证，需在 UI 线程上调 `Device.BeginInvokeOnMainThread`。

### 2.5 导航

| Prism 9.x | 迁移后 |
|---|---|
| `IRegionManager` | 自定义 `INavigationService` |
| `regionManager.RequestNavigate("region", typeof(T))` | `navigationService.NavigateTo<T>()` |
| `RegisterTypeForNavigation<T>("key")` | `导航服务构造函数注入` |
| `PrismRegionAdapter` | WPF `ContentControl` + 动态切换 DataContext |

### 2.6 生命周期

| Prism 9.x | 迁移后 |
|---|---|
| `OnInitialized()` (PrismApplication override) | `Application.Startup` 事件 |
| `CreateShell()` (返回 Window) | `OnStartup` 内直接 `Show()` |
| `OnResumeDispatch()` | `Application.Activated` 事件 |

---

## 3. 事件替换详情

### 3.1 DetectionResultEvent

**Prism:**
```csharp
// VmIntegrationService.cs
_eventAggregator.GetEvent<DetectionResultEvent>().Publish(result);

// MainViewModel.cs
_eventAggregator.GetEvent<DetectionResultEvent>().Subscribe(OnResult);
```

**迁移后:**
```csharp
// 定义消息类 (infrastructure/shared)
public record DetectionResultMessage(DetectionResult Result);

// VmIntegrationService.cs
WeakReferenceMessenger.Default.Send(new DetectionResultMessage(result));

// MainViewModel.cs (构造函数中)
WeakReferenceMessenger.Default.Register<DetectionResultMessage>(this, (r, m) => OnResult(m.Result));
```

### 3.2 LogAddedEvent

**Prism:**
```csharp
// LoggingService.cs
_eventAggregator.GetEvent<LogAddedEvent>().Publish(new LogEntry { Message = msg });

// MainViewModel.cs
_eventAggregator.GetEvent<LogAddedEvent>().Subscribe(OnLogAdded);
```

**迁移后:**
```csharp
// 定义消息类
public record LogAddedMessage(string Message);

// LoggingService.cs
WeakReferenceMessenger.Default.Send(new LogAddedMessage(msg));

// MainViewModel.cs
WeakReferenceMessenger.Default.Register<LogAddedMessage>(this, (r, m) => AddLog(m.Message));
```

### 3.3 ViewOpenedEvent / ViewClosedEvent / ActiveViewChangedEvent

同上替换模式：`PubSubEvent<T>` → `record TMessage`。

---

## 4. 导航服务设计

### 4.1 接口

```csharp
public interface INavigationService
{
    void NavigateTo<TView>() where TView : class;
    void NavigateTo<TView>(string key) where TView : class;
    string CurrentViewKey { get; }
    event Action<string> Navigated;
}
```

### 4.2 实现要点

- 维护 `Dictionary<string, Type>` 路由表（注册时填充）
- 维护 `ContentControl` 引用（构造函数注入或 XAML 关联）
- `NavigateTo<T>()` 从路由表取 Type，用 `IServiceProvider` Resolve 实例
- 切换 `ContentControl.Content` + 更新 `DataContext`
- `Navigated` 事件通知订阅者（如 TabItemViewModel）

---

## 5. App.xaml.cs 引导程序设计

```csharp
public partial class App : Application
{
    private IServiceProvider? _services;
    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "TripleDetectionApp_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("应用程序已在运行中。", "提示");
            Shutdown(); return;
        }

        InitializeDatabase();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();

        // 显示登录窗口
        var loginWindow = _services.GetRequiredService<LoginWindow>();
        if (loginWindow.ShowDialog() != true) { Shutdown(); return; }

        var mainWindow = _services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
        mainWindow.Activate();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Infrastructure
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tripledetection.db");
        services.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory($"Data Source={dbPath}"));
        services.AddSingleton<IRepositoryFactory>(new SqliteRepositoryFactory($"Data Source={dbPath}"));
        services.AddTransient(typeof(IRepository<>), typeof(SqliteRepository<>));
        services.AddTransient<IAuditLogRepository, AuditLogRepository>();
        services.AddTransient<IDetectionRecordRepository, DetectionRecordRepository>();

        // Application services
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IProductService, ProductService>();
        services.AddTransient<ITaskService, TaskService>();
        services.AddTransient<IAuditLogService, AuditLogService>();
        services.AddTransient<IDetectionRecordService, DetectionRecordService>();
        services.AddTransient<CommunicationSettingsService>();
        services.AddTransient<VmSettingsService>();
        services.AddTransient<SystemSettingsService>();
        services.AddTransient<DeviceControlSettingsService>();
        services.AddSingleton<SettingsSyncService>();

        // VM (singleton)
        services.AddSingleton<VmIntegrationService>();

        // Logging (singleton)
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        services.AddSingleton(new LoggingService(logPath));

        // Image Storage (singleton)
        var okDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "OK");
        var ngDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "NG");
        services.AddSingleton(new ImageStorageService(okDir, ngDir));

        // Navigation service
        services.AddSingleton<INavigationService, NavigationService>();

        // ViewModels (transient)
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<TabItemViewModel>();
        services.AddTransient<UserManagementViewModel>();
        services.AddTransient<UserEditViewModel>();
        services.AddTransient<ProductListViewModel>();
        services.AddTransient<ProductEditViewModel>();
        services.AddTransient<TaskListViewModel>();
        services.AddTransient<TaskEditViewModel>();
        services.AddTransient<SettingsShellViewModel>();

        // Views (transient)
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();
    }

    private void InitializeDatabase()
    {
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tripledetection.db");
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        DatabaseInitializer.Initialize();
    }
}
```

---

## 6. ViewModel 替换示例

### 登录 (LoginViewModel)

**Prism:**
```csharp
using Prism.Mvvm;
using Prism.Commands;

public class LoginViewModel : BindableBase
{
    public DelegateCommand LoginCommand { get; }
        = new DelegateCommand(ExecuteLogin, CanExecuteLogin)
            .ObservesProperty(() => IsLoading)
            .ObservesProperty(() => Username);
}
```

**迁移后:**
```csharp
using CommunityToolkit.Mvvm;

public partial class LoginViewModel : ObservableObject
{
    public IRelayCommand LoginCommand { get; }

    public LoginViewModel(IUserService userService)
    {
        LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
    }

    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private bool _isLoading;
    // ...
}
```

---

## 7. 文件变更清单

### 新增
- `Presentation/Navigation/INavigationService.cs`
- `Presentation/Navigation/NavigationService.cs`
- `Presentation/Messages/DetectionResultMessage.cs` (record)
- `Presentation/Messages/LogAddedMessage.cs` (record)
- `Presentation/Messages/ViewOpenedMessage.cs` (record)
- `Presentation/Messages/ViewClosedMessage.cs` (record)
- `Presentation/Messages/ActiveViewChangedMessage.cs` (record)

### 修改
- `TripleDetection.csproj` — 包引用替换
- `Presentation/App.xaml` — 移除 PrismApplication 关联
- `Presentation/App.xaml.cs` — DI 引导程序重写
- `Presentation/Events/DetectionEvents.cs` — 替换为 record
- `Presentation/Events/LogEvents.cs` — 替换为 record
- `Presentation/Events/NavigationEvents.cs` — 替换为 record
- `Presentation/ViewModels/**/*.cs` — BindableBase→ObservableObject, DelegateCommand→RelayCommand
- `Presentation/ViewModels/Detection/MainViewModel.cs` — IRegionManager→INavigationService, event aggregator→messenger

### 删除
- `Presentation/Events/DetectionEvents.cs` (原 PubSubEvent 版本)
- `Presentation/Events/LogEvents.cs` (原 PubSubEvent 版本)
- `Presentation/Events/NavigationEvents.cs` (原 PubSubEvent 版本)

---

## 8. 工作量估算

| 模块 | 文件数 | 复杂度 |
|---|---|---|
| 包引用 + csproj | 1 | 低 |
| App.xaml.cs 引导 | 1 | 高 |
| 事件系统 (record 定义) | 5 | 低 |
| 导航服务 | 2 | 中 |
| ViewModels 替换 | ~13 | 中 |
| 其他 (App.xaml 等) | ~3 | 低 |

**预计总文件变更: ~25 个**

---

## 9. 验证

1. `MSBuild -t:Rebuild` → 0 errors
2. 启动应用 → 登录窗口正常显示
3. 登录成功 → MainWindow 正常显示，导航正常
4. 执行一次检测流程 → DetectionResultEvent 正常触发
5. 检查 logs 目录 → 日志正常写入
6. 所有 ViewModels 的 property changed 正常工作
