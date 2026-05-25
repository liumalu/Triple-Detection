# Triple Detection 主应用布局实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现 Triple Detection WPF 应用的新主布局：顶部标题栏 + 可折叠左侧导航栏 + 主内容区域

**Architecture:** 采用 MVVM 模式，MainWindow 作为容器包含标题栏和导航栏，ContentControl 作为主内容切换不同页面视图

**Tech Stack:** WPF (.NET Framework 4.8), MVVM, XAML

---

## 文件结构映射

| 文件 | 职责 |
|------|------|
| `MainWindow.xaml` | 主窗口布局：标题栏 + 导航栏 + 内容区 |
| `MainWindow.xaml.cs` | 导航逻辑、折叠/展开、页面切换 |
| `MainViewModel.cs` | 导航状态、当前页面、命令 |
| `Views/DetectionView.xaml/.cs` | 检测页面：左侧图像 + 右侧任务信息/方案操作/检测结果 + 底部日志 |
| `Views/DashboardView.xaml/.cs` | 仪表盘页面 |
| `Views/LogsView.xaml/.cs` | 操作日志页面 |
| `Views/SettingsView.xaml/.cs` | 系统配置页面 |
| `Resources/Styles.xaml` | 共享样式：导航项、按钮、颜色 |
| `App.config` | 添加 LogoPath、SystemName 配置 |

---

## 任务清单

### Task 1: 创建共享样式资源

**Files:**
- Create: `TripleDetection.App/Resources/Styles.xaml`

- [ ] **Step 1: 创建 Styles.xaml 文件**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- 颜色定义 -->
    <Color x:Key="PrimaryColor">#4FD1C5</Color>
    <Color x:Key="DarkSlateColor">#2D3748</Color>
    <Color x:Key="TextSecondaryColor">#A0AEC0</Color>
    <Color x:Key="TextPrimaryColor">#FFFFFF</Color>

    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}"/>
    <SolidColorBrush x:Key="DarkSlateBrush" Color="{StaticResource DarkSlateColor}"/>
    <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondaryColor}"/>
    <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimaryColor}"/>

    <!-- 导航按钮样式 -->
    <Style x:Key="NavButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="{StaticResource TextSecondaryBrush}"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="HorizontalContentAlignment" Value="Left"/>
        <Setter Property="Padding" Value="16,12"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="border" Background="{TemplateBinding Background}"
                            BorderThickness="4,0,0,0" BorderBrush="Transparent">
                        <ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
                                          VerticalAlignment="Center" Margin="{TemplateBinding Padding}"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="border" Property="Background" Value="rgba(255,255,255,0.05)"/>
                            <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- 活动导航按钮样式 -->
    <Style x:Key="NavButtonActiveStyle" TargetType="Button" BasedOn="{StaticResource NavButtonStyle}">
        <Setter Property="Foreground" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="border" Background="rgba(79,209,197,0.1)"
                            BorderThickness="4,0,0,0" BorderBrush="{StaticResource PrimaryBrush}">
                        <ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
                                          VerticalAlignment="Center" Margin="{TemplateBinding Padding}"/>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- 主按钮样式 -->
    <Style x:Key="PrimaryButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="Foreground" Value="#1A202C"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="MinWidth" Value="80"/>
        <Setter Property="Cursor" Value="Hand"/>
    </Style>

