using System;
using System.Collections.Generic;
using System.Data.SQLite;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Repositories;

namespace TripleDetection.Infrastructure.Repositories
{

public class SystemConfigRepository : SqliteRepository<SystemConfig>, ISystemConfigRepository
{
    public SystemConfigRepository(string connectionString) : base(connectionString) { }

    public SystemConfig GetByCategoryAndKey(string category, string key)
    {
        var sql = $"SELECT * FROM SystemConfigs WHERE Category = @Category AND Key = @Key AND IsDeleted = 0";
        using (var conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Category", category);
                cmd.Parameters.AddWithValue("@Key", key);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read()) return MapRow<SystemConfig>(reader);
                }
            }
        }
        return null;
    }

    public void SaveOrUpdate(SystemConfig config)
    {
        var existing = GetByCategoryAndKey(config.Category, config.Key);
        if (existing != null)
        {
            var sql = @"UPDATE SystemConfigs SET Value = @Value, Description = @Description, UpdateAt = @UpdateAt WHERE Id = @Id";
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", existing.Id);
                    cmd.Parameters.AddWithValue("@Value", config.Value ?? "");
                    cmd.Parameters.AddWithValue("@Description", config.Description ?? "");
                    cmd.Parameters.AddWithValue("@UpdateAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
            }
        }
        else
        {
            var sql = @"INSERT INTO SystemConfigs (Category, Key, Value, Description, CreateAt, UpdateAt, IsDeleted, CreateBy, UpdateBy) VALUES (@Category, @Key, @Value, @Description, @CreateAt, @UpdateAt, 0, @CreateBy, @UpdateBy)";
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    cmd.Parameters.AddWithValue("@Category", config.Category ?? "");
                    cmd.Parameters.AddWithValue("@Key", config.Key ?? "");
                    cmd.Parameters.AddWithValue("@Value", config.Value ?? "");
                    cmd.Parameters.AddWithValue("@Description", config.Description ?? "");
                    cmd.Parameters.AddWithValue("@CreateAt", now);
                    cmd.Parameters.AddWithValue("@UpdateAt", now);
                    cmd.Parameters.AddWithValue("@CreateBy", config.CreateBy ?? "");
                    cmd.Parameters.AddWithValue("@UpdateBy", config.UpdateBy ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    }
}