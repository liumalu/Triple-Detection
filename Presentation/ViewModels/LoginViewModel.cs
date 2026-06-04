using System;
using System.Windows.Input;
using Prism.Mvvm;
using Prism.Commands;
using TripleDetection.Application.Services;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain;
using Newtonsoft.Json;

namespace TripleDetection.Presentation.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditLogService;

        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    UsernameHasError = false;
                    ErrorMessage = string.Empty;
                    LoginCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    PasswordHasError = false;
                    ErrorMessage = string.Empty;
                    LoginCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    LoginCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _usernameHasError;
        public bool UsernameHasError
        {
            get => _usernameHasError;
            set => SetProperty(ref _usernameHasError, value);
        }

        private bool _passwordHasError;
        public bool PasswordHasError
        {
            get => _passwordHasError;
            set => SetProperty(ref _passwordHasError, value);
        }

        private string _logoPath = string.Empty;
        public string LogoPath
        {
            get => _logoPath;
            set => SetProperty(ref _logoPath, value);
        }

        public LoginViewModel(IUserService userService, IAuditLogService auditLogService)
        {
            _userService = userService;
            _auditLogService = auditLogService;
            _logoPath = System.Configuration.ConfigurationManager.AppSettings["LoginLogoPath"]
                ?? System.Configuration.ConfigurationManager.AppSettings["SystemLogoPath"];
        }

        private DelegateCommand _loginCommand;
        public DelegateCommand LoginCommand => _loginCommand ?? (_loginCommand = new DelegateCommand(ExecuteLogin, CanExecuteLogin));

        private void ExecuteLogin()
        {
            UsernameHasError = string.IsNullOrWhiteSpace(Username);
            PasswordHasError = string.IsNullOrWhiteSpace(Password);
            if (UsernameHasError || PasswordHasError)
            {
                ErrorMessage = "请输入用户名和密码";
                OnLoginFailed?.Invoke();
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var user = _userService.Authenticate(Username, Password);

                if (user == null)
                {
                    ErrorMessage = "用户名或密码错误";
                    _auditLogService.Log(0, "LOGIN_FAILED", "User", 0,
                        JsonConvert.SerializeObject(new { username = Username, reason = "invalid credentials" }));
                    OnLoginFailed?.Invoke();
                    return;
                }

                if (!user.IsEnabled)
                {
                    ErrorMessage = "账号已被禁用";
                    _auditLogService.Log(0, "LOGIN_FAILED", "User", 0,
                        JsonConvert.SerializeObject(new { username = Username, reason = "account disabled" }));
                    OnLoginFailed?.Invoke();
                    return;
                }

                if (user.IsLocked)
                {
                    ErrorMessage = "账号已被锁定";
                    _auditLogService.Log(0, "LOGIN_FAILED", "User", 0,
                        JsonConvert.SerializeObject(new { username = Username, reason = "account locked" }));
                    OnLoginFailed?.Invoke();
                    return;
                }

                SessionManager.SetCurrentUser(user);
                _auditLogService.Log(user.Id, "LOGIN", "User", user.Id,
                    JsonConvert.SerializeObject(new { ip = SessionManager.CurrentIpAddress }));
                LoginSucceeded?.Invoke(user);
            }
            catch (Exception ex)
            {
                ErrorMessage = "数据库连接失败，请稍后重试";
                var msg = ex.ToString();
                System.Diagnostics.Debug.WriteLine($"Login error: {msg}");
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "login_error.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" + msg + "\n\n");
                OnLoginFailed?.Invoke();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteLogin()
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        public event Action<User> LoginSucceeded;
        public event Action OnLoginFailed;
    }
}
