using System;
using System.Collections.Generic;
using System.Data.SQLite;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Entities.Queries;
using TripleDetection.Domain.Repositories;

namespace TripleDetection.Infrastructure.Repositories
{

public class AuditLogRepository : SqliteRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(string connectionString) : base(connectionString) { }

    public IPagedResult<AuditLog> Query(AuditLogQuery query)
    {
        var conditions = new List<string>();
        if (query.StartDate.HasValue) conditions.Add($"CreateAt >= @StartDate");
        if (query.EndDate.HasValue) conditions.Add($"CreateAt <= @EndDate");
        if (query.UserId.HasValue) conditions.Add($"UserId = @UserId");
        if (!string.IsNullOrEmpty(query.Action)) conditions.Add($"Action = @Action");
        if (!string.IsNullOrEmpty(query.ObjectType)) conditions.Add($"ObjectType = @ObjectType");
        if (!string.IsNullOrEmpty(query.Keyword)) conditions.Add($"Details LIKE '%' || @Keyword || '%'");
        if (!string.IsNullOrEmpty(query.IpAddress)) conditions.Add($"IpAddress = @IpAddress");
        return QueryPaged(query, conditions);
    }

    public IEnumerable<AuditLog> Export(AuditLogQuery query)
    {
        var conditions = new List<string>();
        if (query.StartDate.HasValue) conditions.Add($"CreateAt >= @StartDate");
        if (query.EndDate.HasValue) conditions.Add($"CreateAt <= @EndDate");
        if (query.UserId.HasValue) conditions.Add($"UserId = @UserId");
        if (!string.IsNullOrEmpty(query.Action)) conditions.Add($"Action = @Action");
        if (!string.IsNullOrEmpty(query.ObjectType)) conditions.Add($"ObjectType = @ObjectType");
        if (!string.IsNullOrEmpty(query.Keyword)) conditions.Add($"Details LIKE '%' || @Keyword || '%'");
        if (!string.IsNullOrEmpty(query.IpAddress)) conditions.Add($"IpAddress = @IpAddress");
        return QueryAll(query, conditions);
    }

    private IPagedResult<AuditLog> QueryPaged(AuditLogQuery query, List<string> conditions)
    {
        var whereClause = conditions.Count > 0 ? string.Join(" AND ", conditions) : "1=1";
        var orderClause = string.IsNullOrEmpty(query.SortBy) ? "CreateAt DESC" : $"{query.SortBy} {(query.SortDescending ? "DESC" : "ASC")}";
        var offset = (query.PageIndex - 1) * query.PageSize;
        var countSql = $"SELECT COUNT(*) FROM AuditLogs WHERE {whereClause} AND IsDeleted = 0";
        var dataSql = $"SELECT * FROM AuditLogs WHERE {whereClause} AND IsDeleted = 0 ORDER BY {orderClause} LIMIT @PageSize OFFSET @Offset";
        using (var conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();
            int total; using (var cmd = new SQLiteCommand(countSql, conn)) { AddParams(cmd, query); total = Convert.ToInt32(cmd.ExecuteScalar()); }
            List<AuditLog> items;
            using (var cmd = new SQLiteCommand(dataSql, conn)) { cmd.Parameters.AddWithValue("@PageSize", query.PageSize); cmd.Parameters.AddWithValue("@Offset", offset); AddParams(cmd, query); items = ExecuteReader(cmd); }
            return new PagedResult<AuditLog>(items, total, query.PageIndex, query.PageSize);
        }
    }

    private IEnumerable<AuditLog> QueryAll(AuditLogQuery query, List<string> conditions)
    {
        var whereClause = conditions.Count > 0 ? string.Join(" AND ", conditions) : "1=1";
        var sql = $"SELECT * FROM AuditLogs WHERE {whereClause} AND IsDeleted = 0 ORDER BY CreateAt DESC";
        using (var conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                AddParams(cmd, query);
                return ExecuteReader(cmd);
            }
        }
    }

    private void AddParams(SQLiteCommand cmd, AuditLogQuery query)
    {
        if (query.StartDate.HasValue) cmd.Parameters.AddWithValue("@StartDate", query.StartDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        if (query.EndDate.HasValue) cmd.Parameters.AddWithValue("@EndDate", query.EndDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        if (query.UserId.HasValue) cmd.Parameters.AddWithValue("@UserId", query.UserId.Value);
        if (!string.IsNullOrEmpty(query.Action)) cmd.Parameters.AddWithValue("@Action", query.Action);
        if (!string.IsNullOrEmpty(query.ObjectType)) cmd.Parameters.AddWithValue("@ObjectType", query.ObjectType);
        if (!string.IsNullOrEmpty(query.Keyword)) cmd.Parameters.AddWithValue("@Keyword", query.Keyword);
        if (!string.IsNullOrEmpty(query.IpAddress)) cmd.Parameters.AddWithValue("@IpAddress", query.IpAddress);
    }

    private List<T> ExecuteReader<T>(SQLiteCommand cmd) where T : BaseEntity
    {
        var results = new List<T>();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read()) results.Add(MapRow<T>(reader));
        }
        return results;
    }

    private new T MapRow<T>(SQLiteDataReader reader) where T : BaseEntity
    {
        var entity = Activator.CreateInstance<T>();
        var type = typeof(T);
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            var prop = type.GetProperty(name);
            if (prop == null || reader.IsDBNull(i)) continue;
            var value = reader.GetValue(i);
            if (prop.PropertyType == typeof(bool) && value is long l) prop.SetValue(entity, l != 0);
            else if (prop.PropertyType == typeof(DateTime?) || prop.PropertyType == typeof(DateTime)) prop.SetValue(entity, DateTime.Parse(value.ToString()));
            else prop.SetValue(entity, value);
        }
        return entity;
    }
}
}