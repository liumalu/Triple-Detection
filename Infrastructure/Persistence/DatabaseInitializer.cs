using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace TripleDetection.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    private static readonly string DataDirectory = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Config");

    private static readonly string DbPath = Path.Combine(
        DataDirectory, "tripledetection.db");

    public static void Initialize()
    {
        if (!Directory.Exists(DataDirectory))
            Directory.CreateDirectory(DataDirectory);
        EnsureDatabaseCreated();
        SeedInitialData();
    }

    public static void EnsureDatabaseCreated()
    {
        var dbFilePath = DbPath;
        if (File.Exists(dbFilePath)) return;
        using var conn = new SqliteConnection($"Data Source={dbFilePath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Products (
    Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Description TEXT,
    Code TEXT, SolFilePath TEXT, ValidType INTEGER, ValidPeriod INTEGER, Status INTEGER,
    IsDeleted INTEGER NOT NULL DEFAULT 0, CreateAt TEXT NOT NULL, UpdateAt TEXT NOT NULL,
    CreateBy TEXT, UpdateBy TEXT
);
CREATE TABLE IF NOT EXISTS ProdTasks (
    Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, ProductId INTEGER,
    Status INTEGER NOT NULL DEFAULT 0, CreatedBy TEXT, ReviewedBy TEXT, ReviewedAt TEXT,
    ProductionDate TEXT, ExpirationDate TEXT, BatchNumber TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0, CreateAt TEXT NOT NULL, UpdateAt TEXT NOT NULL,
    CreateBy TEXT, UpdateBy TEXT
);
CREATE TABLE IF NOT EXISTS DetectionRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT, TaskId INTEGER, ProductId INTEGER,
    BatchNumber TEXT, IsOK INTEGER NOT NULL DEFAULT 0, ProductionDate TEXT, ExpirationDate TEXT,
    ImagePath TEXT, ElapsedMs INTEGER NOT NULL DEFAULT 0, DetectionTime TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0, CreateAt TEXT NOT NULL, UpdateAt TEXT NOT NULL,
    CreateBy TEXT, UpdateBy TEXT
);
CREATE TABLE IF NOT EXISTS SystemConfigs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT, Category TEXT NOT NULL, ConfigKey TEXT NOT NULL,
    ConfigValue TEXT, Description TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0, CreateAt TEXT NOT NULL, UpdateAt TEXT NOT NULL,
    CreateBy TEXT, UpdateBy TEXT
);
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT NOT NULL UNIQUE,
    RealName TEXT, Password TEXT, PasswordSalt TEXT, PasswordHash TEXT,
    Role TEXT NOT NULL, IsEnabled INTEGER NOT NULL DEFAULT 1, IsLocked INTEGER NOT NULL DEFAULT 0,
    LastLoginAt TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0, CreateAt TEXT NOT NULL, UpdateAt TEXT NOT NULL,
    CreateBy TEXT, UpdateBy TEXT
);
CREATE TABLE IF NOT EXISTS AuditLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL, UserName TEXT NOT NULL,
    Action TEXT NOT NULL, ObjectType TEXT NOT NULL, ObjectId INTEGER NOT NULL,
    Details TEXT, IpAddress TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0, CreateAt TEXT NOT NULL, UpdateAt TEXT NOT NULL,
    CreateBy TEXT, UpdateBy TEXT
);";
        cmd.ExecuteNonQuery();
    }

    public static void SeedInitialData()
    {
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Users";
        var count = Convert.ToInt64(cmd.ExecuteScalar());
        if (count > 0) return;

        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        cmd.CommandText = $@"
INSERT INTO Users (Username, RealName, Password, Role, IsEnabled, IsDeleted, CreateAt, UpdateAt)
VALUES ('admin', 'Administrator', 'admin123', 'Admin', 1, 0, '{now}', '{now}');
INSERT INTO Products (Name, Description, Status, IsDeleted, CreateAt, UpdateAt)
VALUES ('OCR检测产品A', '用于OCR文字识别检测', 1, 0, '{now}', '{now}'),
('缺陷检测产品B', '用于表面缺陷检测', 1, 0, '{now}', '{now}'),
('尺寸测量产品C', '用于尺寸测量', 1, 0, '{now}', '{now}');
INSERT INTO ProdTasks (Name, Status, IsDeleted, CreateAt, UpdateAt)
VALUES ('OCR检测任务-2025-05-01', 1, 0, '{now}', '{now}'),
('缺陷检测任务-2025-05-02', 1, 0, '{now}', '{now}'),
('尺寸测量任务-2025-05-03', 1, 0, '{now}', '{now}'),
('备料任务-待审核', 0, 0, '{now}', '{now}');";
        cmd.ExecuteNonQuery();
    }
}