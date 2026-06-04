# Triple-Detection Settings 模块实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 Settings 模块的 4 个子模块实现完整 UI 和双写持久化（JSON + SystemConfigs DB），并接入审计日志。

**Architecture:** 每个 SettingsService 独立保存 JSON 文件；新增 SystemConfigRepository 将设置以 Category=CategoryName, Key='Settings' 的形式存入 SystemConfigs 表；每个设置 ViewModel 注入 ISystemConfigRepository 和 IAuditLogService，在 SaveCommand 中实现双写 + 审计记录。

**Tech Stack:** .NET Framework 4.8, WPF, SQLite, Entity Framework 6, Prism.DryIoc

---

## 文件结构

```
Application/SettingsServices/
    CommunicationSettingsService.cs  ← 增加 SaveToDb() / LoadFromDb()
    VmSettingsService.cs            ← 同上
    SystemSettingsService.cs         ← 同上
    DeviceControlSettingsService.cs  ← 同上

Domain/Repositories/
    ISystemConfigRepository.cs      ← 新增
    SystemConfigRepository.cs       ← 新增

Presentation/ViewModels/Settings/
    CommunicationSettingsViewModel.cs   ← 新增
    VmSettingsViewModel.cs             ← 新增
    SystemSettingsViewModel.cs         ← 新增
    DeviceControlSettingsViewModel.cs  ← 新增

Presentation/Views/Settings/
    CommunicationSettingsView.xaml  ← 重写
    CommunicationSettingsView.xaml.cs ← 新增 code-behind
    VmSettingsView.xaml             ← 重写
    VmSettingsView.xaml.cs          ← 新增
    SystemSettingsView.xaml         ← 重写
    SystemSettingsView.xaml.cs      ← 新增
    DeviceControlSettingsView.xaml  ← 重写
    DeviceControlSettingsView.xaml.cs ← 新增

Presentation/App.xaml.cs            ← 注册 ViewModel 到 DI
```

---

### Task 1: 创建 SystemConfigRepository

**Files:**
- Create: `Domain/Repositories/ISystemConfigRepository.cs`
- Create: `Infrastructure/Repositories/SystemConfigRepository.cs`
- Test: 手动测试（无 TDD）

- [ ] **Step 1: 创建 ISystemConfigRepository 接口**

```csharp
using System.Collections.Generic;
using TripleDetection.Domain.Entities;

namespace TripleDetection.Domain.Repositories
{
    public interface ISystemConfigRepository
    {
        SystemConfig GetByCategoryAndKey(string category, string key);
        void SaveOrUpdate(SystemConfig config);
        IEnumerable<SystemConfig> GetAll();
    }
}
```

- [ ] **Step 2: 创建 SystemConfigRepository**

```csharp
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Repositories;

namespace TripleDetection.Infrastructure.Repositories
{
    public class SystemConfigRepository : SqliteRepository<SystemConfig>, ISystemConfigRepository
    {
        public SystemConfigRepository(string connectionString) : base(connectionString) { }

        public SystemConfig GetByCategoryAndKey(string category, string key)
        {
            var sql = "SELECT * FROM SystemConfigs WHERE Category = @Category AND Key = @Key AND IsDeleted = 0";
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Category", category);
                    cmd.Parameters.AddWithValue("@Key", key);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) return MapRow<SystemConfig>(reader);
                    }
                }
            }
            return null;
        }

        public void SaveOrUpdate(SystemConfig config)
        {
            var existing = GetByCategoryAndKey(config.Category, config.Key);
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                if (existing != null)
                {
                    var sql = @"UPDATE SystemConfigs SET Value = @Value, UpdateAt = @UpdateAt WHERE Id = @Id";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Value", config.Value);
                        cmd.Parameters.AddWithValue("@UpdateAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@Id", existing.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    var sql = @"INSERT INTO SystemConfigs (Category, Key, Value, IsDeleted, CreateAt, UpdateAt) VALUES (@Category, @Key, @Value, 0, @Now, @Now)";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Category", config.Category);
                        cmd.Parameters.AddWithValue("@Key", config.Key);
                        cmd.Parameters.AddWithValue("@Value", config.Value);
                        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public new SystemConfig MapRow<SystemConfig>(SQLiteDataReader reader) where SystemConfig : BaseEntity
        {
            var entity = Activator.CreateInstance<SystemConfig>();
            var type = typeof(SystemConfig);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                var prop = type.GetProperty(name);
                if (prop == null || reader.IsDBNull(i)) continue;
                prop.SetValue(entity, reader.GetValue(i));
            }
            return entity;
        }
    }
}
```

- [ ] **Step 3: 提交**

