# SQLite + Repository 模式实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将数据存储从 JSON/InMemory 迁移到 SQLite 数据库，使用 Repository 抽象支持未来切换到其他数据库

**Architecture:** Repository 接口 + 抽象工厂 + Unit of Work 事务管理

**Tech Stack:** WPF (.NET Framework 4.8), EF6 + SQLite, C# 8.0

---

## Context

当前数据存储存在问题：
- **用户数据**：JSON 文件存储在 `Config/users.json`
- **产品/任务数据**：内存存储，重启后数据丢失
- **无事务支持**
- **难以切换数据库**

目标：使用 SQLite 持久化存储，Repository 抽象支持未来切换数据库，JSON 作为初始化数据源。

---

## 文件结构

```
TripleDetection.Data/
├── BaseEntity.cs
├── Entities/
│   ├── Entities.cs              # Product, Task, DetectionRecord, SystemConfig, AuditLog
│   ├── User.cs
│   └── UserQuery.cs
├── Repositories/
│   ├── Repository.cs           # IRepository<T>, InMemoryRepository<T> (现有)
│   ├── IUserRepository.cs      # (现有)
│   ├── UserRepository.cs       # JSON 实现 (现有，仅保留用于初始化)
│   │
│   ├── Contracts/              # NEW
│   │   ├── IUnitOfWork.cs
│   │   └── IRepositoryFactory.cs
│   │
│   ├── Sqlite/                 # NEW
│   │   ├── SqliteDbContext.cs
│   │   ├── SqliteRepository.cs
│   │   ├── SqliteUserRepository.cs
│   │   ├── SqliteUnitOfWork.cs
│   │   └── SqliteRepositoryFactory.cs
│   │
│   └── Configuration/          # NEW
│       ├── ProductConfiguration.cs
│       ├── TaskConfiguration.cs
│       ├── DetectionRecordConfiguration.cs
│       ├── SystemConfigConfiguration.cs
│       └── UserConfiguration.cs
│
└── DatabaseInitializer.cs       # NEW
```

---

## 实现步骤

### Task 1: 添加 NuGet 包

**Files:**
- Modify: `TripleDetection.Data\TripleDetection.Data.csproj`

- [ ] **Step 1: 添加 EF6 和 SQLite NuGet 包**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <LangVersion>8.0</LangVersion>
    <RootNamespace>TripleDetection.Data</RootNamespace>
    <AssemblyName>TripleDetection.Data</AssemblyName>
    <Deterministic>true</Deterministic>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Data.SQLite.EF6" Version="1.0.118" />
    <PackageReference Include="EntityFramework" Version="6.4.4" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: 验证编译**

Run: Build TripleDetection.Data project
Expected: Restore packages, build succeeds

---

### Task 2: 创建 Contracts 接口层

**Files:**
- Create: `TripleDetection.Data\Repositories\Contracts\IUnitOfWork.cs`
- Create: `TripleDetection.Data\Repositories\Contracts\IRepositoryFactory.cs`

- [ ] **Step 1: 创建 IUnitOfWork 接口**

```csharp
using System;

namespace TripleDetection.Data.Repositories.Contracts
{
    /// <summary>
    /// 工作单元接口 - 管理事务和仓储
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// 开始事务
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// 提交事务
        /// </summary>
        void Commit();

        /// <summary>
        /// 回滚事务
        /// </summary>
        void Rollback();

        /// <summary>
        /// 获取指定实体类型的仓储
        /// </summary>
        IRepository<T> GetRepository<T>() where T : BaseEntity;

        /// <summary>
        /// 获取用户仓储
        /// </summary>
        IUserRepository GetUserRepository();

        /// <summary>
        /// 保存所有更改
        /// </summary>
        int SaveChanges();

        /// <summary>
        /// 是否处于事务中
        /// </summary>
        bool IsInTransaction { get; }
    }
}
```

- [ ] **Step 2: 创建 IRepositoryFactory 接口和 DatabaseProviderType 枚举**

