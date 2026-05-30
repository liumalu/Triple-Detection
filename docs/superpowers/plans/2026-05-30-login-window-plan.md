# Login Window Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a separate LoginWindow shown before MainWindow. User authenticates with username + password (hashed with SHA256+salt, backward compatible with legacy plain text). Configurable app logo.

**Architecture:**
- Separate `LoginWindow` (400×520px, fixed) shown before Bootstrapper → MainWindow
- LoginViewModel with Prism MVVM pattern, binds username/password fields
- PasswordHashService: SHA256 + salt hashing, dual-mode (hash + legacy plain text for migration)
- SessionManager.SetCurrentUser called on auth success
- Close (X) exits app; auth success closes LoginWindow and continues to MainWindow

**Tech Stack:** WPF (.NET Framework 4.8), MVVM (Prism DryIoc), existing Styles.xaml, C# 8.0

---

## File Structure Map

### New Files

| File | Responsibility |
|------|----------------|
| `TripleDetection.Services/PasswordHashService.cs` | SHA256 + salt generation, hash computation, migration check |
| `TripleDetection.App/ViewModels/LoginViewModel.cs` | Login VM with Username/Password/ErrorMessage/IsLoading, LoginCommand |
| `TripleDetection.App/Views/LoginWindow.xaml` | Login UI (logo, inputs, button, error area, shake animation) |
| `TripleDetection.App/Views/LoginWindow.xaml.cs` | Code-behind, PasswordBox show/hide wiring |
| `TripleDetection.App/Converters/ValidationErrorConverter.cs` | Converts error state to red border brush |

### Modified Files

| File | Change |
|------|--------|
| `TripleDetection.Data/Entities/User.cs` | Add `PasswordSalt` (string), `PasswordHash` (string) fields |
| `TripleDetection.Data/Repositories/Configuration/UserConfiguration.cs` | Map `PasswordSalt` and `PasswordHash` columns |
| `TripleDetection.Services/UserService.cs` | Dual-mode auth: try hash first, then plain text (migrate if plain text matched) |
| `TripleDetection.App/Bootstrapper.cs` | Register `LoginViewModel`, make shell creation skippable for login-first flow |
| `TripleDetection.App/App.xaml.cs` | Show LoginWindow first; on success run Bootstrapper; on close exit app |
| `TripleDetection.App/App.config` | Add `LoginLogoPath` key |
| `docs/database/init.sql` | Add `PasswordSalt`, `PasswordHash` columns to Users table; admin remains plain text |
| `TripleDetection.Data/Entities/BaseEntity.cs` | Verify `Id` property exists (already has it) |

---

## Tasks

### Task 1: Add Password Fields to User Entity

**Files:**
- Modify: `TripleDetection.Data/Entities/User.cs`

**Changes:** Add two new properties for hashed password storage. Existing plain-text `Password` field stays for migration.

```csharp
// Add after Password property
public string PasswordSalt { get; set; }
public string PasswordHash { get; set; }
```

---

### Task 2: Update UserConfiguration - Map New Columns

**Files:**
- Modify: `TripleDetection.Data/Repositories/Configuration/UserConfiguration.cs`

**Changes:** Map `PasswordSalt` (string, max 64) and `PasswordHash` (string, max 128) columns. Keep existing plain-text `Password` column mapped.

```csharp
Property(u => u.PasswordSalt).HasMaxLength(64);
Property(u => u.PasswordHash).HasMaxLength(128);
```

---

### Task 3: Create PasswordHashService

**Files:**
- Create: `TripleDetection.Services/PasswordHashService.cs`

**Implementation:**

