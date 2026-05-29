# VisionMaster 参数传递与真实结果解析设计

> **文档版本:** v1.0
> **更新日期:** 2026-05-29
> **状态:** 设计阶段

---

## 1. 背景与目标

### 现状问题

| 问题 | 位置 | 影响 |
|-----|-----|-----|
| DetectionView 直接调用 VM API | DetectionView.xaml.cs | 耦合高，难以测试 |
| 检测结果用 Random() 模拟 | BtnTaskRun_Click | 无法用于生产 |
| VmIntegrationService 存在但未使用 | Services/ | 封装层未生效 |
| SettingsSyncService 从未调用 | Services/Settings/ | 通信/设备参数未同步 |

### 目标

1. DetectionView 统一通过 VmIntegrationService 调用 VM
2. 实现真实的 OnWorkStatusEvent 结果解析
3. 保持手动同步 BN/Mfg/EXP 的交互方式
4. 建立标准的 VM 服务接口

---

## 2. 架构设计

### 2.1 当前架构

```
DetectionView.xaml.cs
    ├─ VmSolution.Load()           ← 直接调用
    ├─ gvTool.SetGlobalVar()       ← 直接调用
    └─ _procedure.Run()           ← 模拟结果
         └─ Random() → "OK/NG"
```

### 2.2 目标架构

```
DetectionView.xaml.cs
    │
    └─ VmIntegrationService
           ├─ LoadSolution(solPath)     → VmSolution.Load()
           ├─ SetGlobalVariableString()  → GlobalVariableModuleTool.SetGlobalVar()
           ├─ Run()                     → _procedure.Run()
           └─ OnDetectionCompleted    ← 事件回调，真实解析
                    └─ DetectionResult (OK/NG, Confidence, ...)
```

### 2.3 数据流

```
用户选择任务
    ↓
用户点击"加载方案"
    → VmIntegrationService.LoadSolution(solPath)
    → VmRenderControl 绑定显示
    ↓
用户点击"三期ToVM"
    → VmIntegrationService.SetGlobalVariableString("BN", value)
    → VmIntegrationService.SetGlobalVariableString("Mfg", value)
    → VmIntegrationService.SetGlobalVariableString("EXP", value)
    ↓
用户点击"单次运行"
    → VmIntegrationService.Run()
    → VmSolution.OnWorkStatusEvent 触发
    → VmIntegrationService 解析回调数据
    → 触发 DetectionCompleted 事件
    → DetectionView 更新 UI
```

---

## 3. 接口设计

### 3.1 VmIntegrationService 增强

```csharp
public class VmIntegrationService
{
    // 现有成员
    public void LoadSolution(string solPath);
    public void SetGlobalVariableString(string name, string value);
    public void SetProcedure(string procedureName);
    public void InitVmRender(FrameworkElement renderControl);

    // 新增事件
    public event EventHandler<DetectionResult> DetectionCompleted;
    public event EventHandler<string> StatusChanged;

    // 新增方法
    public void Run();                              // 单次运行
    public void RunContinuous();                    // 连续运行
    public void Stop();                             // 停止
}
```

### 3.2 DetectionResult 模型

```csharp
public class DetectionResult
{
    public string Result { get; set; }              // OK / NG
    public double Confidence { get; set; }           // 0.0 ~ 1.0
    public int CharCount { get; set; }               // 识别字符数
    public string CodeInfo { get; set; }             // 识别内容
    public string ImagePath { get; set; }            // 保存图像路径
    public DateTime DetectionTime { get; set; }      // 检测时间
    public string ErrorMessage { get; set; }         // 错误信息
    public long ElapsedMs { get; set; }             // 耗时(毫秒)
}
```

### 3.3 OnWorkStatusEvent 回调解析

```csharp
private void VmSolution_OnWorkStatusEvent(ImvsSdkDefine.IMVS_MODULE_WORK_STAUS workStatusInfo)
{
    // 解析 workStatusInfo 获取检测结果
    // - nWorkStatus: 0=空闲, 1=运行中, 2=成功, 3=失败
    // - strResult: 结果字符串
    // - fConfidence: 置信度

    var result = new DetectionResult
    {
        Result = workStatusInfo.nWorkStatus == 2 ? "OK" : "NG",
        Confidence = workStatusInfo.fConfidence,
        DetectionTime = DateTime.Now
    };

    OnDetectionCompleted?.Invoke(this, result);
}
```

---

## 4. 交互设计

### 4.1 用户操作流程（保持不变）

```
1. 选择任务 → 任务信息显示（产品/批次/日期）
2. 选择方案 → 加载 .sol 文件
3. 三期ToVM → 同步 BN/Mfg/EXP 到 VM
4. 单次运行 → 执行检测，显示真实结果
```

### 4.2 UI 状态变化

| 操作 | 状态显示 |
|-----|---------|
| 选择任务 | 任务信息卡片更新 |
| 加载方案 | 方案路径显示，RenderControl 初始化 |
| 三期ToVM | 日志显示 "BN: xxx → xxx"，状态栏更新 |
| 单次运行 | "运行中..." → 结果显示(OK/NG + 置信度) |
| 运行失败 | 红色提示，错误信息显示 |

---

## 5. 关键文件

| 文件 | 操作 | 说明 |
|-----|-----|-----|
| `Services/VmIntegrationService.cs` | 修改 | 增强，添加事件和 Run/Stop 方法 |
| `Views/DetectionView.xaml.cs` | 修改 | 使用 VmIntegrationService，移除直接 VM 调用 |
| `Models/DetectionResult.cs` | 修改 | 补充字段（ElapsedMs, ErrorMessage） |
| `Services/Settings/SettingsSyncService.cs` | 可选 | 如需同步通信/设备参数 |

---

## 6. 验证标准

- [ ] 编译通过，0 errors
- [ ] 选择任务后，点击"三期ToVM"能正确设置 BN/Mfg/EXP（查看日志验证）
- [ ] 点击"单次运行"后，结果从真实回调获取（非 Random 模拟）
- [ ] 检测结果（OK/NG、置信度、耗时）正确显示
- [ ] 日志中有 `[Callback] nWorkStatus=...` 输出
- [ ] 连续运行模式能持续接收结果回调
- [ ] 方案加载失败时有友好错误提示

---

## 7. 相关文档

- [系统架构文档](./2026-05-29-system-architecture.md)