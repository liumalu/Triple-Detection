using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories
{
    public interface IDetectionRecordRepository : IRepository<DetectionRecord>
    {
        IPagedResult<DetectionRecord> Query(DetectionRecordQuery query);
        IEnumerable<DetectionRecord> Export(DetectionRecordQuery query);
    }

    public class DetectionRecordRepository : SqliteRepository<DetectionRecord>, IDetectionRecordRepository
    {
        public DetectionRecordRepository(IDbConnectionFactory factory) : base(factory) { }

        public IPagedResult<DetectionRecord> Query(DetectionRecordQuery query)
        {
            var conditions = new List<string>();

            if (query.StartDate.HasValue)
                conditions.Add($"DetectionTime >= @StartDate");
            if (query.EndDate.HasValue)
                conditions.Add($"DetectionTime <= @EndDate");
            if (query.TaskId.HasValue)
                conditions.Add($"TaskId = @TaskId");
            if (query.ProductId.HasValue)
                conditions.Add($"ProductId = @ProductId");
            if (!string.IsNullOrEmpty(query.BatchNumber))
                conditions.Add($"BatchNumber LIKE '%' || @BatchNumber || '%'");
            if (query.IsOK.HasValue)
                conditions.Add($"IsOK = @IsOK");

            return QueryPaged(query, conditions);
        }

        public IEnumerable<DetectionRecord> Export(DetectionRecordQuery query)
        {
            var conditions = new List<string>();

            if (query.StartDate.HasValue)
                conditions.Add($"DetectionTime >= @StartDate");
            if (query.EndDate.HasValue)
                conditions.Add($"DetectionTime <= @EndDate");
            if (query.TaskId.HasValue)
                conditions.Add($"TaskId = @TaskId");
            if (query.ProductId.HasValue)
                conditions.Add($"ProductId = @ProductId");
            if (!string.IsNullOrEmpty(query.BatchNumber))
                conditions.Add($"BatchNumber LIKE '%' || @BatchNumber || '%'");
            if (query.IsOK.HasValue)
                conditions.Add($"IsOK = @IsOK");

            return QueryAll(query, conditions);
        }

        private IPagedResult<DetectionRecord> QueryPaged(DetectionRecordQuery query, List<string> conditions)
        {
            var whereClause = conditions.Count > 0 ? string.Join(" AND ", conditions) : "1=1";
            var countSql = $"SELECT COUNT(*) FROM DetectionRecords WHERE {whereClause} AND IsDeleted = 0";

            var orderClause = string.IsNullOrEmpty(query.SortBy)
                ? "DetectionTime DESC"
                : $"{query.SortBy} {(query.SortDescending ? "DESC" : "ASC")}";

            var offset = query.PageIndex * query.PageSize;
            var dataSql = $"SELECT * FROM DetectionRecords WHERE {whereClause} AND IsDeleted = 0 ORDER BY {orderClause} LIMIT @PageSize OFFSET @Offset";

            int total;
            List<DetectionRecord> items;

            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(countSql, conn))
                {
                    AddQueryParams(cmd, query);
                    total = Convert.ToInt32(cmd.ExecuteScalar());
                }
                using (var cmd = new SQLiteCommand(dataSql, conn))
                {
                    cmd.Parameters.AddWithValue("@PageSize", query.PageSize);
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    AddQueryParams(cmd, query);
                    items = ExecuteReader<DetectionRecord>(cmd);
                }
            }

            return new PagedResult<DetectionRecord>(items, total, query.PageIndex, query.PageSize);
        }

        private List<DetectionRecord> QueryAll(DetectionRecordQuery query, List<string> conditions)
        {
            var whereClause = conditions.Count > 0 ? string.Join(" AND ", conditions) : "1=1";
            var sql = $"SELECT * FROM DetectionRecords WHERE {whereClause} AND IsDeleted = 0 ORDER BY DetectionTime DESC";

            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    AddQueryParams(cmd, query);
                    return ExecuteReader<DetectionRecord>(cmd);
                }
            }
        }

        private void AddQueryParams(SQLiteCommand cmd, DetectionRecordQuery query)
        {
            if (query.StartDate.HasValue) cmd.Parameters.AddWithValue("@StartDate", query.StartDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            if (query.EndDate.HasValue) cmd.Parameters.AddWithValue("@EndDate", query.EndDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            if (query.TaskId.HasValue) cmd.Parameters.AddWithValue("@TaskId", query.TaskId.Value);
            if (query.ProductId.HasValue) cmd.Parameters.AddWithValue("@ProductId", query.ProductId.Value);
            if (!string.IsNullOrEmpty(query.BatchNumber)) cmd.Parameters.AddWithValue("@BatchNumber", query.BatchNumber);
            if (query.IsOK.HasValue) cmd.Parameters.AddWithValue("@IsOK", query.IsOK.Value ? 1 : 0);
        }

}