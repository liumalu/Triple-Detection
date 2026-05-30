using System.Windows;
using TripleDetection.ViewModels;
using TripleDetection.ViewModels.Auth;

namespace TripleDetection.Views.Auth
{
    public partial class UserEditWindow : Window
    {
        public UserEditWindow()
        {
            InitializeComponent();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserEditViewModel vm)
            {
                // Password is handled separately since PasswordBox doesn't support binding
                vm.Password = txtPassword.Password;

                vm.RequestClose += (s, result) =>
                {
                    if (result)
                    {
                        DialogResult = true;
                    }
                    else
                    {
                        DialogResult = false;
                    }
                    Close();
                };
                vm.Save();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserEditViewModel vm)
            {
                vm.Cancel();
            }
        }
    }
}