```csharp
using System;
using System.Security.Cryptography;
using System.Text;

namespace TripleDetection.Services
{
    public interface IPasswordHashService
    {
        string GenerateSalt();
        string ComputeHash(string salt, string password);
        bool VerifyPassword(string enteredPassword, string storedSalt, string storedHash);
        bool IsLegacyPlainText(string storedHash);
    }

    public class PasswordHashService : IPasswordHashService
    {
        private const int SaltSize = 16;

        public string GenerateSalt()
        {
            var bytes = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }

        public string ComputeHash(string salt, string password)
        {
            var combined = salt + password;
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(combined);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public bool VerifyPassword(string enteredPassword, string storedSalt, string storedHash)
        {
            if (string.IsNullOrEmpty(storedSalt) || string.IsNullOrEmpty(storedHash))
                return false;
            var hash = ComputeHash(storedSalt, enteredPassword);
            return hash == storedHash;
        }

        public bool IsLegacyPlainText(string storedHash)
        {
            // If it's short and not a valid base64 SHA256 output, treat as legacy plain text
            return storedHash != null && storedHash.Length < 32;
        }
    }
}
```

---

### Task 4: Update UserService - Dual-Mode Authentication

**Files:**
- Modify: `TripleDetection.Services/UserService.cs`

**Changes:** Update `Authenticate` method to try hashed password first, fall back to plain text (legacy), and auto-migrate if plain text matched.

```csharp
private readonly IPasswordHashService _hashService;

public UserService() : this(new SqliteRepositoryFactory().CreateRepository<User>(), null, new PasswordHashService())
{
}

public UserService(IRepository<User> repository, Services.Audit.IAuditLogService auditLogService)
    : this(repository, auditLogService, new PasswordHashService())
{
}

public UserService(IRepository<User> repository, Services.Audit.IAuditLogService auditLogService, IPasswordHashService hashService)
{
    _repository = repository;
    _auditLog = auditLogService;
    _hashService = hashService ?? new PasswordHashService();
}

public User Authenticate(string username, string password)
{
    var user = _repository.Find(u => u.Username == username && u.IsEnabled && !u.IsLocked)
                          .FirstOrDefault();
    if (user == null) return null;

    bool authenticated = false;
    bool needsMigration = false;

    if (!string.IsNullOrEmpty(user.PasswordHash) && !string.IsNullOrEmpty(user.PasswordSalt))
    {
        // Hashed mode
        authenticated = _hashService.VerifyPassword(password, user.PasswordSalt, user.PasswordHash);
    }
    else
    {
        // Legacy plain text mode
        authenticated = user.Password == password;
        needsMigration = authenticated;
    }

    if (!authenticated) return null;

    // Auto-migrate plain text to hashed on successful legacy login
    if (needsMigration)
    {
        user.PasswordSalt = _hashService.GenerateSalt();
        user.PasswordHash = _hashService.ComputeHash(user.PasswordSalt, password);
        _repository.Update(user);
    }

    _auditLog?.Log(user.Id, "登录", "User", user.Id, $"用户登录: {username}");
    return user;
}
```

Also add `using TripleDetection.Services;` for `IPasswordHashService`.

---

### Task 5: Update init.sql - Add Salt and Hash Columns

**Files:**
- Modify: `docs/database/init.sql`

**Changes:** Add `PasswordSalt TEXT, PasswordHash TEXT` columns to `Users` table. Admin user's Salt/Hash remain NULL (will use legacy plain text on first login).

```sql
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    RealName TEXT,
    Password TEXT NOT NULL,
    PasswordSalt TEXT,
    PasswordHash TEXT,
    Role TEXT NOT NULL,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    IsLocked INTEGER NOT NULL DEFAULT 0,
    LastLoginAt TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    CreateBy TEXT,
    UpdateBy TEXT,
    CreateAt TEXT NOT NULL,
    UpdateAt TEXT NOT NULL
);
```

---

### Task 6: Create LoginViewModel

**Files:**
- Create: `TripleDetection.App/ViewModels/LoginViewModel.cs`

**Implementation:**