```csharp
using System;

namespace TripleDetection.Data.Repositories.Contracts
{
    /// <summary>
    /// 支持的数据库提供者类型
    /// </summary>
    public enum DatabaseProviderType
    {
        InMemory,
        Sqlite,
        MySql,       // 未来扩展
        PostgreSql,  // 未来扩展
        SqlServer    // 未来扩展
    }

    /// <summary>
    /// 仓储工厂接口 - 创建仓储实例
    /// </summary>
    public interface IRepositoryFactory
    {
        /// <summary>
        /// 创建工作单元
        /// </summary>
        IUnitOfWork CreateUnitOfWork();

        /// <summary>
        /// 创建独立仓储
        /// </summary>
        IRepository<T> CreateRepository<T>() where T : BaseEntity;

        /// <summary>
        /// 创建用户仓储
        /// </summary>
        IUserRepository CreateUserRepository();

        /// <summary>
        /// 当前数据库提供者类型
        /// </summary>
        DatabaseProviderType ProviderType { get; }
    }
}
```

- [ ] **Step 3: 提交**

```bash
git add TripleDetection.Data/Repositories/Contracts/
git commit -m "feat: add IUnitOfWork and IRepositoryFactory interfaces"
```

---

### Task 3: 创建 EF6 实体配置

**Files:**
- Create: `TripleDetection.Data\Repositories\Configuration\ProductConfiguration.cs`
- Create: `TripleDetection.Data\Repositories\Configuration\TaskConfiguration.cs`
- Create: `TripleDetection.Data\Repositories\Configuration\DetectionRecordConfiguration.cs`
- Create: `TripleDetection.Data\Repositories\Configuration\SystemConfigConfiguration.cs`
- Create: `TripleDetection.Data\Repositories\Configuration\UserConfiguration.cs`

- [ ] **Step 1: 创建 ProductConfiguration**

```csharp
using System.Data.Entity.ModelConfiguration;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories.Configuration
{
    /// <summary>
    /// Product 实体配置
    /// </summary>
    public class ProductConfiguration : EntityTypeConfiguration<Product>
    {
        public ProductConfiguration()
        {
            ToTable("Products");

            HasKey(p => p.Id);
            Property(p => p.Id).HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.DatabaseGeneratedOption.Identity);

            Property(p => p.Code).IsRequired().HasMaxLength(50);
            Property(p => p.Name).IsRequired().HasMaxLength(200);
            Property(p => p.Description).HasMaxLength(1000);
            Property(p => p.SolFilePath).HasMaxLength(500);

            // 软删除查询过滤器
            HasQueryFilter(p => !p.IsDeleted);
        }
    }
}
```

- [ ] **Step 2: 创建 TaskConfiguration**

```csharp
using System.Data.Entity.ModelConfiguration;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories.Configuration
{
    /// <summary>
    /// Task 实体配置
    /// </summary>
    public class TaskConfiguration : EntityTypeConfiguration<Data.Entities.Task>
    {
        public TaskConfiguration()
        {
            ToTable("Tasks");

            HasKey(t => t.Id);
            Property(t => t.Id).HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.DatabaseGeneratedOption.Identity);

            Property(t => t.Name).IsRequired().HasMaxLength(200);
            Property(t => t.BatchNumber).HasMaxLength(50);
            Property(t => t.CreatedBy).HasMaxLength(100);
            Property(t => t.ReviewedBy).HasMaxLength(100);

            // 关系：Task -> Product
            HasRequired(t => t.Product)
                .WithMany()
                .HasForeignKey(t => t.ProductId)
                .WillCascadeOnDelete(false);

            HasQueryFilter(t => !t.IsDeleted);
        }
    }
}
```

- [ ] **Step 3: 创建 DetectionRecordConfiguration**

