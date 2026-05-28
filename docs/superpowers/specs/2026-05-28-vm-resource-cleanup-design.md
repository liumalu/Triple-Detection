# VM 资源清理设计方案

> **日期:** 2026-05-28
> **状态:** 已批准，待实现

## 背景

当 VisionMaster 方案的图像源配置为相机时，关闭应用需要正确释放相机连接。当前代码缺口：
- `MainWindow.xaml.cs` 的 `Window_Closing` 仅注销事件，未调用 `VmSolution.Close()`
- `_vmRender` 和 `_procedure` 无清理逻辑
- 可能导致相机连接未正常释放

## 目标

在应用关闭时，按正确顺序清理 VM 资源：
1. 停止连续运行
2. 停止当前流程
3. 关闭 VmSolution（释放相机连接）
4. 注销事件订阅

## 架构

在 `MainWindow.xaml.cs` 的 `Window_Closing` 中集中处理清理逻辑。DetectionView 无需单独处理。

## 变更文件

**Modify:** `TripleDetection.App\MainWindow.xaml.cs`

## 实现

```csharp
private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
{
    try
    {
        // 1. 停止连续运行
        var procedure = VmSolution.Instance?.GetCurrentProcedure() as VmProcedure;
        if (procedure?.ContinuousRunEnable == true)
        {
            procedure.ContinuousRunEnable = false;
        }

        // 2. 停止流程
        if (procedure != null)
        {
            procedure.Stop();
        }

        // 3. 关闭方案（释放相机连接）
        VmSolution.Close();

        _logService.Log("VM 资源已释放");
    }
    catch (Exception ex)
    {
        _logService.Log($"关闭时清理 VM 资源出错: {ex.Message}");
    }

    // 4. 注销事件订阅
    VmSolution.OnWorkStatusEvent -= VmSolution_OnWorkStatusEvent;
    VmSolution.OnProcessStatusStartEvent -= VmSolution_OnProcessStatusStartEvent;
    VmSolution.OnProcessStatusStopEvent -= VmSolution_OnProcessStatusStopEvent;
}
```

## 验证标准

1. 启动应用 → 选择任务 → 加载方案
2. 触发连续运行
3. 关闭应用
4. 检查日志显示 "VM 资源已释放"
5. 相机连接正常释放，无异常警告