```csharp
using System;
using System.Windows.Input;
using Prism.Mvvm;
using Prism.Commands;
using TripleDetection.Services;
using TripleDetection.Data.Entities;

namespace TripleDetection.ViewModels
{
    public class LoginViewModel : BindableBase
    {
        private readonly IUserService _userService;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isLoading;
        private bool _usernameHasError;
        private bool _passwordHasError;
        private string _logoPath;

        public LoginViewModel() : this(new UserService())
        {
        }

        public LoginViewModel(IUserService userService)
        {
            _userService = userService;
            _logoPath = System.Configuration.ConfigurationManager.AppSettings["LoginLogoPath"]
                ?? System.Configuration.ConfigurationManager.AppSettings["SystemLogoPath"];

            LoginCommand = new DelegateCommand(ExecuteLogin, CanExecuteLogin)
                .ObservesProperty(() => IsLoading)
                .ObservesProperty(() => Username)
                .ObservesProperty(() => Password);
            TogglePasswordVisibilityCommand = new DelegateCommand(() => IsPasswordVisible = !IsPasswordVisible);
        }

        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    UsernameHasError = false;
                    ErrorMessage = string.Empty;
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    PasswordHasError = false;
                    ErrorMessage = string.Empty;
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool UsernameHasError
        {
            get => _usernameHasError;
            set => SetProperty(ref _usernameHasError, value);
        }

        public bool PasswordHasError
        {
            get => _passwordHasError;
            set => SetProperty(ref _passwordHasError, value);
        }

        public bool IsPasswordVisible { get; set; }

        public string LogoPath
        {
            get => _logoPath;
            set => SetProperty(ref _logoPath, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand TogglePasswordVisibilityCommand { get; }

        public event Action<User> LoginSucceeded;

        private bool CanExecuteLogin()
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        private void ExecuteLogin()
        {
            // Validate
            UsernameHasError = string.IsNullOrWhiteSpace(Username);
            PasswordHasError = string.IsNullOrWhiteSpace(Password);
            if (UsernameHasError || PasswordHasError)
            {
                ErrorMessage = "请输入用户名和密码";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var user = _userService.Authenticate(Username, Password);

                if (user == null)
                {
                    ErrorMessage = "用户名或密码错误";
                    UsernameHasError = true;
                    PasswordHasError = true;
                    OnLoginFailed?.Invoke();
                    return;
                }

                if (!user.IsEnabled)
                {
                    ErrorMessage = "账号已被禁用";
                    OnLoginFailed?.Invoke();
                    return;
                }

                if (user.IsLocked)
                {
                    ErrorMessage = "账号已被锁定";
                    OnLoginFailed?.Invoke();
                    return;
                }

                // Success
                SessionManager.SetCurrentUser(user);
                LoginSucceeded?.Invoke(user);
            }
            catch (Exception ex)
            {
                ErrorMessage = "数据库连接失败，请稍后重试";
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event Action OnLoginFailed;
    }
}
```

---

### Task 7: Create LoginWindow.xaml

**Files:**
- Create: `TripleDetection.App/Views/LoginWindow.xaml`

**Implementation:**

