# Phase 1 MVP: VisionMaster 与 WPF应用集成验证

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 验证 VisionMaster 算法平台与 WPF 操作管理应用的集成链路（VM加载、结果接收、图片保存）

**Architecture:** 同进程内紧密集成，VisionMaster SDK (VM.PlatformSDKCS) 直接嵌入 WPF 应用，通过事件回调获取 OCR 检测结果，图片根据 OK/NG 结果分流存储。

**Tech Stack:** WPF (.NET Framework 4.8), VisionMaster SDK (VM.PlatformSDKCS, VM.Core, VMControls.*)

---

## File Structure

```
Triple-Detection/
├── TripleDetection.App/              # WPF 主应用
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── App.config                    # 配置文件（.sol路径、图片目录）
│   ├── MainWindow.xaml               # 主界面
│   ├── MainWindow.xaml.cs
│   ├── ViewModels/
│   │   └── MainViewModel.cs          # 主界面 ViewModel
│   ├── Services/
│   │   ├── VmIntegrationService.cs   # VM 加载、运行、回调管理
│   │   └── ImageStorageService.cs     # 图片保存逻辑（OK/NG分流）
│   └── Models/
│       └── DetectionResult.cs        # 检测结果模型
├── docs/
│   └── superpowers/
│       └── plans/
│           └── 2026-05-19-phase1-mvp-plan.md
└── README.md
```

---

## Task 1: 项目结构搭建

**Files:**
- Create: `Triple-Detection/TripleDetection.App/TripleDetection.App.csproj`
- Create: `Triple-Detection/TripleDetection.App/App.xaml`
- Create: `Triple-Detection/TripleDetection.App/App.xaml.cs`
- Create: `Triple-Detection/TripleDetection.App/App.config`
- Create: `Triple-Detection/TripleDetection.App/MainWindow.xaml`
- Create: `Triple-Detection/TripleDetection.App/MainWindow.xaml.cs`
- Create: `Triple-Detection/TripleDetection.App/ViewModels/MainViewModel.cs`
- Create: `Triple-Detection/TripleDetection.App/Services/VmIntegrationService.cs`
- Create: `Triple-Detection/TripleDetection.App/Services/ImageStorageService.cs`
- Create: `Triple-Detection/TripleDetection.App/Models/DetectionResult.cs`

- [ ] **Step 1: 创建项目文件**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net48</TargetFramework>
    <UseWPF>true</UseWPF>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="VM.Core">
      <Private>false</Private>
    </Reference>
    <Reference Include="VM.PlatformSDKCS">
      <Private>false</Private>
    </Reference>
    <Reference Include="VMControls.*">
      <Private>false</Private>
    </Reference>
    <Reference Include="PresentationFramework" />
    <Reference Include="PresentationCore" />
    <Reference Include="WindowsBase" />
    <Reference Include="System.Xaml" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: 创建 App.xaml**

```xml
<Application x:Class="TripleDetection.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
</Application>
```

- [ ] **Step 3: 创建 App.xaml.cs**

```csharp
using System.Windows;

namespace TripleDetection.App
{
    public partial class App : Application
    {
    }
}
```

- [ ] **Step 4: 创建 App.config**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="SolFilePath" value="C:\VisionMaster\Solutions\OCRDemo.sol" />
    <add key="OkImageDir" value="D:\Images\OK" />
    <add key="NgImageDir" value="D:\Images\NG" />
  </appSettings>
</configuration>
```

- [ ] **Step 5: 创建 MainWindow.xaml（基础布局）**

```xml
<Window x:Class="TripleDetection.App.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Triple Detection - Phase 1 MVP"
        Height="600" Width="900"
        WindowStartupLocation="CenterScreen">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 配置区域 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Background="#E0E0E0" Margin="5">
            <TextBlock Text="Solution:" VerticalAlignment="Center" Margin="5"/>
            <TextBox x:Name="txtSolPath" Width="400" Margin="5"/>
            <Button x:Name="btnBrowse" Content="Browse" Width="80" Margin="5" Click="BtnBrowse_Click"/>
            <Button x:Name="btnLoadSolu" Content="Load" Width="80" Margin="5" Click="BtnLoadSolu_Click"/>
            <Button x:Name="btnRun" Content="Run" Width="80" Margin="5" Click="BtnRun_Click"/>
            <Button x:Name="btnStop" Content="Stop" Width="80" Margin="5" Click="BtnStop_Click"/>
        </StackPanel>

        <!-- VM 显示区域 -->
        <Border Grid.Row="1" BorderBrush="Black" BorderThickness="1" Margin="5">
            <Grid x:Name="VmContainer">
                <!-- vmRenderControl 将在这里动态添加 -->
            </Grid>
        </Border>

        <!-- 结果显示区域 -->
        <Grid Grid.Row="2" Background="#F5F5F5" Margin="5">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                <TextBlock Text="Result:" FontWeight="Bold" VerticalAlignment="Center" Margin="10,5"/>
                <Border x:Name="borderResult" Width="80" Height="40" Background="Gray" CornerRadius="4" Margin="5">
                    <TextBlock x:Name="txtResult" Text="--" Foreground="White" FontSize="18"
                               HorizontalAlignment="Center" VerticalAlignment="Center"/>
                </Border>
            </StackPanel>
            <TextBlock Grid.Column="1" x:Name="txtDetails" Text="Detection details will appear here"
                       VerticalAlignment="Center" Margin="10,5" TextWrapping="Wrap"/>
        </Grid>
    </Grid>
