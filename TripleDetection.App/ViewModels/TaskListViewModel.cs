using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories;
using TripleDetection.Services;

namespace TripleDetection.ViewModels
{
    public class TaskListViewModel : INotifyPropertyChanged
    {
        private readonly TaskService _taskService;
        private readonly ProductService _productService;
        private string _queryName = "";
        private int? _queryProductId;
        private int? _queryStatus;
        private DateTime? _queryProductionDateFrom;
        private DateTime? _queryProductionDateTo;
        private string _queryBatchNumber = "";
        private int _pageIndex = 0;
        private int _pageSize = 20;
        private int _totalCount = 0;
        private int _totalPages = 0;
        private Data.Entities.Task _selectedTask;

        public ObservableCollection<Data.Entities.Task> Tasks { get; } = new ObservableCollection<Data.Entities.Task>();
        public ObservableCollection<Product> Products { get; } = new ObservableCollection<Product>();

        public string QueryName
        {
            get => _queryName;
            set { _queryName = value; OnPropertyChanged(); }
        }

        public int? QueryProductId
        {
            get => _queryProductId;
            set { _queryProductId = value; OnPropertyChanged(); }
        }

        public int? QueryStatus
        {
            get => _queryStatus;
            set { _queryStatus = value; OnPropertyChanged(); }
        }

        public DateTime? QueryProductionDateFrom
        {
            get => _queryProductionDateFrom;
            set { _queryProductionDateFrom = value; OnPropertyChanged(); }
        }

        public DateTime? QueryProductionDateTo
        {
            get => _queryProductionDateTo;
            set { _queryProductionDateTo = value; OnPropertyChanged(); }
        }

        public string QueryBatchNumber
        {
            get => _queryBatchNumber;
            set { _queryBatchNumber = value; OnPropertyChanged(); }
        }

        public int PageIndex
        {
            get => _pageIndex;
            set { _pageIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentPageDisplay)); }
        }

        public int PageSize
        {
            get => _pageSize;
            set { _pageSize = value; OnPropertyChanged(); }
        }

        public int TotalCount
        {
            get => _totalCount;
            set { _totalCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPagesDisplay)); }
        }

        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPagesDisplay)); OnPropertyChanged(nameof(HasNextPage)); OnPropertyChanged(nameof(HasPreviousPage)); }
        }

        public string TotalPagesDisplay => $"共 {TotalCount} 条";
        public string CurrentPageDisplay => $"{PageIndex + 1} / {TotalPages} 页";
        public bool HasNextPage => PageIndex < TotalPages - 1;
        public bool HasPreviousPage => PageIndex > 0;

        public Data.Entities.Task SelectedTask
        {
            get => _selectedTask;
            set { _selectedTask = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public TaskListViewModel()
        {
            _taskService = new TaskService();
            _productService = new ProductService();
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
            _taskService.Delete(id, "admin", SessionManager.CurrentUserId);
            Search();
        }

        public void ApproveTask(int id)
        {
            _taskService.Approve(id, "admin", SessionManager.CurrentUserId);
            Search();
        }

        public void OpenEditWindow(Data.Entities.Task task = null)
        {
            var editVm = new TaskEditViewModel(task);
            var editWindow = new Views.TaskEditWindow { DataContext = editVm };
            editWindow.Owner = Application.Current.MainWindow;
            if (editWindow.ShowDialog() == true)
            {
                Search();
            }
        }
    }
}