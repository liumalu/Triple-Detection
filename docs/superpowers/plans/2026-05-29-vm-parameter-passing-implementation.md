# VisionMaster 参数传递与真实结果解析实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** DetectionView 改用 VmIntegrationService，实现真实的 OnWorkStatusEvent 结果解析，替换 Random() 模拟结果

**Architecture:** DetectionView 通过 VmIntegrationService 调用 VM，结果通过事件回调驱动

**Tech Stack:** WPF (.NET Framework 4.8), VisionMaster SDK, C# 8.0

---

## Context

当前 DetectionView 直接调用 VM API，绕过 VmIntegrationService；BtnTaskRun_Click 用 Random() 生成假结果。VmIntegrationService 已有完整的 OnWorkStatusEvent 回调机制，但未被使用。

---

## 关键文件

| 文件 | 说明 |
|-----|-----|
| `TripleDetection.App/Services/VmIntegrationService.cs` | VM 服务封装，已有回调机制 |
| `TripleDetection.App/Views/DetectionView.xaml.cs` | 检测UI，直接调用VM（需重构） |
| `TripleDetection.App/Models/DetectionResult.cs` | 结果模型，需补充字段 |

---

## 实现步骤

### Task 1: VmIntegrationService 增强

**Files:**
- Modify: `TripleDetection.App/Services/VmIntegrationService.cs`

- [ ] **Step 1: 添加 ElapsedMs 和 ErrorMessage 字段到 DetectionResult 模型**

文件: `TripleDetection.App/Models/DetectionResult.cs`

```csharp
namespace TripleDetection.Models
{
    public class DetectionResult
    {
        public bool IsOK { get; set; }
        public string CodeInfo { get; set; }
        public int CharCount { get; set; }
        public double Confidence { get; set; }
        public string ImagePath { get; set; }
        public DateTime DetectionTime { get; set; }
        public long ElapsedMs { get; set; }       // NEW
        public string ErrorMessage { get; set; }  // NEW
    }
}
```

- [ ] **Step 2: 添加 Stop() 方法和 Run() 方法区分**

文件: `TripleDetection.App/Services/VmIntegrationService.cs`

在 `RunOnce()` 方法后添加:

```csharp
public void Stop()
{
    if (_procedure != null)
    {
        _procedure.ContinuousRunEnable = false;
    }
}

public bool IsContinuousRun => _procedure?.ContinuousRunEnable ?? false;

public void SetProcedure(string procedureName)
{
    if (_isSolutionLoad && !string.IsNullOrEmpty(procedureName))
    {
        _procedure = VmSolution.Instance[procedureName] as VmProcedure;
    }
}
```

- [ ] **Step 3: 在 VmSolution_OnWorkStatusEvent 中补充 ElapsedMs**

找到现有的回调方法，在 ParseResult 调用处添加计时：

```csharp
private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();

private void VmSolution_OnWorkStatusEvent(ImvsSdkDefine.IMVS_MODULE_WORK_STAUS workStatusInfo)
{
    if (workStatusInfo.nWorkStatus == 0 && workStatusInfo.nProcessID == 10000)
    {
        _stopwatch.Stop();
        try
        {
            // ... existing code ...
            var result = ParseResult(strResult);
            result.ElapsedMs = _stopwatch.ElapsedMilliseconds;
            OnDetectionResult?.Invoke(this, result);
        }
        finally
        {
            _stopwatch.Restart();
        }
    }
}

// 在 RunOnce() 开始时启动计时
public void RunOnce()
{
    _stopwatch.Restart();
    _procedure?.Run();
}
```

### Task 2: DetectionView 重构使用 VmIntegrationService

**Files:**
- Modify: `TripleDetection.App/Views/DetectionView.xaml.cs`

- [ ] **Step 1: 添加 VmIntegrationService 实例和订阅**

在 DetectionView 类中添加:

```csharp
private readonly VmIntegrationService _vmService;
private GlobalVariableModuleTool _gvTool;

// 构造函数中添加:
_vmService = new VmIntegrationService(null, _logService);
_vmService.OnDetectionResult += VmService_OnDetectionResult;
```

添加回调处理:

```csharp
private void VmService_OnDetectionResult(object sender, DetectionResult result)
{
    Dispatcher.Invoke(() =>
    {
        UpdateDetectionResult(result.IsOK ? "OK" : "NG", result.Confidence);
    });
}
```

- [ ] **Step 2: 提取全局变量模块查找为辅助方法**

添加私有方法:

```csharp
private GlobalVariableModuleTool GetGlobalVariableTool()
{
    if (_procedure == null) return null;

    string[] possibleNames = { "GlobalVariable", "全局变量1", "GlobalVariableModule", "全局变量" };
    foreach (var name in possibleNames)
    {
        var mod = _procedure.Modules[name];
        if (mod is GlobalVariableModuleTool)
        {
            _logService.Log($"使用模块: {name}");
            return mod as GlobalVariableModuleTool;
        }
    }
    return null;
}
```

- [ ] **Step 3: 重构 BtnLoadSol_Click 使用 VmIntegrationService**

将 `VmSolution.Load(_selectedSolPath)` 替换为:

```csharp
try
{
    _vmService.LoadSolution(_selectedSolPath);
    _isSolutionLoad = true;
    _logService.Log("加载方案成功!");
    MessageBox.Show("加载方案成功!", "信息", MessageBoxButton.OK, MessageBoxImage.Information);

    cmbProcedure.Items.Clear();
    foreach (var name in _vmService.GetAllProcedureNames())
    {
        cmbProcedure.Items.Add(name);
    }
    if (cmbProcedure.Items.Count > 0)
    {
        cmbProcedure.SelectedIndex = 0;
    }
}
catch (Exception ex)
{
    // 现有错误处理...
}
```

