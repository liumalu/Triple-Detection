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
    public class ProductListViewModel : INotifyPropertyChanged
    {
        private readonly ProductService _productService;
        private string _queryCode = "";
        private string _queryName = "";
        private int? _queryStatus;
        private DateTime? _queryCreateAtFrom;
        private DateTime? _queryCreateAtTo;
        private int _pageIndex = 0;
        private int _pageSize = 20;
        private int _totalCount = 0;
        private int _totalPages = 0;
        private Product _selectedProduct;

        public ObservableCollection<Product> Products { get; } = new ObservableCollection<Product>();

        public string QueryCode
        {
            get => _queryCode;
            set { _queryCode = value; OnPropertyChanged(); }
        }

        public string QueryName
        {
            get => _queryName;
            set { _queryName = value; OnPropertyChanged(); }
        }

        public int? QueryStatus
        {
            get => _queryStatus;
            set { _queryStatus = value; OnPropertyChanged(); }
        }

        public DateTime? QueryCreateAtFrom
        {
            get => _queryCreateAtFrom;
            set { _queryCreateAtFrom = value; OnPropertyChanged(); }
        }

        public DateTime? QueryCreateAtTo
        {
            get => _queryCreateAtTo;
            set { _queryCreateAtTo = value; OnPropertyChanged(); }
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

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ProductListViewModel()
        {
            _productService = new ProductService();
        }

        public void Search()
        {
            var query = new ProductQuery
            {
                Code = QueryCode,
                Name = QueryName,
                Status = QueryStatus,
                CreateAtFrom = QueryCreateAtFrom,
                CreateAtTo = QueryCreateAtTo,
                PageIndex = PageIndex,
                PageSize = PageSize,
                SortBy = "CreateAt",
                SortDescending = true
            };

            var result = _productService.Query(query);
            Products.Clear();
            foreach (var item in result.Items)
            {
                Products.Add(item);
            }
            TotalCount = result.TotalCount;
            TotalPages = result.TotalPages;
            PageIndex = result.PageIndex;
        }

        public void Reset()
        {
            QueryCode = "";
            QueryName = "";
            QueryStatus = null;
            QueryCreateAtFrom = null;
            QueryCreateAtTo = null;
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

        public void DeleteProduct(int id)
        {
            _productService.Delete(id, "admin");
            Search();
        }

        public void OpenEditWindow(Product product = null)
        {
            var editVm = new ProductEditViewModel(product);
            var editWindow = new Views.ProductEditWindow { DataContext = editVm };
            editWindow.Owner = Application.Current.MainWindow;
            if (editWindow.ShowDialog() == true)
            {
                Search();
            }
        }
    }
}