using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Linq.Expressions;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories
{
    /// <summary>
    /// Audit log repository interface
    /// </summary>
    public interface IAuditLogRepository : IRepository<AuditLog>
    {
        IPagedResult<AuditLog> Query(AuditLogQuery query);
        IEnumerable<AuditLog> Export(AuditLogQuery query);
    }

    public class AuditLogRepository : SqliteRepository<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(IDbConnectionFactory factory) : base(factory) { }

        public IPagedResult<AuditLog> Query(AuditLogQuery query)
        {
            var conditions = new List<string>();

            if (query.StartDate.HasValue)
                conditions.Add($"CreateAt >= @StartDate");
            if (query.EndDate.HasValue)
                conditions.Add($"CreateAt <= @EndDate");
            if (query.UserId.HasValue)
                conditions.Add($"UserId = @UserId");
            if (!string.IsNullOrEmpty(query.Action))
                conditions.Add($"Action = @Action");
            if (!string.IsNullOrEmpty(query.ObjectType))
                conditions.Add($"ObjectType = @ObjectType");
            if (!string.IsNullOrEmpty(query.Keyword))
                conditions.Add($"Details LIKE '%' || @Keyword || '%'");
            if (!string.IsNullOrEmpty(query.IpAddress))
                conditions.Add($"IpAddress = @IpAddress");

            return QueryPaged(query, conditions);
        }

        public IEnumerable<AuditLog> Export(AuditLogQuery query)
        {
            var conditions = new List<string>();

            if (query.StartDate.HasValue)
                conditions.Add($"CreateAt >= @StartDate");
            if (query.EndDate.HasValue)
                conditions.Add($"CreateAt <= @EndDate");
            if (query.UserId.HasValue)
                conditions.Add($"UserId = @UserId");
            if (!string.IsNullOrEmpty(query.Action))
                conditions.Add($"Action = @Action");
            if (!string.IsNullOrEmpty(query.ObjectType))
                conditions.Add($"ObjectType = @ObjectType");
            if (!string.IsNullOrEmpty(query.Keyword))
                conditions.Add($"Details LIKE '%' || @Keyword || '%'");
            if (!string.IsNullOrEmpty(query.IpAddress))
                conditions.Add($"IpAddress = @IpAddress");

            return QueryAll(query, conditions);
        }

        private IPagedResult<AuditLog> QueryPaged(AuditLogQuery query, List<string> conditions)
        {
            var whereClause = conditions.Count > 0 ? string.Join(" AND ", conditions) : "1=1";
            var countSql = $"SELECT COUNT(*) FROM AuditLogs WHERE {whereClause} AND IsDeleted = 0";

            var orderClause = string.IsNullOrEmpty(query.SortBy)
                ? "CreateAt DESC"
                : $"{query.SortBy} {(query.SortDescending ? "DESC" : "ASC")}";

            var offset = query.PageIndex * query.PageSize;
            var dataSql = $"SELECT * FROM AuditLogs WHERE {whereClause} AND IsDeleted = 0 ORDER BY {orderClause} LIMIT @PageSize OFFSET @Offset";

            int total;
            List<AuditLog> items;

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
                    items = ExecuteReader<AuditLog>(cmd);
                }
            }

            return new PagedResult<AuditLog>(items, total, query.PageIndex, query.PageSize);
        }

        private List<AuditLog> QueryAll(AuditLogQuery query, List<string> conditions)
        {
            var whereClause = conditions.Count > 0 ? string.Join(" AND ", conditions) : "1=1";
            var sql = $"SELECT * FROM AuditLogs WHERE {whereClause} AND IsDeleted = 0 ORDER BY CreateAt DESC";

            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    AddQueryParams(cmd, query);
                    return ExecuteReader<AuditLog>(cmd);
                }
            }
        }

        private void AddQueryParams(SQLiteCommand cmd, AuditLogQuery query)
        {
            if (query.StartDate.HasValue) cmd.Parameters.AddWithValue("@StartDate", query.StartDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            if (query.EndDate.HasValue) cmd.Parameters.AddWithValue("@EndDate", query.EndDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            if (query.UserId.HasValue) cmd.Parameters.AddWithValue("@UserId", query.UserId.Value);
            if (!string.IsNullOrEmpty(query.Action)) cmd.Parameters.AddWithValue("@Action", query.Action);
            if (!string.IsNullOrEmpty(query.ObjectType)) cmd.Parameters.AddWithValue("@ObjectType", query.ObjectType);
            if (!string.IsNullOrEmpty(query.Keyword)) cmd.Parameters.AddWithValue("@Keyword", query.Keyword);
            if (!string.IsNullOrEmpty(query.IpAddress)) cmd.Parameters.AddWithValue("@IpAddress", query.IpAddress);
        }

}