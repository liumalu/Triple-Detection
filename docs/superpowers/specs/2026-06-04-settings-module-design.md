# Triple-Detection Settings 模块增强设计文档

**日期:** 2026-06-04
**版本:** 1.0
**状态:** 待批准

---

## 1. 概述

### 1.1 目标

为 Settings 模块的 4 个子模块实现完整的 UI 和数据持久化，并统一接入审计日志系统。

### 1.2 范围

- CommunicationSettingsView — 通讯设置 UI + 保存逻辑
- VmSettingsView — VisionMaster 设置 UI + 保存逻辑
- SystemSettingsView — 系统设置 UI + 保存逻辑
- DeviceControlSettingsView — 设备控制设置 UI + 保存逻辑
- 所有设置同时保存到 JSON 文件和 SystemConfigs 数据库表
- 所有设置变更记录 `SETTINGS_UPDATE` 审计日志

---

## 2. 持久化架构

### 2.1 双写策略

```
SettingsService.Save(model)
    │
    ├──► JSON 文件保存
    │    Config/communication.json
    │    Config/vm_settings.json
    │    Config/system.json
    │    Config/device_control.json
    │
    └──► SystemConfigs 表保存（审计追溯用）
         Category = 'Communication' | 'VmSettings' | 'System' | 'DeviceControl'
         ConfigKey = 'Settings'
         ConfigValue = JSON序列化整个设置对象
```

### 2.2 SystemConfigs 表映射

| Category | ConfigKey | ConfigValue 结构 |
|----------|-----------|-----------------|
| `Communication` | `Settings` | `CommunicationSettings` 完整 JSON |
| `VmSettings` | `Settings` | `VmSettings` 完整 JSON |
| `System` | `Settings` | `SystemSettings` 完整 JSON |
| `DeviceControl` | `Settings` | `DeviceControlSettings` 完整 JSON |

更新使用 `INSERT OR REPLACE`，每个 Category 只有一行。

---

## 3. 审计日志规范

### 3.1 SETTINGS_UPDATE Action

| 字段 | 值 |
|------|-----|
| Action | `SETTINGS_UPDATE` |
| ObjectType | `SystemConfig` |
| ObjectId | SystemConfigs 表主键 |
| Details | `{ "category": "Communication", "changes": ["CameraIp", "PlcPort"] }` |

**触发时机：** 保存按钮点击后，数据库写入成功时记录。

---

## 4. UI 设计

### 4.1 CommunicationSettingsView

布局：表单式，每项一行（Label + TextBox/Spinner）

| 字段 | 控件 | 说明 |
|------|------|------|
| Camera IP | TextBox | 默认 192.168.1.100 |
| Camera Port | TextBox (数字) | 默认 5000 |
| PLC IP | TextBox | 默认 192.168.1.200 |
| PLC Port | TextBox (数字) | 默认 5001 |
| PLC Type | ComboBox | Mitsubishi / Siemens / Omron |
| 波特率 | ComboBox | 9600 / 19200 / 38400 / 115200 |

### 4.2 VmSettingsView

| 字段 | 控件 | 说明 |
|------|------|------|
| VisionMaster 安装路径 | TextBox + Browse按钮 | 默认 C:\Program Files\VisionMaster4.2.0 |

### 4.3 SystemSettingsView

| 字段 | 控件 | 说明 |
|------|------|------|
| 日志保存方式 | ComboBox | ByDate / BySize |
| 日志保留天数 | TextBox (数字) | 默认 30 |
| 日志导出路径 | TextBox + Browse | 默认 D:\Logs\Export |
| 自动清理日志 | CheckBox | 默认 True |
| 工厂代号 | TextBox | 默认 F001 |
| 产线代码 | TextBox | 默认 L001 |
| 数据库备份目录 | TextBox + Browse | 默认 D:\Database\Backup |
| 图片保留数量 | TextBox (数字) | 默认 1000 |
| 自动清理图片 | CheckBox | 默认 True |

### 4.4 DeviceControlSettingsView

**采集参数：**

