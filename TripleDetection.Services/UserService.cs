using System;
using System.Collections.Generic;
using System.Linq;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories;
using TripleDetection.Data.Repositories.Sqlite;

namespace TripleDetection.Services
{
    public interface IUserService
    {
        User Authenticate(string username, string password);
        IEnumerable<User> GetAll();
        User GetByUsername(string username);
        User GetById(int id);
        void Create(User user, string createBy, int currentUserId);
        void Update(User user, string updateBy, int currentUserId);
        void Delete(string username, string updateBy, int currentUserId);
        void Enable(string username, string updateBy, int currentUserId);
        void Disable(string username, string updateBy, int currentUserId);
        void Lock(string username, string updateBy, int currentUserId);
        void Unlock(string username, string updateBy, int currentUserId);
        IPagedResult<User> Query(UserQuery query);
    }

    public class UserService : IUserService
    {
        private readonly IRepository<User> _repository;
        private readonly Services.Audit.IAuditLogService _auditLog;
        private readonly IPasswordHashService _hashService;

        public UserService() : this(new SqliteRepositoryFactory().CreateRepository<User>(), null, new PasswordHashService())
        {
        }

        public UserService(IRepository<User> repository, Services.Audit.IAuditLogService auditLogService)
            : this(repository, auditLogService, new PasswordHashService())
        {
        }

        public UserService(IRepository<User> repository, Services.Audit.IAuditLogService auditLogService, IPasswordHashService hashService)
        {
            _repository = repository;
            _auditLog = auditLogService;
            _hashService = hashService ?? new PasswordHashService();
        }

        public User Authenticate(string username, string password)
        {
            var user = _repository.Find(u => u.Username == username && u.IsEnabled && !u.IsLocked)
                                  .FirstOrDefault();
            if (user == null) return null;

            bool authenticated = false;
            bool needsMigration = false;

            if (!string.IsNullOrEmpty(user.PasswordHash) && !string.IsNullOrEmpty(user.PasswordSalt))
            {
                // Hashed mode
                authenticated = _hashService.VerifyPassword(password, user.PasswordSalt, user.PasswordHash);
            }
            else
            {
                // Legacy plain text mode
                authenticated = user.Password == password;
                needsMigration = authenticated;
            }

            if (!authenticated) return null;

            // Auto-migrate plain text to hashed on successful legacy login
            if (needsMigration)
            {
                user.PasswordSalt = _hashService.GenerateSalt();
                user.PasswordHash = _hashService.ComputeHash(user.PasswordSalt, password);
                _repository.Update(user);
            }

            _auditLog?.Log(user.Id, "登录", "User", user.Id, $"用户登录: {username}");
            return user;
        }

        public IEnumerable<User> GetAll()
        {
            return _repository.GetAll();
        }

        public User GetByUsername(string username)
        {
            return _repository.Find(u => u.Username == username).FirstOrDefault();
        }

        public User GetById(int id)
        {
            return _repository.GetById(id);
        }

        public void Create(User user, string createBy, int currentUserId)
        {
            if (string.IsNullOrEmpty(user.Username))
                throw new ArgumentException("Username is required");

            var existing = GetByUsername(user.Username);
            if (existing != null)
                throw new InvalidOperationException($"User '{user.Username}' already exists");

            user.CreateBy = createBy;
            _repository.Add(user);
            _auditLog?.Log(currentUserId, "创建", "User", user.Id, $"创建用户: {user.Username}");
        }

        public void Update(User user, string updateBy, int currentUserId)
        {
            var existing = GetByUsername(user.Username);
            if (existing == null)
                throw new InvalidOperationException($"User '{user.Username}' not found");

            user.Id = existing.Id;
            user.UpdateBy = updateBy;
            _repository.Update(user);
            _auditLog?.Log(currentUserId, "修改", "User", user.Id, $"修改用户: {user.Username}");
        }

        public void Delete(string username, string updateBy, int currentUserId)
        {
            var existing = GetByUsername(username);
            if (existing == null)
                throw new InvalidOperationException($"User '{username}' not found");

            _repository.Delete(existing.Id);
            _auditLog?.Log(currentUserId, "删除", "User", existing.Id, $"删除用户: {username}");
        }

        public void Enable(string username, string updateBy, int currentUserId)
        {
            var user = GetByUsername(username);
            if (user == null)
                throw new InvalidOperationException($"User '{username}' not found");

            user.IsEnabled = true;
            user.UpdateBy = updateBy;
            _repository.Update(user);
            _auditLog?.Log(currentUserId, "启用", "User", user.Id, $"启用用户: {username}");
        }

        public void Disable(string username, string updateBy, int currentUserId)
        {
            var user = GetByUsername(username);
            if (user == null)
                throw new InvalidOperationException($"User '{username}' not found");

            user.IsEnabled = false;
            user.UpdateBy = updateBy;
            _repository.Update(user);
            _auditLog?.Log(currentUserId, "禁用", "User", user.Id, $"禁用用户: {username}");
        }

        public void Lock(string username, string updateBy, int currentUserId)
        {
            var user = GetByUsername(username);
            if (user == null)
                throw new InvalidOperationException($"User '{username}' not found");

            user.IsLocked = true;
            user.UpdateBy = updateBy;
            _repository.Update(user);
            _auditLog?.Log(currentUserId, "锁定", "User", user.Id, $"锁定用户: {username}");
        }

        public void Unlock(string username, string updateBy, int currentUserId)
        {
            var user = GetByUsername(username);
            if (user == null)
                throw new InvalidOperationException($"User '{username}' not found");

            user.IsLocked = false;
            user.UpdateBy = updateBy;
            _repository.Update(user);
            _auditLog?.Log(currentUserId, "解锁", "User", user.Id, $"解锁用户: {username}");
        }

        public IPagedResult<User> Query(UserQuery query)
        {
            return _repository.Query(query);
        }
    }
}
