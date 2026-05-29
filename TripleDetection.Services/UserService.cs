using System;
using System.Collections.Generic;
using System.Linq;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories;

namespace TripleDetection.Services
{
    /// <summary>
    /// User service interface
    /// </summary>
    public interface IUserService
    {
        User Authenticate(string username, string password);
        IEnumerable<User> GetAll();
        User GetByUsername(string username);
        void Create(User user, string createBy, int currentUserId);
        void Update(User user, string updateBy, int currentUserId);
        void Delete(string username, string updateBy, int currentUserId);
        void Enable(string username, string updateBy, int currentUserId);
        void Disable(string username, string updateBy, int currentUserId);
        void Lock(string username, string updateBy, int currentUserId);
        void Unlock(string username, string updateBy, int currentUserId);
        PagedResult<User> Query(UserQuery query);
    }

    /// <summary>
    /// User service implementation using UserRepository
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IAuditLogService _auditLog;

        public UserService() : this(new UserRepository(), null)
        {
        }

        public UserService(IUserRepository repository, IAuditLogService auditLogService)
        {
            _repository = repository;
            _auditLog = auditLogService;
        }

        public User Authenticate(string username, string password)
        {
            var user = _repository.GetByUsername(username);
            if (user != null && user.Password == password && user.IsEnabled && !user.IsLocked)
            {
                _auditLog?.Log(user.Id, "登录", "User", user.Id, $"用户登录: {username}");
                return user;
            }
            return null;
        }

        public IEnumerable<User> GetAll()
        {
            return _repository.GetAll();
        }

        public User GetByUsername(string username)
        {
            return _repository.GetByUsername(username);
        }

        public void Create(User user, string createBy, int currentUserId)
        {
            if (string.IsNullOrEmpty(user.Username))
                throw new ArgumentException("Username is required");

            var existing = _repository.GetByUsername(user.Username);
            if (existing != null)
                throw new InvalidOperationException($"User '{user.Username}' already exists");

            user.CreateBy = createBy;
            user.CreateAt = DateTime.Now;
            _repository.Add(user);
            _auditLog?.Log(currentUserId, "创建", "User", user.Id, $"创建用户: {user.Username}");
        }

        public void Update(User user, string updateBy, int currentUserId)
        {
            var existing = _repository.GetByUsername(user.Username);
            if (existing == null)
                throw new InvalidOperationException($"User '{user.Username}' not found");

            user.UpdateBy = updateBy;
            user.UpdateAt = DateTime.Now;
            _repository.Update(user);
            _auditLog?.Log(currentUserId, "修改", "User", user.Id, $"修改用户: {user.Username}");
        }

        public void Delete(string username, string updateBy, int currentUserId)
        {
            var existing = _repository.GetByUsername(username);
            if (existing == null)
                throw new InvalidOperationException($"User '{username}' not found");

            _repository.Delete(username);
            _auditLog?.Log(currentUserId, "删除", "User", existing.Id, $"删除用户: {username}");
        }

        public void Enable(string username, string updateBy, int currentUserId)
        {
            var user = _repository.GetByUsername(username);
            if (user == null)
                throw new InvalidOperationException($"User '{username}' not found");

            user.IsEnabled = true;
            user.UpdateBy = updateBy;
            user.UpdateAt = DateTime.Now;
            _repository.Update(user);
            _auditLog?.Log(currentUserId, "启用", "User", user.Id, $"启用用户: {username}");
        }

        public void Disable(string username, string updateBy, int currentUserId)
        {
            var user = _repository.GetByUsername(username);
            if (user == null)
                throw new InvalidOperationException($"User '{username}' not found");

            user.IsEnabled = false;
            user.UpdateBy = updateBy;
            user.UpdateAt = DateTime.Now;
            _repository.Update(user);
            _auditLog?.Log(currentUserId, "禁用", "User", user.Id, $"禁用用户: {username}");
        }

        public void Lock(string username, string updateBy, int currentUserId)
        {
            var user = _repository.GetByUsername(username);
            if (user == null)
                throw new InvalidOperationException($"User '{username}' not found");

            user.IsLocked = true;
            user.UpdateBy = updateBy;
            user.UpdateAt = DateTime.Now;
            _repository.Update(user);
            _auditLog?.Log(currentUserId, "锁定", "User", user.Id, $"锁定用户: {username}");
        }

        public void Unlock(string username, string updateBy, int currentUserId)
        {
            var user = _repository.GetByUsername(username);
            if (user == null)
                throw new InvalidOperationException($"User '{username}' not found");

            user.IsLocked = false;
            user.UpdateBy = updateBy;
            user.UpdateAt = DateTime.Now;
            _repository.Update(user);
            _auditLog?.Log(currentUserId, "解锁", "User", user.Id, $"解锁用户: {username}");
        }

        public PagedResult<User> Query(UserQuery query)
        {
            return _repository.Query(query);
        }
    }
}