| 字段 | 控件 | 默认值 |
|------|------|--------|
| 光源类型 | ComboBox | LED / Halogen / Laser |
| 采集延迟(ms) | TextBox | 100 |
| 采集反馈超时(ms) | TextBox | 5000 |

**Modbus TCP：**

| 字段 | 控件 | 默认值 |
|------|------|--------|
| IP地址 | TextBox | 192.168.1.100 |
| 端口 | TextBox | 502 |
| 剔除继电器地址 | TextBox | 1 |
| 产线停止继电器地址 | TextBox | 2 |
| 连接超时(ms) | TextBox | 3000 |

**剔除参数：**

| 字段 | 控件 | 默认值 |
|------|------|--------|
| 剔除延迟(ms) | TextBox | 50 |
| 剔除持续时间(ms) | TextBox | 200 |
| 连续剔除次数停线 | TextBox | 10 |
| 启用连续剔除停线 | CheckBox | False |
| 启动任务前需连接IO | CheckBox | False |

---

## 5. 实现计划

### Task 1: 增强 SettingsService 基类 / 各 Service 实现

**文件:**
- Modify: `Application/SettingsServices/CommunicationSettingsService.cs`
- Modify: `Application/SettingsServices/VmSettingsService.cs`
- Modify: `Application/SettingsServices/SystemSettingsService.cs`
- Modify: `Application/SettingsServices/DeviceControlSettingsService.cs`

每个 Service 增加 `SaveToDb()` 方法，将设置对象 JSON 序列化后存入 SystemConfigs 表。

### Task 2: 实现 CommunicationSettingsView

**文件:**
- Modify: `Presentation/Views/Settings/CommunicationSettingsView.xaml`
- Create: `Presentation/Views/Settings/CommunicationSettingsView.xaml.cs`
- Create: `Presentation/ViewModels/Settings/CommunicationSettingsViewModel.cs`

### Task 3: 实现 VmSettingsView

**文件:**
- Modify: `Presentation/Views/Settings/VmSettingsView.xaml`
- Create: `Presentation/Views/Settings/VmSettingsView.xaml.cs`
- Create: `Presentation/ViewModels/Settings/VmSettingsViewModel.cs`

### Task 4: 实现 SystemSettingsView

**文件:**
- Modify: `Presentation/Views/Settings/SystemSettingsView.xaml`
- Create: `Presentation/Views/Settings/SystemSettingsView.xaml.cs`
- Create: `Presentation/ViewModels/Settings/SystemSettingsViewModel.cs`

### Task 5: 实现 DeviceControlSettingsView

**文件:**
- Modify: `Presentation/Views/Settings/DeviceControlSettingsView.xaml`
- Create: `Presentation/Views/Settings/DeviceControlSettingsView.xaml.cs`
- Create: `Presentation/ViewModels/Settings/DeviceControlSettingsViewModel.cs`

### Task 6: 添加审计日志记录点

在每个 SettingsViewModel 的 SaveCommand 中，增加 `SETTINGS_UPDATE` 审计日志记录：
- 文件: 4 个 ViewModel
- Details JSON: `{ "category": "Xxx", "changes": ["field1", "field2"] }`

### Task 7: 注册 ViewModel 到 DI

**文件:**
- Modify: `Presentation/App.xaml.cs`

`containerRegistry.Register<CommunicationSettingsViewModel>();` 等 4 个注册。

---

## 6. 依赖关系

```
Task 1 (Service增强)
    ↓
Task 2-5 (各View实现) ← 可并行
    ↓
Task 6 (审计日志)
    ↓
Task 7 (DI注册)
```

---

## 7. 风险与注意事项

1. **SettingsService 直接实例化**：目前 SettingsShellViewModel 中 `new SettingsSyncService()`，后续应通过 DI 注入
2. **JSON 与 DB 双写一致性**：JSON 保存失败时不应写入 DB；DB 保存失败时应回滚 JSON（使用事务或补偿机制）
3. **审计日志 Details 的 changes 数组**：由 ViewModel 在保存时比对旧值与新值，仅记录实际变更字段