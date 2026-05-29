using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories
{
    /// <summary>
    /// 通用仓储接口
    /// </summary>
    public interface IRepository<T> where T : BaseEntity
    {
        T GetById(int id);
        IEnumerable<T> GetAll();
        IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
        void Add(T entity);
        void Update(T entity);
        void Delete(int id);
        int Count();
        int Count(Expression<Func<T, bool>> predicate);
    }

    /// <summary>
    /// 分页结果接口
    /// </summary>
    public interface IPagedResult<T>
    {
        IEnumerable<T> Items { get; }
        int TotalCount { get; }
        int PageIndex { get; }
        int PageSize { get; }
        int TotalPages { get; }
        bool HasPreviousPage { get; }
        bool HasNextPage { get; }
    }

    /// <summary>
    /// 分页查询参数
    /// </summary>
    public class PagedQuery
    {
        public int PageIndex { get; set; } = 0;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
    }

    /// <summary>
    /// 产品查询条件
    /// </summary>
    public class ProductQuery : PagedQuery
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public int? Status { get; set; }
        public DateTime? CreateAtFrom { get; set; }
        public DateTime? CreateAtTo { get; set; }
    }

    /// <summary>
    /// 任务查询条件
    /// </summary>
    public class TaskQuery : PagedQuery
    {
        public string Name { get; set; }
        public int? ProductId { get; set; }
        public int? Status { get; set; }
        public DateTime? ProductionDateFrom { get; set; }
        public DateTime? ProductionDateTo { get; set; }
        public string BatchNumber { get; set; }
    }

    /// <summary>
    /// Audit log query conditions
    /// </summary>
    public class AuditLogQuery : PagedQuery
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? UserId { get; set; }
        public string Action { get; set; }
        public string ObjectType { get; set; }
        public string Keyword { get; set; }   // Details fuzzy search
        public string IpAddress { get; set; }
    }

    /// <summary>
    /// Detection record query conditions
    /// </summary>
    public class DetectionRecordQuery : PagedQuery
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? TaskId { get; set; }
        public int? ProductId { get; set; }
        public string BatchNumber { get; set; }
        public bool? IsOK { get; set; }
    }

    /// <summary>
    /// 内存仓储实现（演示用，生产环境替换为 EF6 + SQLite）
    /// </summary>
    public class InMemoryRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected static readonly List<T> _items = new List<T>();
        protected static int _idCounter = 1;
        protected readonly object _lock = new object();

        public virtual T GetById(int id)
        {
            lock (_lock)
            {
                return _items.FirstOrDefault(x => x.Id == id && !x.IsDeleted);
            }
        }

        public virtual IEnumerable<T> GetAll()
        {
            lock (_lock)
            {
                return _items.Where(x => !x.IsDeleted).ToList();
            }
        }

        public virtual IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
        {
            lock (_lock)
            {
                return _items.Where(predicate.Compile()).Where(x => !x.IsDeleted).ToList();
            }
        }

        public virtual void Add(T entity)
        {
            lock (_lock)
            {
                entity.Id = _idCounter++;
                entity.CreateAt = DateTime.Now;
                entity.UpdateAt = DateTime.Now;
                entity.IsDeleted = false;
                _items.Add(entity);
            }
        }

        public virtual void Update(T entity)
        {
            lock (_lock)
            {
                var existing = _items.FirstOrDefault(x => x.Id == entity.Id && !x.IsDeleted);
                if (existing != null)
                {
                    entity.UpdateAt = DateTime.Now;
                    _items.Remove(existing);
                    _items.Add(entity);
                }
            }
        }

        public virtual void Delete(int id)
        {
            lock (_lock)
            {
                var item = _items.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    item.IsDeleted = true;
                    item.UpdateAt = DateTime.Now;
                }
            }
        }

        public virtual int Count()
        {
            lock (_lock)
            {
                return _items.Count(x => !x.IsDeleted);
            }
        }

        public virtual int Count(Expression<Func<T, bool>> predicate)
        {
            lock (_lock)
            {
                return _items.Where(predicate.Compile()).Count(x => !x.IsDeleted);
            }
        }

        public IPagedResult<T> Query(PagedQuery query)
        {
            lock (_lock)
            {
                var filtered = _items.Where(x => !x.IsDeleted);

                // Sort
                if (!string.IsNullOrEmpty(query.SortBy))
                {
                    var prop = typeof(T).GetProperty(query.SortBy);
                    if (prop != null)
                    {
                        filtered = query.SortDescending
                            ? filtered.OrderByDescending(x => prop.GetValue(x))
                            : filtered.OrderBy(x => prop.GetValue(x));
                    }
                }

                var total = filtered.Count();
                var items = filtered
                    .Skip(query.PageIndex * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();

                return new PagedResult<T>(items, total, query.PageIndex, query.PageSize);
            }
        }

        public IPagedResult<T> Query(ProductQuery query)
        {
            lock (_lock)
            {
                var filtered = _items.Where(x => !x.IsDeleted);

                // Filter by ProductQuery conditions
                if (query is ProductQuery pq)
                {
                    if (!string.IsNullOrEmpty(pq.Code))
                        filtered = filtered.Where(x => (x as Product)?.Code?.Contains(pq.Code) == true);
                    if (!string.IsNullOrEmpty(pq.Name))
                        filtered = filtered.Where(x => (x as Product)?.Name?.Contains(pq.Name) == true);
                    if (pq.Status.HasValue)
                        filtered = filtered.Where(x => (x as Product)?.Status == (ProductStatus)pq.Status.Value);
                    if (pq.CreateAtFrom.HasValue)
                        filtered = filtered.Where(x => (x as Product)?.CreateAt >= pq.CreateAtFrom.Value);
                    if (pq.CreateAtTo.HasValue)
                        filtered = filtered.Where(x => (x as Product)?.CreateAt <= pq.CreateAtTo.Value);
                }

                // Sort
                if (!string.IsNullOrEmpty(query.SortBy))
                {
                    var prop = typeof(T).GetProperty(query.SortBy);
                    if (prop != null)
                    {
                        filtered = query.SortDescending
                            ? filtered.OrderByDescending(x => prop.GetValue(x))
                            : filtered.OrderBy(x => prop.GetValue(x));
                    }
                }

                var total = filtered.Count();
                var items = filtered
                    .Skip(query.PageIndex * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();

                return new PagedResult<T>(items, total, query.PageIndex, query.PageSize);
            }
        }

        public IPagedResult<T> Query(TaskQuery query)
        {
            lock (_lock)
            {
                var filtered = _items.Where(x => !x.IsDeleted);

                if (query is TaskQuery tq)
                {
                    if (!string.IsNullOrEmpty(tq.Name))
                        filtered = filtered.Where(x => (x as Task)?.Name?.Contains(tq.Name) == true);
                    if (tq.ProductId.HasValue)
                        filtered = filtered.Where(x => (x as Task)?.ProductId == tq.ProductId.Value);
                    if (tq.Status.HasValue)
                        filtered = filtered.Where(x => (x as Task)?.Status == (TaskStatus)tq.Status.Value);
                    if (tq.ProductionDateFrom.HasValue)
                        filtered = filtered.Where(x => (x as Task)?.ProductionDate >= tq.ProductionDateFrom.Value);
                    if (tq.ProductionDateTo.HasValue)
                        filtered = filtered.Where(x => (x as Task)?.ProductionDate <= tq.ProductionDateTo.Value);
                    if (!string.IsNullOrEmpty(tq.BatchNumber))
                        filtered = filtered.Where(x => (x as Task)?.BatchNumber?.Contains(tq.BatchNumber) == true);
                }

                if (!string.IsNullOrEmpty(query.SortBy))
                {
                    var prop = typeof(T).GetProperty(query.SortBy);
                    if (prop != null)
                    {
                        filtered = query.SortDescending
                            ? filtered.OrderByDescending(x => prop.GetValue(x))
                            : filtered.OrderBy(x => prop.GetValue(x));
                    }
                }

                var total = filtered.Count();
                var items = filtered
                    .Skip(query.PageIndex * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();

                return new PagedResult<T>(items, total, query.PageIndex, query.PageSize);
            }
        }
    }

    public class PagedResult<T> : IPagedResult<T>
    {
        public IEnumerable<T> Items { get; }
        public int TotalCount { get; }
        public int PageIndex { get; }
        public int PageSize { get; }
        public int TotalPages { get; }
        public bool HasPreviousPage { get; }
        public bool HasNextPage { get; }

        public PagedResult(IEnumerable<T> items, int totalCount, int pageIndex, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            HasPreviousPage = pageIndex > 0;
            HasNextPage = pageIndex < TotalPages - 1;
        }
    }
}