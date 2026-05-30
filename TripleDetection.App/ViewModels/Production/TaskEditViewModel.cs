using System;
using System.Collections.ObjectModel;
using System.Windows;
using Prism.Mvvm;
using TripleDetection.Data.Entities;
using TripleDetection.Services;
using TripleDetection.Services.Production;

namespace TripleDetection.ViewModels.Production
{
    public class TaskEditViewModel : BindableBase
    {
        private readonly ITaskService _taskService;
        private readonly IProductService _productService;
        private bool _isEditMode;
        private int _taskId;
        private string _name = "";
        private int _productId;
        private TaskStatus _status = TaskStatus.Pending;
        private DateTime _productionDate = DateTime.Today;
        private DateTime? _expirationDate;
        private string _batchNumber = "";
        private string _errorMessage = "";

        public ObservableCollection<Product> Products { get; } = new ObservableCollection<Product>();

        public ObservableCollection<TaskStatus> Statuses { get; } = new ObservableCollection<TaskStatus>
        {
            TaskStatus.Pending,
            TaskStatus.Approved,
            TaskStatus.Running,
            TaskStatus.Completed
        };

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string Name
        {
            get => _name;
            set { if (SetProperty(ref _name, value)) ErrorMessage = ""; }
        }

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

        public TaskStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public DateTime ProductionDate
        {
            get => _productionDate;
            set
            {
                if (SetProperty(ref _productionDate, value))
                {
                    CalculateExpirationDate();
                }
            }
        }

        public DateTime? ExpirationDate
        {
            get => _expirationDate;
            set => SetProperty(ref _expirationDate, value);
        }

        public string BatchNumber
        {
            get => _batchNumber;
            set { if (SetProperty(ref _batchNumber, value)) ErrorMessage = ""; }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string WindowTitle => IsEditMode ? "编辑任务" : "新增任务";

        public event EventHandler<bool> RequestClose;

        public TaskEditViewModel(Data.Entities.ProdTask task = null)
            : this(task, new TaskService(), new ProductService())
        {
        }

        public TaskEditViewModel(Data.Entities.ProdTask task, ITaskService taskService, IProductService productService)
        {
            _taskService = taskService;
            _productService = productService;
            LoadProducts();

            if (task != null)
            {
                IsEditMode = true;
                _taskId = task.Id;
                _name = task.Name;
                _productId = task.ProductId;
                _status = task.Status;
                _productionDate = task.ProductionDate;
                _expirationDate = task.ExpirationDate;
                _batchNumber = task.BatchNumber ?? "";
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

            var task = new Data.Entities.ProdTask
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
                task.Id = _taskId;
                task.CreateBy = "admin";
                task.CreateAt = DateTime.Now;
                _taskService.Update(task, "admin", SessionManager.CurrentUserId);
            }
            else
            {
                _taskService.Create(task, "admin", SessionManager.CurrentUserId);
            }

            RequestClose?.Invoke(this, true);
        }

        public void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }
    }
}