```csharp
using System.Data.Entity.ModelConfiguration;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories.Configuration
{
    /// <summary>
    /// DetectionRecord 实体配置
    /// </summary>
    public class DetectionRecordConfiguration : EntityTypeConfiguration<DetectionRecord>
    {
        public DetectionRecordConfiguration()
        {
            ToTable("DetectionRecords");

            HasKey(d => d.Id);
            Property(d => d.Id).HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.DatabaseGeneratedOption.Identity);

            Property(d => d.Result).IsRequired().HasMaxLength(10);
            Property(d => d.CodeInfo).HasMaxLength(200);
            Property(d => d.ImagePath).HasMaxLength(500);

            // 关系：DetectionRecord -> Task
            HasRequired(d => d.Task)
                .WithMany()
                .HasForeignKey(d => d.TaskId)
                .WillCascadeOnDelete(false);

            HasQueryFilter(d => !d.IsDeleted);
        }
    }
}
```

- [ ] **Step 4: 创建 SystemConfigConfiguration**

```csharp
using System.Data.Entity.ModelConfiguration;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories.Configuration
{
    /// <summary>
    /// SystemConfig 实体配置
    /// </summary>
    public class SystemConfigConfiguration : EntityTypeConfiguration<SystemConfig>
    {
        public SystemConfigConfiguration()
        {
            ToTable("SystemConfigs");

            HasKey(s => s.Id);
            Property(s => s.Id).HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.DatabaseGeneratedOption.Identity);

            Property(s => s.Category).IsRequired().HasMaxLength(50);
            Property(s => s.Key).IsRequired().HasMaxLength(100);
            Property(s => s.Value).HasMaxLength(1000);
            Property(s => s.Description).HasMaxLength(500);

            HasQueryFilter(s => !s.IsDeleted);
        }
    }
}
```

- [ ] **Step 5: 创建 UserConfiguration**

```csharp
using System.Data.Entity.ModelConfiguration;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories.Configuration
{
    /// <summary>
    /// User 实体配置 - Username 作为主键（非自增）
    /// </summary>
    public class UserConfiguration : EntityTypeConfiguration<User>
    {
        public UserConfiguration()
        {
            ToTable("Users");

            // Username 是主键，不自增
            HasKey(u => u.Username);
            Property(u => u.Username).HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.DatabaseGeneratedOption.None);
            Property(u => u.Username).HasMaxLength(100).IsRequired();

            Property(u => u.RealName).HasMaxLength(100);
            Property(u => u.Password).HasMaxLength(256).IsRequired();
            Property(u => u.Role).HasMaxLength(50).IsRequired();

            // StatusText 是计算属性，不映射到数据库
            Ignore(u => u.StatusText);
        }
    }
}
```

- [ ] **Step 6: 提交**

```bash
git add TripleDetection.Data/Repositories/Configuration/
git commit -m "feat: add EF6 entity configurations for all entities"
```

---

### Task 4: 创建 SqliteDbContext

**Files:**
- Create: `TripleDetection.Data\Repositories\Sqlite\SqliteDbContext.cs`

- [ ] **Step 1: 创建 SqliteDbContext**

```csharp
using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.IO;
using System.Linq;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories.Configuration;

namespace TripleDetection.Data.Repositories.Sqlite
{
    /// <summary>
    /// SQLite 数据库上下文
    /// </summary>
    public class SqliteDbContext : DbContext
    {
        private static readonly string DefaultDbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Data",
            "tripledetection.db");

        public SqliteDbContext() : base($"Data Source={DefaultDbPath}")
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        public SqliteDbContext(string connectionString) : base(connectionString)
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        // DbSet 实体集
        public DbSet<Product> Products { get; set; }
        public DbSet<Data.Entities.Task> Tasks { get; set; }
        public DbSet<DetectionRecord> DetectionRecords { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 加载所有实体配置
            modelBuilder.Configurations.Add(new ProductConfiguration());
            modelBuilder.Configurations.Add(new TaskConfiguration());
            modelBuilder.Configurations.Add(new DetectionRecordConfiguration());
            modelBuilder.Configurations.Add(new SystemConfigConfiguration());
            modelBuilder.Configurations.Add(new UserConfiguration());
        }
    }
}
```

- [ ] **Step 2: 提交**

```bash
git add TripleDetection.Data/Repositories/Sqlite/SqliteDbContext.cs
git commit -m "feat: add SqliteDbContext for EF6 + SQLite"
```

