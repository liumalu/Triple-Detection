using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using TripleDetection.Data.Entities;
using TripleDetection.Data;

namespace TripleDetection.Data.Repositories
{
    /// <summary>
    /// 基于 JSON 文件的用户仓储实现
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly string _filePath;
        private List<User> _users;
        private bool _isLoaded = false;
        private readonly object _lock = new object();

        public UserRepository(string filePath = null)
        {
            _filePath = filePath ?? GetDefaultFilePath();
            _users = new List<User>();
        }

        private static string GetDefaultFilePath()
        {
            return System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Config", "users.json");
        }

        private void EnsureLoaded()
        {
            if (_isLoaded) return;

            lock (_lock)
            {
                if (_isLoaded) return;

                var userList = SimpleJsonHelper.Load<UserList>(_filePath);
                _users = new List<User>(userList?.Users ?? new User[0]);
                _isLoaded = true;
            }
        }

        private void Save()
        {
            lock (_lock)
            {
                var userList = new UserList { Users = _users.ToArray() };
                SimpleJsonHelper.Save(userList, _filePath);
            }
        }

        public User GetByUsername(string username)
        {
            EnsureLoaded();
            lock (_lock)
            {
                return _users.FirstOrDefault(u => u.Username == username);
            }
        }

        public IEnumerable<User> GetAll()
        {
            EnsureLoaded();
            lock (_lock)
            {
                return _users.ToList();
            }
        }

        public IEnumerable<User> Find(Expression<Func<User, bool>> predicate)
        {
            EnsureLoaded();
            lock (_lock)
            {
                return _users.Where(predicate.Compile()).ToList();
            }
        }

        public void Add(User entity)
        {
            EnsureLoaded();
            lock (_lock)
            {
                entity.CreateAt = DateTime.Now;
                _users.Add(entity);
                Save();
            }
        }

        public void Update(User entity)
        {
            EnsureLoaded();
            lock (_lock)
            {
                var existing = _users.FirstOrDefault(u => u.Username == entity.Username);
                if (existing != null)
                {
                    var index = _users.IndexOf(existing);
                    _users[index] = entity;
                    Save();
                }
            }
        }

        public void Delete(string username)
        {
            EnsureLoaded();
            lock (_lock)
            {
                var user = _users.FirstOrDefault(u => u.Username == username);
                if (user != null)
                {
                    _users.Remove(user);
                    Save();
                }
            }
        }

        public int Count()
        {
            EnsureLoaded();
            lock (_lock)
            {
                return _users.Count;
            }
        }

        public int Count(Expression<Func<User, bool>> predicate)
        {
            EnsureLoaded();
            lock (_lock)
            {
                return _users.Count(predicate.Compile());
            }
        }

        public PagedResult<User> Query(UserQuery query)
        {
            EnsureLoaded();
            lock (_lock)
            {
                var filtered = _users.AsEnumerable();

                // Apply filters
                if (!string.IsNullOrEmpty(query.Username))
                    filtered = filtered.Where(u => u.Username.Contains(query.Username));
                if (!string.IsNullOrEmpty(query.Role))
                    filtered = filtered.Where(u => u.Role == query.Role);
                if (!string.IsNullOrEmpty(query.StatusText))
                    filtered = filtered.Where(u => u.StatusText == query.StatusText);

                var total = filtered.Count();
                var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)query.PageSize);
                var pageIndex = query.PageIndex >= totalPages ? totalPages - 1 : query.PageIndex;
                pageIndex = pageIndex < 0 ? 0 : pageIndex;

                var items = filtered
                    .Skip(pageIndex * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();

                return new PagedResult<User>(items, total, pageIndex, query.PageSize);
            }
        }
    }
}