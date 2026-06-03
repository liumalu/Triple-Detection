# IO 剔除与产线控制集成实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现 IO 模块的 Modbus TCP 连接 + NG 剔除触发 + 产线停止控制，通过 Prism DI 接入现有 DetectionView

**Architecture:** 
- `IIODeviceService` 接口定义在 Domain 层
- `ModbusTcpIOService` 实现 Modbus TCP 通信（Infrastructure 层）
- `RejectService` 处理剔除逻辑（Application 层）
- IO 连接生命周期绑定在 DetectionView 加载/卸载

**Tech Stack:** .NET Framework 4.8, NModbus 2.x, Prism.DryIoc,现有 WPF 项目结构

---

## 文件结构

```
Domain/Repositories/
  IIODeviceService.cs          ← 新增：IO 设备抽象接口

Infrastructure/IO/
  ModbusTcpIOService.cs         ← 新增：Modbus TCP 实现

Application/Services/
  IRejectService.cs             ← 新增：剔除服务接口
  RejectService.cs              ← 新增：剔除逻辑实现

Presentation/Models/
  DeviceControlSettings.cs      ← 修改：增补 Modbus 配置字段

Presentation/App.xaml.cs        ← 修改：DI 注册 + 事件订阅

Presentation/ViewModels/Detection/
  MainViewModel.cs              ← 修改：绑定 IO 连接状态

Presentation/Views/Detection/
  DetectionView.xaml.cs         ← 修改：IO 连接生命周期管理

Presentation/MainWindow.xaml    ← 修改：状态栏显示 IO 状态
Presentation/MainWindow.xaml.cs ← 修改：状态栏更新逻辑

Config/device_control.json      ← 修改：增补 Modbus 配置项
```

---

## 准备工作

- [ ] **Task 0: 添加 NModbus NuGet 包**
  - Modify: `TripleDetection.csproj`
  - 在 `<ItemGroup>` 中添加：
    ```xml
    <PackageReference Include="NModbus" Version="2.1.0" />
    ```

---

## Task 1: IO 设备抽象接口（Domain 层）

**Files:**
- Create: `Domain/Repositories/IIODeviceService.cs`

- [ ] **Step 1: 创建 IIODeviceService 接口**

```csharp
using System.Threading;

namespace TripleDetection.Domain.Repositories
{
    public interface IIODeviceService
    {
        Task WriteCoilAsync(int coilAddress, bool value, CancellationToken ct = default);
        Task<bool> ReadDiscreteInputAsync(int inputAddress, CancellationToken ct = default);
        Task<bool[]> ReadDiscreteInputsAsync(int startAddress, int count, CancellationToken ct = default);
        bool IsConnected { get; }
        Task ConnectAsync(string ip, int port, CancellationToken ct = default);
        void Disconnect();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Domain/Repositories/IIODeviceService.cs
git commit -m "feat: add IIODeviceService interface in Domain layer"
```

---

## Task 2: ModbusTcpIOService 实现（Infrastructure 层）

**Files:**
- Create: `Infrastructure/IO/ModbusTcpIOService.cs`

- [ ] **Step 1: 创建 Infrastructure/IO 目录并实现 ModbusTcpIOService**