---

### Task 5: 创建 SqliteRepository

**Files:**
- Create: `TripleDetection.Data\Repositories\Sqlite\SqliteRepository.cs`

- [ ] **Step 1: 创建 SqliteRepository 通用实现**

```csharp
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using TripleDetection.Data;
using TripleDetection.Data.Repositories.Contracts;

namespace TripleDetection.Data.Repositories.Sqlite
{
    /// <summary>
    /// SQLite 通用仓储实现
    /// </summary>
    /// <typeparam name="T">实体类型，继承自 BaseEntity</typeparam>
    public class SqliteRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly SqliteDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public SqliteRepository(SqliteDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        public virtual T GetById(int id)
        {
            return _dbSet.Find(id);
        }

        public virtual IEnumerable<T> GetAll()
        {
            return _dbSet.Where(x => !x.IsDeleted).ToList();
        }

        public virtual IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.Where(predicate).Where(x => !x.IsDeleted).ToList();
        }

        public virtual void Add(T entity)
        {
            entity.CreateAt = DateTime.Now;
            entity.UpdateAt = DateTime.Now;
            entity.IsDeleted = false;
            _dbSet.Add(entity);
        }

        public virtual void Update(T entity)
        {
            entity.UpdateAt = DateTime.Now;
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        public virtual void Delete(int id)
        {
            var entity = _dbSet.Find(id);
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.UpdateAt = DateTime.Now;
            }
        }

        public virtual int Count()
        {
            return _dbSet.Count(x => !x.IsDeleted);
        }

        public virtual int Count(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.Where(predicate).Count(x => !x.IsDeleted);
        }

        public IPagedResult<T> Query(PagedQuery query)
        {
            var q = _dbSet.Where(x => !x.IsDeleted);

            // 应用排序
            if (!string.IsNullOrEmpty(query.SortBy))
            {
                q = ApplySorting(q, query.SortBy, query.SortDescending);
            }

            var total = q.Count();
            var items = q.Skip(query.PageIndex * query.PageSize)
                         .Take(query.PageSize)
                         .ToList();

            return new PagedResult<T>(items, total, query.PageIndex, query.PageSize);
        }

        protected IQueryable<T> ApplySorting(IQueryable<T> query, string sortBy, bool descending)
        {
            var property = typeof(T).GetProperty(sortBy);
            if (property == null) return query;

            var param = Expression.Parameter(typeof(T), "x");
            var body = Expression.Property(param, property);
            var lambda = Expression.Lambda<Func<T, object>>(
                Expression.Convert(body, typeof(object)), param);

            return descending ? query.OrderByDescending(lambda) : query.OrderBy(lambda);
        }
    }
}
```

- [ ] **Step 2: 提交**

```bash
git add TripleDetection.Data/Repositories/Sqlite/SqliteRepository.cs
git commit -m "feat: add SqliteRepository generic implementation"
```

---

### Task 6: 创建 SqliteUserRepository

**Files:**
- Create: `TripleDetection.Data\Repositories\Sqlite\SqliteUserRepository.cs`

- [ ] **Step 1: 创建 SqliteUserRepository**

