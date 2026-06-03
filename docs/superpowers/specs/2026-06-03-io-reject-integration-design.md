# IO 剔除与产线控制集成设计

**日期：** 2026-06-03
**状态：** 已设计，待实现

---

## 1. 整体架构

```
检测流程：
VisionMaster 回调 → VmIntegrationService.OnDetectionResult
    → RejectService.OnDetectionResultReceived（新增订阅）
        → NG? → ModbusTcpIOService.WriteCoilAsync(线圈1) → 触发剔除脉冲
        → 连续NG达阈值 → ModbusTcpIOService.WriteCoilAsync(线圈2) → 产线停止
    → DetectionView.VmService_OnDetectionResult → UI更新

配置流：
DeviceControlSettings（Modbus配置） → ModbusTcpIOService
                                      → RejectService
```

**三层职责：**
- `VmIntegrationService` — 只管视觉检测和结果解析，不关心 IO
- `RejectService` — 剔除逻辑（延迟、脉冲、连续NG停止、手动复位）
- `ModbusTcpIOService` — Modbus TCP 通信，硬件抽象

---

## 2. IO 硬件抽象接口

### 2.1 接口定义（Domain 层）

```csharp
public interface IIODeviceService
{
    Task WriteCoilAsync(int coilAddress, bool value, CancellationToken ct = default);
    Task<bool> ReadDiscreteInputAsync(int inputAddress, CancellationToken ct = default);
    Task<bool[]> ReadDiscreteInputsAsync(int startAddress, int count, CancellationToken ct = default);
    bool IsConnected { get; }
    Task ConnectAsync(string ip, int port, CancellationToken ct = default);
    void Disconnect();
}
```

### 2.2 ModbusTCP 实现（Infrastructure 层）

**NuGet 依赖：** `NModbus`（TCP Master 实现）

**地址约定：** 所有 Modbus 地址从 **1** 开始，内部库自动 -1 转为 0-based。

**自动重连：** 写入/读取失败后自动重试最多 3 次，每次间隔 100ms。

**异常处理：** 连接断开后标记 `_isConnected = false`，不自动重连，由上层决定何时重连。

---

## 3. 配置模型

`DeviceControlSettings` 增补以下字段（保存路径：`Config/device_control.json`）：

| 字段 | 默认值 | 说明 |
|------|--------|------|
| `RejectDelayMs` | 50 | NG后延迟多久触发（已有） |
| `RejectDurationMs` | 200 | 脉冲宽度（已有） |
| `ConsecutiveRejectsToStopLine` | 10 | 连续NG触发产线停止阈值（已有） |
| `ModbusTcpIp` | `192.168.1.100` | IO 模块 IP |
| `ModbusTcpPort` | `502` | IO 模块端口 |
| `RejectCoilAddress` | `1` | 剔除继电器线圈地址 |
| `LineStopCoilAddress` | `2` | 产线停止继电器线圈地址 |
| `ConnectionTimeoutMs` | `3000` | 连接超时 |
| `EnableLineStopOnConsecutiveRejects` | `false` | 是否启用连续NG停止产线 |
| `RequireIOConnectionToStartTask` | `false` | 启动任务前是否强制 IO 连接 |

---

## 4. RejectService

### 4.1 接口

```csharp
public interface IRejectService
{
    void OnDetectionResultReceived(DetectionResult result);
    void ResetConsecutiveRejectCount();
    void ResetLineStop();
    int ConsecutiveRejectCount { get; }
    bool IsLineStopped { get; }
}
```

### 4.2 核心逻辑

**OnDetectionResultReceived 处理流程：**

```
收到 DetectionResult
    ├── IsOK == true
    │       → 重置 _consecutiveRejectCount
    │       → 如果 _isLineStopped == true，恢复产线运行
    │       → 记录日志
    │
    └── IsOK == false
            → _consecutiveRejectCount++
            → Task.Delay(RejectDelayMs).ContinueWith(TriggerRejectPulse)
            → 如果 EnableLineStopOnConsecutiveRejects
                且 _consecutiveRejectCount >= ConsecutiveRejectsToStopLine
                且 _isLineStopped == false
                    → _isLineStopped = true
                    → ModbusTcpIOService.WriteCoilAsync(LineStopCoilAddress, true)
                    → 记录日志
```

