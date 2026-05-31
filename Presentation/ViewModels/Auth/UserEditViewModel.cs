using System;
using System.Collections.ObjectModel;
using System.Windows;
using Prism.Mvvm;
using TripleDetection.Domain.Entities;
using TripleDetection.Application.Services;
using TripleDetection.Domain;

namespace TripleDetection.Presentation.ViewModels.Auth
{
    public class UserEditViewModel : BindableBase
    {
        private readonly IUserService _userService;
        private bool _isEditMode;
        private string _originalUsername = "";
        private string _username = "";
        private string _realName = "";
        private string _password = "";
        private string _role = "Operator";
        private bool _isEnabled = true;
        private string _errorMessage = "";

        public ObservableCollection<string> Roles { get; } = new ObservableCollection<string>
        {
            "Admin",
            "Supervisor",
            "Operator",
            "Viewer"
        };

        public bool IsEditMode
        {
            get => _isEditMode;
            private set => SetProperty(ref _isEditMode, value);
        }

        public string Username
        {
            get => _username;
            set { if (SetProperty(ref _username, value)) ErrorMessage = ""; }
        }

        public string RealName
        {
            get => _realName;
            set { if (SetProperty(ref _realName, value)) ErrorMessage = ""; }
        }

        public string Password
        {
            get => _password;
            set { if (SetProperty(ref _password, value)) ErrorMessage = ""; }
        }

        public string Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string WindowTitle => IsEditMode ? "编辑用户" : "新增用户";

        public event EventHandler<bool> RequestClose;

        public UserEditViewModel(User user = null, IUserService userService = null)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _isEditMode = user != null;

            if (user != null)
            {
                _originalUsername = user.Username;
                _username = user.Username;
                _realName = user.RealName;
                _password = user.Password;
                _role = user.Role;
                _isEnabled = user.IsEnabled;
            }
        }

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
                if (Username != _originalUsername)
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