</ResourceDictionary>
```

- [ ] **Step 2: 提交**

```bash
git add TripleDetection.App/Resources/Styles.xaml
git commit -m "feat: add shared styles resource dictionary"
```

---

### Task 2: 更新 MainWindow.xaml 布局

**Files:**
- Modify: `TripleDetection.App/MainWindow.xaml`

- [ ] **Step 1: 替换整个 MainWindow.xaml 内容**

```xml
<Window x:Class="TripleDetection.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:TripleDetection"
        Title="Triple Detection"
        Height="750" Width="1284"
        WindowStartupLocation="CenterScreen"
        Loaded="Window_Loaded"
        Closing="Window_Closing">

    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Resources/Styles.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="48"/>  <!-- 标题栏 -->
            <RowDefinition Height="*"/>   <!-- 主内容 -->
        </Grid.RowDefinitions>

        <!-- 标题栏 -->
        <Border Grid.Row="0" Background="{StaticResource DarkSlateBrush}">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <!-- Logo + 系统名称 -->
                <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center" Margin="16,0">
                    <Image x:Name="imgLogo" Width="32" Height="32" Stretch="Uniform"/>
                    <TextBlock x:Name="txtSystemName" Text="Triple Detection"
                               Foreground="White" FontSize="16" FontWeight="Bold"
                               VerticalAlignment="Center" Margin="12,0,0,0"/>
                </StackPanel>

                <!-- 右侧用户信息 -->
                <StackPanel Grid.Column="2" Orientation="Horizontal" VerticalAlignment="Center" Margin="16,0">
                    <!-- 通知图标 -->
                    <Button x:Name="btnNotifications" Style="{StaticResource NavButtonStyle}"
                            Content="🔔" FontSize="16" Padding="8" Margin="0,0,8,0"
                            Click="BtnNotifications_Click" ToolTip="操作日志"/>
                    <!-- 用户名 -->
                    <TextBlock x:Name="txtUsername" Text="Admin" Foreground="White"
                               VerticalAlignment="Center" Margin="0,0,16,0"/>
                    <!-- 退出按钮 -->
                    <Button x:Name="btnLogout" Content="退出" Style="{StaticResource PrimaryButtonStyle}"
                            Background="#E53E3E" Foreground="White" Click="BtnLogout_Click"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- 主内容区域 -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition x:Name="navColumn" Width="200"/>  <!-- 导航栏 -->
                <ColumnDefinition Width="*"/>                        <!-- 内容区 -->
            </Grid.ColumnDefinitions>

            <!-- 导航栏 -->
            <Border Grid.Column="0" Background="#1A202C">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>

                    <!-- 折叠按钮 -->
                    <Button Grid.Row="0" x:Name="btnToggleNav" Content="◀" FontSize="12"
                            Style="{StaticResource NavButtonStyle}" HorizontalAlignment="Right"
                            Margin="0,8,8,0" Click="BtnToggleNav_Click" ToolTip="折叠导航"/>

                    <!-- 导航项 -->
                    <StackPanel Grid.Row="1" x:Name="navPanel">
                        <Button x:Name="btnNavDashboard" Content="📊 仪表盘"
                                Style="{StaticResource NavButtonActiveStyle}" Click="BtnNav_Click" Tag="Dashboard"/>
                        <Button x:Name="btnNavDetection" Content="🔍 检测执行"
                                Style="{StaticResource NavButtonStyle}" Click="BtnNav_Click" Tag="Detection"/>
                        <Button x:Name="btnNavProducts" Content="📦 产品管理"
                                Style="{StaticResource NavButtonStyle}" Click="BtnNav_Click" Tag="Products"/>
                        <Button x:Name="btnNavTasks" Content="📋 任务管理"
                                Style="{StaticResource NavButtonStyle}" Click="BtnNav_Click" Tag="Tasks"/>
                        <Button x:Name="btnNavLogs" Content="📝 操作日志"
                                Style="{StaticResource NavButtonStyle}" Click="BtnNav_Click" Tag="Logs"/>
                        <Button x:Name="btnNavSettings" Content="⚙️ 系统配置"
                                Style="{StaticResource NavButtonStyle}" Click="BtnNav_Click" Tag="Settings"/>
                    </StackPanel>
                </Grid>
            </Border>

            <!-- 主内容 -->
            <Border Grid.Column="1" Background="#404040">
                <ContentControl x:Name="MainContent"/>
            </Border>
        </Grid>
    </Grid>
</Window>
```

- [ ] **Step 2: 提交**

```bash
git add TripleDetection.App/MainWindow.xaml
git commit -m "feat: rewrite MainWindow.xaml with header + nav rail + content layout"
```

---

### Task 3: 更新 MainWindow.xaml.cs 导航逻辑

**Files:**
- Modify: `TripleDetection.App/MainWindow.xaml.cs`

- [ ] **Step 1: 添加导航相关字段和方法**

在现有字段区域添加：
```csharp
private bool _isNavExpanded = true;
private readonly Dictionary<string, Button> _navButtons = new Dictionary<string, Button>();
private object _currentPage;
```

在构造函数后添加导航初始化（在 InitializeComponent 后）：
```csharp
// 初始化导航按钮字典
_navButtons["Dashboard"] = btnNavDashboard;
_navButtons["Detection"] = btnNavDetection;
_navButtons["Products"] = btnNavProducts;
_navButtons["Tasks"] = btnNavTasks;
_navButtons["Logs"] = btnNavLogs;
_navButtons["Settings"] = btnNavSettings;

