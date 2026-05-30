-- ============================================================
-- Triple-Detection Database Initialization Script
-- Database: SQLite (tripledetection.db)
-- ============================================================

-- -----------------------------------------------------------
-- Users Table Seed Data
-- Default admin user (password is a placeholder - hashing
-- is handled by the application service layer)
-- -----------------------------------------------------------

INSERT INTO Users (Id, Username, RealName, Password, PasswordSalt, PasswordHash, Role, IsEnabled, IsLocked, LastLoginAt, IsDeleted, CreateBy, UpdateBy, CreateAt, UpdateAt)
VALUES (
    NULL,  -- Id auto-increment
    'admin',
    'Administrator',
    'admin123',
    NULL,  -- PasswordSalt (legacy plain text, will migrate on first login)
    NULL,  -- PasswordHash (legacy plain text, will migrate on first login)
    'Admin',
    1,  -- IsEnabled = true
    0,  -- IsLocked = false
    NULL,  -- LastLoginAt (no login yet)
    0,  -- IsDeleted = false
    'system',
    'system',
    '2025-01-01T00:00:00',
    '2025-01-01T00:00:00'
);

-- -----------------------------------------------------------
-- Products Table Seed Data
-- -----------------------------------------------------------

INSERT INTO Products (Code, Name, Description, SolFilePath, ValidType, ValidPeriod, Status, IsEnabled, IsDeleted, CreateBy, UpdateBy, CreateAt, UpdateAt)
VALUES (
    'P001',
    'OCR检测产品A',
    '用于OCR文字识别检测',
    'D:\xcm\ApplicationDemo\OCRDemoCs\OCRDemoChinese.sol',
    1,  -- ValidType.Month
    6,  -- ValidPeriod = 6 months
    1,  -- Status.Active
    1,  -- IsEnabled = true
    0,  -- IsDeleted = false
    'system',
    'system',
    '2025-01-01T00:00:00',
    '2025-01-01T00:00:00'
);

INSERT INTO Products (Code, Name, Description, SolFilePath, ValidType, ValidPeriod, Status, IsEnabled, IsDeleted, CreateBy, UpdateBy, CreateAt, UpdateAt)
VALUES (
    'P002',
    '缺陷检测产品B',
    '用于表面缺陷检测',
    'D:\xcm\ApplicationDemo\OCRDemoCs\OCRDemoChinese.sol',
    0,  -- ValidType.Year
    1,  -- ValidPeriod = 1 year
    1,  -- Status.Active
    1,  -- IsEnabled = true
    0,  -- IsDeleted = false
    'system',
    'system',
    '2025-01-01T00:00:00',
    '2025-01-01T00:00:00'
);

INSERT INTO Products (Code, Name, Description, SolFilePath, ValidType, ValidPeriod, Status, IsEnabled, IsDeleted, CreateBy, UpdateBy, CreateAt, UpdateAt)
VALUES (
    'P003',
    '尺寸测量产品C',
    '用于尺寸测量',
    'D:\xcm\ApplicationDemo\OCRDemoCs\OCRDemoChinese.sol',
    2,  -- ValidType.Day
    30,  -- ValidPeriod = 30 days
    0,  -- Status.Inactive
    1,  -- IsEnabled = true
    0,  -- IsDeleted = false
    'system',
    'system',
    '2025-01-01T00:00:00',
    '2025-01-01T00:00:00'
);

-- -----------------------------------------------------------
-- Tasks Table Seed Data
-- Note: ProductId references the Products table (1=P001, 2=P002, 3=P003)
-- Status: 0=Pending, 1=Approved, 2=Running, 3=Completed
-- -----------------------------------------------------------