```csharp
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Modbus;
using Modbus.Device;
using TripleDetection.Domain.Repositories;

namespace TripleDetection.Infrastructure.IO
{
    public class ModbusTcpIOService : IIODeviceService, IDisposable
    {
        private TcpClient _tcpClient;
        private ModbusIpMaster _master;
        private readonly LoggingService _logService;
        private bool _isConnected = false;
        private readonly object _connLock = new object();

        public bool IsConnected
        {
            get { lock (_connLock) return _isConnected; }
        }

        public ModbusTcpIOService(LoggingService logService)
        {
            _logService = logService;
        }

        public async Task ConnectAsync(string ip, int port, CancellationToken ct = default)
        {
            lock (_connLock)
            {
                if (_isConnected) return;
                _tcpClient?.Dispose();
                _tcpClient = new TcpClient();
            }

            await _tcpClient.ConnectAsync(ip, port);
            _master = ModbusIpMaster.CreateIp(_tcpClient);
            lock (_connLock) { _isConnected = true; }
            _logService.Log($"[ModbusTCP] 已连接 {ip}:{port}");
        }

        public async Task WriteCoilAsync(int coilAddress, bool value, CancellationToken ct = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("ModbusTCP 未连接，请先调用 ConnectAsync");

            const int maxRetries = 3;
            Exception? lastEx = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // NModbus 地址从 0 开始，内部自动 -1
                    await _master.WriteSingleCoilAsync((ushort)(coilAddress - 1), value, ct);
                    return;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    lock (_connLock) { _isConnected = false; }

                    if (attempt < maxRetries)
                    {
                        _logService.Log($"[ModbusTCP] WriteCoil 第 {attempt} 次失败，100ms 后重试...");
                        await Task.Delay(100, ct);
                    }
                }
            }

            _logService.Log($"[ModbusTCP] WriteCoil 最终失败（已重试 {maxRetries} 次）: {lastEx?.Message}");
            throw lastEx!;
        }

        public async Task<bool> ReadDiscreteInputAsync(int inputAddress, CancellationToken ct = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("ModbusTCP 未连接");

            const int maxRetries = 3;
            Exception? lastEx = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    bool[] result = await _master.ReadInputsAsync((ushort)(inputAddress - 1), 1, ct);
                    return result[0];
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    lock (_connLock) { _isConnected = false; }

                    if (attempt < maxRetries)
                    {
                        _logService.Log($"[ModbusTCP] ReadInput 第 {attempt} 次失败，100ms 后重试...");
                        await Task.Delay(100, ct);
                    }
                }
            }

            _logService.Log($"[ModbusTCP] ReadInput 最终失败（已重试 {maxRetries} 次）: {lastEx?.Message}");
            throw lastEx!;
        }

        public async Task<bool[]> ReadDiscreteInputsAsync(int startAddress, int count, CancellationToken ct = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("ModbusTCP 未连接");

            const int maxRetries = 3;
            Exception? lastEx = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await _master.ReadInputsAsync((ushort)(startAddress - 1), (ushort)count, ct);
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    lock (_connLock) { _isConnected = false; }

                    if (attempt < maxRetries)
                    {
                        _logService.Log($"[ModbusTCP] ReadInputs 第 {attempt} 次失败，100ms 后重试...");
                        await Task.Delay(100, ct);
                    }
                }
            }

            _logService.Log($"[ModbusTCP] ReadInputs 最终失败（已重试 {maxRetries} 次）: {lastEx?.Message}");
            throw lastEx!;
        }

        public void Disconnect()
        {
            lock (_connLock)
            {
                _isConnected = false;
                _master?.Dispose();
                _tcpClient?.Close();
                _master = null;
                _tcpClient = null;
            }
            _logService.Log("[ModbusTCP] 连接已断开");
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Infrastructure/IO/ModbusTcpIOService.cs
git commit -m "feat: add ModbusTcpIOService in Infrastructure layer"
```

---

## Task 3: DeviceControlSettings 增补 Modbus 字段

**Files:**
- Modify: `Presentation/Models/DeviceControlSettings.cs`

- [ ] **Step 1: 读取现有文件内容**

- [ ] **Step 2: 替换为新内容**

```csharp
namespace TripleDetection.Presentation.Models
{
    public class DeviceControlSettings
    {
        // === 既有字段 ===
        public string LightSourceType { get; set; } = "LED";
        public int CaptureDelayMs { get; set; } = 100;
        public int CaptureFeedbackTimeoutMs { get; set; } = 5000;
        public int RejectDelayMs { get; set; } = 50;
        public int RejectDurationMs { get; set; } = 200;
        public int ConsecutiveRejectsToStopLine { get; set; } = 10;

        // === Modbus TCP 配置 ===
        public string ModbusTcpIp { get; set; } = "192.168.1.100";
        public int ModbusTcpPort { get; set; } = 502;
        public int RejectCoilAddress { get; set; } = 1;       // 剔除继电器
        public int LineStopCoilAddress { get; set; } = 2;     // 产线停止继电器
        public int ConnectionTimeoutMs { get; set; } = 3000;
        public bool EnableLineStopOnConsecutiveRejects { get; set; } = false;
        public bool RequireIOConnectionToStartTask { get; set; } = false;
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Presentation/Models/DeviceControlSettings.cs
git commit -m "feat: add Modbus TCP config fields to DeviceControlSettings"
```

