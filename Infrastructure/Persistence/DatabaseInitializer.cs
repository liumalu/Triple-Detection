using System;
using System.IO;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;

namespace TripleDetection.Infrastructure.Persistence
{

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
        using (var conn = new SQLiteConnection($"Data Source={dbFilePath}"))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Products (
    Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Description TEXT,
    Code TEXT, SolFilePath TEXT, ValidType INTEGER, ValidPeriod INTEGER, Status INTEGER,
    IsDeleted INTEGER NOT NULL DEFAULT 0, CreateAt TEXT NOT NULL, UpdateAt TEXT NOT NULL,
    CreateBy TEXT, UpdateBy TEXT
);
CREATE TABLE IF NOT EXISTS ProdTasks (
    Id INTEGER PRIMARY KEY AUTOINCREMENT, TaskName TEXT NOT NULL, ProductId INTEGER, ProductName TEXT,
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
    CreateBy TEXT, UpdateBy TEXT,
    TaskName TEXT, ProductName TEXT, ProductCode TEXT
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
    FromStatus TEXT, ToStatus TEXT, RelatedRecordId INTEGER,
    IsDeleted INTEGER NOT NULL DEFAULT 0, CreateAt TEXT NOT NULL, UpdateAt TEXT NOT NULL,
    CreateBy TEXT, UpdateBy TEXT
);
CREATE INDEX IF NOT EXISTS idx_users_deleted ON Users(IsDeleted);
CREATE INDEX IF NOT EXISTS idx_tasks_status ON ProdTasks(Status);
CREATE INDEX IF NOT EXISTS idx_tasks_productid ON ProdTasks(ProductId);
CREATE INDEX IF NOT EXISTS idx_tasks_deleted ON ProdTasks(IsDeleted);
CREATE INDEX IF NOT EXISTS idx_auditlogs_userid ON AuditLogs(UserId);
CREATE INDEX IF NOT EXISTS idx_auditlogs_deleted ON AuditLogs(IsDeleted);
CREATE INDEX IF NOT EXISTS idx_detectionrecords_taskid ON DetectionRecords(TaskId);
CREATE INDEX IF NOT EXISTS idx_detectionrecords_productid ON DetectionRecords(ProductId);
CREATE INDEX IF NOT EXISTS idx_detectionrecords_deleted ON DetectionRecords(IsDeleted);
CREATE INDEX IF NOT EXISTS idx_products_code ON Products(Code);
CREATE INDEX IF NOT EXISTS idx_products_deleted ON Products(IsDeleted);
CREATE INDEX IF NOT EXISTS idx_detectionrecords_date ON DetectionRecords(DetectionTime);
CREATE INDEX IF NOT EXISTS idx_detectionrecords_task_date ON DetectionRecords(TaskId, DetectionTime);
CREATE INDEX IF NOT EXISTS idx_auditlogs_action_date ON AuditLogs(Action, CreateAt);
CREATE INDEX IF NOT EXISTS idx_auditlogs_user_date ON AuditLogs(UserId, CreateAt);
CREATE INDEX IF NOT EXISTS idx_auditlogs_object ON AuditLogs(ObjectType, ObjectId);
";
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static void SeedInitialData()
    {
        using (var conn = new SQLiteConnection($"Data Source={DbPath}"))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Users";
                var count = Convert.ToInt64(cmd.ExecuteScalar());
                if (count > 0) return;

                var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                // Generate password hash for admin (salt: TriD4dminS4lt==, password: admin123)
                var salt = "TriD4dminS4lt==";
                var passwordHash = ComputeSha256Hash(salt + "admin123");

                // Insert admin user with hashed password
                cmd.CommandText = $@"
INSERT INTO Users (Username, RealName, Password, PasswordSalt, PasswordHash, Role, IsEnabled, IsDeleted, CreateAt, UpdateAt)
VALUES ('admin', 'Administrator', '', '{salt}', '{passwordHash}', 'Admin', 1, 0, '{now}', '{now}');
INSERT INTO Products (Name, Description, Code, Status, IsDeleted, CreateAt, UpdateAt)
VALUES
('OCR检测产品A', '用于OCR文字识别检测', 'OCR-2025-001', 1, 0, '{now}', '{now}'),
('缺陷检测产品B', '用于表面缺陷检测', 'DEF-2025-001', 1, 0, '{now}', '{now}'),
('尺寸测量产品C', '用于尺寸测量', 'DIM-2025-001', 1, 0, '{now}', '{now}');
INSERT INTO ProdTasks (TaskName, ProductId, ProductName, Status, IsDeleted, CreateAt, UpdateAt)
VALUES
('OCR检测任务-2025-05-01', 1, 'OCR检测产品A', 1, 0, '{now}', '{now}'),
('缺陷检测任务-2025-05-02', 2, '缺陷检测产品B', 1, 0, '{now}', '{now}'),
('尺寸测量任务-2025-05-03', 3, '尺寸测量产品C', 1, 0, '{now}', '{now}'),
('备料任务-待审核', 1, 'OCR检测产品A', 0, 0, '{now}', '{now}'),
('质检任务-执行中', 2, '缺陷检测产品B', 2, 0, '{now}', '{now}'),
('入库任务-已完成', 3, '尺寸测量产品C', 3, 0, '{now}', '{now}');
";
                cmd.ExecuteNonQuery();
            }
        }
    }

    private static string ComputeSha256Hash(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
}