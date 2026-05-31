using System;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm;
using TripleDetection.Domain.Entities;
using TripleDetection.Application.Services;

namespace TripleDetection.Presentation.ViewModels.Production
{
    public class ProductListViewModel : ObservableObject
    {
        private readonly IProductService _productService;

        [ObservableProperty] private string _queryCode = "";
        [ObservableProperty] private string _queryName = "";
        [ObservableProperty] private int? _queryStatus;
        [ObservableProperty] private DateTime? _queryCreateAtFrom;
        [ObservableProperty] private DateTime? _queryCreateAtTo;
        [ObservableProperty] private int _pageIndex = 0;
        [ObservableProperty] private int _pageSize = 20;
        [ObservableProperty] private int _totalCount = 0;
        [ObservableProperty] private int _totalPages = 0;
        [ObservableProperty] private Product _selectedProduct;

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

        public ProductListViewModel(IProductService productService)
        {
            _productService = productService;
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
            _productService.Delete(id, "admin", SessionManager.CurrentUserId);
            Search();
        }

        public void OpenEditWindow(Product product = null)
        {
            var editVm = new ProductEditViewModel(product, _productService);
            var editWindow = new Views.Production.ProductEditWindow { DataContext = editVm };
            editWindow.Owner = Application.Current.MainWindow;
            if (editWindow.ShowDialog() == true)
            {
                Search();
            }
        }
    }
}