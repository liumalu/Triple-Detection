using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories.Sqlite
{
    /// <summary>
    /// SQLite 仓储实现 - 原生 ADO.NET，不依赖 EF6
    /// </summary>
    public class SqliteRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly string ConnectionString;

        public SqliteRepository(IDbConnectionFactory factory)
        {
            var conn = factory.CreateConnection();
            ConnectionString = conn.ConnectionString;
            conn.Dispose();
        }

        public T GetById(int id)
        {
            var sql = $"SELECT * FROM {GetTableName()} WHERE Id = @Id AND IsDeleted = 0";
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) return MapRow<T>(reader);
                    }
                }
            }
            return null;
        }

        public IEnumerable<T> GetAll()
        {
            var sql = $"SELECT * FROM {GetTableName()} WHERE IsDeleted = 0";
            return ExecuteQuery(sql);
        }

        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
        {
            var whereClause = TranslatePredicate(predicate);
            var sql = $"SELECT * FROM {GetTableName()} WHERE {whereClause} AND IsDeleted = 0";
            return ExecuteQuery(sql);
        }

        public void Add(T entity)
        {
            var columns = GetInsertColumns();
            var values = GetInsertValues(entity);
            var sql = $"INSERT INTO {GetTableName()} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values.Select(c => "@" + c))})";

            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    AddEntityParameters(cmd, entity);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(T entity)
        {
            var sets = GetUpdateSets();
            var sql = $"UPDATE {GetTableName()} SET {string.Join(", ", sets.Select(c => c + " = @" + c))} WHERE Id = @Id";

            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", entity.Id);
                    AddEntityParameters(cmd, entity, excludeKeys: new[] { "Id", "CreateBy", "CreateAt" });
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            var sql = $"UPDATE {GetTableName()} SET IsDeleted = 1, UpdateAt = @Now WHERE Id = @Id";
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public int Count()
        {
            var sql = $"SELECT COUNT(*) FROM {GetTableName()} WHERE IsDeleted = 0";
            return ExecuteScalar<int>(sql);
        }

        public int Count(Expression<Func<T, bool>> predicate)
        {
            var whereClause = TranslatePredicate(predicate);
            var sql = $"SELECT COUNT(*) FROM {GetTableName()} WHERE {whereClause} AND IsDeleted = 0";
            return ExecuteScalar<int>(sql);
        }

        public IPagedResult<T> Query(PagedQuery query)
        {
            return QueryInternal(query, null);
        }

        public IPagedResult<T> Query(ProductQuery query)
        {
            var conditions = new List<string>();

            if (!string.IsNullOrEmpty(query.Code))
                conditions.Add($"Code LIKE '%' || @Code || '%'");
            if (!string.IsNullOrEmpty(query.Name))
                conditions.Add($"Name LIKE '%' || @Name || '%'");
            if (query.Status.HasValue)
                conditions.Add($"Status = @Status");
            if (query.CreateAtFrom.HasValue)
                conditions.Add($"CreateAt >= @CreateAtFrom");
            if (query.CreateAtTo.HasValue)
                conditions.Add($"CreateAt <= @CreateAtTo");

            return QueryInternal(query, conditions);
        }

        public IPagedResult<T> Query(TaskQuery query)
        {
            var conditions = new List<string>();

            if (!string.IsNullOrEmpty(query.Name))
                conditions.Add($"Name LIKE '%' || @Name || '%'");
            if (query.ProductId.HasValue)
                conditions.Add($"ProductId = @ProductId");
            if (query.Status.HasValue)
                conditions.Add($"Status = @Status");
            if (query.ProductionDateFrom.HasValue)
                conditions.Add($"ProductionDate >= @ProductionDateFrom");
            if (query.ProductionDateTo.HasValue)
                conditions.Add($"ProductionDate <= @ProductionDateTo");
            if (!string.IsNullOrEmpty(query.BatchNumber))
                conditions.Add($"BatchNumber LIKE '%' || @BatchNumber || '%'");

            return QueryInternal(query, conditions);
        }

        public IPagedResult<T> Query(UserQuery query)
        {
            var conditions = new List<string>();

            if (!string.IsNullOrEmpty(query.ExactUsername))
                conditions.Add($"Username = @ExactUsername");
            if (!string.IsNullOrEmpty(query.Username))
                conditions.Add($"Username LIKE '%' || @Username || '%'");
            if (!string.IsNullOrEmpty(query.Role))
                conditions.Add($"Role = @Role");
            if (!string.IsNullOrEmpty(query.StatusText))
                conditions.Add($"(CASE WHEN NOT IsEnabled THEN '已禁用' WHEN IsLocked THEN '已锁定' ELSE '正常' END) = @StatusText");

            return QueryInternal(query, conditions);
        }

        private IPagedResult<T> QueryInternal(PagedQuery query, List<string> extraConditions)
        {
            var whereClause = extraConditions != null && extraConditions.Count > 0
                ? string.Join(" AND ", extraConditions)
                : "1=1";

            var countSql = $"SELECT COUNT(*) FROM {GetTableName()} WHERE {whereClause} AND IsDeleted = 0";

            var orderClause = string.IsNullOrEmpty(query.SortBy)
                ? "Id DESC"
                : $"{query.SortBy} {(query.SortDescending ? "DESC" : "ASC")}";

            var offset = query.PageIndex * query.PageSize;
            var dataSql = $"SELECT * FROM {GetTableName()} WHERE {whereClause} AND IsDeleted = 0 ORDER BY {orderClause} LIMIT @PageSize OFFSET @Offset";

            int total;
            List<T> items;

            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(countSql, conn))
                {
                    total = Convert.ToInt32(cmd.ExecuteScalar());
                }
                using (var cmd = new SQLiteCommand(dataSql, conn))
                {
                    cmd.Parameters.AddWithValue("@PageSize", query.PageSize);
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    items = ExecuteReader(cmd);
                }
            }

            return new PagedResult<T>(items, total, query.PageIndex, query.PageSize);
        }

        // --- 表达式树翻译 ---
        private string TranslatePredicate(Expression<Func<T, bool>> predicate)
        {
            return Visit(predicate.Body);
        }

        private string Visit(Expression expr)
        {
            if (expr is BinaryExpression binary)
            {
                if (binary.NodeType == ExpressionType.Equal || binary.NodeType == ExpressionType.NotEqual)
                {
                    var left = Visit(binary.Left);
                    var right = Visit(binary.Right);
                    var op = binary.NodeType == ExpressionType.Equal ? "=" : "!=";
                    return $"({left} {op} '{EscapeValue(right)}')";
                }
                if (binary.NodeType == ExpressionType.AndAlso)
                {
                    return $"({Visit(binary.Left)} AND {Visit(binary.Right)})";
                }
                if (binary.NodeType == ExpressionType.OrElse)
                {
                    return $"({Visit(binary.Left)} OR {Visit(binary.Right)})";
                }
            }

            if (expr is MemberExpression member)
            {
                // 简单属性访问
                return GetColumnName(member.Member as PropertyInfo);
            }

            if (expr is ConstantExpression constant)
            {
                return constant.Value?.ToString() ?? "NULL";
            }

            if (expr is MethodCallExpression call)
            {
                // 支持 .Contains()
                if (call.Method.Name == "Contains")
                {
                    var obj = call.Object as MemberExpression;
                    var arg = call.Arguments[0] as ConstantExpression;
                    var colName = GetColumnName(obj.Member as PropertyInfo);
                    return $"{colName} LIKE '%{EscapeValue(arg.Value?.ToString())}%'";
                }
            }

            // 处理 !x.IsDeleted 这种情况
            if (expr is UnaryExpression unary && unary.NodeType == ExpressionType.Not)
            {
                var member = unary.Operand as MemberExpression;
                if (member != null)
                {
                    var colName = GetColumnName(member.Member as PropertyInfo);
                    return $"({colName} = 0 OR {colName} = FALSE)";
                }
            }

            return "1=1";
        }

        // --- SQL 辅助 ---
        private List<T> ExecuteQuery(string sql)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    return ExecuteReader(cmd);
                }
            }
        }

        private List<T> ExecuteReader(SQLiteCommand cmd)
        {
            var results = new List<T>();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    results.Add(MapRow<T>(reader));
                }
            }
            return results;
        }

        private T ExecuteScalar<T>(string sql)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value) return default(T);
                    return (T)Convert.ChangeType(result, typeof(T));
                }
            }
        }

        private T MapRow<T>(SQLiteDataReader reader) where T : BaseEntity
        {
            var entity = Activator.CreateInstance<T>();
            var type = typeof(T);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                var prop = type.GetProperty(name);
                if (prop == null || reader.IsDBNull(i)) continue;

                var value = reader.GetValue(i);
                if (prop.PropertyType == typeof(bool) && value is long l)
                    prop.SetValue(entity, l != 0);
                else if (prop.PropertyType == typeof(DateTime?) || prop.PropertyType == typeof(DateTime))
                    prop.SetValue(entity, DateTime.Parse(value.ToString()));
                else
                    prop.SetValue(entity, value);
            }

            return entity;
        }

        private string GetTableName()
        {
            // User -> Users, Product -> Products, DetectionRecord -> DetectionRecords
            var name = typeof(T).Name;
            if (name.EndsWith("y")) return name.Substring(0, name.Length - 1) + "ies";
            if (name.EndsWith("s")) return name + "es";
            return name + "s";
        }

        private string GetColumnName(PropertyInfo prop)
        {
            return prop?.Name ?? "";
        }

        private string EscapeValue(object value)
        {
            return value?.ToString().Replace("'", "''") ?? "";
        }

        private string[] GetInsertColumns()
        {
            return new[] { "CreateAt", "UpdateAt", "IsDeleted", "CreateBy", "UpdateBy" }
                .Concat(GetTypeProperties().Select(p => p.Name))
                .ToArray();
        }

        private IEnumerable<string> GetInsertValues(T entity)
        {
            yield return "@CreateAt";
            yield return "@UpdateAt";
            yield return "0"; // IsDeleted
            yield return "@CreateBy";
            yield return "@UpdateBy";

            foreach (var prop in GetTypeProperties())
            {
                yield return "@" + prop.Name;
            }
        }

        private string[] GetUpdateSets()
        {
            return GetTypeProperties()
                .Select(p => p.Name)
                .Concat(new[] { "UpdateAt", "UpdateBy" })
                .ToArray();
        }

        private void AddEntityParameters(SQLiteCommand cmd, T entity, string[] excludeKeys = null)
        {
            excludeKeys = excludeKeys ?? new string[0];
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            cmd.Parameters.AddWithValue("@CreateAt", entity.CreateAt == default(DateTime) ? now : entity.CreateAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@UpdateAt", now);
            cmd.Parameters.AddWithValue("@CreateBy", entity.CreateBy ?? "");
            cmd.Parameters.AddWithValue("@UpdateBy", entity.UpdateBy ?? "");

            foreach (var prop in GetTypeProperties())
            {
                if (excludeKeys.Contains(prop.Name)) continue;
                var val = prop.GetValue(entity);
                if (prop.PropertyType == typeof(bool))
                    val = (bool)val ? 1 : 0;
                cmd.Parameters.AddWithValue("@" + prop.Name, val ?? DBNull.Value);
            }
        }

        private PropertyInfo[] GetTypeProperties()
        {
            return typeof(T).GetProperties()
                .Where(p => p.Name != "Id" && p.Name != "CreateBy" && p.Name != "UpdateBy" &&
                            p.Name != "CreateAt" && p.Name != "UpdateAt" && p.Name != "IsDeleted")
                .ToArray();
        }
    }
}