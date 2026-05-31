using System;
using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
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

public partial class App : Application
{
    private IServiceProvider? _services;
    private static Mutex? _mutex;

    public static IServiceProvider Services => ((App)Current)._services!;

    protected override void OnStartup(StartupEventArgs e)
    {
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
        var result = loginWindow.ShowDialog();
        if (result != true) { Shutdown(); return; }

        // Then show main window
        var mainWindow = _services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
        mainWindow.WindowState = WindowState.Normal;
        mainWindow.Activate();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Infrastructure
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tripledetection.db");
        var connectionString = $"Data Source={dbPath}";
        services.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory(connectionString));
        services.AddSingleton<IRepositoryFactory>(new SqliteRepositoryFactory(connectionString));
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
        services.AddTransient<MainWindow>();
    }

    private void InitializeDatabase()
    {
        try
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tripledetection.db");
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