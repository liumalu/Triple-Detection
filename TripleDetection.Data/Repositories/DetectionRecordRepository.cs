using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories.Sqlite;

namespace TripleDetection.Data.Repositories
{
    public interface IDetectionRecordRepository : IRepository<DetectionRecord>
    {
        IPagedResult<DetectionRecord> Query(DetectionRecordQuery query);
        IEnumerable<DetectionRecord> Export(DetectionRecordQuery query);
    }

    public class DetectionRecordRepository : SqliteRepository<DetectionRecord>, IDetectionRecordRepository
    {
        public DetectionRecordRepository(SqliteDbContext context) : base(context) { }

        public IPagedResult<DetectionRecord> Query(DetectionRecordQuery query)
        {
            var q = _dbSet.Where(x => !x.IsDeleted);

            if (query.StartDate.HasValue)
                q = q.Where(x => x.DetectionTime >= query.StartDate.Value);
            if (query.EndDate.HasValue)
                q = q.Where(x => x.DetectionTime <= query.EndDate.Value);
            if (query.TaskId.HasValue)
                q = q.Where(x => x.TaskId == query.TaskId.Value);
            if (query.ProductId.HasValue)
                q = q.Where(x => x.ProductId == query.ProductId.Value);
            if (!string.IsNullOrEmpty(query.BatchNumber))
                q = q.Where(x => x.BatchNumber.Contains(query.BatchNumber));
            if (query.IsOK.HasValue)
                q = q.Where(x => x.IsOK == query.IsOK.Value);

            if (!string.IsNullOrEmpty(query.SortBy))
                q = ApplySorting(q, query.SortBy, query.SortDescending);
            else
                q = q.OrderByDescending(x => x.DetectionTime);

            var total = q.Count();
            var items = q.Skip(query.PageIndex * query.PageSize)
                         .Take(query.PageSize)
                         .ToList();

            return new PagedResult<DetectionRecord>(items, total, query.PageIndex, query.PageSize);
        }

        public IEnumerable<DetectionRecord> Export(DetectionRecordQuery query)
        {
            var q = _dbSet.Where(x => !x.IsDeleted);

            if (query.StartDate.HasValue)
                q = q.Where(x => x.DetectionTime >= query.StartDate.Value);
            if (query.EndDate.HasValue)
                q = q.Where(x => x.DetectionTime <= query.EndDate.Value);
            if (query.TaskId.HasValue)
                q = q.Where(x => x.TaskId == query.TaskId.Value);
            if (query.ProductId.HasValue)
                q = q.Where(x => x.ProductId == query.ProductId.Value);
            if (!string.IsNullOrEmpty(query.BatchNumber))
                q = q.Where(x => x.BatchNumber.Contains(query.BatchNumber));
            if (query.IsOK.HasValue)
                q = q.Where(x => x.IsOK == query.IsOK.Value);

            return q.OrderByDescending(x => x.DetectionTime).ToList();
        }
    }
}