```xaml
<Window x:Class="TripleDetection.Views.LoginWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        Title="登录 - Triple Detection"
        Width="400" Height="520"
        WindowStartupLocation="CenterScreen"
        ResizeMode="NoResize"
        Background="#FFFFFF">

    <Window.Resources>
        <BooleanToVisibilityConverter x:Key="BoolToVis"/>

        <!-- 输入框样式 -->
        <Style x:Key="LoginInputStyle" TargetType="TextBox">
            <Setter Property="Background" Value="#F5F5F5"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="BorderBrush" Value="#E0E0E0"/>
            <Setter Property="Padding" Value="12,10"/>
            <Setter Property="FontSize" Value="14"/>
            <Setter Property="VerticalContentAlignment" Value="Center"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="TextBox">
                        <Border x:Name="border"
                                Background="{TemplateBinding Background}"
                                BorderBrush="{TemplateBinding BorderBrush}"
                                BorderThickness="{TemplateBinding BorderThickness}"
                                CornerRadius="6">
                            <ScrollViewer x:Name="PART_ContentHost"
                                          VerticalAlignment="Center"
                                          Margin="{TemplateBinding Padding}"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="BorderBrush" Value="#E53E3E">
                                <Setter TargetName="border" Property="BorderBrush" Value="#E53E3E"/>
                            </Trigger>
                            <Trigger Property="BorderBrush" Value="#E0E0E0">
                                <Setter TargetName="border" Property="BorderBrush" Value="#E0E0E0"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
            <Style.Triggers>
                <Trigger Property="BorderBrush" Value="#E53E3E">
                    <Setter Property="BorderBrush" Value="#E53E3E"/>
                </Trigger>
            </Style.Triggers>
        </Style>

        <!-- 密码框样式 -->
        <Style x:Key="LoginPasswordStyle" TargetType="PasswordBox">
            <Setter Property="Background" Value="#F5F5F5"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="BorderBrush" Value="#E0E0E0"/>
            <Setter Property="Padding" Value="12,10"/>
            <Setter Property="FontSize" Value="14"/>
            <Setter Property="VerticalContentAlignment" Value="Center"/>
        </Style>
    </Window.Resources>

    <Grid>
        <!-- Shake animation -->
        <Grid.Triggers>
            <EventTrigger RoutedEvent="Grid.Loaded">
                <BeginStoryboard x:Name="ShakeStoryboard">
                    <Storyboard x:Name="ShakeAnimation">
                        <DoubleAnimationUsingKeyFrames
                            Storyboard.TargetName="MainGrid"
                            Storyboard.TargetProperty="(UIElement.RenderTransform).(TranslateTransform.X)">
                            <EasingDoubleKeyFrame KeyTime="0:0:0" Value="0"/>
                            <EasingDoubleKeyFrame KeyTime="0:0:0.05" Value="-4"/>
                            <EasingDoubleKeyFrame KeyTime="0:0:0.1" Value="4"/>
                            <EasingDoubleKeyFrame KeyTime="0:0:0.15" Value="-4"/>
                            <EasingDoubleKeyFrame KeyTime="0:0:0.2" Value="4"/>
                            <EasingDoubleKeyFrame KeyTime="0:0:0.25" Value="-4"/>
                            <EasingDoubleKeyFrame KeyTime="0:0:0.3" Value="0"/>
                        </DoubleAnimationUsingKeyFrames>
                    </Storyboard>
                </BeginStoryboard>
            </EventTrigger>
        </Grid.Triggers>

        <Grid x:Name="MainGrid" RenderTransformOrigin="0.5,0.5" Margin="40,30">
            <Grid.RenderTransform>
                <TranslateTransform/>
            </Grid.RenderTransform>

            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>

            <!-- Logo -->
            <Border Grid.Row="0" Height="120" Margin="0,0,0,8">
                <Image x:Name="LogoImage"
                       Width="120" Height="120"
                       HorizontalAlignment="Center"
                       VerticalAlignment="Center"
                       RenderOptions.BitmapScalingMode="HighQuality"/>
            </Border>

            <!-- System Name + Subtitle -->
            <StackPanel Grid.Row="1" HorizontalAlignment="Center" Margin="0,0,0,24">
                <TextBlock x:Name="SystemNameText"
                           Text="Triple Detection"
                           FontSize="20" FontWeight="Bold"
                           Foreground="#1A202C"
                           HorizontalAlignment="Center"/>
                <TextBlock Text="欢迎登录"
                           FontSize="14"
                           Foreground="#666666"
                           HorizontalAlignment="Center"
                           Margin="0,4,0,0"/>
            </StackPanel>

            <!-- Username Field -->
            <Border Grid.Row="2" x:Name="UsernameBorder"
                    Background="#F5F5F5"
                    BorderBrush="#E0E0E0"
                    BorderThickness="1"
                    CornerRadius="6"
                    Margin="0,0,0,12">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="40"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="👤"
                               VerticalAlignment="Center" HorizontalAlignment="Center"
                               FontSize="16" Foreground="#666666"/>
                    <TextBox x:Name="UsernameTextBox"
                             Grid.Column="1"
                             Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}"
                             Background="Transparent"
                             BorderThickness="0"
                             FontSize="14"
                             VerticalAlignment="Center"
                             Padding="0,10"
                             Foreground="#1A202C"/>
                </Grid>
            </Border>

            <!-- Password Field -->
            <Border Grid.Row="3" x:Name="PasswordBorder"
                    Background="#F5F5F5"
                    BorderBrush="#E0E0E0"
                    BorderThickness="1"
                    CornerRadius="6"
                    Margin="0,0,0,16">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="40"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="40"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="🔒"
                               VerticalAlignment="Center" HorizontalAlignment="Center"
                               FontSize="16" Foreground="#666666"/>
                    <PasswordBox x:Name="PasswordBox"
                                 Grid.Column="1"
                                 Background="Transparent"
                                 BorderThickness="0"
                                 FontSize="14"
                                 VerticalAlignment="Center"
                                 Padding="0,10"
                                 PasswordChanged="PasswordBox_PasswordChanged"/>
                    <TextBox x:Name="PasswordVisibleTextBox"
                             Grid.Column="1"
                             Text="{Binding Password, UpdateSourceTrigger=PropertyChanged}"
                             Background="Transparent"
                             BorderThickness="0"
                             FontSize="14"
                             VerticalAlignment="Center"
                             Padding="0,10"
                             Visibility="Collapsed"
                             Foreground="#1A202C"/>
                    <Button Grid.Column="2"
                            x:Name="TogglePasswordBtn"
                            Content="👁"
                            Background="Transparent"
                            BorderThickness="0"
                            FontSize="16"
                            Cursor="Hand"
                            Click="TogglePasswordVisibility_Click"
                            VerticalAlignment="Center"
                            HorizontalAlignment="Center"/>
                </Grid>
            </Border>

            <!-- Login Button -->
            <Button Grid.Row="4"
                    x:Name="LoginButton"
                    Content="{Binding IsLoading, Converter={StaticResource LoginButtonTextConverter}, ConverterParameter=登录}"
                    Command="{Binding LoginCommand}"
                    Background="#4FD1C5"
                    Foreground="#1A202C"
                    FontSize="14" FontWeight="Bold"
                    BorderThickness="0"
                    Padding="16,12"
                    Cursor="Hand"
                    HorizontalAlignment="Stretch"
                    Margin="0,0,0,8">
                <Button.Style>
                    <Style TargetType="Button">
                        <Setter Property="Template">
                            <Setter.Value>
                                <ControlTemplate TargetType="Button">
                                    <Border x:Name="border"
                                            Background="{TemplateBinding Background}"
                                            CornerRadius="6">
                                        <ContentPresenter HorizontalAlignment="Center"
                                                          VerticalAlignment="Center"/>
                                    </Border>
                                    <ControlTemplate.Triggers>
                                        <Trigger Property="IsMouseOver" Value="True">
                                            <Setter TargetName="border" Property="Background" Value="#3FC9BC"/>
                                        </Trigger>
                                        <Trigger Property="IsEnabled" Value="False">
                                            <Setter TargetName="border" Property="Background" Value="#A0E0DA"/>
                                        </Trigger>
                                    </ControlTemplate.Triggers>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                    </Style>
                </Button.Style>
            </Button>

            <!-- Error Message -->
            <TextBlock Grid.Row="4"
                       x:Name="ErrorText"
                       Text="{Binding ErrorMessage}"
                       Foreground="#E53E3E"
                       FontSize="12"
                       HorizontalAlignment="Center"
                       VerticalAlignment="Bottom"
                       Margin="0,50,0,0"
                       Visibility="{Binding ErrorMessage, Converter={StaticResource StringToVisibilityConverter}}"/>

            <!-- Footer -->
            <TextBlock Grid.Row="5"
                       Text="Triple Detection v1.0"
                       FontSize="11"
                       Foreground="#999999"
                       HorizontalAlignment="Center"
                       Margin="0,16,0,0"/>
        </Grid>
    </Grid>
</Window>
```

