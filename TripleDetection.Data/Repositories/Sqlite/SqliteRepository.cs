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

        public IPagedResult<T> Query(ProductQuery query)
        {
            // Start with base filter
            var q = _dbSet.Where(x => !x.IsDeleted).AsEnumerable();

            // Filter by ProductQuery conditions (use LINQ to Objects - expression trees don't support ?. in EF)
            if (query is ProductQuery pq)
            {
                if (!string.IsNullOrEmpty(pq.Code))
                    q = q.Where(x => (x as Product)?.Code?.Contains(pq.Code) == true);
                if (!string.IsNullOrEmpty(pq.Name))
                    q = q.Where(x => (x as Product)?.Name?.Contains(pq.Name) == true);
                if (pq.Status.HasValue)
                    q = q.Where(x => (x as Product)?.Status == (ProductStatus)pq.Status.Value);
                if (pq.CreateAtFrom.HasValue)
                    q = q.Where(x => (x as Product)?.CreateAt >= pq.CreateAtFrom.Value);
                if (pq.CreateAtTo.HasValue)
                    q = q.Where(x => (x as Product)?.CreateAt <= pq.CreateAtTo.Value);
            }

            // Apply sorting (in memory since we already materialized)
            if (!string.IsNullOrEmpty(query.SortBy))
            {
                var prop = typeof(T).GetProperty(query.SortBy);
                if (prop != null)
                {
                    q = query.SortDescending
                        ? q.OrderByDescending(x => prop.GetValue(x))
                        : q.OrderBy(x => prop.GetValue(x));
                }
            }

            var total = q.Count();
            var items = q.Skip(query.PageIndex * query.PageSize)
                         .Take(query.PageSize)
                         .ToList();

            return new PagedResult<T>(items, total, query.PageIndex, query.PageSize);
        }

        public IPagedResult<T> Query(UserQuery query)
        {
            var q = _dbSet.Where(x => !x.IsDeleted).AsEnumerable();

            if (!string.IsNullOrEmpty(query.ExactUsername))
                q = q.Where(x => (x as User)?.Username == query.ExactUsername);
            if (!string.IsNullOrEmpty(query.Username))
                q = q.Where(x => (x as User)?.Username != null && (x as User).Username.Contains(query.Username));
            if (!string.IsNullOrEmpty(query.Role))
                q = q.Where(x => (x as User)?.Role == query.Role);
            if (!string.IsNullOrEmpty(query.StatusText))
                q = q.Where(x => (x as User)?.StatusText == query.StatusText);

            if (!string.IsNullOrEmpty(query.SortBy))
            {
                var prop = typeof(T).GetProperty(query.SortBy);
                if (prop != null)
                {
                    q = query.SortDescending
                        ? q.OrderByDescending(x => prop.GetValue(x))
                        : q.OrderBy(x => prop.GetValue(x));
                }
            }

            var total = q.Count();
            var items = q.Skip(query.PageIndex * query.PageSize)
                         .Take(query.PageSize)
                         .ToList();

            return new PagedResult<T>(items, total, query.PageIndex, query.PageSize);
        }

        public IPagedResult<T> Query(TaskQuery query)
        {
            var q = _dbSet.Where(x => !x.IsDeleted).AsEnumerable();

            if (query is TaskQuery tq)
            {
                if (!string.IsNullOrEmpty(tq.Name))
                    q = q.Where(x => (x as ProdTask)?.Name?.Contains(tq.Name) == true);
                if (tq.ProductId.HasValue)
                    q = q.Where(x => (x as ProdTask)?.ProductId == tq.ProductId.Value);
                if (tq.Status.HasValue)
                    q = q.Where(x => (x as ProdTask)?.Status == (TaskStatus)tq.Status.Value);
                if (tq.ProductionDateFrom.HasValue)
                    q = q.Where(x => (x as ProdTask)?.ProductionDate >= tq.ProductionDateFrom.Value);
                if (tq.ProductionDateTo.HasValue)
                    q = q.Where(x => (x as ProdTask)?.ProductionDate <= tq.ProductionDateTo.Value);
                if (!string.IsNullOrEmpty(tq.BatchNumber))
                    q = q.Where(x => (x as ProdTask)?.BatchNumber?.Contains(tq.BatchNumber) == true);
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(query.SortBy))
            {
                var prop = typeof(T).GetProperty(query.SortBy);
                if (prop != null)
                {
                    q = query.SortDescending
                        ? q.OrderByDescending(x => prop.GetValue(x))
                        : q.OrderBy(x => prop.GetValue(x));
                }
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