// 加载配置
LoadConfiguration();
```

在 Window_Loaded 中添加：
```csharp
// 加载系统配置
var logoPath = ConfigurationManager.AppSettings["SystemLogoPath"];
var systemName = ConfigurationManager.AppSettings["SystemName"];
if (!string.IsNullOrEmpty(logoPath))
{
    try { imgLogo.Source = new BitmapImage(new Uri(logoPath, UriKind.Relative)); } catch { }
}
if (!string.IsNullOrEmpty(systemName))
{
    txtSystemName.Text = systemName;
}
```

- [ ] **Step 2: 添加导航按钮点击处理**

```csharp
private void BtnNav_Click(object sender, RoutedEventArgs e)
{
    if (sender is Button btn && btn.Tag is string tag)
    {
        NavigateTo(tag);
    }
}

private void NavigateTo(string pageName)
{
    // 更新导航按钮样式
    foreach (var kvp in _navButtons)
    {
        kvp.Value.Style = kvp.Key == pageName
            ? (Style)FindResource("NavButtonActiveStyle")
            : (Style)FindResource("NavButtonStyle");
    }

    // 切换页面
    switch (pageName)
    {
        case "Dashboard":
            MainContent.Content = new Views.DashboardView();
            break;
        case "Detection":
            MainContent.Content = new Views.DetectionView();
            break;
        case "Products":
            MainContent.Content = new Views.ProductListView();
            break;
        case "Tasks":
            MainContent.Content = new Views.TaskListView();
            break;
        case "Logs":
            MainContent.Content = new Views.LogsView();
            break;
        case "Settings":
            MainContent.Content = new Views.SettingsView();
            break;
    }

    _logService.Log($"导航到: {pageName}");
}
```

- [ ] **Step 3: 添加折叠/展开功能**

```csharp
private void BtnToggleNav_Click(object sender, RoutedEventArgs e)
{
    _isNavExpanded = !_isNavExpanded;
    if (_isNavExpanded)
    {
        navColumn.Width = new GridLength(200);
        btnToggleNav.Content = "◀";
        // 显示所有导航项文字
        foreach (var btn in _navButtons.Values)
        {
            btn.HorizontalContentAlignment = HorizontalAlignment.Left;
        }
    }
    else
    {
        navColumn.Width = new GridLength(48);
        btnToggleNav.Content = "▶";
        // 隐藏文字，仅显示图标
        foreach (var btn in _navButtons.Values)
        {
            btn.HorizontalContentAlignment = HorizontalAlignment.Center;
        }
    }
}
```

- [ ] **Step 4: 添加通知和退出按钮处理**

```csharp
private void BtnNotifications_Click(object sender, RoutedEventArgs e)
{
    NavigateTo("Logs");
}

