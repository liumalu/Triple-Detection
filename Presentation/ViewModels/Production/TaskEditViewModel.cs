using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using Prism.Mvvm;
using TripleDetection.Domain;
using TripleDetection.Domain.Entities;
using TripleDetection.Application.Services;
using TripleDetection.Domain.Enums;

namespace TripleDetection.Presentation.ViewModels.Production
{
    public class TaskEditViewModel : ViewModelBase
    {
        private readonly ITaskService _taskService;
        private readonly IProductService _productService;
        private readonly IAuditLogService _auditLogService;

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        private int _taskId;
        public int TaskId
        {
            get => _taskId;
            set => SetProperty(ref _taskId, value);
        }

        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                    ErrorMessage = "";
            }
        }

        private int _productId;
        public int ProductId
        {
            get => _productId;
            set
            {
                if (SetProperty(ref _productId, value))
                {
                    CalculateExpirationDate();
                    ErrorMessage = "";
                }
            }
        }

        private TaskStatus _status = TaskStatus.Pending;
        private TaskStatus _originalStatus;
        public TaskStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private DateTime _productionDate = DateTime.Today;
        public DateTime ProductionDate
        {
            get => _productionDate;
            set
            {
                if (SetProperty(ref _productionDate, value))
                    CalculateExpirationDate();
            }
        }

        private DateTime? _expirationDate = default(DateTime?);
        public DateTime? ExpirationDate
        {
            get => _expirationDate;
            set => SetProperty(ref _expirationDate, value);
        }

        private string _batchNumber = "";
        public string BatchNumber
        {
            get => _batchNumber;
            set
            {
                if (SetProperty(ref _batchNumber, value))
                    ErrorMessage = "";
            }
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

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

        public TaskEditViewModel(ProdTask task, ITaskService taskService, IProductService productService, IAuditLogService auditLogService)
        {
            _taskService = taskService;
            _productService = productService;
            _auditLogService = auditLogService;
            LoadProducts();

            if (task != null)
            {
                IsEditMode = true;
                TaskId = task.Id;
                Name = task.Name;
                ProductId = task.ProductId;
                _originalStatus = task.Status;
                Status = task.Status;
                ProductionDate = task.ProductionDate;
                ExpirationDate = task.ExpirationDate;
                BatchNumber = task.BatchNumber ?? "";
                CalculateExpirationDate();
            }
            else
            {
                _originalStatus = TaskStatus.Pending;
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

            var currentUserId = SessionManager.CurrentUserId;
            var currentUserName = SessionManager.CurrentUserName ?? "Unknown";

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
                task.CreateBy = currentUserName;
                task.CreateAt = DateTime.Now;
                _taskService.Update(task, currentUserName, currentUserId);

                // Audit log for TASK_UPDATE
                _auditLogService?.Log(currentUserId, "TASK_UPDATE", "ProdTask", task.Id,
                    JsonConvert.SerializeObject(new { taskId = task.Id, taskName = task.Name, productId = task.ProductId }));

                // Detect status changes for TASK_APPROVE, TASK_START, TASK_COMPLETE
                if (_originalStatus == TaskStatus.Pending && Status == TaskStatus.Approved)
                {
                    _auditLogService?.Log(currentUserId, "TASK_APPROVE", "ProdTask", task.Id,
                        JsonConvert.SerializeObject(new { taskId = task.Id, taskName = task.Name, fromStatus = "Pending", toStatus = "Approved" }));
                }
                else if (_originalStatus == TaskStatus.Approved && Status == TaskStatus.Running)
                {
                    _auditLogService?.Log(currentUserId, "TASK_START", "ProdTask", task.Id,
                        JsonConvert.SerializeObject(new { taskId = task.Id, taskName = task.Name }));
                }
                else if (_originalStatus == TaskStatus.Running && Status == TaskStatus.Completed)
                {
                    _auditLogService?.Log(currentUserId, "TASK_COMPLETE", "ProdTask", task.Id,
                        JsonConvert.SerializeObject(new { taskId = task.Id, taskName = task.Name }));
                }
            }
            else
            {
                _taskService.Create(task, currentUserName, currentUserId);

                // Audit log for TASK_CREATE
                _auditLogService?.Log(currentUserId, "TASK_CREATE", "ProdTask", task.Id,
                    JsonConvert.SerializeObject(new { taskId = task.Id, taskName = task.Name, productId = task.ProductId }));
            }

            RequestClose?.Invoke(this, true);
        }

        public void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }
    }
}