using System;
using System.Collections.ObjectModel;
using System.Windows;
using Prism.Mvvm;
using TripleDetection.Domain;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Entities.Queries;
using TripleDetection.Application.Services;

namespace TripleDetection.Presentation.ViewModels.Production
{
    public partial class TaskListViewModel : ViewModelBase
    {
        private readonly ITaskService _taskService;
        private readonly IProductService _productService;
        private readonly IAuditLogService _auditLogService;

        private string _queryName = "";
        private int? _queryProductId = default(int?);
        private int? _queryStatus = default(int?);
        private DateTime? _queryProductionDateFrom = default(DateTime?);
        private DateTime? _queryProductionDateTo = default(DateTime?);
        private string _queryBatchNumber = "";
        private int _pageIndex = 0;
        private int _pageSize = 20;
        private int _totalCount = 0;
        private int _totalPages = 0;
        private ProdTask _selectedTask = null;

        public string QueryName
        {
            get => _queryName;
            set => SetProperty(ref _queryName, value);
        }

        public int? QueryProductId
        {
            get => _queryProductId;
            set => SetProperty(ref _queryProductId, value);
        }

        public int? QueryStatus
        {
            get => _queryStatus;
            set => SetProperty(ref _queryStatus, value);
        }

        public DateTime? QueryProductionDateFrom
        {
            get => _queryProductionDateFrom;
            set => SetProperty(ref _queryProductionDateFrom, value);
        }

        public DateTime? QueryProductionDateTo
        {
            get => _queryProductionDateTo;
            set => SetProperty(ref _queryProductionDateTo, value);
        }

        public string QueryBatchNumber
        {
            get => _queryBatchNumber;
            set => SetProperty(ref _queryBatchNumber, value);
        }

        public int PageIndex
        {
            get => _pageIndex;
            set
            {
                if (SetProperty(ref _pageIndex, value))
                {
                    OnPropertyChanged(nameof(CurrentPageDisplay));
                }
            }
        }

        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (SetProperty(ref _totalCount, value))
                {
                    OnPropertyChanged(nameof(TotalPagesDisplay));
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set
            {
                if (SetProperty(ref _totalPages, value))
                {
                    OnPropertyChanged(nameof(TotalPagesDisplay));
                    OnPropertyChanged(nameof(HasNextPage));
                    OnPropertyChanged(nameof(HasPreviousPage));
                }
            }
        }

        public ProdTask SelectedTask
        {
            get => _selectedTask;
            set => SetProperty(ref _selectedTask, value);
        }

        public ObservableCollection<ProdTask> Tasks { get; } = new ObservableCollection<ProdTask>();
        public ObservableCollection<Product> Products { get; } = new ObservableCollection<Product>();

        public string TotalPagesDisplay => $"共 {TotalCount} 条";
        public string CurrentPageDisplay => $"{PageIndex + 1} / {TotalPages} 页";
        public bool HasNextPage => PageIndex < TotalPages - 1;
        public bool HasPreviousPage => PageIndex > 0;

        public TaskListViewModel(ITaskService taskService, IProductService productService, IAuditLogService auditLogService)
        {
            _taskService = taskService;
            _productService = productService;
            _auditLogService = auditLogService;
            LoadProducts();
        }

        private void LoadProducts()
        {
            Products.Clear();
            foreach (var product in _productService.GetAll())
            {
                Products.Add(product);
            }
        }

        public void Search()
        {
            var query = new TaskQuery
            {
                Name = QueryName,
                ProductId = QueryProductId,
                Status = QueryStatus,
                ProductionDateFrom = QueryProductionDateFrom,
                ProductionDateTo = QueryProductionDateTo,
                BatchNumber = QueryBatchNumber,
                PageIndex = PageIndex,
                PageSize = PageSize,
                SortBy = "CreateAt",
                SortDescending = true
            };

            var result = _taskService.Query(query);
            Tasks.Clear();
            foreach (var item in result.Items)
            {
                // 填充 ProductName 以便 UI 绑定
                var product = _productService.GetById(item.ProductId);
                item.ProductName = product?.Name ?? "";
                Tasks.Add(item);
            }
            TotalCount = result.TotalCount;
            TotalPages = result.TotalPages;
            PageIndex = result.PageIndex;
        }

        public void Reset()
        {
            QueryName = "";
            QueryProductId = null;
            QueryStatus = null;
            QueryProductionDateFrom = null;
            QueryProductionDateTo = null;
            QueryBatchNumber = "";
            PageIndex = 0;
            Search();
        }

        public void FirstPage()
        {
            PageIndex = 0;
            Search();
        }

        public void PreviousPage()
        {
            if (PageIndex > 0)
            {
                PageIndex--;
                Search();
            }
        }

        public void NextPage()
        {
            if (PageIndex < TotalPages - 1)
            {
                PageIndex++;
                Search();
            }
        }

        public void LastPage()
        {
            PageIndex = TotalPages > 0 ? TotalPages - 1 : 0;
            Search();
        }

        public void DeleteTask(int id)
        {
            _taskService.Delete(id, SessionManager.CurrentUserName ?? "Unknown", SessionManager.CurrentUserId);
            Search();
        }

        public void ApproveTask(int id)
        {
            _taskService.Approve(id, SessionManager.CurrentUserName ?? "Unknown", SessionManager.CurrentUserId);
            var task = _taskService.GetById(id);
            _auditLogService?.Log(SessionManager.CurrentUserId, "TASK_APPROVE", "ProdTask", id,
                Newtonsoft.Json.JsonConvert.SerializeObject(new { taskId = id, taskName = task?.Name }));
            Search();
        }

        public void OpenEditWindow(ProdTask task = null)
        {
            var editVm = new TaskEditViewModel(task, _taskService, _productService, _auditLogService);
            var editWindow = new Views.Production.TaskEditWindow { DataContext = editVm };
            editWindow.Owner = System.Windows.Application.Current.MainWindow;
            if (editWindow.ShowDialog() == true)
            {
                Search();
            }
        }
    }
}