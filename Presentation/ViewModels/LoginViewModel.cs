using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm;
using TripleDetection.Application.Services;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain;

namespace TripleDetection.Presentation.ViewModels
{
    public class LoginViewModel : ObservableObject
    {
        private readonly IUserService _userService;

        [ObservableProperty] private string _username = string.Empty;
        [ObservableProperty] private string _password = string.Empty;
        [ObservableProperty] private string _errorMessage = string.Empty;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private bool _usernameHasError;
        [ObservableProperty] private bool _passwordHasError;
        [ObservableProperty] private string _logoPath;

        public LoginViewModel(IUserService userService)
        {
            _userService = userService;
            _logoPath = System.Configuration.ConfigurationManager.AppSettings["LoginLogoPath"]
                ?? System.Configuration.ConfigurationManager.AppSettings["SystemLogoPath"];

            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin)
                .ObserveProperty(nameof(IsLoading))
                .ObserveProperty(nameof(Username))
                .ObserveProperty(nameof(Password));
        }

        partial void OnUsernameChanged(string value)
        {
            UsernameHasError = false;
            ErrorMessage = string.Empty;
        }

        partial void OnPasswordChanged(string value)
        {
            PasswordHasError = false;
            ErrorMessage = string.Empty;
        }

        public ICommand LoginCommand { get; }

        public event Action<User> LoginSucceeded;
        public event Action OnLoginFailed;

        private bool CanExecuteLogin()
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

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
                    OnLoginFailed?.Invoke();
                    return;
                }

                if (!user.IsEnabled)
                {
                    ErrorMessage = "账号已被禁用";
                    OnLoginFailed?.Invoke();
                    return;
                }

                if (user.IsLocked)
                {
                    ErrorMessage = "账号已被锁定";
                    OnLoginFailed?.Invoke();
                    return;
                }

                SessionManager.SetCurrentUser(user);
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
    }
}