```bash
git add Domain/Repositories/ISystemConfigRepository.cs Infrastructure/Repositories/SystemConfigRepository.cs
git commit -m "feat: add SystemConfigRepository for settings persistence"
```

---

### Task 2: 增强 CommunicationSettingsService

**Files:**
- Modify: `Application/SettingsServices/CommunicationSettingsService.cs`

- [ ] **Step 1: 添加 DbPath 和 GetConnectionString() 方法**

在构造函数中添加获取数据库连接字符串的逻辑（参考 DatabaseInitializer 中的路径）。

```csharp
private static readonly string DbPath = Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory, "Config", "tripledetection.db");
```

- [ ] **Step 2: 添加 SaveToDb() 方法**

```csharp
public void SaveToDb(CommunicationSettings settings)
{
    var config = new SystemConfig
    {
        Category = "Communication",
        Key = "Settings",
        Value = JsonHelper.Serialize(settings)
    };
    var repo = new SystemConfigRepository($"Data Source={DbPath}");
    repo.SaveOrUpdate(config);
}
```

- [ ] **Step 3: 修改 Save() 方法，在 JSON 保存成功后调用 SaveToDb()**

在 `JsonHelper.Save` 之后添加：
```csharp
SaveToDb(settings);
```

- [ ] **Step 4: 提交**

```bash
git add Application/SettingsServices/CommunicationSettingsService.cs
git commit -m "feat: add SaveToDb to CommunicationSettingsService"
```

---

### Task 3: 增强 VmSettingsService

**Files:**
- Modify: `Application/SettingsServices/VmSettingsService.cs`

同上，增加 DbPath、SaveToDb()，Save() 中双写。

- [ ] **Step: 提交**

```bash
git add Application/SettingsServices/VmSettingsService.cs
git commit -m "feat: add SaveToDb to VmSettingsService"
```

---

### Task 4: 增强 SystemSettingsService

**Files:**
- Modify: `Application/SettingsServices/SystemSettingsService.cs`

同上，Category = "System"。

- [ ] **Step: 提交**

```bash
git add Application/SettingsServices/SystemSettingsService.cs
git commit -m "feat: add SaveToDb to SystemSettingsService"
```

---

### Task 5: 增强 DeviceControlSettingsService

**Files:**
- Modify: `Application/SettingsServices/DeviceControlSettingsService.cs`

同上，Category = "DeviceControl"。

- [ ] **Step: 提交**

```bash
git add Application/SettingsServices/DeviceControlSettingsService.cs
git commit -m "feat: add SaveToDb to DeviceControlSettingsService"
```

---

### Task 6: 实现 CommunicationSettingsViewModel

**Files:**
- Create: `Presentation/ViewModels/Settings/CommunicationSettingsViewModel.cs`

- [ ] **Step 1: 创建 ViewModel**

```csharp
using System;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using TripleDetection.Application.SettingsServices;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.SessionManager;

namespace TripleDetection.Presentation.ViewModels.Settings
{
    public class CommunicationSettingsViewModel : ViewModelBase
    {
        private readonly CommunicationSettingsService _service;
        private readonly IAuditLogService _auditLogService;

        private string _cameraIp;
        private int _cameraPort;
        private string _plcIp;
        private int _plcPort;
        private string _plcType;
        private int _baudRate;
        private CommunicationSettings _originalSettings;

        public DelegateCommand SaveCommand { get; }

        public CommunicationSettingsViewModel(CommunicationSettingsService service, IAuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
            SaveCommand = new DelegateCommand(Save);
            Load();
        }

        public string CameraIp { get => _cameraIp; set => SetProperty(ref _cameraIp, value); }
        public int CameraPort { get => _cameraPort; set => SetProperty(ref _cameraPort, value); }
        public string PlcIp { get => _plcIp; set => SetProperty(ref _plcIp, value); }
        public int PlcPort { get => _plcPort; set => SetProperty(ref _plcPort, value); }
        public string PlcType { get => _plcType; set => SetProperty(ref _plcType, value); }
        public int BaudRate { get => _baudRate; set => SetProperty(ref _baudRate, value); }

        public string[] PlcTypeOptions => new[] { "Mitsubishi", "Siemens", "Omron" };
        public int[] BaudRateOptions => new[] { 9600, 19200, 38400, 115200 };

        private void Load()
        {
            var settings = _service.Load();
            _originalSettings = settings;
            CameraIp = settings.CameraIp;
            CameraPort = settings.CameraPort;
            PlcIp = settings.PlcIp;
            PlcPort = settings.PlcPort;
            PlcType = settings.PlcType;
            BaudRate = settings.BaudRate;
        }

        private void Save()
        {
            try
            {
                var newSettings = new CommunicationSettings
                {
                    CameraIp = CameraIp,
                    CameraPort = CameraPort,
                    PlcIp = PlcIp,
                    PlcPort = PlcPort,
                    PlcType = PlcType,
                    BaudRate = BaudRate
                };
                _service.Save(newSettings);

                var changes = DetectChanges(_originalSettings, newSettings);
                _auditLogService.LogAsync(
                    SessionManager.CurrentUserId ?? 0,
                    SessionManager.CurrentUserName ?? "Unknown",
                    "SETTINGS_UPDATE",
                    "SystemConfig",
                    0,
                    $"{{\"category\":\"Communication\",\"changes\":{JsonHelper.Serialize(changes)}}}");

                MessageBox.Show("保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string[] DetectChanges(CommunicationSettings old, CommunicationSettings new_)
        {
            var changes = new System.Collections.Generic.List<string>();
            if (old.CameraIp != new_.CameraIp) changes.Add("CameraIp");
            if (old.CameraPort != new_.CameraPort) changes.Add("CameraPort");
            if (old.PlcIp != new_.PlcIp) changes.Add("PlcIp");
            if (old.PlcPort != new_.PlcPort) changes.Add("PlcPort");
            if (old.PlcType != new_.PlcType) changes.Add("PlcType");
            if (old.BaudRate != new_.BaudRate) changes.Add("BaudRate");
            return changes.ToArray();
        }
    }
}
```

