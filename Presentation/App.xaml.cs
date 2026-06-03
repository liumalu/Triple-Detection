using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Reflection;
using Prism.DryIoc;
using Prism.Ioc;
using TripleDetection.Application.Services;
using TripleDetection.Application.SettingsServices;
using TripleDetection.Application.VmServices;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Repositories;
using TripleDetection.Infrastructure.Persistence;
using TripleDetection.Infrastructure.Repositories;
using TripleDetection.Infrastructure.IO;
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
    public partial class App : PrismApplication
    {
        private static Mutex _mutex;

        [STAThread]
        public static void Main()
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Immediate diagnostic logging at start
            var debugPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "startup_debug.log");
            var dir = Path.GetDirectoryName(debugPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(debugPath, $"[{DateTime.Now:HH:mm:ss.fff}] OnStartup ENTER");

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // VisionMaster DLL resolver
            File.AppendAllText(debugPath, $"\n[{DateTime.Now:HH:mm:ss.fff}] Before AssemblyResolve");

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = new AssemblyName(args.Name).Name;
                if (name == null) return null;

                var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Infrastructure", "libs", "VisionMaster", name + ".dll");
                if (File.Exists(localPath))
                    return Assembly.LoadFrom(localPath);

                var vmPath = Path.Combine(@"C:\Program Files\VisionMaster4.2.0\Development\V4.x\Libraries\win64\C#", name + ".dll");
                if (File.Exists(vmPath))
                    return Assembly.LoadFrom(vmPath);

                return null;
            };

            _mutex = new Mutex(true, "TripleDetectionApp_SingleInstance", out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show("应用程序已在运行中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown(); return;
            }

            InitializeDatabase();

            File.AppendAllText(debugPath, $"\n[{DateTime.Now:HH:mm:ss.fff}] After InitializeDatabase, calling base.OnStartup");

            base.OnStartup(e);

            File.AppendAllText(debugPath, $"\n[{DateTime.Now:HH:mm:ss.fff}] After base.OnStartup");
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Infrastructure
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "tripledetection.db");
            var connectionString = $"Data Source={dbPath}";

            containerRegistry.RegisterInstance<IDbConnectionFactory>(new SqliteConnectionFactory(connectionString));
            containerRegistry.RegisterSingleton<IRepositoryFactory>(sp => new SqliteRepositoryFactory(sp.Resolve<IDbConnectionFactory>()));

            // Repository registrations
            // All repositories need factory delegate because they take string connectionString directly
            containerRegistry.Register<IAuditLogRepository>(sp => new AuditLogRepository(connectionString));
            containerRegistry.Register<IRepository<AuditLog>>(sp => new AuditLogRepository(connectionString));
            containerRegistry.Register<IDetectionRecordRepository>(sp => new DetectionRecordRepository(connectionString));
            containerRegistry.Register<IRepository<DetectionRecord>>(sp => new DetectionRecordRepository(connectionString));
            containerRegistry.Register<IRepository<User>>(sp => new SqliteRepository<User>(connectionString));
            containerRegistry.Register<IRepository<ProdTask>>(sp => new SqliteRepository<ProdTask>(connectionString));
            containerRegistry.Register<IRepository<Product>>(sp => new SqliteRepository<Product>(connectionString));

            // Password Hash Service
            containerRegistry.RegisterSingleton<IPasswordHashService, PasswordHashService>();

            // Logging (singleton)
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            containerRegistry.RegisterInstance(new LoggingService(logPath));

            // Image Storage (singleton)
            var okDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "OK");
            var ngDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "NG");
            containerRegistry.RegisterInstance(new ImageStorageService(okDir, ngDir));

            // VM Integration (singleton)
            containerRegistry.RegisterSingleton<VmIntegrationService>();

            // Settings services
            containerRegistry.Register<CommunicationSettingsService>();
            containerRegistry.Register<VmSettingsService>();
            containerRegistry.Register<SystemSettingsService>();
            containerRegistry.Register<DeviceControlSettingsService>();
            containerRegistry.RegisterSingleton<SettingsSyncService>();

            // Application Services
            containerRegistry.Register<IUserService, UserService>();
            containerRegistry.Register<IProductService, ProductService>();
            containerRegistry.Register<ITaskService, TaskService>();
            containerRegistry.Register<IAuditLogService, AuditLogService>();
            containerRegistry.Register<IDetectionRecordService, DetectionRecordService>();

            // Navigation service
            containerRegistry.RegisterSingleton<NavigationService>();
            containerRegistry.RegisterSingleton<INavigationService>(sp => sp.Resolve<NavigationService>());

            // ViewModels
            containerRegistry.Register<LoginViewModel>();
            containerRegistry.Register<MainViewModel>();
            containerRegistry.Register<TabItemViewModel>();
            containerRegistry.Register<UserManagementViewModel>();
            containerRegistry.Register<UserEditViewModel>();
            containerRegistry.Register<ProductListViewModel>();
            containerRegistry.Register<ProductEditViewModel>();
            containerRegistry.Register<TaskListViewModel>();
            containerRegistry.Register<TaskEditViewModel>();
            containerRegistry.Register<SettingsShellViewModel>();

            // Views (transient) - registered for manual creation via Container.Resolve
            containerRegistry.Register<LoginWindow>();
            containerRegistry.Register<MainWindow>();
            containerRegistry.Register<Views.Detection.DetectionView>();
            containerRegistry.Register<Views.App.DashboardView>();
            containerRegistry.Register<Views.App.LogsView>();
            containerRegistry.Register<Views.Production.ProductListView>();
            containerRegistry.Register<Views.Production.TaskListView>();
            containerRegistry.Register<Views.Audit.AuditLogView>();
            containerRegistry.Register<Views.Auth.UserManagementView>();
            containerRegistry.Register<Views.SettingsView>();

            // Register views for navigation (Prism knows how to resolve them)
            containerRegistry.RegisterForNavigation<Views.Detection.DetectionView, MainViewModel>();
            containerRegistry.RegisterForNavigation<Views.App.DashboardView, TabItemViewModel>();
            containerRegistry.RegisterForNavigation<Views.App.LogsView, TabItemViewModel>();
            containerRegistry.RegisterForNavigation<Views.Production.ProductListView, ProductListViewModel>();
            containerRegistry.RegisterForNavigation<Views.Production.TaskListView, TaskListViewModel>();
            containerRegistry.RegisterForNavigation<Views.Audit.AuditLogView, TabItemViewModel>();
            containerRegistry.RegisterForNavigation<Views.Auth.UserManagementView, UserManagementViewModel>();
            containerRegistry.RegisterForNavigation<Views.SettingsView, SettingsShellViewModel>();

            // IO Device Service
            containerRegistry.RegisterSingleton<IIODeviceService, ModbusTcpIOService>();

            // Reject Service
            containerRegistry.RegisterSingleton<IRejectService, RejectService>();
        }

        protected override Window CreateShell()
        {
            // Shell is created manually in OnInitialized (login -> main window flow)
            return null;
        }

        protected override void OnInitialized()
        {
            var debugPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "startup_debug.log");
            File.AppendAllText(debugPath, $"\n[{DateTime.Now:HH:mm:ss.fff}] OnInitialized ENTER");

            base.OnInitialized();

            File.AppendAllText(debugPath, $"\n[{DateTime.Now:HH:mm:ss.fff}] OnInitialized after base.OnInitialized");

            // Create and show login window first
            File.AppendAllText(debugPath, $"\n[{DateTime.Now:HH:mm:ss.fff}] About to resolve LoginWindow");
            try
            {
                var loginWindow = Container.Resolve<LoginWindow>();
                File.AppendAllText(debugPath, $"\n[{DateTime.Now:HH:mm:ss.fff}] LoginWindow resolved, about to ShowDialog");

                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logDir);
                File.WriteAllText(Path.Combine(logDir, "startup.log"), $"[{DateTime.Now:HH:mm:ss}] LoginWindow created");

                var result = loginWindow.ShowDialog();

                File.AppendAllText(Path.Combine(logDir, "startup.log"), $"\n[{DateTime.Now:HH:mm:ss}] ShowDialog returned: {result}");

                if (result != true)
                {
                    Shutdown();
                    return;
                }

                // Then show main window
                var mainWindow = Container.Resolve<MainWindow>();

                // 建立 RejectService 对 VmIntegrationService 的事件订阅
                var vmService = Container.Resolve<VmIntegrationService>();
                var rejectService = Container.Resolve<IRejectService>();
                vmService.OnDetectionResult += rejectService.OnDetectionResultReceived;

                File.AppendAllText(Path.Combine(logDir, "startup.log"), $"\n[{DateTime.Now:HH:mm:ss}] MainWindow created from DI");

                MainWindow = mainWindow;
                mainWindow.Show();

                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            catch (Exception ex)
            {
                File.AppendAllText(debugPath, $"\n[{DateTime.Now:HH:mm:ss.fff}] EXCEPTION in OnInitialized: {ex.GetType().Name}: {ex.Message}");
                var inner = ex.InnerException;
                while (inner != null)
                {
                    File.AppendAllText(debugPath, $"\n  Inner: {inner.GetType().Name}: {inner.Message}");
                    inner = inner.InnerException;
                }
                File.AppendAllText(debugPath, $"\n  StackTrace: {ex.StackTrace}");
                MessageBox.Show($"OnInitialized error: {ex.Message}\nInner: {ex.InnerException?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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