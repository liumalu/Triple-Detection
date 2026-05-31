using System;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm;
using TripleDetection.Domain;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Entities.Queries;
using TripleDetection.Application.Services;

namespace TripleDetection.Presentation.ViewModels.Production
{
    public partial class TaskListViewModel : ObservableObject
    {
        private readonly ITaskService _taskService;
        private readonly IProductService _productService;

        [ObservableProperty] private string _queryName = "";
        [ObservableProperty] private int? _queryProductId;
        [ObservableProperty] private int? _queryStatus;
        [ObservableProperty] private DateTime? _queryProductionDateFrom;
        [ObservableProperty] private DateTime? _queryProductionDateTo;
        [ObservableProperty] private string _queryBatchNumber = "";
        [ObservableProperty] private int _pageIndex = 0;
        [ObservableProperty] private int _pageSize = 20;
        [ObservableProperty] private int _totalCount = 0;
        [ObservableProperty] private int _totalPages = 0;
        [ObservableProperty] private ProdTask? _selectedTask;

        public ObservableCollection<ProdTask> Tasks { get; } = new ObservableCollection<ProdTask>();
        public ObservableCollection<Product> Products { get; } = new ObservableCollection<Product>();

        partial void OnPageIndexChanged(int value)
        {
            OnPropertyChanged(nameof(CurrentPageDisplay));
        }

        partial void OnTotalCountChanged(int value)
        {
            OnPropertyChanged(nameof(TotalPagesDisplay));
        }

        partial void OnTotalPagesChanged(int value)
        {
            OnPropertyChanged(nameof(TotalPagesDisplay));
            OnPropertyChanged(nameof(HasNextPage));
            OnPropertyChanged(nameof(HasPreviousPage));
        }

        public string TotalPagesDisplay => $"共 {TotalCount} 条";
        public string CurrentPageDisplay => $"{PageIndex + 1} / {TotalPages} 页";
        public bool HasNextPage => PageIndex < TotalPages - 1;
        public bool HasPreviousPage => PageIndex > 0;

        public TaskListViewModel(ITaskService taskService, IProductService productService)
        {
            _taskService = taskService;
            _productService = productService;
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
            Search();
        }

        public void OpenEditWindow(ProdTask? task = null)
        {
            var editVm = new TaskEditViewModel(task, _taskService, _productService);
            var editWindow = new Views.Production.TaskEditWindow { DataContext = editVm };
            editWindow.Owner = System.Windows.Application.Current.MainWindow;
            if (editWindow.ShowDialog() == true)
            {
                Search();
            }
        }
    }
}