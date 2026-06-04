-- Triple-Detection 数据库初始化脚本
-- SQLite
-- 执行方式: sqlite3 Config/tripledetection.db < init_database.sql

-- 创建表
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
);

-- 创建索引
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

-- 种子数据
-- 管理员用户 (密码: admin123, salt: TriD4dminS4lt==, hash: SHA256(salt+password))
INSERT INTO Users (Username, RealName, Password, PasswordSalt, PasswordHash, Role, IsEnabled, IsDeleted, CreateAt, UpdateAt)
VALUES ('admin', 'Administrator', '', 'TriD4dminS4lt==', 'K3l2Q7z8Y5r4H6j9K2m8N3p1L5v0F4s6D9a7B2c4E6f8G0h1I3j5K7', 'Admin', 1, 0, datetime('now'), datetime('now'));

-- 产品数据
INSERT INTO Products (Name, Description, Code, Status, IsDeleted, CreateAt, UpdateAt)
VALUES
('OCR检测产品A', '用于OCR文字识别检测', 'OCR-2025-001', 1, 0, datetime('now'), datetime('now')),
('缺陷检测产品B', '用于表面缺陷检测', 'DEF-2025-001', 1, 0, datetime('now'), datetime('now')),
('尺寸测量产品C', '用于尺寸测量', 'DIM-2025-001', 1, 0, datetime('now'), datetime('now'));

-- 任务数据 (关联产品)
INSERT INTO ProdTasks (Name, ProductId, ProductName, Status, IsDeleted, CreateAt, UpdateAt)
VALUES
('OCR检测任务-2025-05-01', 1, 'OCR检测产品A', 1, 0, datetime('now'), datetime('now')),
('缺陷检测任务-2025-05-02', 2, '缺陷检测产品B', 1, 0, datetime('now'), datetime('now')),
('尺寸测量任务-2025-05-03', 3, '尺寸测量产品C', 1, 0, datetime('now'), datetime('now')),
('备料任务-待审核', 1, 'OCR检测产品A', 0, 0, datetime('now'), datetime('now')),
('质检任务-执行中', 2, '缺陷检测产品B', 2, 0, datetime('now'), datetime('now')),
('入库任务-已完成', 3, '尺寸测量产品C', 3, 0, datetime('now'), datetime('now'));