**Note:** A `LoginButtonTextConverter` and `StringToVisibilityConverter` are referenced. These will be created in Task 11.

---

### Task 8: Create LoginWindow.xaml.cs

**Files:**
- Create: `TripleDetection.App/Views/LoginWindow.xaml.cs`

**Implementation:**

```csharp
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using TripleDetection.ViewModels;

namespace TripleDetection.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;
        private bool _isPasswordVisible;

        public LoginWindow()
        {
            InitializeComponent();

            _viewModel = new LoginViewModel();
            DataContext = _viewModel;

            _viewModel.LoginSucceeded += OnLoginSucceeded;
            _viewModel.OnLoginFailed += OnLoginFailed;

            // Load logo
            var logoPath = _viewModel.LogoPath;
            if (!string.IsNullOrEmpty(logoPath))
            {
                var fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logoPath);
                if (File.Exists(fullPath))
                {
                    LogoImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri(fullPath, UriKind.Absolute));
                }
                else
                {
                    ShowLogoPlaceholder();
                }
            }
            else
            {
                ShowLogoPlaceholder();
            }

            // Load system name
            var systemName = System.Configuration.ConfigurationManager.AppSettings["SystemName"];
            if (!string.IsNullOrEmpty(systemName))
                SystemNameText.Text = systemName;

            // Enter key submits
            UsernameTextBox.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Enter) SubmitLogin(); };
            PasswordBox.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Enter) SubmitLogin(); };
            PasswordVisibleTextBox.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Enter) SubmitLogin(); };
        }

        private void ShowLogoPlaceholder()
        {
            // Show text placeholder: "TD" for Triple Detection
            SystemNameText.Visibility = Visibility.Visible;
            LogoImage.Visibility = Visibility.Collapsed;
        }

        private void SubmitLogin()
        {
            // Sync password from PasswordBox to ViewModel before command
            _viewModel.Password = PasswordBox.Password;
            if (_viewModel.LoginCommand.CanExecute(null))
                _viewModel.LoginCommand.Execute();
        }

        private void OnLoginSucceeded(Data.Entities.User user)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void OnLoginFailed()
        {
            // Play shake animation
            var storyboard = (Storyboard)Resources["ShakeAnimation"];
            storyboard?.Begin();

            // Apply red borders
            UsernameBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E53E3E"));
            PasswordBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E53E3E"));
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = PasswordBox.Password;
        }

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                PasswordVisibleTextBox.Text = PasswordBox.Password;
                PasswordVisibleTextBox.Visibility = Visibility.Visible;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordVisibleTextBox.Focus();
            }
            else
            {
                PasswordBox.Password = PasswordVisibleTextBox.Text;
                PasswordBox.Visibility = Visibility.Visible;
                PasswordVisibleTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Focus();
            }
        }
    }
}
```

