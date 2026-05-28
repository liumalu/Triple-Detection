using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories
{
    /// <summary>
    /// 用户仓储接口（Username 作为主键，非 int Id）
    /// </summary>
    public interface IUserRepository
    {
        User GetByUsername(string username);
        IEnumerable<User> GetAll();
        IEnumerable<User> Find(Expression<Func<User, bool>> predicate);
        void Add(User entity);
        void Update(User entity);
        void Delete(string username);
        int Count();
        int Count(Expression<Func<User, bool>> predicate);
        PagedResult<User> Query(UserQuery query);
    }
}