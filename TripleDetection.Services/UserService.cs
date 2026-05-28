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
        void Create(User user, string createBy);
        void Update(User user, string updateBy);
        void Delete(string username, string updateBy);
        void Enable(string username, string updateBy);
        void Disable(string username, string updateBy);
        void Lock(string username, string updateBy);
        void Unlock(string username, string updateBy);
        PagedResult<User> Query(UserQuery query);
    }

    /// <summary>
    /// User service implementation using UserRepository
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService() : this(new UserRepository())
        {
        }

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public User Authenticate(string username, string password)
        {
            var user = _repository.GetByUsername(username);
            if (user != null && user.Password == password && user.IsEnabled && !user.IsLocked)
            {
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

        public void Create(User user, string createBy)
        {
            if (string.IsNullOrEmpty(user.Username))
                throw new ArgumentException("Username is required");

            var existing = _repository.GetByUsername(user.Username);
            if (existing != null)
                throw new InvalidOperationException($"User '{user.Username}' already exists");

            user.CreateBy = createBy;
            user.CreateAt = DateTime.Now;
            _repository.Add(user);
        }

        public void Update(User user, string updateBy)
        {
            var existing = _repository.GetByUsername(user.Username);
            if (existing == null)
                throw new InvalidOperationException($"User '{user.Username}' not found");

            user.UpdateBy = updateBy;
            user.UpdateAt = DateTime.Now;
            _repository.Update(user);
        }

        public void Delete(string username, string updateBy)
        {
            var existing = _repository.GetByUsername(username);
            if (existing == null)
                throw new InvalidOperationException($"User '{username}' not found");

            _repository.Delete(username);
        }

        public void Enable(string username, string updateBy)
        {
            var user = _repository.GetByUsername(username);
            if (user == null)
                throw new InvalidOperationException($"User '{username}' not found");

            user.IsEnabled = true;
            user.UpdateBy = updateBy;
            user.UpdateAt = DateTime.Now;
            _repository.Update(user);
        }

        public void Disable(string username, string updateBy)
        {
            var user = _repository.GetByUsername(username);
            if (user == null)
                throw new InvalidOperationException($"User '{username}' not found");

            user.IsEnabled = false;
            user.UpdateBy = updateBy;
            user.UpdateAt = DateTime.Now;
            _repository.Update(user);
        }

        public void Lock(string username, string updateBy)
        {
            var user = _repository.GetByUsername(username);
            if (user == null)
                throw new InvalidOperationException($"User '{username}' not found");

            user.IsLocked = true;
            user.UpdateBy = updateBy;
            user.UpdateAt = DateTime.Now;
            _repository.Update(user);
        }

        public void Unlock(string username, string updateBy)
        {
            var user = _repository.GetByUsername(username);
            if (user == null)
                throw new InvalidOperationException($"User '{username}' not found");

            user.IsLocked = false;
            user.UpdateBy = updateBy;
            user.UpdateAt = DateTime.Now;
            _repository.Update(user);
        }

        public PagedResult<User> Query(UserQuery query)
        {
            return _repository.Query(query);
        }
    }
}