- [ ] **Step 2: 提交**

```bash
git add Presentation/ViewModels/Settings/CommunicationSettingsViewModel.cs
git commit -m "feat: add CommunicationSettingsViewModel with audit logging"
```

---

### Task 7: 实现 CommunicationSettingsView XAML

**Files:**
- Modify: `Presentation/Views/Settings/CommunicationSettingsView.xaml`
- Create: `Presentation/Views/Settings/CommunicationSettingsView.xaml.cs`

- [ ] **Step 1: 重写 CommunicationSettingsView.xaml**

```xml
<UserControl x:Class="TripleDetection.Presentation.Views.Settings.CommunicationSettingsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True"
             Background="White">
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="20" MaxWidth="600" HorizontalAlignment="Left">
            <TextBlock Text="通讯设置" FontSize="20" FontWeight="Bold" Margin="0,0,0,20"/>

            <!-- Camera Settings -->
            <TextBlock Text="相机设置" FontSize="14" FontWeight="SemiBold" Margin="0,0,0,10"/>
            <Grid Margin="0,0,0,15">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="120"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <Grid.RowDefinitions>
                    <RowDefinition Height="35"/>
                    <RowDefinition Height="35"/>
                </Grid.RowDefinitions>
                <TextBlock Grid.Row="0" Text="相机 IP:" VerticalAlignment="Center"/>
                <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding CameraIp, UpdateSourceTrigger=PropertyChanged}" Style="{StaticResource InputTextBoxStyle}"/>
                <TextBlock Grid.Row="1" Text="相机端口:" VerticalAlignment="Center" Margin="0,5,0,0"/>
                <TextBox Grid.Row="1" Grid.Column="1" Text="{Binding CameraPort, UpdateSourceTrigger=PropertyChanged}" Style="{StaticResource InputTextBoxStyle}" Margin="0,5,0,0"/>
            </Grid>

            <!-- PLC Settings -->
            <TextBlock Text="PLC 设置" FontSize="14" FontWeight="SemiBold" Margin="0,10,0,10"/>
            <Grid Margin="0,0,0,15">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="120"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <Grid.RowDefinitions>
                    <RowDefinition Height="35"/>
                    <RowDefinition Height="35"/>
                    <RowDefinition Height="35"/>
                    <RowDefinition Height="35"/>
                </Grid.RowDefinitions>
                <TextBlock Grid.Row="0" Text="PLC IP:" VerticalAlignment="Center"/>
                <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding PlcIp, UpdateSourceTrigger=PropertyChanged}" Style="{StaticResource InputTextBoxStyle}"/>
                <TextBlock Grid.Row="1" Text="PLC 端口:" VerticalAlignment="Center" Margin="0,5,0,0"/>
                <TextBox Grid.Row="1" Grid.Column="1" Text="{Binding PlcPort, UpdateSourceTrigger=PropertyChanged}" Style="{StaticResource InputTextBoxStyle}" Margin="0,5,0,0"/>
                <TextBlock Grid.Row="2" Text="PLC 类型:" VerticalAlignment="Center" Margin="0,5,0,0"/>
                <ComboBox Grid.Row="2" Grid.Column="1" ItemsSource="{Binding PlcTypeOptions}" SelectedItem="{Binding PlcType}" Margin="0,5,0,0"/>
                <TextBlock Grid.Row="3" Text="波特率:" VerticalAlignment="Center" Margin="0,5,0,0"/>
                <ComboBox Grid.Row="3" Grid.Column="1" ItemsSource="{Binding BaudRateOptions}" SelectedItem="{Binding BaudRate}" Margin="0,5,0,0"/>
            </Grid>

            <Button Content="保存" Command="{Binding SaveCommand}" Style="{StaticResource PrimaryButtonStyle}" Width="100" HorizontalAlignment="Left" Margin="0,10,0,0"/>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 2: 创建 code-behind**

```csharp
using System.Windows.Controls;