---

## Task 4: IRejectService 接口（Application 层）

**Files:**
- Create: `Application/Services/IRejectService.cs`

- [ ] **Step 1: 创建 IRejectService 接口**

```csharp
using System;
using TripleDetection.Presentation.Models;

namespace TripleDetection.Application.Services
{
    public interface IRejectService
    {
        void OnDetectionResultReceived(DetectionResult result);
        void ResetConsecutiveRejectCount();
        void ResetLineStop();
        int ConsecutiveRejectCount { get; }
        bool IsLineStopped { get; }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Application/Services/IRejectService.cs
git commit -m "feat: add IRejectService interface"
```

---

## Task 5: RejectService 实现（Application 层）

**Files:**
- Create: `Application/Services/RejectService.cs`

- [ ] **Step 1: 创建 RejectService**

```csharp
using System;
using System.Threading.Tasks;
using TripleDetection.Domain.Repositories;
using TripleDetection.Presentation.Models;

namespace TripleDetection.Application.Services
{
    public class RejectService : IRejectService
    {
        private readonly IIODeviceService _ioService;
        private readonly DeviceControlSettings _settings;
        private readonly LoggingService _logService;
        private int _consecutiveRejectCount = 0;
        private bool _isLineStopped = false;
        private readonly object _lock = new object();

        public int ConsecutiveRejectCount => _consecutiveRejectCount;
        public bool IsLineStopped => _isLineStopped;

        public RejectService(
            IIODeviceService ioService,
            DeviceControlSettings settings,
            LoggingService logService)
        {
            _ioService = ioService;
            _settings = settings;
            _logService = logService;
        }

        public void OnDetectionResultReceived(DetectionResult result)
        {
            if (result.IsOK)
            {
                lock (_lock)
                {
                    _consecutiveRejectCount = 0;
                    if (_isLineStopped)
                    {
                        _isLineStopped = false;
                        _logService.Log("[Reject] 产线恢复运行（收到OK）");
                    }
                }
                return;
            }

            lock (_lock)
            {
                _consecutiveRejectCount++;
                _logService.Log($"[Reject] NG #{_consecutiveRejectCount}");

                // 延迟后触发剔除脉冲（非阻塞）
                _ = Task.Delay(_settings.RejectDelayMs).ContinueWith(_ =>
                {
                    TriggerRejectPulse().Wait();
                });

                // 连续NG超过阈值，触发产线停止
                if (_settings.EnableLineStopOnConsecutiveRejects &&
                    _consecutiveRejectCount >= _settings.ConsecutiveRejectsToStopLine &&
                    !_isLineStopped)
                {
                    _isLineStopped = true;
                    _logService.Log($"[Reject] 连续NG达到 {_consecutiveRejectCount} 次，产线停止");
                    TriggerLineStop().Wait();
                }
            }
        }

        private async Task TriggerRejectPulse()
        {
            try
            {
                await _ioService.WriteCoilAsync(_settings.RejectCoilAddress, true);
                _logService.Log($"[Reject] 继电器吸合，地址={_settings.RejectCoilAddress}");
                await Task.Delay(_settings.RejectDurationMs);
                _logService.Log($"[Reject] 脉冲结束，宽度={_settings.RejectDurationMs}ms");
            }
            catch (Exception ex)
            {
                _logService.Log($"[Reject] 继电器控制异常: {ex.Message}");
            }
        }

        private async Task TriggerLineStop()
        {
            try
            {
                await _ioService.WriteCoilAsync(_settings.LineStopCoilAddress, true);
                _logService.Log($"[Reject] 产线停止继电器吸合，地址={_settings.LineStopCoilAddress}");
            }
            catch (Exception ex)
            {
                _logService.Log($"[Reject] 产线停止控制异常: {ex.Message}");
            }
        }

        public void ResetConsecutiveRejectCount()
        {
            lock (_lock)
            {
                _consecutiveRejectCount = 0;
            }
        }

        public void ResetLineStop()
        {
            lock (_lock)
            {
                if (!_isLineStopped) return;
                _isLineStopped = false;
                _consecutiveRejectCount = 0;
                _logService.Log("[Reject] 产线已手动复位，操作员确认恢复");
            }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Application/Services/RejectService.cs
git commit -m "feat: add RejectService with pulse and line-stop logic"
```