INSERT INTO Tasks (Name, ProductId, Status, CreatedBy, ReviewedBy, ReviewedAt, ProductionDate, ExpirationDate, BatchNumber, IsEnabled, IsDeleted, CreateBy, UpdateBy, CreateAt, UpdateAt)
VALUES (
    'OCR检测任务-2025-05-01',
    1,  -- ProductId = P001
    1,  -- Status.Approved
    'admin',
    'admin',
    '2025-05-29T00:00:00',  -- ReviewedAt = DateTime.Now.AddDays(-1) from seed date 2025-05-30
    '2025-04-30T00:00:00',  -- ProductionDate = DateTime.Today.AddDays(-30) from seed date 2025-05-30
    '2025-10-27T00:00:00',  -- ExpirationDate = DateTime.Today.AddDays(150) from seed date 2025-05-30
    'BATCH20250501',
    1,  -- IsEnabled = true
    0,  -- IsDeleted = false
    'admin',
    'admin',
    '2025-05-01T00:00:00',
    '2025-05-01T00:00:00'
);

INSERT INTO Tasks (Name, ProductId, Status, CreatedBy, ReviewedBy, ReviewedAt, ProductionDate, ExpirationDate, BatchNumber, IsEnabled, IsDeleted, CreateBy, UpdateBy, CreateAt, UpdateAt)
VALUES (
    '缺陷检测任务-2025-05-02',
    2,  -- ProductId = P002
    1,  -- Status.Approved
    'admin',
    'admin',
    '2025-05-28T00:00:00',  -- ReviewedAt = DateTime.Now.AddDays(-2) from seed date 2025-05-30
    '2025-05-10T00:00:00',  -- ProductionDate = DateTime.Today.AddDays(-20) from seed date 2025-05-30
    '2026-05-25T00:00:00',  -- ExpirationDate = DateTime.Today.AddDays(340) from seed date 2025-05-30
    'BATCH20250502',
    1,  -- IsEnabled = true
    0,  -- IsDeleted = false
    'admin',
    'admin',
    '2025-05-02T00:00:00',
    '2025-05-02T00:00:00'
);

INSERT INTO Tasks (Name, ProductId, Status, CreatedBy, ReviewedBy, ReviewedAt, ProductionDate, ExpirationDate, BatchNumber, IsEnabled, IsDeleted, CreateBy, UpdateBy, CreateAt, UpdateAt)
VALUES (
    '尺寸测量任务-2025-05-03',
    1,  -- ProductId = P001
    1,  -- Status.Approved
    'operator',
    'admin',
    '2025-05-30T00:00:00',  -- ReviewedAt = DateTime.Now.AddHours(-5) from seed date 2025-05-30
    '2025-05-20T00:00:00',  -- ProductionDate = DateTime.Today.AddDays(-10) from seed date 2025-05-30
    '2025-11-26T00:00:00',  -- ExpirationDate = DateTime.Today.AddDays(180) from seed date 2025-05-30
    'BATCH20250503',
    1,  -- IsEnabled = true
    0,  -- IsDeleted = false
    'operator',
    'admin',
    '2025-05-03T00:00:00',
    '2025-05-03T00:00:00'
);

INSERT INTO Tasks (Name, ProductId, Status, CreatedBy, ReviewedBy, ReviewedAt, ProductionDate, ExpirationDate, BatchNumber, IsEnabled, IsDeleted, CreateBy, UpdateBy, CreateAt, UpdateAt)
VALUES (
    '备料任务-待审核',
    3,  -- ProductId = P003
    0,  -- Status.Pending
    'operator',
    NULL,  -- ReviewedBy = null (no review yet)
    NULL,  -- ReviewedAt = null (no review yet)
    '2025-05-30T00:00:00',  -- ProductionDate = DateTime.Today from seed date 2025-05-30
    NULL,  -- ExpirationDate = null (no expiration set)
    'BATCH20250504',
    1,  -- IsEnabled = true
    0,  -- IsDeleted = false
    'operator',
    NULL,
    '2025-05-30T00:00:00',
    '2025-05-30T00:00:00'
);

-- ============================================================
-- End of Initialization Script
-- ============================================================