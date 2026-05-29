using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using TripleDetection.Data;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories.Contracts;

namespace TripleDetection.Data.Repositories.Sqlite
{
    /// <summary>
    /// SQLite 通用仓储实现
    /// </summary>
    /// <typeparam name="T">实体类型，继承自 BaseEntity</typeparam>
    public class SqliteRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly SqliteDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public SqliteRepository(SqliteDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        public virtual T GetById(int id)
        {
            return _dbSet.Find(id);
        }

        public virtual IEnumerable<T> GetAll()
        {
            return _dbSet.Where(x => !x.IsDeleted).ToList();
        }

        public virtual IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.Where(predicate).Where(x => !x.IsDeleted).ToList();
        }

        public virtual void Add(T entity)
        {
            entity.CreateAt = DateTime.Now;
            entity.UpdateAt = DateTime.Now;
            entity.IsDeleted = false;
            _dbSet.Add(entity);
        }

        public virtual void Update(T entity)
        {
            entity.UpdateAt = DateTime.Now;
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        public virtual void Delete(int id)
        {
            var entity = _dbSet.Find(id);
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.UpdateAt = DateTime.Now;
            }
        }

        public virtual int Count()
        {
            return _dbSet.Count(x => !x.IsDeleted);
        }

        public virtual int Count(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.Where(predicate).Count(x => !x.IsDeleted);
        }

        public IPagedResult<T> Query(PagedQuery query)
        {
            var q = _dbSet.Where(x => !x.IsDeleted);

            // 应用排序
            if (!string.IsNullOrEmpty(query.SortBy))
            {
                q = ApplySorting(q, query.SortBy, query.SortDescending);
            }

            var total = q.Count();
            var items = q.Skip(query.PageIndex * query.PageSize)
                         .Take(query.PageSize)
                         .ToList();

            return new PagedResult<T>(items, total, query.PageIndex, query.PageSize);
        }

        protected IQueryable<T> ApplySorting(IQueryable<T> query, string sortBy, bool descending)
        {
            var property = typeof(T).GetProperty(sortBy);
            if (property == null) return query;

            var param = Expression.Parameter(typeof(T), "x");
            var body = Expression.Property(param, property);
            var lambda = Expression.Lambda<Func<T, object>>(
                Expression.Convert(body, typeof(object)), param);

            return descending ? query.OrderByDescending(lambda) : query.OrderBy(lambda);
        }
    }
}