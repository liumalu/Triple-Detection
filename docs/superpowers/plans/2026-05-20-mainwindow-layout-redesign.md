# MainWindow Layout Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign WPF MainWindow to match OCRDemoCs WinForms three-column layout — center VM render area, right sidebar (solution ops / process ops / log), bottom result bar with large OK/NG label.

**Architecture:** Grid-based layout with right-side panel for controls/logs and bottom dock for results. WindowsFormsHost hosts VmRenderControl. Dark theme (#404040 background, white text, flat buttons).

**Tech Stack:** WPF (.NET 4.8), WindowsFormsHost, VisionMaster SDK (VM.PlatformSDKCS, VM.Core), MVVM pattern.

---

## File Structure

| File | Responsibility |
|------|----------------|
| `MainWindow.xaml` | Complete layout rewrite — Grid three-column: center content area, right sidebar, bottom result bar |
| `MainWindow.xaml.cs` | Event handlers for all buttons, VM callbacks, log updates, language toggle |
| `Services/VmIntegrationService.cs` | VM lifecycle, result parsing — add log callback support |
| `Services/LoggingService.cs` | **Create new** — centralized log management, file logging, in-memory buffer |
| `Services/ImageStorageService.cs` | Unchanged — already functional |
| `ViewModels/MainViewModel.cs` | Update to support log list, result list, OK/NG label binding |
| `Models/DetectionResult.cs` | Unchanged |

---

## Task 1: Create LoggingService

**Files:**
- Create: `TripleDetection.App/Services/LoggingService.cs`

- [ ] **Step 1: Write LoggingService.cs**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TripleDetection.Services
{
    public class LoggingService
    {
        private readonly string _logPath;
        private readonly object _lock = new object();
        private const int MaxLogItems = 10000;

        public event EventHandler<LogEntry> OnLogAdded;

        public LoggingService(string logPath)
        {
            _logPath = logPath;
        }

        public void Log(string message)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = message
            };

            OnLogAdded?.Invoke(this, entry);

            Task.Run(() => SaveLog(entry));
        }

        private void SaveLog(LogEntry entry)
        {
            try
            {
                if (!Directory.Exists(_logPath))
                    Directory.CreateDirectory(_logPath);

                string filename = Path.Combine(_logPath, DateTime.Now.ToString("yyyy-MM-dd") + ".log");
                string line = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss:ffff}\t{entry.Message}";

                lock (_lock)
                {
                    File.AppendAllText(filename, line + Environment.NewLine);
                }
            }
            catch
            {
                // Suppress logging errors
            }
        }

        public void Clear()
        {
            OnLogAdded = null;
        }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add TripleDetection.App/Services/LoggingService.cs
git commit -m "feat: add LoggingService for centralized log management"
```

---

## Task 2: Update MainViewModel

**Files:**
- Modify: `TripleDetection.App/ViewModels/MainViewModel.cs`

- [ ] **Step 1: Update MainViewModel with observable collections and properties**

Current file content:
```csharp
using System;

