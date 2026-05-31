# MVVM Framework Migration Plan — Prism 9.x → CommunityToolkit.Mvvm

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate Presentation layer from Prism 9.x (DryIoc + BindableBase + IEventAggregator) to CommunityToolkit.Mvvm (Microsoft.Extensions.DI + ObservableObject + WeakReferenceMessenger), targeting net8.0-windows.

**Architecture:** Replace Prism bootstrapper with Microsoft.Extensions.DependencyInjection, replace Prism MVVM base classes with CommunityToolkit equivalents, replace Prism IEventAggregator with WeakReferenceMessenger, replace IRegionManager navigation with a custom INavigationService.

**Tech Stack:** CommunityToolkit.Mvvm 8.x, Microsoft.Extensions.DependencyInjection 8.x, WPF net8.0-windows

---

## Dependency Map (all Presentation layer)

```
App.xaml.cs               ← PrismApplication → Application + Microsoft.Extensions.DI
App.xaml                  ← PrismApplication → Application
Events/DetectionEvents.cs ← PubSubEvent<T> → message record
Events/LogEvents.cs      ← PubSubEvent<T> → message record
Events/NavigationEvents.cs ← PubSubEvent<T> → message record
ViewModels/LoginViewModel.cs        ← BindableBase → ObservableObject, DelegateCommand → RelayCommand
ViewModels/MainViewModel.cs         ← + IRegionManager → INavigationService, event → messenger
ViewModels/TabItemViewModel.cs       ← BindableBase → ObservableObject, DelegateCommand → RelayCommand
ViewModels/SettingsShellViewModel.cs ← BindableBase → ObservableObject, DelegateCommand → RelayCommand
ViewModels/Auth/*.cs               ← BindableBase → ObservableObject
ViewModels/Production/*.cs         ← BindableBase → ObservableObject
Application/VmServices/VmIntegrationService.cs ← IEventAggregator → WeakReferenceMessenger
Application/Services/LoggingService.cs        ← IEventAggregator → WeakReferenceMessenger, remove LogAddedEvent placeholder
```

**Application and Infrastructure layers are NOT changed** — they have no Prism using statements.

---

## Task 1: Update csproj Package References

**Files:**
- Modify: `TripleDetection.csproj`

**Steps:**

- [ ] **Step 1: Remove Prism.DryIoc and add CommunityToolkit.Mvvm + Microsoft.Extensions.DI**

Find in `TripleDetection.csproj`:
```xml
<PackageReference Include="Prism.DryIoc" Version="9.0.537" />
```

Replace with:
```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
```

- [ ] **Step 2: Verify package restore**

Run: `dotnet restore TripleDetection.csproj`
Expected: `CommunityToolkit.Mvvm 8.3.2` and `Microsoft.Extensions.DependencyInjection 8.0.1` restored successfully

- [ ] **Step 3: Commit**

```bash
git add TripleDetection.csproj
git commit -m "chore: replace Prism.DryIoc with CommunityToolkit.Mvvm + DI"
```

---

## Task 2: Create Message Record Types

**Files:**
- Create: `Presentation/Messages/DetectionResultMessage.cs`
- Create: `Presentation/Messages/LogAddedMessage.cs`
- Create: `Presentation/Messages/ViewOpenedMessage.cs`
- Create: `Presentation/Messages/ViewClosedMessage.cs`
- Create: `Presentation/Messages/ActiveViewChangedMessage.cs`

**Steps:**

- [ ] **Step 1: Create DetectionResultMessage.cs**

```csharp
using TripleDetection.Presentation.Models;

namespace TripleDetection.Presentation.Messages;

public record DetectionResultMessage(DetectionResult Result);
```

- [ ] **Step 2: Create LogAddedMessage.cs**

```csharp
namespace TripleDetection.Presentation.Messages;

public record LogAddedMessage(string Message);
```

- [ ] **Step 3: Create ViewOpenedMessage.cs**

