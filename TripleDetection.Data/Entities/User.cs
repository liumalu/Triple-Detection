using System;
using System.ComponentModel;
using TripleDetection.Data;

namespace TripleDetection.Data.Entities
{
    public class User : BaseEntity, INotifyPropertyChanged
    {
        private string _username;
        private string _realName;
        private string _password;
        private string _role = "Operator";
        private bool _isEnabled = true;
        private bool _isLocked = false;
        private DateTime? _lastLoginAt;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(nameof(Username)); }
        }

        public string RealName
        {
            get => _realName;
            set { _realName = value; OnPropertyChanged(nameof(RealName)); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        // Alias for Password - for compatibility with existing Services code
        public string PasswordHash
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(PasswordHash)); }
        }

        public string Role
        {
            get => _role;
            set { _role = value; OnPropertyChanged(nameof(Role)); }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); OnPropertyChanged(nameof(StatusText)); }
        }

        public bool IsLocked
        {
            get => _isLocked;
            set { _isLocked = value; OnPropertyChanged(nameof(IsLocked)); OnPropertyChanged(nameof(StatusText)); }
        }

        public DateTime? LastLoginAt
        {
            get => _lastLoginAt;
            set { _lastLoginAt = value; OnPropertyChanged(nameof(LastLoginAt)); }
        }

        public string StatusText
        {
            get
            {
                if (!IsEnabled) return "已禁用";
                if (IsLocked) return "已锁定";
                return "正常";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class UserList
    {
        public User[] Users { get; set; } = new User[0];
    }
}