namespace TripleDetection.Presentation.Views.Settings
{
    public partial class CommunicationSettingsView : UserControl
    {
        public CommunicationSettingsView()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 3: 提交**

```bash
git add Presentation/Views/Settings/CommunicationSettingsView.xaml Presentation/Views/Settings/CommunicationSettingsView.xaml.cs
git commit -m "feat: implement CommunicationSettingsView"
```

---

### Task 8: 实现 VmSettingsViewModel + VmSettingsView

**Files:**
- Create: `Presentation/ViewModels/Settings/VmSettingsViewModel.cs`
- Modify: `Presentation/Views/Settings/VmSettingsView.xaml`
- Create: `Presentation/Views/Settings/VmSettingsView.xaml.cs`

- [ ] **Step: 提交**

```bash
git add Presentation/ViewModels/Settings/VmSettingsViewModel.cs Presentation/Views/Settings/VmSettingsView.xaml Presentation/Views/Settings/VmSettingsView.xaml.cs
git commit -m "feat: implement VmSettingsView"
```

---

### Task 9: 实现 SystemSettingsViewModel + SystemSettingsView

**Files:**
- Create: `Presentation/ViewModels/Settings/SystemSettingsViewModel.cs`
- Modify: `Presentation/Views/Settings/SystemSettingsView.xaml`
- Create: `Presentation/Views/Settings/SystemSettingsView.xaml.cs`

- [ ] **Step: 提交**

```bash
git add Presentation/ViewModels/Settings/SystemSettingsViewModel.cs Presentation/Views/Settings/SystemSettingsView.xaml Presentation/Views/Settings/SystemSettingsView.xaml.cs
git commit -m "feat: implement SystemSettingsView"
```

---

### Task 10: 实现 DeviceControlSettingsViewModel + DeviceControlSettingsView

**Files:**
- Create: `Presentation/ViewModels/Settings/DeviceControlSettingsViewModel.cs`
- Modify: `Presentation/Views/Settings/DeviceControlSettingsView.xaml`
- Create: `Presentation/Views/Settings/DeviceControlSettingsView.xaml.cs`

- [ ] **Step: 提交**

```bash
git add Presentation/ViewModels/Settings/DeviceControlSettingsViewModel.cs Presentation/Views/Settings/DeviceControlSettingsView.xaml Presentation/Views/Settings/DeviceControlSettingsView.xaml.cs
git commit -m "feat: implement DeviceControlSettingsView"
```

---

### Task 11: 注册 ViewModel 到 DI 容器

**Files:**
- Modify: `Presentation/App.xaml.cs`

- [ ] **Step 1: 在 App.xaml.cs 中添加注册**

在现有的 `containerRegistry.RegisterSingleton` 区域添加：

```csharp
// Settings ViewModels
containerRegistry.Register<ViewModels.Settings.CommunicationSettingsViewModel>();
containerRegistry.Register<ViewModels.Settings.VmSettingsViewModel>();
containerRegistry.Register<ViewModels.Settings.SystemSettingsViewModel>();
containerRegistry.Register<ViewModels.Settings.DeviceControlSettingsViewModel>();
```

- [ ] **Step 2: 提交**

```bash
git add Presentation/App.xaml.cs
git commit -m "feat: register settings viewmodels in DI container"
```

---

## 自检清单

**1. Spec 覆盖检查：**
- [x] 双写（JSON + SystemConfigs DB）— Task 2-5 实现各 Service 的 SaveToDb
- [x] 4个 View 完整 UI — Task 6-10
- [x] SETTINGS_UPDATE 审计日志 — Task 6-10 中 Save() 方法包含 _auditLogService.LogAsync
- [x] DI 注册 — Task 11

**2. 占位符扫描：** 无 TBD/TODO/未填写的内容

**3. 类型一致性：** 所有 Task 使用相同的 `DelegateCommand`、`MessageBox` 调用模式、`SessionManager` 获取当前用户