```csharp
namespace TripleDetection.Presentation.Messages;

public record ViewOpenedMessage(string Tag, string DisplayName);
```

- [ ] **Step 4: Create ViewClosedMessage.cs**

```csharp
namespace TripleDetection.Presentation.Messages;

public record ViewClosedMessage(string Tag);
```

- [ ] **Step 5: Create ActiveViewChangedMessage.cs**

```csharp
namespace TripleDetection.Presentation.Messages;

public record ActiveViewChangedMessage(string Tag);
```

- [ ] **Step 6: Commit**

```bash
git add Presentation/Messages/
git commit -m "feat: add message records for WeakReferenceMessenger event replacement"
```

---

## Task 3: Create Navigation Service

**Files:**
- Create: `Presentation/Navigation/INavigationService.cs`
- Create: `Presentation/Navigation/NavigationService.cs`

**Steps:**

- [ ] **Step 1: Create INavigationService.cs**

```csharp
namespace TripleDetection.Presentation.Navigation;

public interface INavigationService
{
    void NavigateTo<TView>() where TView : class;
    void NavigateTo<TView>(string key) where TView : class;
    string CurrentViewKey { get; }
    event Action<string> Navigated;
}
```

- [ ] **Step 2: Create NavigationService.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace TripleDetection.Presentation.Navigation;

public class NavigationService : INavigationService
{
    private readonly Dictionary<string, Type> _routes = new();
    private readonly IServiceProvider _serviceProvider;
    private ContentControl? _region;

    public string CurrentViewKey { get; private set; } = "";
    public event Action<string>? Navigated;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetRegion(ContentControl region)
    {
        _region = region;
    }

    public void RegisterRoute(string key, Type viewType)
    {
        _routes[key] = viewType;
    }

    public void NavigateTo<TView>() where TView : class
    {
        foreach (var kvp in _routes)
        {
            if (kvp.Value == typeof(TView))
            {
                NavigateTo<TView>(kvp.Key);
                return;
            }
        }
        throw new InvalidOperationException($"View {typeof(TView).Name} not registered");
    }

    public void NavigateTo<TView>(string key) where TView : class
    {
        if (_region == null) throw new InvalidOperationException("Region not set. Call SetRegion first.");
        if (!_routes.TryGetValue(key, out var viewType) || viewType != typeof(TView))
            throw new InvalidOperationException($"Route key '{key}' does not match view type {typeof(TView).Name}");

        var view = (TView)_serviceProvider.GetService(typeof(TView))!;
        _region.Content = view;
        CurrentViewKey = key;
        Navigated?.Invoke(key);
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Presentation/Navigation/
git commit -m "feat: add INavigationService and NavigationService implementation"
```

---

## Task 4: Rewrite App.xaml.cs Bootstrapper

**Files:**
- Modify: `Presentation/App.xaml.cs`
- Modify: `Presentation/App.xaml`

**Steps:**

- [ ] **Step 1: Rewrite App.xaml.cs — full DI bootstrapper**

Read the current `Presentation/App.xaml.cs` and replace the entire body (keeping imports) with:

```csharp
using System;
using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TripleDetection.Application.Services;
using TripleDetection.Application.SettingsServices;
using TripleDetection.Application.VmServices;
using TripleDetection.Domain.Repositories;
using TripleDetection.Infrastructure.Persistence;
using TripleDetection.Infrastructure.Repositories;
using TripleDetection.Presentation.ViewModels;
using TripleDetection.Presentation.ViewModels.Auth;
using TripleDetection.Presentation.ViewModels.Detection;
using TripleDetection.Presentation.ViewModels.Production;
using TripleDetection.Presentation.ViewModels.Settings;
using TripleDetection.Presentation.Views;
using TripleDetection.Presentation.Views.App;
using TripleDetection.Presentation.Views.Audit;
using TripleDetection.Presentation.Views.Auth;
using TripleDetection.Presentation.Views.Detection;
using TripleDetection.Presentation.Views.Production;
using TripleDetection.Presentation.Views.Settings;

public partial class App : Application
{
    private IServiceProvider? _services;
    private static Mutex? _mutex;

    public static IServiceProvider Services => ((App)Current)._services!;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "TripleDetectionApp_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("应用程序已在运行中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown(); return;
        }

        InitializeDatabase();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();

        // Show login window first
        var loginWindow = _services.GetRequiredService<LoginWindow>();
        var result = loginWindow.ShowDialog();
        if (result != true) { Shutdown(); return; }

        // Then show main window
        var mainWindow = _services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
        mainWindow.WindowState = WindowState.Normal;
        mainWindow.Activate();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Infrastructure
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tripledetection.db");
        var connectionString = $"Data Source={dbPath}";
        services.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory(connectionString));
        services.AddSingleton<IRepositoryFactory>(new SqliteRepositoryFactory(connectionString));
        services.AddTransient(typeof(IRepository<>), typeof(SqliteRepository<>));
        services.AddTransient<IAuditLogRepository, AuditLogRepository>();
        services.AddTransient<IDetectionRecordRepository, DetectionRecordRepository>();

