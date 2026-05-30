using System;
using System.IO;
using System.Threading;
using System.Windows;
using DryIoc;
using Prism.DryIoc;
using Prism.Events;
using Prism.Ioc;
using TripleDetection;
using TripleDetection.App;
using TripleDetection.App.Services.Detection;
using TripleDetection.App.Services.System;
using TripleDetection.Data.Repositories;
using TripleDetection.Data.Repositories.Contracts;
using TripleDetection.Data.Repositories.Sqlite;
using TripleDetection.Services;
using TripleDetection.Services.Audit;
using TripleDetection.Services.Data;
using TripleDetection.Services.Production;
using TripleDetection.ViewModels;
using TripleDetection.ViewModels.Auth;
using TripleDetection.ViewModels.Detection;
using TripleDetection.ViewModels.Production;
using TripleDetection.ViewModels.Settings;
using TripleDetection.Views;
using TripleDetection.Views.App;
using TripleDetection.Views.Audit;
using TripleDetection.Views.Auth;
using TripleDetection.Views.Detection;
using TripleDetection.Views.Production;
using TripleDetection.Views.Settings;

public partial class App : PrismApplication
{
    private static Mutex _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string mutexName = "TripleDetectionApp_SingleInstance";
        _mutex = new Mutex(true, mutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("应用程序已在运行中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        InitializeDatabase();
        base.OnStartup(e);
    }

    protected override Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // IContainerRegistry in Prism.DryIoc IS the DryIoc IContainer directly
        // The IContainer interface is exposed directly
        var container = containerRegistry as IContainer;
        if (container == null) return;

        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tripledetection.db");
        var connectionString = $"Data Source={dbPath}";

        container.Register<SqliteDbContext>(Reuse.Transient,
            Made.Of(() => new SqliteDbContext(connectionString)));

        container.Register<IRepositoryFactory, SqliteRepositoryFactory>(Reuse.Singleton);
        container.Register(typeof(IRepository<>), typeof(SqliteRepository<>), Reuse.Transient);
        container.Register<IDetectionRecordRepository, DetectionRecordRepository>(Reuse.Transient);
        container.Register<IAuditLogRepository, AuditLogRepository>(Reuse.Transient);

        var logPath = System.Configuration.ConfigurationManager.AppSettings["LogPath"];
        if (string.IsNullOrEmpty(logPath))
            logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        var eventAggregator = Container.Resolve<IEventAggregator>();
        container.RegisterInstance(new LoggingService(logPath, eventAggregator));

        var okDir = System.Configuration.ConfigurationManager.AppSettings["OkImageDir"]
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "OK");
        var ngDir = System.Configuration.ConfigurationManager.AppSettings["NgImageDir"]
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "NG");
        container.RegisterInstance(new ImageStorageService(okDir, ngDir));

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

        container.Register<VmIntegrationService>(Reuse.Singleton);

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

        container.RegisterTypeForNavigation<DashboardView>("Dashboard");
        container.RegisterTypeForNavigation<DetectionView>("Detection");
        container.RegisterTypeForNavigation<ProductListView>("Products");
        container.RegisterTypeForNavigation<TaskListView>("Tasks");
        container.RegisterTypeForNavigation<LogsView>("Logs");
        container.RegisterTypeForNavigation<SettingsView>("Settings");
        container.RegisterTypeForNavigation<UserManagementView>("UserManagement");
        container.RegisterTypeForNavigation<AuditLogView>("AuditLog");
        container.RegisterTypeForNavigation<DetectionHistoryView>("DetectionHistory");

        // Register MainWindow as singleton
        container.Register<MainWindow>(Reuse.Singleton);
    }

    protected override void OnInitialized()
    {
        // 先显示登录窗口，不调用 base.OnInitialized() 是为了阻止 Prism 自动显示 MainWindow
        var loginWindow = new LoginWindow();
        var result = loginWindow.ShowDialog();

        if (result != true)
        {
            Shutdown();
            return;
        }

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