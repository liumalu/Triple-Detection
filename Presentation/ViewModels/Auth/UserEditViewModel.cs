using System;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm;
using TripleDetection.Domain.Entities;
using TripleDetection.Application.Services;
using TripleDetection.Domain;

namespace TripleDetection.Presentation.ViewModels.Auth
{
    public partial class UserEditViewModel : ObservableObject
    {
        private readonly IUserService _userService;

        [ObservableProperty] private bool _isEditMode;
        [ObservableProperty] private string _originalUsername = "";
        [ObservableProperty] private string _username = "";
        [ObservableProperty] private string _realName = "";
        [ObservableProperty] private string _password = "";
        [ObservableProperty] private string _role = "Operator";
        [ObservableProperty] private bool _isEnabled = true;
        [ObservableProperty] private string _errorMessage = "";

        public ObservableCollection<string> Roles { get; } = new ObservableCollection<string>
        {
            "Admin",
            "Supervisor",
            "Operator",
            "Viewer"
        };

        public string WindowTitle => IsEditMode ? "编辑用户" : "新增用户";

        public event EventHandler<bool> RequestClose;

        public UserEditViewModel(User user = null, IUserService userService = null)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            IsEditMode = user != null;

            if (user != null)
            {
                OriginalUsername = user.Username;
                Username = user.Username;
                RealName = user.RealName;
                Password = user.Password;
                Role = user.Role;
                IsEnabled = user.IsEnabled;
            }
        }

        partial void OnUsernameChanged(string value) => ErrorMessage = "";
        partial void OnRealNameChanged(string value) => ErrorMessage = "";
        partial void OnPasswordChanged(string value) => ErrorMessage = "";

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "用户名不能为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "密码不能为空";
                return false;
            }

            if (Password.Length < 4)
            {
                ErrorMessage = "密码长度至少4位";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Role))
            {
                ErrorMessage = "请选择角色";
                return false;
            }

            // Check duplicate username for new user
            if (!IsEditMode)
            {
                var existing = _userService.GetByUsername(Username);
                if (existing != null)
                {
                    ErrorMessage = "用户名已存在";
                    return false;
                }
            }
            else
            {
                // For edit mode, check if username changed and new username exists
                if (Username != OriginalUsername)
                {
                    var existing = _userService.GetByUsername(Username);
                    if (existing != null)
                    {
                        ErrorMessage = "用户名已存在";
                        return false;
                    }
                }
            }

            return true;
        }

        public void Save()
        {
            if (!Validate())
                return;

            var user = new User
            {
                Username = Username,
                RealName = RealName,
                Password = Password,
                Role = Role,
                IsEnabled = IsEnabled,
                IsLocked = false
            };

            try
            {
                if (IsEditMode)
                {
                    user.CreateAt = DateTime.Now;
                    user.CreateBy = "admin";
                    _userService.Update(user, "admin", SessionManager.CurrentUserId);
                }
                else
                {
                    user.CreateAt = DateTime.Now;
                    user.CreateBy = "admin";
                    _userService.Create(user, "admin", SessionManager.CurrentUserId);
                }

                RequestClose?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        public void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }
    }
}