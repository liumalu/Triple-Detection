using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories.Contracts;

namespace TripleDetection.Data.Repositories.Sqlite
{
    /// <summary>
    /// SQLite 用户仓储实现 - Username 作为主键
    /// </summary>
    public class SqliteUserRepository : IUserRepository
    {
        private readonly SqliteDbContext _context;
        private readonly DbSet<User> _dbSet;

        public SqliteUserRepository(SqliteDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<User>();
        }

        public User GetByUsername(string username)
        {
            return _dbSet.Find(username);
        }

        public IEnumerable<User> GetAll()
        {
            return _dbSet.ToList();
        }

        public IEnumerable<User> Find(Expression<Func<User, bool>> predicate)
        {
            return _dbSet.Where(predicate).ToList();
        }

        public void Add(User entity)
        {
            entity.CreateAt = DateTime.Now;
            entity.UpdateAt = DateTime.Now;
            _dbSet.Add(entity);
        }

        public void Update(User entity)
        {
            entity.UpdateAt = DateTime.Now;
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        public void Delete(string username)
        {
            var user = _dbSet.Find(username);
            if (user != null)
            {
                _dbSet.Remove(user);
            }
        }

        public int Count()
        {
            return _dbSet.Count();
        }

        public int Count(Expression<Func<User, bool>> predicate)
        {
            return _dbSet.Count(predicate);
        }

        public PagedResult<User> Query(UserQuery query)
        {
            var q = _dbSet.AsQueryable();

            if (!string.IsNullOrEmpty(query.Username))
                q = q.Where(u => u.Username.Contains(query.Username));
            if (!string.IsNullOrEmpty(query.Role))
                q = q.Where(u => u.Role == query.Role);
            if (!string.IsNullOrEmpty(query.StatusText))
                q = q.Where(u => u.StatusText == query.StatusText);

            var total = q.Count();
            var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)query.PageSize);
            var pageIndex = Math.Min(query.PageIndex, totalPages - 1);
            pageIndex = Math.Max(pageIndex, 0);

            var items = q.OrderBy(u => u.Username)
                        .Skip(pageIndex * query.PageSize)
                        .Take(query.PageSize)
                        .ToList();

            return new PagedResult<User>(items, total, pageIndex, query.PageSize);
        }
    }
}