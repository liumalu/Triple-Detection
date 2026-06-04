using System.Windows;
using System.Windows.Controls;
using TripleDetection.Presentation.ViewModels;
using TripleDetection.Presentation.ViewModels.Production;
using TripleDetection.Application.Services;

namespace TripleDetection.Presentation.Views.Production
{
    public partial class ProductListView : UserControl
    {
        private ProductListViewModel ViewModel => (ProductListViewModel)DataContext;

        public ProductListView(IProductService productService, IAuditLogService auditLogService)
        {
            InitializeComponent();
            DataContext = new ProductListViewModel(productService, auditLogService);
            Loaded += (s, e) => ViewModel.Search();
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            var vm = ViewModel;
            var conditions = new System.Text.StringBuilder();
            conditions.AppendLine("查询条件确认");
            conditions.AppendLine("-------------");
            if (!string.IsNullOrWhiteSpace(vm.QueryCode))
                conditions.AppendLine($"产品编码: {vm.QueryCode}");
            if (!string.IsNullOrWhiteSpace(vm.QueryName))
                conditions.AppendLine($"产品名称: {vm.QueryName}");
            if (vm.QueryStatus.HasValue)
                conditions.AppendLine($"状态: {(vm.QueryStatus == 1 ? "启用" : "停用")}");
            if (vm.QueryCreateAtFrom.HasValue)
                conditions.AppendLine($"创建日期从: {vm.QueryCreateAtFrom:yyyy-MM-dd}");
            if (vm.QueryCreateAtTo.HasValue)
                conditions.AppendLine($"创建日期至: {vm.QueryCreateAtTo:yyyy-MM-dd}");
            if (string.IsNullOrEmpty(conditions.ToString().Replace("查询条件确认", "").Replace("-------------", "").Trim()))
                conditions.AppendLine("(无查询条件 - 查询全部)");

            var result = MessageBox.Show(conditions.ToString(), "确认查询", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.OK)
            {
                vm.Search();
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Reset();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenEditWindow();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var product = (TripleDetection.Domain.Entities.Product)button.DataContext;
            ViewModel.OpenEditWindow(product);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var product = (TripleDetection.Domain.Entities.Product)button.DataContext;
            var result = MessageBox.Show($"确定要删除产品【{product.Name}】吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                ViewModel.DeleteProduct(product.Id);
            }
        }

        private void BtnFirstPage_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.FirstPage();
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PreviousPage();
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.NextPage();
        }

        private void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LastPage();
        }
    }
}