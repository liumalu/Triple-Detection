using System;
using Prism.Mvvm;
using Prism.Commands;
using TripleDetection.Application.Services;
using TripleDetection.Application.SettingsServices;
using TripleDetection.Domain;
using TripleDetection.Presentation.Models;

namespace TripleDetection.Presentation.ViewModels.Settings
{
    public class SystemSettingsViewModel : ViewModelBase
    {
        private readonly SystemSettingsService _service;
        private readonly IAuditLogService _auditLogService;
        private SystemSettings _originalSettings;

        private string _logSaveMethod = "ByDate";
        public string LogSaveMethod
        {
            get => _logSaveMethod;
            set => SetProperty(ref _logSaveMethod, value);
        }

        private int _logRetentionDays = 30;
        public int LogRetentionDays
        {
            get => _logRetentionDays;
            set => SetProperty(ref _logRetentionDays, value);
        }

        private string _logExportPath = string.Empty;
        public string LogExportPath
        {
            get => _logExportPath;
            set => SetProperty(ref _logExportPath, value);
        }

        private bool _autoCleanLog = true;
        public bool AutoCleanLog
        {
            get => _autoCleanLog;
            set => SetProperty(ref _autoCleanLog, value);
        }

        private string _factoryCode = string.Empty;
        public string FactoryCode
        {
            get => _factoryCode;
            set => SetProperty(ref _factoryCode, value);
        }

        private string _productionLine = string.Empty;
        public string ProductionLine
        {
            get => _productionLine;
            set => SetProperty(ref _productionLine, value);
        }

        private string _dbBackupRoot = string.Empty;
        public string DbBackupRoot
        {
            get => _dbBackupRoot;
            set => SetProperty(ref _dbBackupRoot, value);
        }

        private int _imageRetentionCount = 1000;
        public int ImageRetentionCount
        {
            get => _imageRetentionCount;
            set => SetProperty(ref _imageRetentionCount, value);
        }

        private bool _autoCleanImages = true;
        public bool AutoCleanImages
        {
            get => _autoCleanImages;
            set => SetProperty(ref _autoCleanImages, value);
        }

        public string[] LogSaveMethodOptions { get; } = { "ByDate", "BySize" };

        public DelegateCommand<string> BrowseLogPathCommand { get; }
        public DelegateCommand<string> BrowseBackupPathCommand { get; }
        public DelegateCommand SaveCommand { get; }

        public SystemSettingsViewModel(SystemSettingsService service, IAuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
            BrowseLogPathCommand = new DelegateCommand<string>(ExecuteBrowseLogPath);
            BrowseBackupPathCommand = new DelegateCommand<string>(ExecuteBrowseBackupPath);
            SaveCommand = new DelegateCommand(ExecuteSave);
        }

        public void Load()
        {
            var settings = _service.Load();
            _originalSettings = new SystemSettings
            {
                LogSaveMethod = settings.LogSaveMethod,
                LogRetentionDays = settings.LogRetentionDays,
                LogExportPath = settings.LogExportPath,
                AutoCleanLog = settings.AutoCleanLog,
                FactoryCode = settings.FactoryCode,
                ProductionLine = settings.ProductionLine,
                DbBackupRoot = settings.DbBackupRoot,
                ImageRetentionCount = settings.ImageRetentionCount,
                AutoCleanImages = settings.AutoCleanImages
            };

            LogSaveMethod = settings.LogSaveMethod;
            LogRetentionDays = settings.LogRetentionDays;
            LogExportPath = settings.LogExportPath;
            AutoCleanLog = settings.AutoCleanLog;
            FactoryCode = settings.FactoryCode;
            ProductionLine = settings.ProductionLine;
            DbBackupRoot = settings.DbBackupRoot;
            ImageRetentionCount = settings.ImageRetentionCount;
            AutoCleanImages = settings.AutoCleanImages;
        }

        private void ExecuteBrowseLogPath(string parameter)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "选择日志导出目录";
                dialog.SelectedPath = LogExportPath;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    LogExportPath = dialog.SelectedPath;
                }
            }
        }

        private void ExecuteBrowseBackupPath(string parameter)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "选择数据库备份目录";
                dialog.SelectedPath = DbBackupRoot;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    DbBackupRoot = dialog.SelectedPath;
                }
            }
        }

        private void ExecuteSave()
        {
            try
            {
                var newSettings = new SystemSettings
                {
                    LogSaveMethod = LogSaveMethod,
                    LogRetentionDays = LogRetentionDays,
                    LogExportPath = LogExportPath,
                    AutoCleanLog = AutoCleanLog,
                    FactoryCode = FactoryCode,
                    ProductionLine = ProductionLine,
                    DbBackupRoot = DbBackupRoot,
                    ImageRetentionCount = ImageRetentionCount,
                    AutoCleanImages = AutoCleanImages
                };

                _service.Save(newSettings);

                var changes = DetectChanges();
                if (changes.Count > 0)
                {
                    var details = Newtonsoft.Json.JsonConvert.SerializeObject(new { category = "SystemSettings", changes = changes });
                    _auditLogService.Log(SessionManager.CurrentUserId, "SETTINGS_UPDATE", "SystemSettings", 0, details);
                }

                System.Windows.MessageBox.Show(
                    "保存成功",
                    "提示",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                Load();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"保存失败: {ex.Message}",
                    "错误",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private System.Collections.Generic.List<string> DetectChanges()
        {
            var changes = new System.Collections.Generic.List<string>();

            if (_originalSettings == null)
                return changes;

            if (_originalSettings.LogSaveMethod != LogSaveMethod)
                changes.Add("LogSaveMethod");
            if (_originalSettings.LogRetentionDays != LogRetentionDays)
                changes.Add("LogRetentionDays");
            if (_originalSettings.LogExportPath != LogExportPath)
                changes.Add("LogExportPath");
            if (_originalSettings.AutoCleanLog != AutoCleanLog)
                changes.Add("AutoCleanLog");
            if (_originalSettings.FactoryCode != FactoryCode)
                changes.Add("FactoryCode");
            if (_originalSettings.ProductionLine != ProductionLine)
                changes.Add("ProductionLine");
            if (_originalSettings.DbBackupRoot != DbBackupRoot)
                changes.Add("DbBackupRoot");
            if (_originalSettings.ImageRetentionCount != ImageRetentionCount)
                changes.Add("ImageRetentionCount");
            if (_originalSettings.AutoCleanImages != AutoCleanImages)
                changes.Add("AutoCleanImages");

            return changes;
        }
    }
}