```csharp
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories.Contracts;

namespace TripleDetection.Data.Repositories.Sqlite
{
    /// <summary>
    /// SQLite 用户仓储实现 - Username 作为主键
    /// </summary>
    public class SqliteUserRepository : IUserRepository
    {
        private readonly SqliteDbContext _context;
        private readonly DbSet<User> _dbSet;

        public SqliteUserRepository(SqliteDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<User>();
        }

        public User GetByUsername(string username)
        {
            return _dbSet.Find(username);
        }

        public IEnumerable<User> GetAll()
        {
            return _dbSet.ToList();
        }

        public IEnumerable<User> Find(Expression<Func<User, bool>> predicate)
        {
            return _dbSet.Where(predicate).ToList();
        }

        public void Add(User entity)
        {
            entity.CreateAt = DateTime.Now;
            entity.UpdateAt = DateTime.Now;
            _dbSet.Add(entity);
        }

        public void Update(User entity)
        {
            entity.UpdateAt = DateTime.Now;
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        public void Delete(string username)
        {
            var user = _dbSet.Find(username);
            if (user != null)
            {
                _dbSet.Remove(user);
            }
        }

        public int Count()
        {
            return _dbSet.Count();
        }

        public int Count(Expression<Func<User, bool>> predicate)
        {
            return _dbSet.Count(predicate);
        }

        public PagedResult<User> Query(UserQuery query)
        {
            var q = _dbSet.AsQueryable();

            if (!string.IsNullOrEmpty(query.Username))
                q = q.Where(u => u.Username.Contains(query.Username));
            if (!string.IsNullOrEmpty(query.Role))
                q = q.Where(u => u.Role == query.Role);
            if (!string.IsNullOrEmpty(query.StatusText))
                q = q.Where(u => u.StatusText == query.StatusText);

            var total = q.Count();
            var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)query.PageSize);
            var pageIndex = Math.Min(query.PageIndex, totalPages - 1);
            pageIndex = Math.Max(pageIndex, 0);

            var items = q.OrderBy(u => u.Username)
                        .Skip(pageIndex * query.PageSize)
                        .Take(query.PageSize)
                        .ToList();

            return new PagedResult<User>(items, total, pageIndex, query.PageSize);
        }
    }
}
```

- [ ] **Step 2: 提交**

```bash
git add TripleDetection.Data/Repositories/Sqlite/SqliteUserRepository.cs
git commit -m "feat: add SqliteUserRepository with Username PK support"
```

---

### Task 7: 创建 SqliteUnitOfWork

**Files:**
- Create: `TripleDetection.Data\Repositories\Sqlite\SqliteUnitOfWork.cs`

- [ ] **Step 1: 创建 SqliteUnitOfWork**

```csharp
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using TripleDetection.Data;
using TripleDetection.Data.Repositories.Contracts;

namespace TripleDetection.Data.Repositories.Sqlite
{
    /// <summary>
    /// SQLite 工作单元实现 - 事务管理
    /// </summary>
    public class SqliteUnitOfWork : IUnitOfWork
    {
        private readonly SqliteDbContext _context;
        private DbContextTransaction _transaction;
        private bool _disposed;
        private readonly Dictionary<Type, object> _repositories;

        public SqliteUnitOfWork() : this(GetDefaultConnectionString())
        {
        }

        public SqliteUnitOfWork(string connectionString)
        {
            _context = new SqliteDbContext(connectionString);
            _repositories = new Dictionary<Type, object>();
        }

        private static string GetDefaultConnectionString()
        {
            var dbPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data",
                "tripledetection.db");
            return $"Data Source={dbPath}";
        }

        public bool IsInTransaction => _transaction != null;

        public void BeginTransaction()
        {
            if (_transaction != null)
                throw new InvalidOperationException("A transaction is already in progress.");

            _transaction = _context.Database.BeginTransaction();
        }

        public void Commit()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction in progress.");

            try
            {
                _context.SaveChanges();
                _transaction.Commit();
            }
            catch
            {
                Rollback();
                throw;
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void Rollback()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction in progress.");

            _transaction.Rollback();
            _transaction.Dispose();
            _transaction = null;
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public IRepository<T> GetRepository<T>() where T : BaseEntity
        {
            var entityType = typeof(T);

            if (!_repositories.ContainsKey(entityType))
            {
                IRepository<T> repository;

                if (entityType == typeof(User))
                {
                    repository = new SqliteUserRepository(_context) as IRepository<T>;
                }
                else
                {
                    repository = new SqliteRepository<T>(_context);
                }

                _repositories[entityType] = repository;
            }

            return (IRepository<T>)_repositories[entityType];
        }

        public IUserRepository GetUserRepository()
        {
            if (!_repositories.ContainsKey(typeof(User)))
            {
                _repositories[typeof(User)] = new SqliteUserRepository(_context);
            }

            return (IUserRepository)_repositories[typeof(User)];
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _context?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
```

