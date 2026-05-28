using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TripleDetection.Models;

namespace TripleDetection.Services.Settings
{
    public class UserSettingsService
    {
        private readonly string _configPath;
        private UserList _userList;

        public UserSettingsService()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _configPath = Path.Combine(baseDir, "Config", "users.json");

            System.IO.File.AppendAllText(
                System.IO.Path.Combine(baseDir, "debug.log"),
                $"[UserSettingsService] BaseDirectory: {baseDir}\n[_configPath] {_configPath}\n[FileExists] {File.Exists(_configPath)}\n");
        }

        private void EnsureLoaded()
        {
            _userList = SimpleJsonHelper.Load<UserList>(_configPath);
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"),
                $"[EnsureLoaded] Loaded {_userList?.Users?.Length ?? -1} users\n");
            if (_userList.Users == null)
            {
                _userList.Users = new User[0];
            }
        }

        private void Save()
        {
            SimpleJsonHelper.Save(_userList, _configPath);
        }

        public List<User> GetAll()
        {
            EnsureLoaded();
            return _userList.Users.ToList();
        }

        public User GetByUsername(string username)
        {
            EnsureLoaded();
            return _userList.Users.FirstOrDefault(u => u.Username == username);
        }

        public void Add(User user)
        {
            EnsureLoaded();
            var existing = _userList.Users.FirstOrDefault(u => u.Username == user.Username);
            if (existing != null)
            {
                throw new InvalidOperationException($"用户名 {user.Username} 已存在");
            }
            var newList = _userList.Users.ToList();
            newList.Add(user);
            _userList.Users = newList.ToArray();
            Save();
        }

        public void Update(User user)
        {
            EnsureLoaded();
            var existing = _userList.Users.FirstOrDefault(u => u.Username == user.Username);
            if (existing == null)
            {
                throw new InvalidOperationException($"用户名 {user.Username} 不存在");
            }
            var newList = _userList.Users.ToList();
            var index = newList.FindIndex(u => u.Username == user.Username);
            newList[index] = user;
            _userList.Users = newList.ToArray();
            Save();
        }

        public void Delete(string username)
        {
            EnsureLoaded();
            var newList = _userList.Users.ToList();
            newList.RemoveAll(u => u.Username == username);
            _userList.Users = newList.ToArray();
            Save();
        }

        public void Enable(string username)
        {
            EnsureLoaded();
            var user = _userList.Users.FirstOrDefault(u => u.Username == username);
            if (user != null)
            {
                user.IsEnabled = true;
                Save();
            }
        }

        public void Disable(string username)
        {
            EnsureLoaded();
            var user = _userList.Users.FirstOrDefault(u => u.Username == username);
            if (user != null)
            {
                user.IsEnabled = false;
                Save();
            }
        }

        public void Lock(string username)
        {
            EnsureLoaded();
            var user = _userList.Users.FirstOrDefault(u => u.Username == username);
            if (user != null)
            {
                user.IsLocked = true;
                Save();
            }
        }

        public void Unlock(string username)
        {
            EnsureLoaded();
            var user = _userList.Users.FirstOrDefault(u => u.Username == username);
            if (user != null)
            {
                user.IsLocked = false;
                Save();
            }
        }

        public bool ValidateUser(string username, string password)
        {
            EnsureLoaded();
            var user = _userList.Users.FirstOrDefault(u => u.Username == username);
            if (user == null || !user.IsEnabled || user.IsLocked)
            {
                return false;
            }
            return user.Password == password;
        }
    }
}