</Window>
```

- [ ] **Step 6: 创建 DetectionResult.cs 模型**

```csharp
namespace TripleDetection.App.Models
{
    public class DetectionResult
    {
        public bool IsOK { get; set; }
        public string CodeInfo { get; set; }
        public int CharCount { get; set; }
        public double Confidence { get; set; }
        public string ImagePath { get; set; }
        public System.DateTime DetectionTime { get; set; }
    }
}
```

- [ ] **Step 7: 创建 MainViewModel.cs（简化版，TODO后续完善）**

```csharp
using System.ComponentModel;

namespace TripleDetection.App.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _resultText = "--";
        public string ResultText
        {
            get => _resultText;
            set { _resultText = value; OnPropertyChanged(nameof(ResultText)); }
        }

        private string _details = "Detection details will appear here";
        public string Details
        {
            get => _details;
            set { _details = value; OnPropertyChanged(nameof(Details)); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
```

- [ ] **Step 8: 创建 ImageStorageService.cs**

```csharp
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace TripleDetection.App.Services
{
    public class ImageStorageService
    {
        private string _okDir;
        private string _ngDir;

        public ImageStorageService(string okDir, string ngDir)
        {
            _okDir = okDir;
            _ngDir = CreateDirIfNotExists(okDir);
            _ngDir = CreateDirIfNotExists(ngDir);
        }

        private string CreateDirIfNotExists(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        public string SaveImage(Bitmap image, bool isOK)
        {
            string dir = isOK ? _okDir : _ngDir;
            string filename = $"{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
            string fullPath = Path.Combine(dir, filename);

            try
            {
                image.Save(fullPath, ImageFormat.Png);
                return fullPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save image: {ex.Message}");
                return null;
            }
        }
    }
}
```

- [ ] **Step 9: 创建 VmIntegrationService.cs**

```csharp
using System;
using VM.Core;
using VM.PlatformSDKCS;
using System.Drawing;
using TripleDetection.App.Models;

namespace TripleDetection.App.Services
{
    public class VmIntegrationService
    {
        private VmSolution _vmSolution;
        private VmProcedure _procedure;
        private ImageStorageService _imageStorage;
        private bool _isSolutionLoad = false;

        public event EventHandler<DetectionResult> OnDetectionResult;

        public VmIntegrationService(ImageStorageService imageStorage)
        {
            _imageStorage = imageStorage;
            VmSolution.OnWorkStatusEvent += VmSolution_OnWorkStatusEvent;
        }

        public void LoadSolution(string solPath)
        {
            VmSolution.Load(solPath);
            _isSolutionLoad = true;

            ProcessInfoList processList = VmSolution.Instance.GetAllProcedureList();
            if (processList.nNum > 0)
            {
                _procedure = VmSolution.Instance[processList.astProcessInfo[0].strProcessName] as VmProcedure;
            }
        }

        public void RunOnce()
        {
            _procedure?.Run();
        }

        public void SetContinuousRun(bool enable)
        {
            if (_procedure != null)
            {
                _procedure.ContinuousRunEnable = enable;
            }
        }

        public VmProcedure GetProcedure()
        {
            return _procedure;
        }

        private void VmSolution_OnWorkStatusEvent(ImvsSdkDefine.IMVS_MODULE_WORK_STAUS workStatusInfo)
        {
            if (workStatusInfo.nWorkStatus == 1 && workStatusInfo.nProcessID == 10000)
            {
                try
                {
                    var ioNameInfos = _procedure.ModuResult.GetAllOutputNameInfo();
                    if (ioNameInfos.Count > 0 && ioNameInfos[0].TypeName == IMVS_MODULE_BASE_DATA_TYPE.IMVS_GRAP_TYPE_STRING)
                    {
                        string strResult = _procedure.ModuResult.GetOutputString(ioNameInfos[0].Name).astStringVal[0].strValue;
                        var result = ParseResult(strResult);

                        // TODO: 获取图片并保存
                        // var image = GetCurrentImage();
                        // result.ImagePath = _imageStorage.SaveImage(image, result.IsOK);

                        OnDetectionResult?.Invoke(this, result);
                    }
                }
                catch (VmException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"VM Error: 0x{ex.errorCode:X}");
                }
            }
        }

        private DetectionResult ParseResult(string strResult)
        {
            var parts = strResult.Split(';');
            return new DetectionResult
            {
                IsOK = parts[0] == "1",
                CharCount = int.Parse(parts[1]),
                CodeInfo = parts[2],
                Confidence = double.Parse(parts[3]),
                DetectionTime = DateTime.Now
            };
        }
    }
}
```

- [ ] **Step 10: 创建 MainWindow.xaml.cs**

```csharp
using System;
using System.Windows;
using System.Windows.Forms;
using VM.PlatformSDKCS;
using TripleDetection.App.Services;
using TripleDetection.App.ViewModels;

namespace TripleDetection.App
{
    public partial class MainWindow : Window
    {
        private VmIntegrationService _vmService;
        private ImageStorageService _imageStorage;
        private readonly string _solPath;
        private readonly string _okDir;
        private readonly string _ngDir;

        public MainWindow()
        {
            InitializeComponent();

            _solPath = System.Configuration.ConfigurationManager.AppSettings["SolFilePath"];
            _okDir = System.Configuration.ConfigurationManager.AppSettings["OkImageDir"];
            _ngDir = System.Configuration.ConfigurationManager.AppSettings["NgImageDir"];

            txtSolPath.Text = _solPath;

            _imageStorage = new ImageStorageService(_okDir, _ngDir);
            _vmService = new VmIntegrationService(_imageStorage);
            _vmService.OnDetectionResult += VmService_OnDetectionResult;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "VM Sol File|*.sol*";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtSolPath.Text = dialog.FileName;
                }
            }
        }

        private void BtnLoadSolu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _vmService.LoadSolution(txtSolPath.Text);
                System.Windows.MessageBox.Show("Solution loaded successfully!", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (VmException ex)
            {
                System.Windows.MessageBox.Show($"Failed to load solution: 0x{ex.errorCode:X}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            _vmService.SetContinuousRun(true);
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _vmService.SetContinuousRun(false);
        }

        private void VmService_OnDetectionResult(object sender, Models.DetectionResult result)
        {
            Dispatcher.Invoke(() =>
            {
                txtResult.Text = result.IsOK ? "OK" : "NG";
                borderResult.Background = result.IsOK ?
                    System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
                txtDetails.Text = $"Code: {result.CodeInfo}, Chars: {result.CharCount}, Confidence: {result.Confidence:P}";
            });
        }
    }
}
```

- [ ] **Step 11: 添加 WindowsFormsIntegration 引用到 App.xaml.cs (如果需要)

```csharp
// 需要在 MainWindow 中使用 Windows.Forms 控件（如 OpenFileDialog）
// 确保项目引用包含 System.Windows.Forms
```

- [ ] **Step 12: Commit**

```bash
git add Triple-Detection/
git commit -m "feat(phase1): initial project structure for VisionMaster integration"
```

---

## Task 2: VM RenderControl 嵌入

**Files:**
- Modify: `Triple-Detection/TripleDetection.App/MainWindow.xaml.cs`
- Modify: `Triple-Detection/TripleDetection.App/MainWindow.xaml`

- [ ] **Step 1: 在 MainWindow_Loaded 中嵌入 vmRenderControl**

```csharp
private void Window_Loaded(object sender, RoutedEventArgs e)
{
    var procedure = _vmService.GetProcedure();
    if (procedure != null)
    {
        var renderControl = new VMControls.WPF.WinForm_WpfControl();
        renderControl.vmRenderControl1.ModuleSource = procedure;
        VmContainer.Child = renderControl;
    }
}
```

- [ ] **Step 2: 更新 XAML 添加 Loaded 事件**

```xml
<Window x:Class="TripleDetection.App.MainWindow"
        ...
        Loaded="Window_Loaded">
```

- [ ] **Step 3: Commit**

```bash
git add Triple-Detection/
git commit -m "feat(phase1): embed vmRenderControl in MainWindow"
```

---

## Task 3: 图片获取与保存

**Files:**
- Modify: `Triple-Detection/TripleDetection.App/Services/VmIntegrationService.cs`

- [ ] **Step 1: 实现图片获取逻辑**

```csharp
// 在 VmIntegrationService 中添加获取当前图片的方法
// 需要咨询海康SDK文档中如何从 VmProcedure 获取当前帧图片
// 可能需要使用: procedure.ModuResult.GetOutputImage() 或类似接口
```

- [ ] **Step 2: 在回调中保存图片**

```csharp
private void VmSolution_OnWorkStatusEvent(...)
{
    // 获取图片
    var image = GetCurrentImage();
    if (image != null)
    {
        result.ImagePath = _imageStorage.SaveImage(image, result.IsOK);
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Triple-Detection/
git commit -m "feat(phase1): add image capture and storage based on detection result"
```

---

## Task 4: 配置管理（可配置化）

**Files:**
- Modify: `Triple-Detection/TripleDetection.App/App.config`
- Modify: `Triple-Detection/TripleDetection.App/MainWindow.xaml.cs`

- [ ] **Step 1: 添加配置 UI（可选）或保持 App.config 方式**

保持 App.config 作为配置中心，支持后续扩展为独立的配置管理界面。

- [ ] **Step 2: Commit**

```bash
git add Triple-Detection/
git commit -m "feat(phase1): add configuration management via App.config"
```

---

## Task 5: 测试与验证

**Files:**
- Create: `Triple-Detection/TripleDetection.App.Tests/`

- [ ] **Step 1: 创建单元测试项目**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\TripleDetection.App\TripleDetection.App.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.0.0" />
    <PackageReference Include="xunit" Version="2.4.1" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.4.3" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: 编写 ImageStorageService 测试**

```csharp
using Xunit;
using System.IO;
using System.Drawing;

public class ImageStorageServiceTests
{
    [Fact]
    public void SaveImage_SavesToOkDirectory_WhenIsOKTrue()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "test_images");
        var okDir = Path.Combine(tempDir, "OK");
        var ngDir = Path.Combine(tempDir, "NG");
        var service = new ImageStorageService(okDir, ngDir);
        using var bitmap = new Bitmap(100, 100);

        // Act
        var path = service.SaveImage(bitmap, true);

        // Assert
        Assert.NotNull(path);
        Assert.StartsWith(okDir, path);
        Assert.True(File.Exists(path));

        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void SaveImage_SavesToNgDirectory_WhenIsOKFalse()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "test_images");
        var okDir = Path.Combine(tempDir, "OK");
        var ngDir = Path.Combine(tempDir, "NG");
        var service = new ImageStorageService(okDir, ngDir);
        using var bitmap = new Bitmap(100, 100);

        // Act
        var path = service.SaveImage(bitmap, false);

        // Assert
        Assert.NotNull(path);
        Assert.StartsWith(ngDir, path);
        Assert.True(File.Exists(path));

        // Cleanup
        Directory.Delete(tempDir, true);
    }
}
```

- [ ] **Step 3: 编写 DetectionResult 模型测试**

```csharp
using Xunit;
using TripleDetection.App.Models;

public class DetectionResultTests
{
    [Fact]
    public void DetectionResult_DefaultValues_AreCorrect()
    {
        var result = new DetectionResult();

        Assert.False(result.IsOK);
        Assert.Equal(string.Empty, result.CodeInfo);
        Assert.Equal(0, result.CharCount);
        Assert.Equal(0.0, result.Confidence);
        Assert.Null(result.ImagePath);
    }
}
```

- [ ] **Step 4: 运行测试验证**

```bash
cd Triple-Detection
dotnet test
# 预期: 所有测试通过
```

- [ ] **Step 5: Commit**

```bash
git add Triple-Detection/
git commit -m "test(phase1): add unit tests for ImageStorageService and DetectionResult"
```

---

## 验证成功标准

1. ✅ 能加载并运行 .sol 流程
2. ✅ 能获取检测结果（OK/NG、字符、置信度）
3. ✅ 能根据结果保存图片到对应目录（OK/NG分离）
4. ✅ 结果和图片能在UI上正确显示

---

## 注意事项

1. **VisionMaster SDK 引用**: 需要确保 `VM.PlatformSDKCS`, `VM.Core`, `VMControls.*` 等 DLL 可用，这些通常随 VisionMaster 安装提供
2. **.NET Framework 4.8**: 确保开发环境支持 .NET Framework 4.8
3. **图片格式**: 根据实际需求，可能需要调整图片格式（JPG/PNG/BMP）和压缩质量
4. **异常处理**: 实际部署时需要完善异常处理和日志记录

## 参考

- Demo路径: `D:\xcm\ApplicationDemo\OCRDemoCs\`
- 关键类: `VmSolution`, `VmProcedure`, `vmRenderControl`