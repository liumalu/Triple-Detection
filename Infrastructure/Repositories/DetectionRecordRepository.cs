using System;
using System.Collections.Generic;
using System.Data.SQLite;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Entities.Queries;
using TripleDetection.Domain.Repositories;

namespace TripleDetection.Infrastructure.Repositories
{

public class DetectionRecordRepository : SqliteRepository<DetectionRecord>, IDetectionRecordRepository
{
    public DetectionRecordRepository(string connectionString) : base(connectionString) { }

    public IPagedResult<DetectionRecord> Query(DetectionRecordQuery query)
    {
        var conditions = new List<string>();
        if (query.StartDate.HasValue) conditions.Add($"DetectionTime >= @StartDate");
        if (query.EndDate.HasValue) conditions.Add($"DetectionTime <= @EndDate");
        if (query.TaskId.HasValue) conditions.Add($"TaskId = @TaskId");
        if (query.ProductId.HasValue) conditions.Add($"ProductId = @ProductId");
        if (!string.IsNullOrEmpty(query.BatchNumber)) conditions.Add($"BatchNumber LIKE '%' || @BatchNumber || '%'");
        if (query.IsOK.HasValue) conditions.Add($"IsOK = @IsOK");
        return QueryPaged(query, conditions);
    }

    public IEnumerable<DetectionRecord> Export(DetectionRecordQuery query)
    {
        var conditions = new List<string>();
        if (query.StartDate.HasValue) conditions.Add($"DetectionTime >= @StartDate");
        if (query.EndDate.HasValue) conditions.Add($"DetectionTime <= @EndDate");
        if (query.TaskId.HasValue) conditions.Add($"TaskId = @TaskId");
        if (query.ProductId.HasValue) conditions.Add($"ProductId = @ProductId");
        if (!string.IsNullOrEmpty(query.BatchNumber)) conditions.Add($"BatchNumber LIKE '%' || @BatchNumber || '%'");
        if (query.IsOK.HasValue) conditions.Add($"IsOK = @IsOK");
        return QueryAll(query, conditions);
    }

    private IPagedResult<DetectionRecord> QueryPaged(DetectionRecordQuery query, List<string> conditions)
    {
        var whereClause = conditions.Count > 0 ? string.Join(" AND ", conditions) : "1=1";
        var orderClause = string.IsNullOrEmpty(query.SortBy) ? "DetectionTime DESC" : $"{query.SortBy} {(query.SortDescending ? "DESC" : "ASC")}";
        var offset = (query.PageIndex - 1) * query.PageSize;
        var countSql = $"SELECT COUNT(*) FROM DetectionRecords WHERE {whereClause} AND IsDeleted = 0";
        var dataSql = $"SELECT * FROM DetectionRecords WHERE {whereClause} AND IsDeleted = 0 ORDER BY {orderClause} LIMIT @PageSize OFFSET @Offset";
        using (var conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();
            int total;
            using (var cmd = new SQLiteCommand(countSql, conn))
            {
                AddParams(cmd, query);
                total = Convert.ToInt32(cmd.ExecuteScalar());
            }
            List<DetectionRecord> items;
            using (var cmd = new SQLiteCommand(dataSql, conn))
            {
                cmd.Parameters.AddWithValue("@PageSize", query.PageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);
                AddParams(cmd, query);
                items = ExecuteReader(cmd);
            }
            return new PagedResult<DetectionRecord>(items, total, query.PageIndex, query.PageSize);
        }
    }

    private IEnumerable<DetectionRecord> QueryAll(DetectionRecordQuery query, List<string> conditions)
    {
        var whereClause = conditions.Count > 0 ? string.Join(" AND ", conditions) : "1=1";
        var sql = $"SELECT * FROM DetectionRecords WHERE {whereClause} AND IsDeleted = 0 ORDER BY DetectionTime DESC";
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

    private void AddParams(SQLiteCommand cmd, DetectionRecordQuery query)
    {
        if (query.StartDate.HasValue) cmd.Parameters.AddWithValue("@StartDate", query.StartDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        if (query.EndDate.HasValue) cmd.Parameters.AddWithValue("@EndDate", query.EndDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        if (query.TaskId.HasValue) cmd.Parameters.AddWithValue("@TaskId", query.TaskId.Value);
        if (query.ProductId.HasValue) cmd.Parameters.AddWithValue("@ProductId", query.ProductId.Value);
        if (!string.IsNullOrEmpty(query.BatchNumber)) cmd.Parameters.AddWithValue("@BatchNumber", query.BatchNumber);
        if (query.IsOK.HasValue) cmd.Parameters.AddWithValue("@IsOK", query.IsOK.Value ? 1 : 0);
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