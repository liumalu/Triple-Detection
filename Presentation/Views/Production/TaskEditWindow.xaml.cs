using System.Windows;
using TripleDetection.Presentation.ViewModels;
using TripleDetection.Presentation.ViewModels.Production;

namespace TripleDetection.Presentation.Views.Production
{
    public partial class TaskEditWindow : Window
    {
        public TaskEditWindow()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                if (DataContext is TaskEditViewModel vm)
                {
                    vm.RequestClose += (sender, result) =>
                    {
                        DialogResult = result;
                        Close();
                    };
                }
            };
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TaskEditViewModel vm)
            {
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