---

## Task 6: App.xaml.cs DI 注册与事件订阅

**Files:**
- Modify: `Presentation/App.xaml.cs`

- [ ] **Step 1: 在 RegisterTypes 中新增注册**

在现有注册后添加：

```csharp
// IO Device Service
containerRegistry.RegisterSingleton<IIODeviceService, ModbusTcpIOService>();

// Reject Service
containerRegistry.RegisterSingleton<IRejectService, RejectService>();
```

- [ ] **Step 2: 在 OnInitialized 中建立事件订阅**

在 `var mainWindow = Container.Resolve<MainWindow>();` 之前添加：

```csharp
// 建立 RejectService 对 VmIntegrationService 的事件订阅
var vmService = Container.Resolve<VmIntegrationService>();
var rejectService = Container.Resolve<IRejectService>();
vmService.OnDetectionResult += rejectService.OnDetectionResultReceived;
```

- [ ] **Step 3: Commit**

```bash
git add Presentation/App.xaml.cs
git commit -m "feat: register IIODeviceService, RejectService, subscribe to OnDetectionResult"
```

---

## Task 7: DetectionView IO 连接生命周期

**Files:**
- Modify: `Presentation/Views/Detection/DetectionView.xaml.cs`

- [ ] **Step 1: 在构造函数中注入 ModbusTcpIOService 和 DeviceControlSettings**

修改构造函数：
```csharp
private readonly ModbusTcpIOService _ioService;
private readonly DeviceControlSettings _deviceSettings;
private readonly IRejectService _rejectService;

public DetectionView(
    MainViewModel viewModel,
    LoggingService logService,
    VmIntegrationService vmService,
    ITaskService taskService,
    IProductService productService,
    IDetectionRecordService detectionRecordService,
    IIODeviceService ioService,           // 新增
    DeviceControlSettings deviceSettings, // 新增
    IRejectService rejectService)         // 新增
{
    // ...existing code...
    _ioService = ioService;
    _deviceSettings = deviceSettings;
    _rejectService = rejectService;
    // ...existing code...
}
```

- [ ] **Step 2: 在 Loaded 或初始化时连接 IO**

在构造函数末尾添加连接调用：

```csharp
private async void InitIOConnection()
{
    try
    {
        await _ioService.ConnectAsync(
            _deviceSettings.ModbusTcpIp,
            _deviceSettings.ModbusTcpPort);
        _logService.Log($"[DetectionView] IO 模块已连接 {_deviceSettings.ModbusTcpIp}:{_deviceSettings.ModbusTcpPort}");
    }
    catch (Exception ex)
    {
        _logService.Log($"[DetectionView] IO 模块连接失败: {ex.Message}");
        if (_deviceSettings.RequireIOConnectionToStartTask)
        {
            System.Windows.MessageBox.Show(
                $"IO 模块连接失败（{_deviceSettings.ModbusTcpIp}:{_deviceSettings.ModbusTcpPort}），检测将无法触发剔除。",
                "IO 连接异常",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }
}
```

在构造函数最后调用 `InitIOConnection()`。

- [ ] **Step 3: 在 Unloaded 中断开 IO**

在 `DetectionView_Unloaded` 中添加断开调用：

```csharp
private void DetectionView_Unloaded(object sender, RoutedEventArgs e)
{
    // ...existing code...
    _ioService.Disconnect();
    _logService.Log("[DetectionView] IO 模块已断开");
}
```

- [ ] **Step 4: Commit**

```bash
git add Presentation/Views/Detection/DetectionView.xaml.cs
git commit -m "feat: integrate IO connection lifecycle in DetectionView"
```

---

## Task 8: 状态栏显示 IO 连接状态

**Files:**
- Modify: `Presentation/MainWindow.xaml`
- Modify: `Presentation/MainWindow.xaml.cs`

- [ ] **Step 1: 在 MainWindow.xaml 状态栏 Grid 中增加 IO 状态 TextBlock**

在 `<TextBlock Grid.Column="0" x:Name="txtStatus"...>` 后面添加：