        // Logging (singleton — no longer needs IEventAggregator)
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        services.AddSingleton(new LoggingService(logPath));

        // Image Storage (singleton)
        var okDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "OK");
        var ngDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "NG");
        services.AddSingleton(new ImageStorageService(okDir, ngDir));

        // VM Integration (singleton)
        services.AddSingleton<VmIntegrationService>();

        // Settings
        services.AddTransient<CommunicationSettingsService>();
        services.AddTransient<VmSettingsService>();
        services.AddTransient<SystemSettingsService>();
        services.AddTransient<DeviceControlSettingsService>();
        services.AddSingleton<SettingsSyncService>();

        // Application Services (transient)
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IProductService, ProductService>();
        services.AddTransient<ITaskService, TaskService>();
        services.AddTransient<IAuditLogService, AuditLogService>();
        services.AddTransient<IDetectionRecordService, DetectionRecordService>();

        // Navigation service
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());

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
        try
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tripledetection.db");
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            DatabaseInitializer.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"数据库初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
```

- [ ] **Step 2: Update App.xaml — remove PrismApplication**

Read `Presentation/App.xaml`. Current content is likely:
```xml
<prism:PrismApplication x:Class="TripleDetection.Presentation.App"
                       ...>
```

Replace the root element with standard WPF Application:
```xml
<Application x:Class="TripleDetection.Presentation.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="Views/MainWindow.xaml">
</Application>
```

**Important:** Remove the `StartupUri` attribute if the startup is handled in `OnStartup` (which it now is — `OnStartup` shows windows manually). The root should be `<Application>` not `<prism:PrismApplication>`.

- [ ] **Step 3: Commit**

```bash
git add Presentation/App.xaml.cs Presentation/App.xaml
git commit -m "refactor: replace Prism bootstrapper with Microsoft.Extensions.DI"
```

---

## Task 5: Update LoggingService — Remove IEventAggregator

**Files:**
- Modify: `Application/Services/LoggingService.cs`

**Steps:**

- [ ] **Step 1: Rewrite LoggingService.cs — remove IEventAggregator, use WeakReferenceMessenger**

Read the current `Application/Services/LoggingService.cs`. Replace the entire content with:

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using TripleDetection.Presentation.Messages;

namespace TripleDetection.Application.Services;

public class LoggingService
{
    private readonly string _logPath;
    private readonly object _lockObj = new object();

    public event EventHandler<LogEntry>? OnLogAdded;

    public LoggingService(string logPath)
    {
        _logPath = logPath;
        CleanupOldLogs();
    }

    private void CleanupOldLogs()
    {
        try
        {
            if (!Directory.Exists(_logPath)) return;
            var threshold = TimeSpan.FromDays(30);
            var now = DateTime.Now;
            foreach (var file in Directory.GetFiles(_logPath, "*.log"))
            {
                try
                {
                    var fi = new FileInfo(file);
                    if ((now - fi.LastWriteTime) > threshold) File.Delete(file);
                }
                catch { }
            }
        }
        catch { }
    }

    public void Log(string message)
    {
        var entry = new LogEntry { Timestamp = DateTime.Now, Message = message };
        OnLogAdded?.Invoke(this, entry);
        WeakReferenceMessenger.Default.Send(new LogAddedMessage(message));
        Task.Run(() => SaveLog(entry));
    }

    private void SaveLog(LogEntry entry)
    {
        try
        {
            if (!Directory.Exists(_logPath)) Directory.CreateDirectory(_logPath);
            var filename = Path.Combine(_logPath, "app.log");
            var line = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss:ffff}\t{entry.Message}";
            lock (_lockObj) { File.AppendAllText(filename, line + Environment.NewLine); }
        }
        catch { }
    }

    public void Clear()
    {
        if (OnLogAdded == null) return;
        foreach (var handler in OnLogAdded.GetInvocationList())
            OnLogAdded -= (EventHandler<LogEntry>)handler;
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = "";
}
```

