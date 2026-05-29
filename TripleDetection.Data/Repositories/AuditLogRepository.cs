using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories.Sqlite;

namespace TripleDetection.Data.Repositories
{
    /// <summary>
    /// Audit log repository interface
    /// </summary>
    public interface IAuditLogRepository : IRepository<AuditLog>
    {
        IPagedResult<AuditLog> Query(AuditLogQuery query);
        IEnumerable<AuditLog> Export(AuditLogQuery query);  // no pagination, returns all matching data
    }

    public class AuditLogRepository : SqliteRepository<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(SqliteDbContext context) : base(context) { }

        public IPagedResult<AuditLog> Query(AuditLogQuery query)
        {
            var q = _dbSet.Where(x => !x.IsDeleted);

            if (query.StartDate.HasValue)
                q = q.Where(x => x.CreateAt >= query.StartDate.Value);
            if (query.EndDate.HasValue)
                q = q.Where(x => x.CreateAt <= query.EndDate.Value);
            if (query.UserId.HasValue)
                q = q.Where(x => x.UserId == query.UserId.Value);
            if (!string.IsNullOrEmpty(query.Action))
                q = q.Where(x => x.Action == query.Action);
            if (!string.IsNullOrEmpty(query.ObjectType))
                q = q.Where(x => x.ObjectType == query.ObjectType);
            if (!string.IsNullOrEmpty(query.Keyword))
                q = q.Where(x => x.Details.Contains(query.Keyword));
            if (!string.IsNullOrEmpty(query.IpAddress))
                q = q.Where(x => x.IpAddress == query.IpAddress);

            if (!string.IsNullOrEmpty(query.SortBy))
                q = ApplySorting(q, query.SortBy, query.SortDescending);
            else
                q = q.OrderByDescending(x => x.CreateAt);

            var total = q.Count();
            var items = q.Skip(query.PageIndex * query.PageSize)
                         .Take(query.PageSize)
                         .ToList();

            return new PagedResult<AuditLog>(items, total, query.PageIndex, query.PageSize);
        }

        public IEnumerable<AuditLog> Export(AuditLogQuery query)
        {
            var q = _dbSet.Where(x => !x.IsDeleted);

            if (query.StartDate.HasValue)
                q = q.Where(x => x.CreateAt >= query.StartDate.Value);
            if (query.EndDate.HasValue)
                q = q.Where(x => x.CreateAt <= query.EndDate.Value);
            if (query.UserId.HasValue)
                q = q.Where(x => x.UserId == query.UserId.Value);
            if (!string.IsNullOrEmpty(query.Action))
                q = q.Where(x => x.Action == query.Action);
            if (!string.IsNullOrEmpty(query.ObjectType))
                q = q.Where(x => x.ObjectType == query.ObjectType);
            if (!string.IsNullOrEmpty(query.Keyword))
                q = q.Where(x => x.Details.Contains(query.Keyword));
            if (!string.IsNullOrEmpty(query.IpAddress))
                q = q.Where(x => x.IpAddress == query.IpAddress);

            return q.OrderByDescending(x => x.CreateAt).ToList();
        }
    }
}