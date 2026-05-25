using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using TripleDetection.Data.Entities;
using TripleDetection.Services;

namespace TripleDetection.ViewModels
{
    public class TaskEditViewModel : INotifyPropertyChanged
    {
        private readonly TaskService _taskService;
        private readonly ProductService _productService;
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
            set { _isEditMode = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); ErrorMessage = ""; }
        }

        public int ProductId
        {
            get => _productId;
            set
            {
                if (_productId != value)
                {
                    _productId = value;
                    OnPropertyChanged();
                    CalculateExpirationDate();
                    ErrorMessage = "";
                }
            }
        }

        public TaskStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public DateTime ProductionDate
        {
            get => _productionDate;
            set
            {
                if (_productionDate != value)
                {
                    _productionDate = value;
                    OnPropertyChanged();
                    CalculateExpirationDate();
                }
            }
        }

        public DateTime? ExpirationDate
        {
            get => _expirationDate;
            set { _expirationDate = value; OnPropertyChanged(); }
        }

        public string BatchNumber
        {
            get => _batchNumber;
            set { _batchNumber = value; OnPropertyChanged(); ErrorMessage = ""; }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public string WindowTitle => IsEditMode ? "编辑任务" : "新增任务";

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<bool> RequestClose;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public TaskEditViewModel(Data.Entities.Task task = null)
        {
            _taskService = new TaskService();
            _productService = new ProductService();
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

            var task = new Data.Entities.Task
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
                task.CreateBy = "admin"; // preserve original creator
                task.CreateAt = DateTime.Now;
                _taskService.Update(task, "admin");
            }
            else
            {
                _taskService.Create(task, "admin");
            }

            RequestClose?.Invoke(this, true);
        }

        public void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }
    }
}