```xml
<TextBlock x:Name="txtIOStatus" Text="IO: 未连接" Foreground="#A0AEC0"
           VerticalAlignment="Center" Margin="16,0,0,0" FontSize="11"/>
```

- [ ] **Step 2: 在 MainWindow.xaml.cs 中添加 IO 状态更新方法**

在 `txtStatus` 更新逻辑处同步更新 IO 状态。可在 MainWindow 构造或 OnLoaded 中获取 IO 服务引用并轮询状态，或通过事件聚合。

更简单的方式：在 App.xaml.cs 中注入 `IIODeviceService` 后，在 `OnInitialized` 中给 `MainWindow` 传递 IO 服务引用，通过绑定或直接调用更新状态栏。

**推荐方式（最小改动）：** 在 `MainWindow.xaml.cs` 的 OnLoaded 中，通过 Prism 容器解析 `IIODeviceService`，定时器每 2 秒检查 `IsConnected` 并更新 `txtIOStatus`。

```csharp
using System.Windows;
using System.Windows.Threading;
// 在 MainWindow 中添加 DispatcherTimer 和 IIODeviceService 引用
private DispatcherTimer _ioStatusTimer;
private IIODeviceService _ioService;

private void Window_Loaded(object sender, RoutedEventArgs e)
{
    _ioService = (IIODeviceService)Prism.Ioc.IocLocator.Current.GetService(typeof(IIODeviceService));
    _ioStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
    _ioStatusTimer.Tick += (s, args) => UpdateIOStatus();
    _ioStatusTimer.Start();
}

private void UpdateIOStatus()
{
    if (_ioService == null) return;
    txtIOStatus.Text = _ioService.IsConnected ? "IO: 已连接" : "IO: 未连接";
    txtIOStatus.Foreground = _ioService.IsConnected
        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(72, 187, 120))
        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 62, 62));
}
```

- [ ] **Step 3: Commit**

```bash
git add Presentation/MainWindow.xaml Presentation/MainWindow.xaml.cs
git commit -m "feat: display IO connection status in MainWindow status bar"
```

---

## Task 9: 增补 device_control.json 默认配置

**Files:**
- Modify: `Config/device_control.json`（如果存在）或创建

- [ ] **Step 1: 创建或更新配置文件**

```json
{
  "LightSourceType": "LED",
  "CaptureDelayMs": 100,
  "CaptureFeedbackTimeoutMs": 5000,
  "RejectDelayMs": 50,
  "RejectDurationMs": 200,
  "ConsecutiveRejectsToStopLine": 10,
  "ModbusTcpIp": "192.168.1.100",
  "ModbusTcpPort": 502,
  "RejectCoilAddress": 1,
  "LineStopCoilAddress": 2,
  "ConnectionTimeoutMs": 3000,
  "EnableLineStopOnConsecutiveRejects": false,
  "RequireIOConnectionToStartTask": false
}
```

- [ ] **Step 2: Commit**

```bash
git add Config/device_control.json
git commit -m "feat: add Modbus TCP config to device_control.json"
```

---

## Task 10: NModbus using 问题修复

> NModbus 2.x 在 .NET Framework 4.8 上使用的命名空间可能与上面代码中的 `Modbus.Device.TcpClient` 不同。实际 API 以运行时测试为准。

- [ ] **Step 1: 如果 `ModbusIpMaster.CreateIp` 编译失败，查找正确 API**

常见 NModbus 2.x API：
```csharp
// 可能的方式：
_master = ModbusIpMaster.CreateIp(_tcpClient);
// 或：
_master = ModbusMaster.CreateIp(_tcpClient);
// 或需要先创建 Network 工厂
```

根据实际编译错误调整后 commit。

---

## 自检清单

- [ ] Spec 中每个 requirement 都能在计划中找到对应 task
- [ ] 所有文件路径正确，与 spec Section 9 匹配
- [ ] 无 placeholder（TBD/TODO/实现后续）
- [ ] NModbus 包版本正确（.NET Framework 4.8 用 NModbus 2.1.0）
- [ ] RejectService 的 `OnDetectionResultReceived` 线程安全（有 `_lock`）
- [ ] Task 3（DeviceControlSettings）已有字段保留
- [ ] RequireIOConnectionToStartTask 逻辑已体现（Task 7 Step 2）