- [ ] **Step 2: 提交**

```bash
git add TripleDetection.Data/Repositories/Sqlite/SqliteUnitOfWork.cs
git commit -m "feat: add SqliteUnitOfWork with transaction support"
```

---

### Task 8: 创建 SqliteRepositoryFactory

**Files:**
- Create: `TripleDetection.Data\Repositories\Sqlite\SqliteRepositoryFactory.cs`

- [ ] **Step 1: 创建 SqliteRepositoryFactory**

```csharp
using System;
using TripleDetection.Data;
using TripleDetection.Data.Repositories.Contracts;

namespace TripleDetection.Data.Repositories.Sqlite
{
    /// <summary>
    /// SQLite 仓储工厂实现
    /// </summary>
    public class SqliteRepositoryFactory : IRepositoryFactory
    {
        private readonly string _connectionString;

        public SqliteRepositoryFactory() : this(GetDefaultConnectionString())
        {
        }

        public SqliteRepositoryFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        private static string GetDefaultConnectionString()
        {
            var dbPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data",
                "tripledetection.db");
            return $"Data Source={dbPath}";
        }

        public DatabaseProviderType ProviderType => DatabaseProviderType.Sqlite;

        public IUnitOfWork CreateUnitOfWork()
        {
            return new SqliteUnitOfWork(_connectionString);
        }

        public IRepository<T> CreateRepository<T>() where T : BaseEntity
        {
            var context = new SqliteDbContext(_connectionString);

            if (typeof(T) == typeof(Data.Entities.User))
            {
                return new SqliteUserRepository(context) as IRepository<T>;
            }

            return new SqliteRepository<T>(context);
        }

        public IUserRepository CreateUserRepository()
        {
            var context = new SqliteDbContext(_connectionString);
            return new SqliteUserRepository(context);
        }
    }
}
```

- [ ] **Step 2: 提交**

```bash
git add TripleDetection.Data/Repositories/Sqlite/SqliteRepositoryFactory.cs
git commit -m "feat: add SqliteRepositoryFactory for creating repositories"
```

---

### Task 9: 创建 DatabaseInitializer

**Files:**
- Create: `TripleDetection.Data\DatabaseInitializer.cs`

- [ ] **Step 1: 创建 DatabaseInitializer**

```csharp
using System;
using System.IO;
using System.Linq;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories;
using TripleDetection.Data.Repositories.Contracts;
using TripleDetection.Data.Repositories.Sqlite;

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

        private static readonly string ConfigDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Config");

        private static readonly string UsersJsonPath = Path.Combine(
            ConfigDirectory, "users.json");

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
            using (var context = new SqliteDbContext())
            {
                // 创建数据库如果不存在
                if (!context.Database.Exists())
                {
                    context.Database.Create();
                }
            }
        }

        /// <summary>
        /// 导入初始数据（仅当数据库为空时）
        /// </summary>
        public static void SeedInitialData()
        {
            using (var context = new SqliteDbContext())
            {
                // 检查是否已有用户数据
                if (context.Users.Any())
                {
                    return; // 已有数据，跳过
                }

                // 从 JSON 文件读取初始用户
                if (File.Exists(UsersJsonPath))
                {
                    try
                    {
                        var json = File.ReadAllText(UsersJsonPath);
                        var userList = SimpleJsonHelper.Deserialize<UserList>(json);

                        if (userList?.Users != null)
                        {
                            foreach (var user in userList.Users)
                            {
                                context.Users.Add(user);
                            }
                            context.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        // 如果 JSON 解析失败，创建默认管理员
                        CreateDefaultAdmin(context);
                    }
                }
                else
                {
                    // 没有 JSON 文件，创建默认管理员
                    CreateDefaultAdmin(context);
                }
            }
        }

        private static void CreateDefaultAdmin(SqliteDbContext context)
        {
            var admin = new User
            {
                Username = "admin",
                RealName = "管理员",
                Password = "admin123", // 实际应用应加密存储
                Role = "Admin",
                IsEnabled = true,
                IsLocked = false,
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            };
            context.Users.Add(admin);
            context.SaveChanges();
        }
    }
}
```

