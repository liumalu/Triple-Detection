using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm;
using TripleDetection.Domain;
using TripleDetection.Domain.Entities;
using TripleDetection.Application.Services;
using TripleDetection.Domain.Enums;

namespace TripleDetection.Presentation.ViewModels.Production
{
    public partial class TaskEditViewModel : ObservableObject
    {
        private readonly ITaskService _taskService;
        private readonly IProductService _productService;

        [ObservableProperty] private bool _isEditMode;
        [ObservableProperty] private int _taskId;
        [ObservableProperty] private string _name = "";
        [ObservableProperty] private int _productId;
        [ObservableProperty] private TaskStatus _status = TaskStatus.Pending;
        [ObservableProperty] private DateTime _productionDate = DateTime.Today;
        [ObservableProperty] private DateTime? _expirationDate;
        [ObservableProperty] private string _batchNumber = "";
        [ObservableProperty] private string _errorMessage = "";

        public ObservableCollection<Product> Products { get; } = new ObservableCollection<Product>();

        public ObservableCollection<TaskStatus> Statuses { get; } = new ObservableCollection<TaskStatus>
        {
            TaskStatus.Pending,
            TaskStatus.Approved,
            TaskStatus.Running,
            TaskStatus.Completed
        };

        public string WindowTitle => IsEditMode ? "编辑任务" : "新增任务";

        public event EventHandler<bool> RequestClose;

        public TaskEditViewModel(ProdTask task, ITaskService taskService, IProductService productService)
        {
            _taskService = taskService;
            _productService = productService;
            LoadProducts();

            if (task != null)
            {
                IsEditMode = true;
                TaskId = task.Id;
                Name = task.Name;
                ProductId = task.ProductId;
                Status = task.Status;
                ProductionDate = task.ProductionDate;
                ExpirationDate = task.ExpirationDate;
                BatchNumber = task.BatchNumber ?? "";
                CalculateExpirationDate();
            }
            else
            {
                CalculateExpirationDate();
            }
        }

        private void LoadProducts()
        {
            Products.Clear();
            foreach (var product in _productService.GetAll())
            {
                // 只加载启用状态且方案文件存在的产品
                if (product.Status != ProductStatus.Active)
                    continue;
                if (string.IsNullOrWhiteSpace(product.SolFilePath))
                    continue;
                if (!System.IO.File.Exists(product.SolFilePath))
                    continue;
                Products.Add(product);
            }
        }

        partial void OnProductIdChanged(int value)
        {
            CalculateExpirationDate();
            ErrorMessage = "";
        }

        partial void OnProductionDateChanged(DateTime value)
        {
            CalculateExpirationDate();
        }

        partial void OnNameChanged(string value) => ErrorMessage = "";
        partial void OnBatchNumberChanged(string value) => ErrorMessage = "";

        private void CalculateExpirationDate()
        {
            if (ProductId <= 0)
            {
                ExpirationDate = null;
                return;
            }

            var product = _productService.GetById(ProductId);
            if (product == null)
            {
                ExpirationDate = null;
                return;
            }

            switch (product.ValidType)
            {
                case ValidType.Year:
                    ExpirationDate = ProductionDate.AddYears(product.ValidPeriod);
                    break;
                case ValidType.Month:
                    ExpirationDate = ProductionDate.AddMonths(product.ValidPeriod);
                    break;
                case ValidType.Day:
                    ExpirationDate = ProductionDate.AddDays(product.ValidPeriod);
                    break;
                default:
                    ExpirationDate = null;
                    break;
            }
        }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "任务名称不能为空";
                return false;
            }
            if (ProductId <= 0)
            {
                ErrorMessage = "请选择关联产品";
                return false;
            }
            if (string.IsNullOrWhiteSpace(BatchNumber))
            {
                ErrorMessage = "批次号不能为空";
                return false;
            }
            if (ProductionDate == default)
            {
                ErrorMessage = "生产日期不能为空";
                return false;
            }

            return true;
        }

        public void Save()
        {
            if (!Validate())
                return;

            var task = new ProdTask
            {
                Name = Name,
                ProductId = ProductId,
                Status = Status,
                ProductionDate = ProductionDate,
                ExpirationDate = ExpirationDate,
                BatchNumber = BatchNumber
            };

            if (IsEditMode)
            {
                task.Id = TaskId;
                task.CreateBy = SessionManager.CurrentUserName ?? "Unknown";
                task.CreateAt = DateTime.Now;
                _taskService.Update(task, SessionManager.CurrentUserName ?? "Unknown", SessionManager.CurrentUserId);
            }
            else
            {
                _taskService.Create(task, SessionManager.CurrentUserName ?? "Unknown", SessionManager.CurrentUserId);
            }

            RequestClose?.Invoke(this, true);
        }

        public void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }
    }
}