**TriggerRejectPulse 时序：**

```
WriteCoilAsync(RejectCoilAddress, true)  → 继电器吸合
Task.Delay(RejectDurationMs)             → 保持脉冲宽度
（掉电释放型继电器，无需手动写 false）
```

**ResetLineStop：**

```
_isLineStopped = false
_consecutiveRejectCount = 0
记录日志："产线已手动复位，操作员确认恢复"
```

---

## 5. 事件订阅与 DI 接入

**接入方式：** 方式 A（App.xaml.cs 中直接订阅）

```csharp
// App.xaml.cs
_vmService.OnDetectionResult += _rejectService.OnDetectionResultReceived;
_vmService.OnDetectionResult += _detectionView.VmService_OnDetectionResult;
```

**DI 容器注册：**

```csharp
container.RegisterType<DeviceControlSettingsService>();
container.RegisterSingleton<IIODeviceService, ModbusTcpIOService>();
container.RegisterSingleton<IRejectService, RejectService>();
```

---

## 6. 连接生命周期

| 时机 | 行为 |
|------|------|
| App 启动 | `DeviceControlSettingsService.Load()` 加载配置 |
| DetectionView 加载 | 调用 `ModbusTcpIOService.ConnectAsync()` 连接 IO 模块 |
| 连接失败 | 记录日志；若 `RequireIOConnectionToStartTask == true` 则阻止启动任务 |
| 连接成功 | 状态栏显示"IO 在线" |
| DetectionView 卸载 | 调用 `ModbusTcpIOService.Disconnect()` |

**调试模式：** `RequireIOConnectionToStartTask == false` 时，IO 连接异常不阻塞检测流程，允许继续检测。

**状态栏：** 主界面状态栏显示 IO 模块连接状态（在线/离线/未配置）。

---

## 7. 产线停止与手动复位

**停止触发条件：**
- `EnableLineStopOnConsecutiveRejects == true`
- `_consecutiveRejectCount >= ConsecutiveRejectsToStopLine`
- `_isLineStopped == false`（避免重复触发）

**停止时动作：**
1. `WriteCoilAsync(LineStopCoilAddress, true)` → 线圈2吸合
2. `_isLineStopped = true`
3. 记录日志

**手动复位（操作员点击"恢复产线"按钮）：**
1. `RejectService.ResetLineStop()`
2. `_isLineStopped = false`，`_consecutiveRejectCount = 0`
3. 记录日志："产线已手动复位，操作员确认恢复"

---

## 8. 待定事项

- [ ] Modbus 寄存器地址（IO 模块的具体地址分配待测试确认）
- [ ] NModbus 包版本和 API 细节
- [ ] 状态栏 UI 实现细节

---

## 9. 涉及文件

| 层级 | 文件 | 动作 |
|------|------|------|
| Domain | `Domain/Repositories/IIODeviceService.cs`（新增） | 新增接口 |
| Infrastructure | `Infrastructure/IO/ModbusTcpIOService.cs`（新增） | 新增实现 |
| Infrastructure | `Infrastructure/IO/`（新增目录） | 放置 IO 服务 |
| Application | `Application/Services/RejectService.cs`（新增） | 新增服务 |
| Application | `Application/Services/IRejectService.cs`（新增） | 新增接口 |
| Presentation | `Presentation/Models/DeviceControlSettings.cs` | 增补 Modbus 字段 |
| Presentation | `Presentation/ViewModels/Detection/MainViewModel.cs` | 绑定 RejectService 状态 |
| Presentation | `Presentation/Views/Detection/DetectionView.xaml.cs` | IO 连接初始化 |
| App | `Presentation/App.xaml.cs` | DI 注册、事件订阅 |
| Config | `Config/device_control.json` | 增补 Modbus 配置项 |