- [ ] **Step 2: 提交**

```bash
git add TripleDetection.Data/DatabaseInitializer.cs
git commit -m "feat: add DatabaseInitializer for SQLite setup and data seeding"
```

---

### Task 10: 创建 DatabaseConfig

**Files:**
- Create: `TripleDetection.App\DatabaseConfig.cs` (注意：在 App 项目，不是 Data 项目)

- [ ] **Step 1: 创建 DatabaseConfig**

```csharp
using System;
using TripleDetection.Data;
using TripleDetection.Data.Repositories.Contracts;
using TripleDetection.Data.Repositories.Sqlite;

namespace TripleDetection.App
{
    /// <summary>
    /// 数据库配置 - 应用启动时初始化数据库
    /// </summary>
    public static class DatabaseConfig
    {
        private static IRepositoryFactory _factory;

        /// <summary>
        /// 获取当前仓储工厂实例
        /// </summary>
        public static IRepositoryFactory Factory
        {
            get
            {
                if (_factory == null)
                {
                    _factory = new SqliteRepositoryFactory();
                }
                return _factory;
            }
        }

        /// <summary>
        /// 初始化数据库 - 应在应用启动时调用
        /// </summary>
        public static void Initialize()
        {
            DatabaseInitializer.Initialize();
        }
    }
}
```

- [ ] **Step 2: 提交**

```bash
git add TripleDetection.App/DatabaseConfig.cs
git commit -m "feat: add DatabaseConfig for app startup initialization"
```

---

### Task 11: 集成到 MainWindow

**Files:**
- Modify: `TripleDetection.App\MainWindow.xaml.cs`

- [ ] **Step 1: 在 Window_Loaded 中调用初始化**

在 `Window_Loaded` 方法开头添加:

```csharp
// 初始化数据库
DatabaseConfig.Initialize();
```

查找:
```csharp
private void Window_Loaded(object sender, RoutedEventArgs e)
{
    var logoPath = ConfigurationManager.AppSettings["SystemLogoPath"];
```

添加后:
```csharp
private void Window_Loaded(object sender, RoutedEventArgs e)
{
    // 初始化数据库
    DatabaseConfig.Initialize();

    var logoPath = ConfigurationManager.AppSettings["SystemLogoPath"];
```

- [ ] **Step 2: 验证编译**

Run: Build TripleDetection.App project
Expected: Build succeeds, 0 errors

- [ ] **Step 3: 提交**

```bash
git add TripleDetection.App/MainWindow.xaml.cs
git commit -m "feat: integrate DatabaseConfig initialization on app startup"
```

---

## 验证标准

- [ ] `TripleDetection.Data.csproj` 包含 EF6 和 SQLite 包引用
- [ ] `IRepositoryFactory` 和 `IUnitOfWork` 接口定义完整
- [ ] 所有实体配置正确映射到数据库表
- [ ] `SqliteDbContext` 可以创建 SQLite 数据库文件 `Data/tripledetection.db`
- [ ] `SqliteRepository<T>` 支持 CRUD、分页、软删除
- [ ] `SqliteUserRepository` 支持 Username 主键操作
- [ ] `SqliteUnitOfWork` 支持事务（Begin/Commit/Rollback）
- [ ] `SqliteRepositoryFactory` 可以创建所有仓储实例
- [ ] `DatabaseInitializer` 首次启动时从 JSON 导入用户数据
- [ ] 应用启动时自动调用 `DatabaseConfig.Initialize()`
- [ ] 编译通过，0 errors

---

## 未来扩展

切换到 MySQL/PostgreSQL/SQLServer 只需：
1. 创建 `Repositories/MySql/` 文件夹
2. 实现 `MySqlDbContext`, `MySqlRepository<T>`, `MySqlUserRepository`, `MySqlUnitOfWork`, `MySqlRepositoryFactory`
3. 修改 `DatabaseConfig.cs` 中的 Factory 初始化代码

服务层代码无需修改。