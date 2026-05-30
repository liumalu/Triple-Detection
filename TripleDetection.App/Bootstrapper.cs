using System;
using System.Configuration;
using System.IO;
using System.Windows;
using DryIoc;
using Prism.DryIoc;
using Prism.Events;
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

namespace TripleDetection
{
    public class Bootstrapper : DryIocBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell()
        {
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.Show();
            }
        }

        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();

            // =====================================================
            // Infrastructure Layer — Database & Repositories
            // =====================================================
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tripledetection.db");
            var connectionString = $"Data Source={dbPath}";

            Container.Register<SqliteDbContext>(Reuse.Transient,
                Made.Of(() => new SqliteDbContext(connectionString)));

            Container.Register<IRepositoryFactory, SqliteRepositoryFactory>(Reuse.Singleton);
            Container.Register(typeof(IRepository<>), typeof(SqliteRepository<>), Reuse.Transient);
            Container.Register<IDetectionRecordRepository, DetectionRecordRepository>(Reuse.Transient);
            Container.Register<IAuditLogRepository, AuditLogRepository>(Reuse.Transient);

            // =====================================================
            // Application Layer — Services
            // =====================================================
            // LoggingService — singleton with logPath from app config
            var logPath = ConfigurationManager.AppSettings["LogPath"];
            if (string.IsNullOrEmpty(logPath))
                logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            var eventAggregator = Container.Resolve<IEventAggregator>();
            Container.RegisterInstance(new LoggingService(logPath, eventAggregator));

            // ImageStorageService — singleton with paths from app config
            var okDir = ConfigurationManager.AppSettings["OkImageDir"]
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "OK");
            var ngDir = ConfigurationManager.AppSettings["NgImageDir"]
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "NG");
            Container.RegisterInstance(new ImageStorageService(okDir, ngDir));

            // Application Services
            Container.Register<IUserService, UserService>(Reuse.Transient);
            Container.Register<IProductService, ProductService>(Reuse.Transient);
            Container.Register<ITaskService, TaskService>(Reuse.Transient);
            Container.Register<IAuditLogService, AuditLogService>(Reuse.Transient);
            Container.Register<IDetectionRecordService, DetectionRecordService>(Reuse.Transient);

            // Settings Services
            Container.Register<CommunicationSettingsService>(Reuse.Transient);
            Container.Register<VmSettingsService>(Reuse.Transient);
            Container.Register<SystemSettingsService>(Reuse.Transient);
            Container.Register<DeviceControlSettingsService>(Reuse.Transient);
            Container.Register<SettingsSyncService>(Reuse.Transient);

            // VM Integration Service — singleton (owns VM SDK lifecycle)
            Container.Register<VmIntegrationService>(Reuse.Singleton);

            // =====================================================
            // Presentation Layer — ViewModels
            // =====================================================
            Container.Register<MainViewModel>(Reuse.Transient);
            Container.Register<TabItemViewModel>(Reuse.Transient);
            Container.Register<LoginViewModel>(Reuse.Transient);
            Container.Register<UserManagementViewModel>(Reuse.Transient);
            Container.Register<UserEditViewModel>(Reuse.Transient);
            Container.Register<ProductListViewModel>(Reuse.Transient);
            Container.Register<ProductEditViewModel>(Reuse.Transient);
            Container.Register<TaskListViewModel>(Reuse.Transient);
            Container.Register<TaskEditViewModel>(Reuse.Transient);
            Container.Register<SettingsShellViewModel>(Reuse.Transient);

            // =====================================================
            // Presentation Layer — Views for Navigation
            // =====================================================
            Container.RegisterTypeForNavigation<Views.App.DashboardView>("Dashboard");
            Container.RegisterTypeForNavigation<Views.Detection.DetectionView>("Detection");
            Container.RegisterTypeForNavigation<Views.Production.ProductListView>("Products");
            Container.RegisterTypeForNavigation<Views.Production.TaskListView>("Tasks");
            Container.RegisterTypeForNavigation<Views.App.LogsView>("Logs");
            Container.RegisterTypeForNavigation<Views.SettingsView>("Settings");
            Container.RegisterTypeForNavigation<Views.Auth.UserManagementView>("UserManagement");
            Container.RegisterTypeForNavigation<Views.Audit.AuditLogView>("AuditLog");
            Container.RegisterTypeForNavigation<Views.Detection.DetectionHistoryView>("DetectionHistory");

            // =====================================================
            // Shell — MainWindow
            // =====================================================
            Container.Register<MainWindow>(Reuse.Singleton);
        }
    }
}