- [ ] **Step 2: Verify no Prism.Events reference remains**

```bash
grep -rn "Prism.Events\|IEventAggregator" Application/
```
Expected: no matches

- [ ] **Step 3: Commit**

```bash
git add Application/Services/LoggingService.cs
git commit -m "refactor: replace IEventAggregator with WeakReferenceMessenger in LoggingService"
```

---

## Task 6: Update VmIntegrationService — Replace IEventAggregator

**Files:**
- Modify: `Application/VmServices/VmIntegrationService.cs`

**Steps:**

- [ ] **Step 1: Read current file and update using statements + event calls**

Read `Application/VmServices/VmIntegrationService.cs`. Find these lines:
```csharp
using Prism.Events;
```
Remove them. Add:
```csharp
using CommunityToolkit.Mvvm.Messaging;
using TripleDetection.Presentation.Messages;
```

Find the `_eventAggregator.GetEvent<DetectionResultEvent>().Publish(result);` call and replace with:
```csharp
WeakReferenceMessenger.Default.Send(new DetectionResultMessage(result));
```

- [ ] **Step 2: Verify no Prism.Events reference**

```bash
grep -rn "Prism.Events\|IEventAggregator" Application/VmServices/
```
Expected: no matches

- [ ] **Step 3: Commit**

```bash
git add Application/VmServices/VmIntegrationService.cs
git commit -m "refactor: replace IEventAggregator with WeakReferenceMessenger in VmIntegrationService"
```

---

## Task 7: Delete Old Prism PubSubEvent Files

**Files:**
- Delete: `Presentation/Events/DetectionEvents.cs`
- Delete: `Presentation/Events/LogEvents.cs`
- Delete: `Presentation/Events/NavigationEvents.cs`

**Steps:**

- [ ] **Step 1: Delete the old event files**

```bash
rm Presentation/Events/DetectionEvents.cs
rm Presentation/Events/LogEvents.cs
rm Presentation/Events/NavigationEvents.cs
```

- [ ] **Step 2: Commit**

```bash
git add -A
git commit -m "chore: remove old Prism PubSubEvent files"
```

---

## Task 8: Update All ViewModels — BindableBase → ObservableObject, DelegateCommand → RelayCommand

**Files:**
- Modify: `Presentation/ViewModels/LoginViewModel.cs`
- Modify: `Presentation/ViewModels/TabItemViewModel.cs`
- Modify: `Presentation/ViewModels/Settings/SettingsShellViewModel.cs`
- Modify: `Presentation/ViewModels/Auth/UserManagementViewModel.cs`
- Modify: `Presentation/ViewModels/Auth/UserEditViewModel.cs`
- Modify: `Presentation/ViewModels/Production/ProductListViewModel.cs`
- Modify: `Presentation/ViewModels/Production/ProductEditViewModel.cs`
- Modify: `Presentation/ViewModels/Production/TaskListViewModel.cs`
- Modify: `Presentation/ViewModels/Production/TaskEditViewModel.cs`