---

### Task 9: Register LoginViewModel in Bootstrapper

**Files:**
- Modify: `TripleDetection.App/Bootstrapper.cs`

**Changes:** Add `LoginViewModel` registration. Also change `Container.Register<MainWindow>(Reuse.Singleton)` to `Reuse.Transient` so a new instance is created after login (prevents stale state).

```csharp
// In ConfigureContainer(), add:
Container.Register<LoginViewModel>(Reuse.Transient);

// Change MainWindow from Singleton to Transient
Container.Register<MainWindow>(Reuse.Transient);
```

---

### Task 10: Update App.xaml.cs - Login-First Startup

**Files:**
- Modify: `TripleDetection.App/App.xaml.cs`

**Implementation:**

```csharp
using System;
using System.IO;
using System.Windows;
using TripleDetection.Data.Repositories;
using TripleDetection.Data.Repositories.Sqlite;
using TripleDetection.Views;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Ensure database is initialized before login
        InitializeDatabase();

        // Show login window first
        var loginWindow = new LoginWindow();
        var result = loginWindow.ShowDialog();

        if (result != true)
        {
            // User closed login window without success → exit app
            Shutdown();
            return;
        }

        // Auth succeeded → run Bootstrapper to show MainWindow
        var bootstrapper = new Bootstrapper();
        bootstrapper.Run();
    }

    private void InitializeDatabase()
    {
        try
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tripledetection.db");
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Initialize DB schema if needed
            DatabaseConfig.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"数据库初始化失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
```

