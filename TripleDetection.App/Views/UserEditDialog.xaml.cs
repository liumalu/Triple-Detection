using System;
using System.Windows;
using System.Windows.Controls;
using TripleDetection.Models;

namespace TripleDetection.Views
{
    public partial class UserEditDialog : Window
    {
        private readonly User _originalUser;
        private readonly bool _isEditMode;

        public User User { get; private set; }

        public UserEditDialog() : this(null)
        {
        }

        public UserEditDialog(User user)
        {
            InitializeComponent();

            _isEditMode = user != null;
            _originalUser = user;

            if (_isEditMode)
            {
                Title = "编辑用户";
                txtUsername.Text = user.Username;
                txtUsername.IsEnabled = false;
                txtRealName.Text = user.RealName;
                txtPassword.Password = user.Password;

                foreach (ComboBoxItem item in cboRole.Items)
                {
                    if (item.Tag?.ToString() == user.Role)
                    {
                        cboRole.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                Title = "添加用户";
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var username = txtUsername.Text.Trim();
            var realName = txtRealName.Text.Trim();
            var password = txtPassword.Password;
            var role = (cboRole.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Operator";

            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("请输入用户名", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入密码", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            User = _isEditMode ? _originalUser : new User();
            User.Username = username;
            User.RealName = realName;
            User.Password = password;
            User.Role = role;

            if (!_isEditMode)
            {
                User.CreatedAt = DateTime.Now;
                User.IsEnabled = true;
                User.IsLocked = false;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}