**Pattern for each file:**

**Before (Prism):**
```csharp
using Prism.Mvvm;
using Prism.Commands;

public class XxxViewModel : BindableBase
{
    public DelegateCommand Command { get; }
        = new DelegateCommand(Execute, CanExecute)
            .ObservesProperty(() => IsLoading);
}
```

**After (CommunityToolkit.Mvvm):**
```csharp
using CommunityToolkit.Mvvm;

public partial class XxxViewModel : ObservableObject
{
    public IRelayCommand Command { get; }

    public XxxViewModel(...)
    {
        Command = new RelayCommand(Execute, CanExecute);
    }

    [ObservableProperty] private bool _isLoading;
}
```

**ObservableProperty attribute rule:** For every `private string _username = "";` with `{ get; set; }` pattern using `SetProperty`, replace with `[ObservableProperty] private string _username = "";` and remove the manual `{ get; set; }` — CommunityToolkit generates the property automatically. For manually implemented properties not using `[ObservableProperty]`, keep using `SetProperty` or convert to auto-property.

**Command property type change:** `DelegateCommand` → `IRelayCommand`, `DelegateCommand<T>` → `IRelayCommand<T>`.

**ObservableProperty property name transformation:** `_username` field becomes `Username` property automatically (PascalCase from camelCase `_username`).

**For each ViewModel:**
- Replace `using Prism.Mvvm;` → `using CommunityToolkit.Mvvm;`
- Replace `using Prism.Commands;` → `using CommunityToolkit.Mvvm;` (same namespace)
- Replace `: BindableBase` → `: ObservableObject`
- Replace `DelegateCommand` → `RelayCommand`, `DelegateCommand<T>` → `RelayCommand<T>`
- Replace `.ObservesProperty(() => Prop)` → `.ObserveProperty(nameof(Prop))`
- Convert `SetProperty(ref _prop, value)` manual properties to `[ObservableProperty] private type _prop = initialValue;`
- Remove `RaisePropertyChanged` calls — `[ObservableProperty]` handles this automatically

- [ ] **Step 1: Commit each ViewModel**

```bash
git add Presentation/ViewModels/LoginViewModel.cs Presentation/ViewModels/TabItemViewModel.cs
git commit -m "refactor: migrate LoginViewModel and TabItemViewModel to CommunityToolkit.Mvvm"
```

```bash
git add Presentation/ViewModels/Settings/SettingsShellViewModel.cs
git commit -m "refactor: migrate SettingsShellViewModel to CommunityToolkit.Mvvm"
```

```bash
git add Presentation/ViewModels/Auth/
git commit -m "refactor: migrate Auth ViewModels to CommunityToolkit.Mvvm"
```

```bash
git add Presentation/ViewModels/Production/
git commit -m "refactor: migrate Production ViewModels to CommunityToolkit.Mvvm"
```

---

## Task 9: Update MainViewModel — Navigation + Event Aggregator Replacement

**Files:**
- Modify: `Presentation/ViewModels/Detection/MainViewModel.cs`

**Steps:**

- [ ] **Step 1: Read current MainViewModel.cs, then rewrite using + IEventAggregator sections**

Read `Presentation/ViewModels/Detection/MainViewModel.cs`.

**Before (Prism):**
```csharp
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Navigation.Regions;

public class MainViewModel : BindableBase
{
    private readonly IRegionManager _regionManager;
    private readonly IEventAggregator _eventAggregator;

    public MainViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
    {
        _regionManager = regionManager;
        _eventAggregator = eventAggregator;
        _eventAggregator.GetEvent<LogAddedEvent>().Subscribe(OnLogAdded);
        _eventAggregator.GetEvent<DetectionResultEvent>().Subscribe(OnDetectionResult);
    }

    private void NavigateToProduct()
    {
        CurrentView = new Views.Production.ProductListView();
        // or: _regionManager.RequestNavigate("RegionName", typeof(ProductListView));
    }
}
```

