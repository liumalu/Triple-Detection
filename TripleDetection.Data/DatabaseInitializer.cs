using System;
using System.Data.SQLite;
using System.IO;

namespace TripleDetection.Data
{
    /// <summary>
    /// 数据库初始化器 - 创建数据库并导入初始数据
    /// </summary>
    public static class DatabaseInitializer
    {
        private static readonly string DataDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Data");

        private static readonly string DbPath = Path.Combine(
            DataDirectory, "tripledetection.db");

        /// <summary>
        /// 初始化数据库 - 调用此方法启动应用时
        /// </summary>
        public static void Initialize()
        {
            // 确保 Data 目录存在
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            // 创建数据库和表
            EnsureDatabaseCreated();

            // 导入初始数据（如果数据库为空）
            SeedInitialData();
        }

        /// <summary>
        /// 确保数据库和表已创建
        /// </summary>
        public static void EnsureDatabaseCreated()
        {
            // 直接用 SQLite 原生 API 创建数据库文件（绕过 EF provider 问题）
            var dbFilePath = DbPath;
            if (!File.Exists(dbFilePath))
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection($"Data Source={dbFilePath};Version=3;"))
                {
                    conn.Open();
                    // 建表 SQL
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Products (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    CreateAt TEXT NOT NULL,
    UpdateAt TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS Tasks (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    Status INTEGER NOT NULL DEFAULT 0,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    CreateAt TEXT NOT NULL,
    UpdateAt TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS DetectionRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TaskId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    BatchNumber TEXT,
    IsOK INTEGER NOT NULL DEFAULT 0,
    ProductionDate TEXT,
    ExpirationDate TEXT,
    ImagePath TEXT,
    ElapsedMs INTEGER NOT NULL DEFAULT 0,
    DetectionTime TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    CreateAt TEXT NOT NULL,
    UpdateAt TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS SystemConfigs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Category TEXT NOT NULL,
    ConfigKey TEXT NOT NULL,
    ConfigValue TEXT,
    Description TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    CreateAt TEXT NOT NULL,
    UpdateAt TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    RealName TEXT,
    Password TEXT NOT NULL,
    PasswordSalt TEXT,
    PasswordHash TEXT,
    Role TEXT NOT NULL,
    Status INTEGER NOT NULL DEFAULT 0,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    IsLocked INTEGER NOT NULL DEFAULT 0,
    LastLoginAt TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    CreateAt TEXT NOT NULL,
    UpdateAt TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS AuditLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    UserName TEXT NOT NULL,
    Action TEXT NOT NULL,
    ObjectType TEXT NOT NULL,
    ObjectId INTEGER NOT NULL,
    Details TEXT,
    IpAddress TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    CreateAt TEXT NOT NULL,
    UpdateAt TEXT NOT NULL
);";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// 导入初始数据（仅当数据库为空时）
        /// </summary>
        public static void SeedInitialData()
        {
            // 直接用原生 SQLite 查询，避免 EF context 触发 SqlServer provider
            using (var conn = new System.Data.SQLite.SQLiteConnection($"Data Source={DbPath};Version=3;"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM Users";
                    var count = Convert.ToInt64(cmd.ExecuteScalar());
                    if (count > 0)
                    {
                        return; // 已有数据，跳过
                    }
                }
            }
        }

    }
}