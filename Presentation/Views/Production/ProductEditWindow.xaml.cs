using System.Windows;
using TripleDetection.Presentation.ViewModels;
using TripleDetection.Presentation.ViewModels.Production;

namespace TripleDetection.Presentation.Views.Production
{
    public partial class ProductEditWindow : Window
    {
        public ProductEditWindow()
        {
            InitializeComponent();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProductEditViewModel vm)
            {
                vm.BrowseSolFile();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProductEditViewModel vm)
            {
                vm.RequestClose += (s, result) =>
                {
                    if (result)
                    {
                        DialogResult = true;
                        MessageBox.Show("保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                        DialogResult = false;
                    Close();
                };
                vm.Save();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProductEditViewModel vm)
            {
                vm.Cancel();
            }
        }
    }
}