private void BtnLogout_Click(object sender, RoutedEventArgs e)
{
    var result = MessageBox.Show("确认退出系统？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
    if (result == MessageBoxResult.Yes)
    {
        _logService.Log("用户退出系统");
        Application.Current.Shutdown();
    }
}
```

- [ ] **Step 5: 添加 LoadConfiguration 方法**

```csharp
private void LoadConfiguration()
{
    var navExpanded = ConfigurationManager.AppSettings["NavRailExpanded"];
    if (navExpanded == "false")
    {
        _isNavExpanded = false;
        navColumn.Width = new GridLength(48);
        btnToggleNav.Content = "▶";
    }
}
```

- [ ] **Step 6: 提交**

```bash
git add TripleDetection.App/MainWindow.xaml.cs
git commit -m "feat: add navigation logic to MainWindow.xaml.cs"
```

---

### Task 4: 创建 DetectionView 检测页面

**Files:**
- Create: `TripleDetection.App/Views/DetectionView.xaml`
- Create: `TripleDetection.App/Views/DetectionView.xaml.cs`

- [ ] **Step 1: 创建 DetectionView.xaml**

```xml
<UserControl x:Class="TripleDetection.Views.DetectionView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Background="#404040">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>   <!-- 任务选择 -->
            <RowDefinition Height="*"/>       <!-- 主内容 -->
            <RowDefinition Height="120"/>    <!-- 操作日志 -->
        </Grid.RowDefinitions>

        <!-- 任务选择 -->
        <Border Grid.Row="0" Background="#505050" Padding="16,8">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="选择任务:" Foreground="White" VerticalAlignment="Center" Margin="0,0,8,0"/>
                <ComboBox x:Name="cmbTask" Width="300" SelectionChanged="CmbTask_SelectionChanged"/>
            </StackPanel>
        </Border>

        <!-- 主内容：左侧图像 + 右侧信息 -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="320"/>
            </Grid.ColumnDefinitions>

            <!-- 左侧：VisionMaster 显示区域 -->
            <Border Grid.Column="0" Margin="5" BorderBrush="#606060" BorderThickness="1">
                <Grid>
                    <!-- VmRenderControl 占位 -->
                    <Border x:Name="VmRenderHost" Background="#2D2D2D"/>
                    <!-- 原有的 VmRenderControl 将在代码中初始化 -->
                    <TextBlock Text="VisionMaster 显示区域" Foreground="#808080"
                               HorizontalAlignment="Center" VerticalAlignment="Center"
                               FontSize="14" IsHitTestVisible="False"/>
                </Grid>
            </Border>

            <!-- 右侧：任务信息 + 方案操作 + 检测结果 + 控制按钮 -->
            <Border Grid.Column="1" Margin="0,5,5,5" Background="#505050" Padding="16">
                <ScrollViewer VerticalScrollBarVisibility="Auto">
                    <StackPanel>
                        <!-- 任务信息 -->
                        <TextBlock Text="任务信息" FontSize="14" FontWeight="Bold" Foreground="#4FD1C5" Margin="0,0,0,8"/>
                        <Border Background="#404040" Padding="12" Margin="0,0,0,16">
                            <StackPanel>
                                <TextBlock x:Name="txtProduct" Foreground="White" Margin="0,2"/>
                                <TextBlock x:Name="txtBatch" Foreground="White" Margin="0,2"/>
                                <TextBlock x:Name="txtProductionDate" Foreground="White" Margin="0,2"/>
                                <TextBlock x:Name="txtExpirationDate" Foreground="White" Margin="0,2"/>
                            </StackPanel>
                        </Border>

                        <!-- 方案操作 -->
                        <TextBlock Text="方案操作" FontSize="14" FontWeight="Bold" Foreground="#4FD1C5" Margin="0,0,0,8"/>
                        <StackPanel Orientation="Horizontal" Margin="0,0,0,16">
                            <Button x:Name="btnSelectSol" Content="选择方案" Click="BtnSelectSol_Click"
                                    Style="{StaticResource PrimaryButtonStyle}" Margin="0,0,8,0"/>
                            <Button x:Name="btnLoadSol" Content="加载方案" Click="BtnLoadSol_Click"
                                    Style="{StaticResource PrimaryButtonStyle}"/>
                        </StackPanel>

                        <!-- 检测结果 -->
                        <TextBlock Text="检测结果" FontSize="14" FontWeight="Bold" Foreground="#4FD1C5" Margin="0,0,0,8"/>
                        <Border Background="#404040" Padding="12" Margin="0,0,0,16">
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="*"/>
                                </Grid.ColumnDefinitions>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="Auto"/>
                                    <RowDefinition Height="Auto"/>
                                </Grid.RowDefinitions>
                                <TextBlock Grid.Row="0" Grid.Column="0" Text="结果:" Foreground="#A0AEC0"/>
                                <StackPanel Grid.Row="0" Grid.Column="1" Orientation="Horizontal">
                                    <TextBlock x:Name="txtOkCount" Text="OK: 0" Foreground="#00C000" Margin="0,0,8,0"/>
                                    <TextBlock x:Name="txtNgCount" Text="NG: 0" Foreground="#FF0000"/>
                                </StackPanel>
                                <TextBlock Grid.Row="1" Grid.Column="0" Text="置信度:" Foreground="#A0AEC0" Margin="0,4,0,0"/>
                                <TextBlock x:Name="txtConfidence" Grid.Row="1" Grid.Column="1" Text="--" Foreground="White" Margin="0,4,0,0"/>
                            </Grid>
                        </Border>

                        <!-- 控制按钮 -->
                        <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                            <Button x:Name="btnStart" Content="启动" Click="BtnStart_Click"
                                    Style="{StaticResource PrimaryButtonStyle}" Margin="0,0,8,0" MinWidth="80"/>
                            <Button x:Name="btnStop" Content="停止" Click="BtnStop_Click"
                                    Style="{StaticResource PrimaryButtonStyle}" Margin="0,0,8,0" MinWidth="80"
                                    IsEnabled="False"/>
                            <Button x:Name="btnReset" Content="重置" Click="BtnReset_Click"
                                    Style="{StaticResource PrimaryButtonStyle}" MinWidth="80"/>
                        </StackPanel>
                    </StackPanel>
                </ScrollViewer>
            </Border>
        </Grid>

        <!-- 底部：操作日志 -->
        <Border Grid.Row="2" Margin="5,0,5,5" Background="#505050" Padding="8">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>
                <TextBlock Grid.Row="0" Text="操作日志" FontSize="12" FontWeight="Bold" Foreground="#4FD1C5" Margin="0,0,0,4"/>
                <ListBox Grid.Row="1" x:Name="lstLogs" Background="#404040" Foreground="White"
                         BorderThickness="0" FontFamily="Consolas" FontSize="11"/>
            </Grid>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 2: 创建 DetectionView.xaml.cs**

```csharp
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TripleDetection.Services;
using TripleDetection.ViewModels;

namespace TripleDetection.Views
{
    public partial class DetectionView : UserControl
    {
        private readonly LoggingService _logService;
        private readonly TaskService _taskService;
        private readonly MainViewModel _mainViewModel;
        private Data.Entities.Task _selectedTask;

        public DetectionView()
        {
            InitializeComponent();
            _logService = LoggingService.Instance;
            _taskService = new TaskService();
            _mainViewModel = new MainViewModel();

            LoadTasks();
            SubscribeToLogs();

            _logService.Log("检测页面已加载");
        }

        private void LoadTasks()
        {
            var tasks = _taskService.GetAll().Where(t => t.Status == Data.Entities.TaskStatus.Approved).ToList();
            cmbTask.ItemsSource = tasks;
            if (tasks.Count > 0)
                cmbTask.SelectedIndex = 0;
        }

        private void SubscribeToLogs()
        {
            _logService.OnLogAdded += (s, msg) =>
            {
                Dispatcher.Invoke(() =>
                {
                    lstLogs.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
                    if (lstLogs.Items.Count > 100)
                        lstLogs.Items.RemoveAt(lstLogs.Items.Count - 1);
                });
            };
        }

        private void CmbTask_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTask.SelectedItem is Data.Entities.Task task)
            {
                _selectedTask = task;
                txtProduct.Text = $"产品：{task.Product?.Name ?? "-"}";
                txtBatch.Text = $"批次：{task.BatchNumber}";
                txtProductionDate.Text = $"生产日期：{task.ProductionDate:yyyy-MM-dd}";
                txtExpirationDate.Text = task.ExpirationDate.HasValue
                    ? $"有效期至：{task.ExpirationDate:yyyy-MM-dd}"
                    : $"有效期至：-";
                _logService.Log($"已选择任务：{task.Name}");
            }
        }

        private void BtnSelectSol_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "VM Sol File|*.sol*";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _logService.Log($"已选择方案：{dialog.FileName}");
            }
        }

        private void BtnLoadSol_Click(object sender, RoutedEventArgs e)
        {
            _logService.Log("加载方案...");
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            _logService.Log("检测开始");
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            _logService.Log("检测停止");
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            txtOkCount.Text = "OK: 0";
            txtNgCount.Text = "NG: 0";
            txtConfidence.Text = "--";
            _logService.Log("结果已重置");
        }
    }
}
```

- [ ] **Step 3: 提交**

```bash
git add TripleDetection.App/Views/DetectionView.xaml TripleDetection.App/Views/DetectionView.xaml.cs
git commit -m "feat: create DetectionView with task info, solution controls, results, and logs"
```

---

### Task 5: 创建其他页面视图

**Files:**
- Create: `TripleDetection.App/Views/DashboardView.xaml/.cs`
- Create: `TripleDetection.App/Views/LogsView.xaml/.cs`
- Create: `TripleDetection.App/Views/SettingsView.xaml/.cs`

- [ ] **Step 1: 创建 DashboardView.xaml**

```xml
<UserControl x:Class="TripleDetection.Views.DashboardView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Background="#404040">

    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 标题 -->
        <TextBlock Grid.Row="0" Text="仪表盘" FontSize="24" FontWeight="Bold" Foreground="White" Margin="0,0,0,20"/>

        <!-- 统计卡片 -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,20">
            <Border Background="#505050" Padding="20" Margin="0,0,16,0" MinWidth="150">
                <StackPanel>
                    <TextBlock Text="今日OK" Foreground="#A0AEC0" FontSize="12"/>
                    <TextBlock x:Name="txtTodayOk" Text="0" Foreground="#00C000" FontSize="32" FontWeight="Bold"/>
                </StackPanel>
            </Border>
            <Border Background="#505050" Padding="20" Margin="0,0,16,0" MinWidth="150">
                <StackPanel>
                    <TextBlock Text="今日NG" Foreground="#A0AEC0" FontSize="12"/>
                    <TextBlock x:Name="txtTodayNg" Text="0" Foreground="#FF0000" FontSize="32" FontWeight="Bold"/>
                </StackPanel>
            </Border>
            <Border Background="#505050" Padding="20" Margin="0,0,16,0" MinWidth="150">
                <StackPanel>
                    <TextBlock Text="总任务数" Foreground="#A0AEC0" FontSize="12"/>
                    <TextBlock x:Name="txtTotalTasks" Text="0" Foreground="White" FontSize="32" FontWeight="Bold"/>
                </StackPanel>
            </Border>
            <Border Background="#505050" Padding="20" MinWidth="150">
                <StackPanel>
                    <TextBlock Text="待审核" Foreground="#A0AEC0" FontSize="12"/>
                    <TextBlock x:Name="txtPending" Text="0" Foreground="#ED8936" FontSize="32" FontWeight="Bold"/>
                </StackPanel>
            </Border>
        </StackPanel>

        <!-- 最近检测列表 -->
        <Border Grid.Row="2" Background="#505050" Padding="16">
            <StackPanel>
                <TextBlock Text="最近检测" FontSize="16" FontWeight="Bold" Foreground="White" Margin="0,0,0,12"/>
                <DataGrid x:Name="dgRecent" AutoGenerateColumns="False" Background="Transparent"
                          Foreground="White" BorderThickness="0" IsReadOnly="True">
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="时间" Binding="{Binding DetectionTime}" Width="150"/>
                        <DataGridTextColumn Header="任务" Binding="{Binding TaskName}" Width="*"/>
                        <DataGridTextColumn Header="结果" Binding="{Binding Result}" Width="80"/>
                        <DataGridTextColumn Header="置信度" Binding="{Binding Confidence}" Width="100"/>
                    </DataGrid.Columns>
                </DataGrid>
            </StackPanel>
        </Border>

        <!-- 快捷操作 -->
        <StackPanel Grid.Row="3" Orientation="Horizontal" Margin="0,16,0,0">
            <Button Content="新建检测" Style="{StaticResource PrimaryButtonStyle}" Margin="0,0,8,0"/>
            <Button Content="管理产品" Style="{StaticResource PrimaryButtonStyle}" Margin="0,0,8,0"/>
            <Button Content="查看任务" Style="{StaticResource PrimaryButtonStyle}"/>
        </StackPanel>
    </Grid>
</UserControl>
```

- [ ] **Step 2: 创建 DashboardView.xaml.cs**

```csharp
using System.Windows.Controls;

namespace TripleDetection.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            LoadStats();
        }

        private void LoadStats()
        {
            // TODO: 从服务加载实际数据
            txtTodayOk.Text = "156";
            txtTodayNg.Text = "3";
            txtTotalTasks.Text = "42";
            txtPending.Text = "8";
        }
    }
}
```

- [ ] **Step 3: 创建 LogsView.xaml**

```xml
<UserControl x:Class="TripleDetection.Views.LogsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Background="#404040">

    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 标题 -->
        <TextBlock Grid.Row="0" Text="操作日志" FontSize="24" FontWeight="Bold" Foreground="White" Margin="0,0,0,20"/>

        <!-- 搜索筛选 -->
        <Border Grid.Row="1" Background="#505050" Padding="12" Margin="0,0,0,16">
            <StackPanel Orientation="Horizontal">
                <TextBox x:Name="txtSearch" Width="200" Margin="0,0,16,0"
                         Text="" Style="{StaticResource {x:Type TextBox}}"/>
                <ComboBox x:Name="cmbUser" Width="120" Margin="0,0,16,0"/>
                <ComboBox x:Name="cmbAction" Width="120" Margin="0,0,16,0"/>
                <Button Content="搜索" Style="{StaticResource PrimaryButtonStyle}" Margin="0,0,8,0"/>
                <Button Content="清除" Style="{StaticResource PrimaryButtonStyle}"/>
            </StackPanel>
        </Border>

        <!-- 日志列表 -->
        <Border Grid.Row="2" Background="#505050" Padding="16">
            <DataGrid x:Name="dgLogs" AutoGenerateColumns="False" Background="Transparent"
                      Foreground="White" BorderThickness="0" IsReadOnly="True">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="时间" Binding="{Binding CreateAt}" Width="150"/>
                    <DataGridTextColumn Header="用户" Binding="{Binding UserName}" Width="100"/>
                    <DataGridTextColumn Header="操作" Binding="{Binding Action}" Width="120"/>
                    <DataGridTextColumn Header="详情" Binding="{Binding Details}" Width="*"/>
                </DataGrid.Columns>
            </DataGrid>
        </Border>

        <!-- 分页 -->
        <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,16,0,0">
            <Button Content="首页" Style="{StaticResource PrimaryButtonStyle}" Margin="0,0,4,0"/>
            <Button Content="上一页" Style="{StaticResource PrimaryButtonStyle}" Margin="0,0,4,0"/>
            <TextBlock Text="1 / 10" Foreground="White" VerticalAlignment="Center" Margin="16,0"/>
            <Button Content="下一页" Style="{StaticResource PrimaryButtonStyle}" Margin="0,0,4,0"/>
            <Button Content="末页" Style="{StaticResource PrimaryButtonStyle}"/>
        </StackPanel>
    </Grid>
</UserControl>
```

- [ ] **Step 4: 创建 LogsView.xaml.cs**

```csharp
using System.Windows.Controls;

namespace TripleDetection.Views
{
    public partial class LogsView : UserControl
    {
        public LogsView()
        {
            InitializeComponent();
            LoadLogs();
        }

        private void LoadLogs()
        {
            // TODO: 从服务加载实际数据
        }
    }
}
```

- [ ] **Step 5: 创建 SettingsView.xaml**

```xml
<UserControl x:Class="TripleDetection.Views.SettingsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Background="#404040">

    <Grid Margin="20">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="200"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <!-- 左侧分类导航 -->
        <Border Grid.Column="0" Background="#505050" Margin="0,0,16,0">
            <StackPanel>
                <Button Content="VM设置" Style="{StaticResource NavButtonActiveStyle}" Tag="VM"/>
                <Button Content="相机" Style="{StaticResource NavButtonStyle}" Tag="Camera"/>
                <Button Content="PLC" Style="{StaticResource NavButtonStyle}" Tag="PLC"/>
                <Button Content="图像存储" Style="{StaticResource NavButtonStyle}" Tag="Storage"/>
                <Button Content="用户管理" Style="{StaticResource NavButtonStyle}" Tag="Users"/>
            </StackPanel>
        </Border>

        <!-- 右侧设置表单 -->
        <Border Grid.Column="1" Background="#505050" Padding="20">
            <StackPanel>
                <TextBlock Text="VM设置" FontSize="18" FontWeight="Bold" Foreground="White" Margin="0,0,0,20"/>

                <TextBlock Text="安装路径:" Foreground="#A0AEC0" Margin="0,0,0,4"/>
                <Grid Margin="0,0,0,16">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBox Grid.Column="0" x:Name="txtVmPath"/>
                    <Button Grid.Column="1" Content="浏览" Style="{StaticResource PrimaryButtonStyle}" Margin="8,0,0,0"/>
                </Grid>

                <TextBlock Text="方案目录:" Foreground="#A0AEC0" Margin="0,0,0,4"/>
                <Grid Margin="0,0,0,16">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBox Grid.Column="0" x:Name="txtSolPath"/>
                    <Button Grid.Column="1" Content="浏览" Style="{StaticResource PrimaryButtonStyle}" Margin="8,0,0,0"/>
                </Grid>

                <Button Content="保存设置" Style="{StaticResource PrimaryButtonStyle}" HorizontalAlignment="Left"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 6: 创建 SettingsView.xaml.cs**

```csharp
using System.Windows.Controls;

namespace TripleDetection.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 7: 提交**

```bash
git add TripleDetection.App/Views/DashboardView.xaml TripleDetection.App/Views/DashboardView.xaml.cs
git add TripleDetection.App/Views/LogsView.xaml TripleDetection.App/Views/LogsView.xaml.cs
git add TripleDetection.App/Views/SettingsView.xaml TripleDetection.App/Views/SettingsView.xaml.cs
git commit -m "feat: create DashboardView, LogsView, and SettingsView"
```

---

### Task 6: 更新 App.config 配置

**Files:**
- Modify: `TripleDetection.App/App.config`

- [ ] **Step 1: 添加新配置项**

在 `<appSettings>` 中添加：
```xml
<add key="SystemLogoPath" value="Resources/logo.png"/>
<add key="SystemName" value="Triple Detection"/>
<add key="NavRailExpanded" value="true"/>
```

- [ ] **Step 2: 提交**

```bash
git add TripleDetection.App/App.config
git commit -m "feat: add SystemLogoPath, SystemName, NavRailExpanded config"
```

---

### Task 7: 更新 csproj 引用新文件

**Files:**
- Modify: `TripleDetection.App/TripleDetection.App.csproj`

- [ ] **Step 1: 添加新文件的引用**

在 `<ItemGroup>` 中添加：
```xml
<Page Include="Views\DetectionView.xaml">
  <Generator>MSBuild:Compile</Generator>
  <SubType>Designer</SubType>
</Page>
<Compile Include="Views\DetectionView.xaml.cs">
  <DependentUpon>DetectionView.xaml</DependentUpon>
</Compile>
<Page Include="Views\DashboardView.xaml">
  <Generator>MSBuild:Compile</Generator>
  <SubType>Designer</SubType>
</Page>
<Compile Include="Views\DashboardView.xaml.cs">
  <DependentUpon>DashboardView.xaml</DependentUpon>
</Compile>
<Page Include="Views\LogsView.xaml">
  <Generator>MSBuild:Compile</Generator>
  <SubType>Designer</SubType>
</Page>
<Compile Include="Views\LogsView.xaml.cs">
  <DependentUpon>LogsView.xaml</DependentUpon>
</Compile>
<Page Include="Views\SettingsView.xaml">
  <Generator>MSBuild:Compile</Generator>
  <SubType>Designer</SubType>
</Page>
<Compile Include="Views\SettingsView.xaml.cs">
  <DependentUpon>SettingsView.xaml</DependentUpon>
</Compile>
<Resource Include="Resources\Styles.xaml"/>
```

- [ ] **Step 2: 提交**

```bash
git add TripleDetection.App/TripleDetection.App.csproj
git commit -m "feat: add new view files to csproj"
```

---

### Task 8: 构建验证

- [ ] **Step 1: 运行 MSBuild**

```bash
cmd //c "D:\xcm\Triple-Detection\build.bat" 2>&1
```

- [ ] **Step 2: 验证构建成功**

预期：Build succeeded, 0 Error(s)

---

## 验证清单

- [ ] 构建成功无错误
- [ ] 导航栏可折叠/展开
- [ ] 点击导航项切换页面
- [ ] 标题栏显示 Logo、系统名称、用户信息
- [ ] 检测页面布局正确：左侧图像 + 右侧任务信息/方案操作/检测结果 + 底部日志
- [ ] 日志页面显示操作日志列表
- [ ] 仪表盘显示统计数据

---

## 状态

- [ ] Task 1: 创建共享样式资源
- [ ] Task 2: 更新 MainWindow.xaml 布局
- [ ] Task 3: 更新 MainWindow.xaml.cs 导航逻辑
- [ ] Task 4: 创建 DetectionView 检测页面
- [ ] Task 5: 创建 DashboardView, LogsView, SettingsView
- [ ] Task 6: 更新 App.config 配置
- [ ] Task 7: 更新 csproj 引用新文件
- [ ] Task 8: 构建验证