**After (CommunityToolkit.Mvvm):**
```csharp
using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.Messaging;
using TripleDetection.Presentation.Messages;
using TripleDetection.Presentation.Navigation;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    public ObservableCollection<string> LogMessages { get; } = new();
    public ObservableCollection<string> ResultHistory { get; } = new();

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        WeakReferenceMessenger.Default.Register<LogAddedMessage>(this, (r, m) => AddLog(m.Message));
        WeakReferenceMessenger.Default.Register<DetectionResultMessage>(this, (r, m) => OnDetectionResult(m.Result));
    }

    public void AddLog(string message) { ... }
    public void AddResult(string result) { ... }

    private void NavigateToProduct()
    {
        _navigationService.NavigateTo<Views.Production.ProductListView>("Products");
    }
}
```

**Key changes:**
- Remove `using Prism.Events; Prism.Navigation.Regions;`
- Add `using CommunityToolkit.Mvvm.Messaging; TripleDetection.Presentation.Navigation;`
- Replace `: BindableBase` → `: ObservableObject`
- Replace `IRegionManager` field → `INavigationService` field
- Replace `eventAggregator.GetEvent<XxxEvent>().Subscribe()` → `WeakReferenceMessenger.Default.Register<XxxMessage>()`
- Replace `eventAggregator.GetEvent<XxxEvent>().Publish(payload)` → `WeakReferenceMessenger.Default.Send(new XxxMessage(payload))`
- Replace `regionManager.RequestNavigate(...)` → `navigationService.NavigateTo<TView>()`
- `NavigateToProductCommand` with `_regionManager` call → use `navigationService`

- [ ] **Step 2: Verify no Prism references**

```bash
grep -rn "Prism\." Presentation/ViewModels/Detection/MainViewModel.cs
```
Expected: no matches

- [ ] **Step 3: Commit**

```bash
git add Presentation/ViewModels/Detection/MainViewModel.cs
git commit -m "refactor: migrate MainViewModel to CommunityToolkit.Mvvm with INavigationService"
```

---

## Task 10: Build Verification

**Steps:**

- [ ] **Step 1: Run MSBuild rebuild**

```bash
"C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" TripleDetection.csproj -t:Rebuild -p:Configuration=Debug 2>&1 | grep -E "(error|warning|Build succeeded)"
```

Expected: 0 errors, "Build succeeded"

- [ ] **Step 2: If errors remain, fix and repeat** until clean build

---

## Spec Coverage Checklist

- [x] csproj package swap (Task 1)
- [x] Message record types (Task 2)
- [x] INavigationService + NavigationService (Task 3)
- [x] App.xaml.cs DI bootstrapper (Task 4)
- [x] LoggingService without IEventAggregator (Task 5)
- [x] VmIntegrationService without IEventAggregator (Task 6)
- [x] Delete old PubSubEvent files (Task 7)
- [x] All ViewModels migrated (Task 8)
- [x] MainViewModel with navigation + messenger (Task 9)
- [x] Build succeeds (Task 10)

---

## Type Consistency Check

| Type | Defined in Task | Used in |
|---|---|---|
| `DetectionResultMessage` | Task 2 | Task 6 (VmIntegrationService), Task 9 (MainViewModel) |
| `LogAddedMessage` | Task 2 | Task 5 (LoggingService), Task 9 (MainViewModel) |
| `ViewOpenedMessage` | Task 2 | Not used in current code — no-op |
| `ViewClosedMessage` | Task 2 | Not used in current code — no-op |
| `ActiveViewChangedMessage` | Task 2 | Not used in current code — no-op |
| `INavigationService` | Task 3 | Task 4 (ConfigureServices), Task 9 (MainViewModel) |
| `NavigationService` | Task 3 | Task 4 (ConfigureServices) |
| `LogEntry` | Task 5 | Task 5 (LoggingService only) |
