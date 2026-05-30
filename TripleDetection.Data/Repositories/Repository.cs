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
        IPagedResult<T> Query(PagedQuery query);
        IPagedResult<T> Query(ProductQuery query);
        IPagedResult<T> Query(TaskQuery query);
        IPagedResult<T> Query(UserQuery query);
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