---

### Task 11: Add Converters and App.config Updates

**Files:**
- Create: `TripleDetection.App/Converters/LoginButtonTextConverter.cs`
- Create: `TripleDetection.App/Converters/StringToVisibilityConverter.cs`
- Modify: `TripleDetection.App/App.config`
- Modify: `TripleDetection.App/App.xaml`

**LoginButtonTextConverter.cs:**

```csharp
using System;
using System.Globalization;
using System.Windows.Data;

namespace TripleDetection.Converters
{
    public class LoginButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isLoading && isLoading)
                return "登录中...";
            return parameter?.ToString() ?? "登 录";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
```

**StringToVisibilityConverter.cs:**

```csharp
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TripleDetection.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value?.ToString()) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
```

**App.config — add LoginLogoPath:**

```xml
<add key="LoginLogoPath" value="Resources/logo.png" />
```

**App.xaml — register new converters and LoginButtonTextConverter:**

In Styles.xaml (merged into App.xaml), add:

```xml
<local:LoginButtonTextConverter x:Key="LoginButtonTextConverter"/>
<local:StringToVisibilityConverter x:Key="StringToVisibilityConverter"/>
```

---

### Task 12: Add Validation Error Styling for Empty Fields

**Files:**
- Modify: `TripleDetection.App/Resources/Styles.xaml`

**Changes:** The `LoginWindow.xaml` already handles red border on error via code-behind. No additional style changes needed — Task 7 XAML applies red `BorderBrush` on `UsernameBorder` and `PasswordBorder`.

---

### Task 13: Build and Verify (First Pass)

**Command:**
```powershell
powershell.exe -Command "& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' 'd:\xcm\Triple-Detection\TripleDetection.App\TripleDetection.App.csproj' /t:Rebuild /p:Configuration=Debug 2>&1 | Select-Object -Last 30"
```

**Expected:** 0 errors (warnings about unused imports OK to ignore)

**Verify:**
1. Build succeeds
2. LoginWindow appears centered on screen at 400×520px
3. Logo loads if `Resources/logo.png` exists
4. Empty fields show red border on submit attempt
5. Wrong credentials show error + shake animation
6. Valid credentials (admin/admin123) close LoginWindow and show MainWindow

---

### Task 14: Verify End-to-End Authentication

**Verification checklist:**

- [ ] App starts → LoginWindow appears (not MainWindow)
- [ ] Click Login with empty fields → red borders, no API call
- [ ] Enter wrong password → error message "用户名或密码错误" + shake
- [ ] Enter admin / admin123 → MainWindow appears
- [ ] MainWindow header shows correct logged-in username (from SessionManager)
- [ ] Logout button in MainWindow works (currently exits app)
- [ ] Logo not found → shows placeholder gracefully

---

## Implementation Order

1. Task 1: Add PasswordSalt/PasswordHash to User entity
2. Task 2: Update UserConfiguration
3. Task 3: Create PasswordHashService
4. Task 4: Update UserService (dual-mode auth)
5. Task 5: Update init.sql
6. Task 6: Create LoginViewModel
7. Task 7: Create LoginWindow.xaml
8. Task 8: Create LoginWindow.xaml.cs
9. Task 9: Register in Bootstrapper
10. Task 10: Update App.xaml.cs (login-first startup)
11. Task 11: Add converters + App.config
12. Task 12: Validation styling (already handled in XAML)
13. Task 13: Build and verify
14. Task 14: End-to-end verification

---

## Verification Criteria

After all tasks:
1. `MSBuild ... /t:Rebuild /p:Configuration=Debug` → 0 errors
2. App launches → LoginWindow centered on screen
3. Empty username/password → red border on both fields, no error message to server
4. Wrong credentials → shake animation + error "用户名或密码错误"
5. Correct credentials (admin/admin123) → LoginWindow closes, MainWindow appears with Dashboard
6. Session shows correct username in MainWindow header
7. Close LoginWindow via X → app exits (MainWindow never shown)