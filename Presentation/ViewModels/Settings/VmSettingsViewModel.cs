using System;
using Newtonsoft.Json;
using Prism.Mvvm;
using Prism.Commands;
using TripleDetection.Application.Services;
using TripleDetection.Application.SettingsServices;
using TripleDetection.Domain;
using TripleDetection.Presentation.Models;

namespace TripleDetection.Presentation.ViewModels.Settings
{
    public class VmSettingsViewModel : ViewModelBase
    {
        private readonly VmSettingsService _service;
        private readonly IAuditLogService _auditLogService;
        private VmSettings _originalSettings;

        private string _vmInstallPath = string.Empty;
        public string VmInstallPath
        {
            get => _vmInstallPath;
            set => SetProperty(ref _vmInstallPath, value);
        }

        public VmSettingsViewModel(VmSettingsService service, IAuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
            BrowseCommand = new DelegateCommand(ExecuteBrowse);
            SaveCommand = new DelegateCommand(ExecuteSave, CanExecuteSave);
        }

        public DelegateCommand BrowseCommand { get; }
        public DelegateCommand SaveCommand { get; }

        public void Load()
        {
            var settings = _service.Load();
            _originalSettings = new VmSettings { VmInstallPath = settings.VmInstallPath };
            VmInstallPath = settings.VmInstallPath;
        }

        private void ExecuteBrowse()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "选择 VisionMaster 安装目录";
                dialog.SelectedPath = VmInstallPath;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    VmInstallPath = dialog.SelectedPath;
                }
            }
        }

        private bool CanExecuteSave()
        {
            return !string.IsNullOrWhiteSpace(VmInstallPath);
        }

        private void ExecuteSave()
        {
            try
            {
                var newSettings = new VmSettings { VmInstallPath = VmInstallPath };
                _service.Save(newSettings);

                if (DetectChanges())
                {
                    var details = JsonConvert.SerializeObject(new { category = "VmSettings", changes = new[] { "VmInstallPath" } });
                    _auditLogService.Log(SessionManager.CurrentUserId, "SETTINGS_UPDATE", "VmSettings", 0, details);
                }

                var result = System.Windows.MessageBox.Show(
                    "保存成功",
                    "提示",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                Load();
                SaveCommand.RaiseCanExecuteChanged();
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

        private bool DetectChanges()
        {
            return _originalSettings != null &&
                   _originalSettings.VmInstallPath != VmInstallPath;
        }
    }
}