using System.Windows;
using TripleDetection.ViewModels;

namespace TripleDetection.Views
{
    public partial class TaskEditWindow : Window
    {
        public TaskEditWindow()
        {
            InitializeComponent();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TaskEditViewModel vm)
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
            if (DataContext is TaskEditViewModel vm)
            {
                vm.Cancel();
            }
        }
    }
}