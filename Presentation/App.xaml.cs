using System;
using System.IO;
using System.Threading;
using System.Windows;
using DryIoc;
using Prism.DryIoc;
using Prism.Events;
using Prism.Ioc;
using TripleDetection.Application.Services;
using TripleDetection.Application.SettingsServices;
using TripleDetection.Application.VmServices;
using TripleDetection.Domain.Repositories;
using TripleDetection.Infrastructure.Persistence;
using TripleDetection.Infrastructure.Repositories;
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

public partial class App : PrismApplication
{
    private static Mutex _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "TripleDetectionApp_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("应用程序已在运行中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        InitializeDatabase();
        base.OnStartup(e);
    }

    protected override Window CreateShell() => Container.Resolve<MainWindow>();

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        var container = containerRegistry as IContainer;
        if (container == null) return;

        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tripledetection.db");
        var connectionString = $"Data Source={dbPath}";

        // Infrastructure - Connection and Repository Factory
        container.RegisterInstance<IDbConnectionFactory>(new SqliteConnectionFactory(connectionString));
        container.RegisterInstance<IRepositoryFactory>(new SqliteRepositoryFactory(connectionString));

        // Infrastructure - Repositories (transient)
        container.Register(typeof(IRepository<>), typeof(SqliteRepository<>), Reuse.Transient);
        container.Register<IAuditLogRepository, AuditLogRepository>(Reuse.Transient);
        container.Register<IDetectionRecordRepository, DetectionRecordRepository>(Reuse.Transient);

        // Logging Service (singleton)
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        var eventAggregator = Container.Resolve<IEventAggregator>();
        container.RegisterInstance(new LoggingService(logPath, eventAggregator));

        // Image Storage (singleton)
        var okDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "OK");
        var ngDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "NG");
        container.RegisterInstance(new ImageStorageService(okDir, ngDir));

        // Application Services (transient)
        container.Register<IUserService, UserService>(Reuse.Transient);
        container.Register<IProductService, ProductService>(Reuse.Transient);
        container.Register<ITaskService, TaskService>(Reuse.Transient);
        container.Register<IAuditLogService, AuditLogService>(Reuse.Transient);
        container.Register<IDetectionRecordService, DetectionRecordService>(Reuse.Transient);
        container.Register<CommunicationSettingsService>(Reuse.Transient);
        container.Register<VmSettingsService>(Reuse.Transient);
        container.Register<SystemSettingsService>(Reuse.Transient);
        container.Register<DeviceControlSettingsService>(Reuse.Transient);
        container.Register<SettingsSyncService>(Reuse.Transient);

        // VM Integration (singleton)
        container.Register<VmIntegrationService>(Reuse.Singleton);

        // ViewModels (transient)
        container.Register<MainViewModel>(Reuse.Transient);
        container.Register<TabItemViewModel>(Reuse.Transient);
        container.Register<LoginViewModel>(Reuse.Transient);
        container.Register<UserManagementViewModel>(Reuse.Transient);
        container.Register<UserEditViewModel>(Reuse.Transient);
        container.Register<ProductListViewModel>(Reuse.Transient);
        container.Register<ProductEditViewModel>(Reuse.Transient);
        container.Register<TaskListViewModel>(Reuse.Transient);
        container.Register<TaskEditViewModel>(Reuse.Transient);
        container.Register<SettingsShellViewModel>(Reuse.Transient);

        // Navigation
        container.RegisterTypeForNavigation<DashboardView>("Dashboard");
        container.RegisterTypeForNavigation<DetectionView>("Detection");
        container.RegisterTypeForNavigation<ProductListView>("Products");
        container.RegisterTypeForNavigation<TaskListView>("Tasks");
        container.RegisterTypeForNavigation<LogsView>("Logs");
        container.RegisterTypeForNavigation<SettingsView>("Settings");
        container.RegisterTypeForNavigation<UserManagementView>("UserManagement");
        container.RegisterTypeForNavigation<AuditLogView>("AuditLog");
        container.RegisterTypeForNavigation<DetectionHistoryView>("DetectionHistory");

        // MainWindow as singleton
        container.Register<MainWindow>(Reuse.Singleton);
    }

    protected override void OnInitialized()
    {
        var loginWindow = new LoginWindow();
        var result = loginWindow.ShowDialog();
        if (result != true) { Shutdown(); return; }
        if (MainWindow != null)
        {
            MainWindow.Show();
            MainWindow.WindowState = WindowState.Normal;
            MainWindow.Activate();
        }
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