- [ ] **Step 4: 重构 BtnSaveSol_Click 使用服务**

将手动查找 gvTool 和 SetGlobalVar 替换为:

```csharp
private void BtnSaveSol_Click(object sender, RoutedEventArgs e)
{
    if (_selectedTask == null)
    {
        MessageBox.Show("请先选择任务!", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

    if (!_isSolutionLoad)
    {
        MessageBox.Show("请先加载方案!", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

    try
    {
        var batchNumber = _selectedTask.BatchNumber ?? "";
        var mfgDate = _selectedTask.ProductionDate.ToString("yyyyMMdd");
        var expDate = _selectedTask.ExpirationDate.HasValue
            ? _selectedTask.ExpirationDate.Value.ToString("yyyyMMdd")
            : "";

        _vmService.SetGlobalVariableString("BN", batchNumber);
        _vmService.SetGlobalVariableString("Mfg", mfgDate);
        _vmService.SetGlobalVariableString("EXP", expDate);

        _logService.Log($"三期信息已设置ToVM: BN={batchNumber}, Mfg={mfgDate}, EXP={expDate}");
        MessageBox.Show($"三期信息已设置:\nBN={batchNumber}\nMfg={mfgDate}\nEXP={expDate}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    catch (Exception ex)
    {
        _logService.Log($"设置三期信息失败: {ex.Message}");
        MessageBox.Show($"设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

- [ ] **Step 5: 重构 BtnTaskRun_Click 移除模拟结果**

替换为:

```csharp
private void BtnTaskRun_Click(object sender, RoutedEventArgs e)
{
    if (!_isSolutionLoad || _vmService.GetProcedure() == null)
    {
        _logService.Log("流程不存在!");
        return;
    }

    try
    {
        _vmService.RunOnce();
        _logService.Log("单次运行已触发，等待结果回调...");
    }
    catch (Exception ex)
    {
        dynamic vmEx = ex;
        if (vmEx.errorCode != null)
            _logService.Log($"单次运行失败, 错误码: 0x{vmEx.errorCode:X}");
        else
            _logService.Log($"单次运行失败: {ex.Message}");
    }
}
```

- [ ] **Step 6: 重构 BtnTaskPause_Click 使用服务**

替换为:

```csharp
private void BtnTaskPause_Click(object sender, RoutedEventArgs e)
{
    if (!_isSolutionLoad || _vmService.GetProcedure() == null)
    {
        _logService.Log("流程不存在!");
        return;
    }

    try
    {
        bool newState = !_vmService.IsContinuousRun;
        _vmService.SetContinuousRun(newState);
        _isContinuRun = newState;
        _logService.Log($"连续运行: {newState}");
        btnContiRun.Content = _isContinuRun ? "停止连续" : "连续运行";
    }
    catch (Exception ex)
    {
        dynamic vmEx = ex;
        if (vmEx.errorCode != null)
            _logService.Log($"连续运行切换失败, 错误码: 0x{vmEx.errorCode:X}");
        else
            _logService.Log($"连续运行切换失败: {ex.Message}");
    }
}
```

- [ ] **Step 7: 重构 CmbProcedure_SelectionChanged 使用服务**

替换为:

```csharp
private void CmbProcedure_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (cmbProcedure.SelectedItem == null) return;

    try
    {
        _vmService.SetProcedure(cmbProcedure.SelectedItem.ToString());
        _procedure = _vmService.GetProcedure();
        if (_vmRender != null)
            _vmRender.ModuleSource = _procedure;

        _logService.Log($"已选择 [{cmbProcedure.SelectedItem}]");
    }
    catch (Exception ex)
    {
        dynamic vmEx = ex;
        if (vmEx.errorCode != null)
            _logService.Log($"选择流程失败, 错误码: 0x{vmEx.errorCode:X}");
        else
            _logService.Log($"选择流程失败: {ex.Message}");
    }
}
```

- [ ] **Step 8: 移除未使用的 using 和字段**

移除:
- `using VM.Core;` (如果不再需要)
- `using GlobalVariableModuleCs;`
- `private VmProcedure _procedure;` (改用 `_vmService.GetProcedure()`)
- `_gvTool` 字段如果不再使用

### Task 3: 验证

- [ ] **Step 1: 编译验证**

Run: MSBuild 编译，确认 0 errors

- [ ] **Step 2: 运行测试**

手动测试完整流程:
1. 选择任务 → 任务信息显示
2. 加载方案 → 方案加载成功
3. 三期ToVM → 日志显示 `[VM GlobalVariable] BN: xxx -> xxx`
4. 单次运行 → 结果从回调获取（非模拟）
5. 连续运行 → 持续接收结果回调

---

## 验证标准

- [ ] 编译通过，0 errors
- [ ] 选择任务后，点击"三期ToVM"能正确设置 BN/Mfg/EXP（日志验证）
- [ ] 点击"单次运行"后，结果从 OnWorkStatusEvent 回调获取（非 Random）
- [ ] 检测结果（OK/NG、置信度）正确显示
- [ ] 日志中有 `[VM GlobalVariable]` 和 `[Callback]` 输出
- [ ] 连续运行模式能持续接收结果回调
- [ ] 移除 DetectionView 中所有对 `VmSolution` 的直接引用（除必要的静态调用如 `VmSolution.Load`）