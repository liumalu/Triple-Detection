using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Prism.Mvvm;
using TripleDetection.Domain.Entities;
using TripleDetection.Application.Services;
using TripleDetection.Domain;

namespace TripleDetection.Presentation.ViewModels.Auth
{
    public class UserEditViewModel : ViewModelBase
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

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string OriginalUsername
        {
            get => _originalUsername;
            set => SetProperty(ref _originalUsername, value);
        }

        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                    ErrorMessage = "";
            }
        }

        public string RealName
        {
            get => _realName;
            set
            {
                if (SetProperty(ref _realName, value))
                    ErrorMessage = "";
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                    ErrorMessage = "";
            }
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

        public ObservableCollection<string> Roles { get; } = new ObservableCollection<string>
        {
            "Admin",
            "Supervisor",
            "Operator",
            "Viewer"
        };

        public string WindowTitle => IsEditMode ? "编辑用户" : "新增用户";

        public event EventHandler<bool> RequestClose;

        public UserEditViewModel(User user = default(User), IUserService userService = default(IUserService))
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            IsEditMode = !EqualityComparer<User>.Default.Equals(user, default(User));

            if (!EqualityComparer<User>.Default.Equals(user, default(User)))
            {
                OriginalUsername = user.Username;
                Username = user.Username;
                RealName = user.RealName;
                Password = user.Password;
                Role = user.Role;
                IsEnabled = user.IsEnabled;
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
                    user.CreateBy = SessionManager.CurrentUserName ?? "Unknown";
                    _userService.Update(user, SessionManager.CurrentUserName ?? "Unknown", SessionManager.CurrentUserId);
                }
                else
                {
                    user.CreateAt = DateTime.Now;
                    user.CreateBy = SessionManager.CurrentUserName ?? "Unknown";
                    _userService.Create(user, SessionManager.CurrentUserName ?? "Unknown", SessionManager.CurrentUserId);
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
