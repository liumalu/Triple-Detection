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
    public class UserManagementViewModel : INotifyPropertyChanged
    {
        private readonly UserService _userService;
        private string _queryUsername = "";
        private string _queryRole = "";
        private string _queryStatusText = "";
        private int _pageIndex = 0;
        private int _pageSize = 20;
        private int _totalCount = 0;
        private int _totalPages = 0;
        private User _selectedUser;

        public ObservableCollection<User> Users { get; } = new ObservableCollection<User>();

        public string QueryUsername
        {
            get => _queryUsername;
            set { _queryUsername = value; OnPropertyChanged(); }
        }

        public string QueryRole
        {
            get => _queryRole;
            set { _queryRole = value; OnPropertyChanged(); }
        }

        public string QueryStatusText
        {
            get => _queryStatusText;
            set { _queryStatusText = value; OnPropertyChanged(); }
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

        public User SelectedUser
        {
            get => _selectedUser;
            set { _selectedUser = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public UserManagementViewModel()
        {
            _userService = new UserService();
        }

        public void Search()
        {
            var query = new UserQuery
            {
                Username = QueryUsername,
                Role = QueryRole,
                StatusText = QueryStatusText,
                PageIndex = PageIndex,
                PageSize = PageSize,
                SortBy = "CreateAt",
                SortDescending = true
            };

            var result = _userService.Query(query);
            Users.Clear();
            foreach (var item in result.Items)
            {
                Users.Add(item);
            }
            TotalCount = result.TotalCount;
            TotalPages = result.TotalPages;
            PageIndex = result.PageIndex;
        }

        public void Reset()
        {
            QueryUsername = "";
            QueryRole = "";
            QueryStatusText = "";
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

        public void DeleteUser(string username)
        {
            try
            {
                _userService.Delete(username, "admin", SessionManager.CurrentUserId);
                Search();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void EnableUser(string username)
        {
            try
            {
                _userService.Enable(username, "admin", SessionManager.CurrentUserId);
                Search();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启用失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void DisableUser(string username)
        {
            try
            {
                _userService.Disable(username, "admin", SessionManager.CurrentUserId);
                Search();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"禁用失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LockUser(string username)
        {
            try
            {
                _userService.Lock(username, "admin", SessionManager.CurrentUserId);
                Search();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"锁定失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UnlockUser(string username)
        {
            try
            {
                _userService.Unlock(username, "admin", SessionManager.CurrentUserId);
                Search();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解锁失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OpenEditWindow(User user = null)
        {
            var editVm = new UserEditViewModel(user, _userService);
            var editWindow = new Views.UserEditWindow { DataContext = editVm };
            editWindow.Owner = Application.Current.MainWindow;
            if (editWindow.ShowDialog() == true)
            {
                Search();
            }
        }
    }
}