namespace TripleDetection.ViewModels
{
    public class MainViewModel
    {
        public string ResultText { get; set; }
        public string Details { get; set; }
    }
}
```

Replace with:

```csharp
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TripleDetection.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _resultText = "--";
        private string _resultBackground = "#808080";
        private string _details = "Detection details will appear here";
        private bool _isImageViewActive = true;
        private string _selectedProcedure = "";

        public ObservableCollection<string> LogMessages { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> ResultHistory { get; } = new ObservableCollection<string>();

        public string ResultText
        {
            get => _resultText;
            set { _resultText = value; OnPropertyChanged(); }
        }

        public string ResultBackground
        {
            get => _resultBackground;
            set { _resultBackground = value; OnPropertyChanged(); }
        }

        public string Details
        {
            get => _details;
            set { _details = value; OnPropertyChanged(); }
        }

        public bool IsImageViewActive
        {
            get => _isImageViewActive;
            set { _isImageViewActive = value; OnPropertyChanged(); }
        }

        public string SelectedProcedure
        {
            get => _selectedProcedure;
            set { _selectedProcedure = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void AddLog(string message)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            if (LogMessages.Count > 1000)
                LogMessages.RemoveAt(0);
            LogMessages.Add(entry);
        }

        public void AddResult(string result)
        {
            if (ResultHistory.Count > 500)
                ResultHistory.RemoveAt(0);
            ResultHistory.Add(result);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add TripleDetection.App/ViewModels/MainViewModel.cs
git commit -m "feat: update MainViewModel with ObservableCollection for logs and results"
```

---

## Task 3: Rewrite MainWindow.xaml

**Files:**
- Modify: `TripleDetection.App/MainWindow.xaml`

- [ ] **Step 1: Write the complete new MainWindow.xaml layout**

Replace entire content with:

```xaml
<Window x:Class="TripleDetection.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:wfi="clr-namespace:System.Windows.Forms.Integration;assembly=WindowsFormsIntegration"
        xmlns:local="clr-namespace:TripleDetection"
        Title="Triple Detection - Phase 1 MVP"
        Height="750" Width="1284"
        WindowStartupLocation="CenterScreen"
        Background="#404040"
        Loaded="Window_Loaded"
        Closing="Window_Closing">

    <Window.Resources>
        <Style TargetType="Button">
            <Setter Property="Background" Value="#808080"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Padding" Value="10,5"/>
            <Setter Property="Margin" Value="3"/>
            <Setter Property="MinWidth" Value="100"/>
            <Setter Property="MinHeight" Value="35"/>
            <Setter Property="FontSize" Value="12"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border Background="{TemplateBinding Background}"
                                BorderBrush="{TemplateBinding BorderBrush}"
                                BorderThickness="{TemplateBinding BorderThickness}"
                                CornerRadius="0">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
            <Style.Triggers>
                <Trigger Property="IsEnabled" Value="False">
                    <Setter Property="Background" Value="#606060"/>
                    <Setter Property="Foreground" Value="#A0A0A0"/>
                </Trigger>
            </Style.Triggers>
        </Style>

        <Style TargetType="GroupBox">
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="BorderBrush" Value="#606060"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Padding" Value="5"/>
            <Setter Property="Margin" Value="5"/>
        </Style>

        <Style TargetType="ListBox">
            <Setter Property="Background" Value="#404040"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="BorderThickness" Value="0"/>
        </Style>

        <Style TargetType="ListBoxItem">
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="Background" Value="Transparent"/>
        </Style>

        <Style TargetType="ComboBox">
            <Setter Property="Background" Value="White"/>
            <Setter Property="Foreground" Value="Black"/>
            <Setter Property="MinWidth" Value="100"/>
        </Style>

        <Style TargetType="TextBox">
            <Setter Property="Background" Value="White"/>
            <Setter Property="Foreground" Value="Black"/>
            <Setter Property="Padding" Value="3"/>
        </Style>
    </Window.Resources>

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="380"/>
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Top-right toolbar -->
        <StackPanel Grid.Column="1" Grid.Row="0" Orientation="Horizontal"
                    HorizontalAlignment="Right" Margin="0,5,5,0">
            <Button x:Name="btnRender" Content="图像显示" Click="BtnRender_Click"/>
            <Button x:Name="btnConfig" Content="参数配置" Click="BtnConfig_Click"/>
            <Button x:Name="btnLang" Content="中文/English" Click="BtnLang_Click"/>
        </StackPanel>

        <!-- Center content area (VmRenderControl or MainViewControl) -->
        <Border Grid.Column="0" Grid.Row="1" Margin="5" BorderBrush="#606060" BorderThickness="1">
            <WindowsFormsHost x:Name="VmHost"/>
        </Border>

        <!-- Right sidebar -->
        <Grid Grid.Column="1" Grid.Row="1" Margin="0,5,5,5">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <!-- 方案操作 GroupBox -->
            <GroupBox Grid.Row="0" Header="方案操作">
                <StackPanel>
                    <Button x:Name="btnSelectSolu" Content="选择方案" Click="BtnSelectSolu_Click"/>
                    <Button x:Name="btnLoadSolu" Content="加载方案" Click="BtnLoadSolu_Click"/>
                    <Button x:Name="btnSaveSolu" Content="保存方案" Click="BtnSaveSolu_Click"/>
                </StackPanel>
            </GroupBox>

            <!-- 流程操作 GroupBox -->
            <GroupBox Grid.Row="1" Header="流程操作">
                <StackPanel>
                    <StackPanel Orientation="Horizontal" Margin="0,0,0,5">
                        <TextBlock Text="选择流程:" Foreground="White" VerticalAlignment="Center" Margin="0,0,5,0"/>
                        <ComboBox x:Name="comboProcedure" Width="120" SelectionChanged="ComboProcedure_SelectionChanged"/>
                    </StackPanel>
                    <Button x:Name="btnRunOnce" Content="单次运行" Click="BtnRunOnce_Click"/>
                    <Button x:Name="btnContiRun" Content="连续运行" Click="BtnContiRun_Click"/>
                </StackPanel>
            </GroupBox>

            <!-- 日志消息 GroupBox -->
            <GroupBox Grid.Row="2" Header="日志消息">
                <ListBox x:Name="listViewLog"
                         ItemsSource="{Binding LogMessages}"
                         FontFamily="Consolas"
                         FontSize="11"
                         BorderThickness="0">
                    <ListBox.ItemTemplate>
                        <DataTemplate>
                            <TextBlock Text="{Binding}" Foreground="White" Margin="0,1"/>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>
            </GroupBox>
        </Grid>

        <!-- Bottom result area -->
        <Grid Grid.Column="0" Grid.Row="2" Grid.ColumnSpan="2" Margin="5">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <GroupBox Grid.Column="0" Header="结果">
                <ListBox x:Name="listBoxResult"
                         ItemsSource="{Binding ResultHistory}"
                         FontFamily="Consolas"
                         FontSize="11"
                         BorderThickness="0"/>
            </GroupBox>

            <Border Grid.Column="1" Width="120" Height="100" Margin="10,5"
                    Background="{Binding ResultBackground}" CornerRadius="0">
                <TextBlock x:Name="txtResult"
                           Text="{Binding ResultText}"
                           FontSize="48"
                           FontWeight="Bold"
                           Foreground="White"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center"/>
            </Border>
        </Grid>
    </Grid>
</Window>
```

- [ ] **Step 2: Commit**

```bash
git add TripleDetection.App/MainWindow.xaml
git commit -m "feat: rewrite MainWindow.xaml to match OCRDemoCs three-column layout"
```

---

## Task 4: Rewrite MainWindow.xaml.cs

**Files:**
- Modify: `TripleDetection.App/MainWindow.xaml.cs`

- [ ] **Step 1: Write complete MainWindow.xaml.cs with all event handlers matching Demo**

Replace entire content with:

```csharp
using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using VM.Core;
using VM.PlatformSDKCS;
using TripleDetection.Services;
using TripleDetection.ViewModels;
using MessageBox = System.Windows.MessageBox;

namespace TripleDetection
{
    public partial class MainWindow : Window
    {
        private VmIntegrationService _vmService;
        private ImageStorageService _imageStorage;
        private LoggingService _logService;
        private MainViewModel _viewModel;

        private string _solPath;
        private readonly string _okDir;
        private readonly string _ngDir;
        private string _selectedSolPath;
        private bool _isSolutionLoad = false;
        private bool _isContinuRun = false;
        private VmProcedure _procedure;

        public MainWindow()
        {
            InitializeComponent();

            _okDir = ConfigurationManager.AppSettings["OkImageDir"];
            _ngDir = ConfigurationManager.AppSettings["NgImageDir"];
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "Message");

            _imageStorage = new ImageStorageService(_okDir, _ngDir);
            _logService = new LoggingService(logPath);
            _vmService = new VmIntegrationService(_imageStorage);

            _viewModel = new MainViewModel();
            _logService.OnLogAdded += (s, e) =>
            {
                Dispatcher.Invoke(() => _viewModel.AddLog(e.Message));
            };

            this.DataContext = _viewModel;

            VmSolution.OnWorkStatusEvent += VmSolution_OnWorkStatusEvent;
            VmSolution.OnProcessStatusStartEvent += VmSolution_OnProcessStatusStartEvent;
            VmSolution.OnProcessStatusStopEvent += VmSolution_OnProcessStatusStopEvent;

            btnRender.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 140, 0));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ShowRenderControl();
            _logService.Log("Application started");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isSolutionLoad && _procedure != null)
            {
                var result = MessageBox.Show("Save solution or not?", "Information",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _procedure.ContinuousRunEnable = false;
                    VmSolution.Save();
                }
            }
        }

        private void ShowRenderControl()
        {
            VmHost.Child = new VMControls.Winform.Release.VmRenderControl();
            if (_procedure != null)
            {
                ((VMControls.Winform.Release.VmRenderControl)VmHost.Child).ModuleSource = _procedure;
            }
            btnRender.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 140, 0));
            btnConfig.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(128, 128, 128));
            _viewModel.IsImageViewActive = true;
        }

        private void ShowMainViewControl()
        {
            VmHost.Child = null;
            btnConfig.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 140, 0));
            btnRender.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(128, 128, 128));
            _viewModel.IsImageViewActive = false;
            _logService.Log("Switched to parameter configuration view");
        }

        private void BtnSelectSolu_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "VM Sol File|*.sol*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _selectedSolPath = dialog.FileName;
                _isSolutionLoad = false;
                _logService.Log("Selected solution: " + _selectedSolPath);
                MessageBox.Show("Solution path: " + _selectedSolPath + "\nNext click Load button!",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnLoadSolu_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedSolPath))
            {
                MessageBox.Show("Please select a solution file first!", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetControlsEnabled(false);

            try
            {
                if (_isSolutionLoad)
                {
                    _isSolutionLoad = false;
                }

                VmSolution.Load(_selectedSolPath);
                _isSolutionLoad = true;

                _logService.Log("Loading solution succeeded!");
                MessageBox.Show("Loading Solution succeeded!", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                comboProcedure.Items.Clear();
                ProcessInfoList processList = VmSolution.Instance.GetAllProcedureList();
                for (int i = 0; i < processList.nNum; i++)
                {
                    comboProcedure.Items.Add(processInfoList.astProcessInfo[i].strProcessName);
                }

                if (comboProcedure.Items.Count > 0)
                {
                    comboProcedure.SelectedIndex = 0;
                    _procedure = VmSolution.Instance[processInfoList.astProcessInfo[0].strProcessName] as VmProcedure;

                    if (_procedure == null)
                    {
                        _logService.Log("Procedure is null, check the solution!");
                        return;
                    }

                    var vmRender = VmHost.Child as VMControls.Winform.Release.VmRenderControl;
                    if (vmRender != null)
                        vmRender.ModuleSource = _procedure;
                }
                else
                {
                    _logService.Log("Number of flows is 0, check the solution!");
                }
            }
            catch (VmException ex)
            {
                _logService.Log($"Failed to load solution, Error code: 0x{ex.errorCode:X}");
                MessageBox.Show($"Failed to load solution: 0x{ex.errorCode:X}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                _logService.Log($"Failed to load solution: {ex.Message}");
                MessageBox.Show($"Failed to load solution: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        private void BtnSaveSolu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                VmSolution.Save();
                _logService.Log("Succeeded to save solution!");
            }
            catch (VmException ex)
            {
                _logService.Log($"Failed to save solution, Error code: 0x{ex.errorCode:X}");
            }
            catch (Exception ex)
            {
                _logService.Log($"Failed to save solution: {ex.Message}");
            }
        }

        private void ComboProcedure_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (comboProcedure.SelectedItem == null) return;

            try
            {
                _procedure = VmSolution.Instance[comboProcedure.SelectedItem.ToString()] as VmProcedure;
                var vmRender = VmHost.Child as VMControls.Winform.Release.VmRenderControl;
                if (vmRender != null)
                    vmRender.ModuleSource = _procedure;

                _logService.Log($"Selected [{comboProcedure.SelectedItem}] succeeded!");
            }
            catch (VmException ex)
            {
                _logService.Log($"Failed to select procedure, Error code: 0x{ex.errorCode:X}");
            }
            catch (Exception ex)
            {
                _logService.Log($"Failed to select procedure: {ex.Message}");
            }
        }

        private void BtnRunOnce_Click(object sender, RoutedEventArgs e)
        {
            if (!_isSolutionLoad || _procedure == null)
            {
                _logService.Log("The procedure does not exist!");
                return;
            }

            try
            {
                _procedure.Run();
                _logService.Log("Run once triggered");
            }
            catch (VmException ex)
            {
                _logService.Log($"Failed to run once, Error code: 0x{ex.errorCode:X}");
            }
            catch (Exception ex)
            {
                _logService.Log($"Failed to run once: {ex.Message}");
            }
        }

        private void BtnContiRun_Click(object sender, RoutedEventArgs e)
        {
            if (!_isSolutionLoad || _procedure == null)
            {
                _logService.Log("The procedure does not exist!");
                return;
            }

            try
            {
                _procedure.ContinuousRunEnable = _procedure.ContinuousRunEnable ^ true;
                _isContinuRun = _isContinuRun ^ true;
            }
            catch (VmException ex)
            {
                _logService.Log($"Failed to run continuous, Error code: 0x{ex.errorCode:X}");
            }
            catch (Exception ex)
            {
                _logService.Log($"Failed to run continuous: {ex.Message}");
            }
        }

        private void BtnRender_Click(object sender, RoutedEventArgs e)
        {
            ShowRenderControl();
        }

        private void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            ShowMainViewControl();
        }

        private void BtnLang_Click(object sender, RoutedEventArgs e)
        {
            if (System.Threading.Thread.CurrentThread.CurrentUICulture.Name == "zh-CN")
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
            }
            else
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
            }
            _logService.Log("Language switched");
        }

        private void VmSolution_OnWorkStatusEvent(ImvsSdkDefine.IMVS_MODULE_WORK_STAUS workStatusInfo)
        {
            if (workStatusInfo.nWorkStatus == 0 && workStatusInfo.nProcessID == 10000)
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var ioNameInfos = _procedure.ModuResult.GetAllOutputNameInfo();
                        if (ioNameInfos.Count != 0 && ioNameInfos[0].TypeName == IMVS_MODULE_BASE_DATA_TYPE.IMVS_GRAP_TYPE_STRING)
                        {
                            string strResult = _procedure.ModuResult.GetOutputString(ioNameInfos[0].Name).astStringVal[0].strValue;
                            if (strResult != null)
                            {
                                UpdateResult(strResult);
                                _logService.Log($"Process running time: {_procedure.ProcessTime}ms");
                            }
                        }
                    }
                    catch (VmException ex)
                    {
                        _logService.Log($"Failed to get results, Error code: 0x{ex.errorCode:X}");
                    }
                    catch (Exception ex)
                    {
                        _logService.Log($"Failed to get results: {ex.Message}");
                    }
                });
            }
        }

        private void VmSolution_OnProcessStatusStartEvent(ImvsSdkDefine.IMVS_STATUS_PROCESS_START_CONTINUOUSLY_INFO statusInfo)
        {
            if (statusInfo.nStatus == 0)
            {
                Dispatcher.Invoke(() =>
                {
                    _isContinuRun = true;
                    btnContiRun.Content = "停止连续";
                    SetControlsEnabled(false);
                    _logService.Log("Start continuous run!");
                });
            }
        }

        private void VmSolution_OnProcessStatusStopEvent(ImvsSdkDefine.IMVS_STATUS_PROCESS_STOP_INFO statusInfo)
        {
            if (statusInfo.nStopAction == 1)
            {
                Dispatcher.Invoke(() =>
                {
                    _isContinuRun = false;
                    btnContiRun.Content = "连续运行";
                    SetControlsEnabled(true);
                    _logService.Log("End Run!");
                });
            }
        }

        private void UpdateResult(string strResult)
        {
            var vs = strResult.Split(';');
            if (vs[0] == "1")
            {
                _viewModel.ResultText = "OK";
                _viewModel.ResultBackground = "#00C000";
            }
            else
            {
                _viewModel.ResultText = "NG";
                _viewModel.ResultBackground = "#FF0000";
            }

            string result = $"Results: CodeInfo: {vs[2]}; Number of characters: {vs[1]}; Confidence: {vs[3]}";
            _viewModel.AddResult(result);
            _logService.Log(result);
        }

        private void SetControlsEnabled(bool enabled)
        {
            btnSelectSolu.IsEnabled = enabled;
            btnLoadSolu.IsEnabled = enabled;
            btnSaveSolu.IsEnabled = enabled;
            btnRunOnce.IsEnabled = enabled;
            btnContiRun.IsEnabled = enabled;
            comboProcedure.IsEnabled = enabled;
            btnRender.IsEnabled = enabled;
            btnConfig.IsEnabled = enabled;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add TripleDetection.App/MainWindow.xaml.cs
git commit -m "feat: rewrite MainWindow.xaml.cs with complete Demo functionality"
```

---

## Task 5: Update VmIntegrationService

**Files:**
- Modify: `TripleDetection.App/Services/VmIntegrationService.cs`

- [ ] **Step 1: Add procedure list method**

Add after `GetProcedure()` method:

```csharp
public List<string> GetAllProcedureNames()
{
    var names = new List<string>();
    if (_isSolutionLoad)
    {
        var processList = VmSolution.Instance.GetAllProcedureList();
        for (int i = 0; i < processList.nNum; i++)
        {
            names.Add(processList.astProcessInfo[i].strProcessName);
        }
    }
    return names;
}
```

- [ ] **Step 2: Commit**

```bash
git add TripleDetection.App/Services/VmIntegrationService.cs
git commit -m "feat: add GetAllProcedureNames to VmIntegrationService"
```

---

## Task 6: Verify and Build

**Files:**
- No file changes — verification only

- [ ] **Step 1: Build the project**

Run: `cd TripleDetection.App && dotnet build`

Expected: Build succeeds with no errors

- [ ] **Step 2: Fix any compilation errors**

If errors occur, fix them inline and rebuild.

- [ ] **Step 3: Run and verify**

Start the application and verify:
1. Load button loads .sol file
2. Procedure appears in dropdown
3. Run once executes detection
4. OK/NG result displays
5. Log messages appear in log panel

---

## Verification Checklist

- [ ] Layout matches Demo three-column structure
- [ ] All buttons function (Select/Load/Save Solution, Run Once/Continuous, Render/Config)
- [ ] OK/NG label updates correctly with green/red background
- [ ] Log panel shows timestamped messages
- [ ] Result history list updates
- [ ] Procedure dropdown populates after loading solution
- [ ] Dark theme colors applied (#404040 background, white text)
- [ ] Application starts and communicates with VisionMaster successfully
