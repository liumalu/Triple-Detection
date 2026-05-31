using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Entities.Queries;
using TripleDetection.Domain.Repositories;
using TripleDetection.Infrastructure.Exceptions;
using TripleDetection.Infrastructure.Persistence;

namespace TripleDetection.Infrastructure.Repositories;

public class SqliteRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly string _connectionString;

    public SqliteRepository(DbConnection connection)
    {
        _connectionString = connection.ConnectionString;
    }

    protected string? ConnectionString => _connectionString;

    public T? GetById(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var sql = $"SELECT * FROM {GetTableName()} WHERE Id = @Id AND IsDeleted = 0";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = cmd.ExecuteReader();
        if (reader.Read()) return MapRow<T>(reader);
        return null;
    }

    public IEnumerable<T> GetAll()
    {
        var sql = $"SELECT * FROM {GetTableName()} WHERE IsDeleted = 0";
        return ExecuteQuery(sql, null!);
    }

    public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
    {
        var whereClause = TranslatePredicate(predicate);
        var sql = $"SELECT * FROM {GetTableName()} WHERE {whereClause} AND IsDeleted = 0";
        return ExecuteQuery(sql, null!);
    }

    public void Add(T entity)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var columns = GetInsertColumns();
        var values = GetInsertValues(entity);
        var sql = $"INSERT INTO {GetTableName()} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values.Select(c => "@" + c))})";
        using var cmd = new SqliteCommand(sql, conn);
        AddEntityParameters(cmd, entity);
        cmd.ExecuteNonQuery();
    }

    public void Update(T entity)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var sets = GetUpdateSets();
        var sql = $"UPDATE {GetTableName()} SET {string.Join(", ", sets.Select(c => c + " = @" + c))} WHERE Id = @Id";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", entity.Id);
        AddEntityParameters(cmd, entity, excludeKeys: new[] { "Id", "CreateBy", "CreateAt" });
        cmd.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var sql = $"UPDATE {GetTableName()} SET IsDeleted = 1, UpdateAt = @Now WHERE Id = @Id";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@Now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.ExecuteNonQuery();
    }

    public int Count() => ExecuteScalar<int>($"SELECT COUNT(*) FROM {GetTableName()} WHERE IsDeleted = 0");

    public int Count(Expression<Func<T, bool>> predicate)
    {
        var whereClause = TranslatePredicate(predicate);
        return ExecuteScalar<int>($"SELECT COUNT(*) FROM {GetTableName()} WHERE {whereClause} AND IsDeleted = 0");
    }

    public IPagedResult<T> Query(PagedQuery query)
    {
        return QueryInternal(query, null!);
    }

    private IPagedResult<T> QueryInternal(PagedQuery query, List<string> extraConditions)
    {
        var whereClause = extraConditions != null && extraConditions.Count > 0
            ? string.Join(" AND ", extraConditions)
            : "1=1";

        var orderClause = string.IsNullOrEmpty(query.SortBy)
            ? "Id DESC"
            : $"{query.SortBy} {(query.SortDescending ? "DESC" : "ASC")}";

        var offset = (query.PageIndex - 1) * query.PageSize;
        var dataSql = $"SELECT * FROM {GetTableName()} WHERE {whereClause} AND IsDeleted = 0 ORDER BY {orderClause} LIMIT @PageSize OFFSET @Offset";
        var countSql = $"SELECT COUNT(*) FROM {GetTableName()} WHERE {whereClause} AND IsDeleted = 0";

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        int total;
        using (var cmd = new SqliteCommand(countSql, conn)) { total = Convert.ToInt32(cmd.ExecuteScalar()); }
        List<T> items;
        using (var cmd = new SqliteCommand(dataSql, conn))
        {
            cmd.Parameters.AddWithValue("@PageSize", query.PageSize);
            cmd.Parameters.AddWithValue("@Offset", offset);
            items = ExecuteReader(cmd);
        }
        return new PagedResult<T>(items, total, query.PageIndex, query.PageSize);
    }

    private string TranslatePredicate(Expression<Func<T, bool>> predicate) => Visit(predicate.Body);

    private string Visit(Expression expr)
    {
        if (expr is BinaryExpression binary)
        {
            if (binary.NodeType == ExpressionType.Equal || binary.NodeType == ExpressionType.NotEqual)
            {
                var left = Visit(binary.Left);
                var right = Visit(binary.Right);
                var op = binary.NodeType == ExpressionType.Equal ? "=" : "!=";
                return $"({left} {op} {right})";
            }
            if (binary.NodeType == ExpressionType.AndAlso) return $"({Visit(binary.Left)} AND {Visit(binary.Right)})";
            if (binary.NodeType == ExpressionType.OrElse) return $"({Visit(binary.Left)} OR {Visit(binary.Right)})";
        }
        if (expr is MemberExpression member)
        {
            // Handle closure-captured variables: member is a field of a closure class
            if (member.Expression is ConstantExpression closureConstant)
            {
                var closure = closureConstant.Value;
                if (closure != null)
                {
                    var fi = member.Member as System.Reflection.FieldInfo;
                    if (fi != null)
                    {
                        var fieldValue = fi.GetValue(closure);
                        var str = fieldValue?.ToString() ?? "";
                        // Handle boolean field values
                        if (str == "True" || str == "true") return "1";
                        if (str == "False" || str == "false") return "0";
                        return $"'{EscapeValue(str)}'";
                    }
                }
            }
            return GetColumnName(member.Member as PropertyInfo);
        }
        if (expr is ConstantExpression constant)
        {
            if (constant.Value == null) return "NULL";
            var val = constant.Value.ToString();
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "sql_debug.log"),
                $"  ConstantExpr val='{val}', type={constant.Type.Name}\n");
            // Handle boolean constants: output 1/0 instead of 'True'/'False'
            if (val == "True" || val == "true") return "1";
            if (val == "False" || val == "false") return "0";
            return $"'{EscapeValue(val)}'";
        }
        if (expr is MethodCallExpression call && call.Method.Name == "Contains")
        {
            var obj = call.Object as MemberExpression;
            var arg = call.Arguments[0] as ConstantExpression;
            return $"{GetColumnName(obj?.Member as PropertyInfo)} LIKE '%{EscapeValue(arg.Value?.ToString())}%'";
        }
        if (expr is UnaryExpression unary && unary.NodeType == ExpressionType.Not)
        {
            var notExpr = unary.Operand as MemberExpression;
            return $"({GetColumnName(notExpr?.Member as PropertyInfo)} = 0 OR {GetColumnName(notExpr?.Member as PropertyInfo)} = 0)";
        }
        return "1=1";
    }

    private List<T> ExecuteQuery(string sql, object[] _, List<SqliteParameter>? extraParams = null)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(sql, conn);
        if (extraParams != null) foreach (var p in extraParams) cmd.Parameters.Add(p);
        return ExecuteReader(cmd);
    }

    protected List<T> ExecuteReader(SqliteCommand cmd)
    {
        var results = new List<T>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) results.Add(MapRow<T>(reader));
        return results;
    }

    private T ExecuteScalar<T>(string sql)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(sql, conn);
        var result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value) return default(T)!;
        // Handle Int64 -> Int32 conversion for SQLite
        if (typeof(T) == typeof(int) && result is Int64 l) return (T)(object)(int)l;
        return (T)Convert.ChangeType(result, typeof(T));
    }

    protected T MapRow<T>(SqliteDataReader reader) where T : BaseEntity
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
            else if (prop.PropertyType == typeof(int) && value is Int64 il) prop.SetValue(entity, (int)il);
            else if (prop.PropertyType == typeof(int?) && value is Int64 il2) prop.SetValue(entity, (int?)il2);
            else if (prop.PropertyType == typeof(DateTime?) || prop.PropertyType == typeof(DateTime)) prop.SetValue(entity, DateTime.Parse(value.ToString()!));
            else prop.SetValue(entity, value);
        }
        return entity;
    }

    private string GetTableName()
    {
        var name = typeof(T).Name;
        if (name.EndsWith("y")) return name.Substring(0, name.Length - 1) + "ies";
        if (name.EndsWith("s")) return name + "es";
        return name + "s";
    }

    private string GetColumnName(PropertyInfo prop) => prop?.Name ?? "";

    private string EscapeValue(object value) => value?.ToString()?.Replace("'", "''") ?? "";

    private string[] GetInsertColumns() =>
        new[] { "CreateAt", "UpdateAt", "IsDeleted", "CreateBy", "UpdateBy" }
        .Concat(GetTypeProperties().Select(p => p.Name)).ToArray();

    private IEnumerable<string> GetInsertValues(T entity)
    {
        yield return "@CreateAt"; yield return "@UpdateAt"; yield return "0";
        yield return "@CreateBy"; yield return "@UpdateBy";
        foreach (var prop in GetTypeProperties()) yield return "@" + prop.Name;
    }

    private string[] GetUpdateSets() =>
        GetTypeProperties().Select(p => p.Name).Concat(new[] { "UpdateAt", "UpdateBy" }).ToArray();

    private void AddEntityParameters(SqliteCommand cmd, T entity, string[]? excludeKeys = null)
    {
        excludeKeys ??= Array.Empty<string>();
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        cmd.Parameters.AddWithValue("@CreateAt", entity.CreateAt == default ? now : entity.CreateAt.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@UpdateAt", now);
        cmd.Parameters.AddWithValue("@CreateBy", entity.CreateBy ?? "");
        cmd.Parameters.AddWithValue("@UpdateBy", entity.UpdateBy ?? "");
        foreach (var prop in GetTypeProperties())
        {
            if (excludeKeys.Contains(prop.Name)) continue;
            var val = prop.GetValue(entity);
            if (prop.PropertyType == typeof(bool)) val = (bool)val! ? 1 : 0;
            cmd.Parameters.AddWithValue("@" + prop.Name, val ?? DBNull.Value);
        }
    }

    private PropertyInfo[] GetTypeProperties() =>
        typeof(T).GetProperties()
            .Where(p => !ExcludedProperties.Contains(p.Name) && p.CanRead && p.CanWrite).ToArray();

    private static readonly string[] ExcludedProperties = new[]
    {
        "Id", "CreateBy", "UpdateBy", "CreateAt", "UpdateAt", "IsDeleted",
        "StatusText"
    };
}