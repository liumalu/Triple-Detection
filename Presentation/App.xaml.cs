using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using TripleDetection;
using TripleDetection.Application.Services;
using TripleDetection.Application.SettingsServices;
using TripleDetection.Application.VmServices;
using TripleDetection.Domain.Repositories;
using TripleDetection.Infrastructure.Persistence;
using TripleDetection.Infrastructure.Repositories;
using TripleDetection.Presentation.Navigation;
using TripleDetection.Presentation.ViewModels;
using TripleDetection.Presentation.ViewModels.Auth;
using TripleDetection.Presentation.ViewModels.Detection;
using TripleDetection.Presentation.ViewModels.Production;
using TripleDetection.Presentation.ViewModels.Settings;
using TripleDetection.Presentation.Views;
using TripleDetection.Presentation.Views.App;
using TripleDetection.Presentation.Views.Audit;
using TripleDetection.Presentation.Views.Auth;
using TripleDetection.Presentation.Views.Detection;
using TripleDetection.Presentation.Views.Production;
using TripleDetection.Presentation.Views.Settings;

namespace TripleDetection.Presentation
{
public partial class App : System.Windows.Application
{
    private IServiceProvider? _services;
    private static Mutex? _mutex;

    public static IServiceProvider Services => ((App)Current)._services!;

    [STAThread]
    public static void Main()
    {
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Set shutdown mode first - before showing any window
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _mutex = new Mutex(true, "TripleDetectionApp_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("应用程序已在运行中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown(); return;
        }

        InitializeDatabase();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();

        // Show login window first
        var loginWindow = _services.GetRequiredService<LoginWindow>();
        var logDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        System.IO.Directory.CreateDirectory(logDir);
        System.IO.File.WriteAllText(
            System.IO.Path.Combine(logDir, "startup.log"),
            $"[{DateTime.Now:HH:mm:ss}] LoginWindow created");

        var result = loginWindow.ShowDialog();
        System.IO.File.AppendAllText(
            System.IO.Path.Combine(logDir, "startup.log"),
            $"\n[{DateTime.Now:HH:mm:ss}] ShowDialog returned: {result}");

        if (result != true) { Shutdown(); return; }

        // Then show main window
        var mainWindow = _services.GetRequiredService<MainWindow>();
        System.IO.File.AppendAllText(
            System.IO.Path.Combine(logDir, "startup.log"),
            $"\n[{DateTime.Now:HH:mm:ss}] MainWindow created from DI");

        MainWindow = mainWindow;
        System.IO.File.AppendAllText(
            System.IO.Path.Combine(logDir, "startup.log"),
            $"\n[{DateTime.Now:HH:mm:ss}] MainWindow assigned to App.MainWindow");

        // Now show the main window
        mainWindow.Show();
        System.IO.File.AppendAllText(
            System.IO.Path.Combine(logDir, "startup.log"),
            $"\n[{DateTime.Now:HH:mm:ss}] mainWindow.Show() called");

        // Keep app alive
        this.ShutdownMode = ShutdownMode.OnMainWindowClose;
        System.IO.File.AppendAllText(
            System.IO.Path.Combine(logDir, "startup.log"),
            $"\n[{DateTime.Now:HH:mm:ss}] ShutdownMode set to OnMainWindowClose");
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Infrastructure
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "tripledetection.db");
        var connectionString = $"Data Source={dbPath}";
        services.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory(connectionString));
        services.AddSingleton<IRepositoryFactory>(sp => new SqliteRepositoryFactory(sp.GetRequiredService<IDbConnectionFactory>()));
        services.AddTransient(sp => sp.GetRequiredService<IDbConnectionFactory>().CreateConnection());
        services.AddTransient(typeof(IRepository<>), typeof(SqliteRepository<>));
        services.AddTransient<IAuditLogRepository, AuditLogRepository>();
        services.AddTransient<IDetectionRecordRepository, DetectionRecordRepository>();

        // Logging (singleton)
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        services.AddSingleton(new LoggingService(logPath));

        // Image Storage (singleton)
        var okDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "OK");
        var ngDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "NG");
        services.AddSingleton(new ImageStorageService(okDir, ngDir));

        // VM Integration (singleton)
        services.AddSingleton<VmIntegrationService>();

        // Settings
        services.AddTransient<CommunicationSettingsService>();
        services.AddTransient<VmSettingsService>();
        services.AddTransient<SystemSettingsService>();
        services.AddTransient<DeviceControlSettingsService>();
        services.AddSingleton<SettingsSyncService>();

        // Password Hash Service
        services.AddSingleton<IPasswordHashService, PasswordHashService>();

        // Application Services (transient)
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IProductService, ProductService>();
        services.AddTransient<ITaskService, TaskService>();
        services.AddTransient<IAuditLogService, AuditLogService>();
        services.AddTransient<IDetectionRecordService, DetectionRecordService>();

        // Navigation service
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());

        // ViewModels (transient)
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<TabItemViewModel>();
        services.AddTransient<UserManagementViewModel>();
        services.AddTransient<UserEditViewModel>();
        services.AddTransient<ProductListViewModel>();
        services.AddTransient<ProductEditViewModel>();
        services.AddTransient<TaskListViewModel>();
        services.AddTransient<TaskEditViewModel>();
        services.AddTransient<SettingsShellViewModel>();

        // Views (transient)
        services.AddTransient<LoginWindow>();
        services.AddTransient<TripleDetection.MainWindow>();
        services.AddTransient<TripleDetection.Presentation.Views.Detection.DetectionView>();
    }

    private void InitializeDatabase()
    {
        try
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "tripledetection.db");
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            DatabaseInitializer.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"数据库初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
}