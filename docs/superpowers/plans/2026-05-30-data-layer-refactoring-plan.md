# Data Layer Refactoring - User Unification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Unify User persistence with other entities using SQLite + generic Repository pattern. Remove dual JSON/SQLite persistence for User.

**Architecture:**
- User entity gets `Id int` auto-increment PK (like other entities)
- `Username` remains as unique index for authentication lookups
- `IUserRepository` deleted, replaced by `IRepository<User>`
- JSON-based `UserRepository.cs` deleted
- `UserService` updated to use `IRepository<User>`

**Tech Stack:** .NET Framework 4.8, Entity Framework 6, SQLite, C# 8.0

---

## File Structure Map

### Current vs Target

| Layer | Current | Target |
|-------|---------|--------|
| Entity | `User.cs` (Username PK) | `User.cs` (Id PK + Username unique) |
| Repository | `IUserRepository.cs` + `UserRepository.cs` (JSON) + `SqliteUserRepository.cs` | Delete both, use `SqliteRepository<User>` |
| Query | `UserQuery.cs` (Username fuzzy) | `UserQuery.cs` + `ExactUsername` for auth |
| Factory | `CreateUserRepository()` | Remove, use `CreateRepository<User>()` |
| Service | `UserService(IUserRepository)` | `UserService(IRepository<User>)` |

### Files to Delete

- `TripleDetection.Data/Repositories/UserRepository.cs` (JSON-based)
- `TripleDetection.Data/Repositories/IUserRepository.cs` (replaced by IRepository<User>)
- `TripleDetection.Data/Repositories/Sqlite/SqliteUserRepository.cs` (replaced by SqliteRepository<User>)

### Files to Modify

- `TripleDetection.Data/Entities/User.cs` — add `Id int` PK, implement `BaseEntity` properly
- `TripleDetection.Data/Entities/UserQuery.cs` — add `ExactUsername` for login
- `TripleDetection.Data/Entities/BaseEntity.cs` — verify `Id` property
- `TripleDetection.Data/Repositories/Configuration/UserConfiguration.cs` — map `Id` PK, keep `Username` unique
- `TripleDetection.Data/Repositories/Sqlite/SqliteRepository.cs` — add `Query(UserQuery)` for User
- `TripleDetection.Data/Repositories/Contracts/IRepositoryFactory.cs` — remove `CreateUserRepository()`
- `TripleDetection.Data/Repositories/Contracts/IUnitOfWork.cs` — remove `GetUserRepository()`
- `TripleDetection.Data/Repositories/Sqlite/SqliteRepositoryFactory.cs` — remove `CreateUserRepository()`
- `TripleDetection.Data/Repositories/Sqlite/SqliteUnitOfWork.cs` — remove `GetUserRepository()`
- `TripleDetection.Services/UserService.cs` — change `IUserRepository` to `IRepository<User>`
- `docs/database/init.sql` — add `Id` column to Users INSERT

---

## Tasks

### Task 1: Update User Entity - Add Id PK

**Files:**
- Modify: `TripleDetection.Data/Entities/User.cs`

**Changes:**
- Remove `INotifyPropertyChanged` implementation (BaseEntity doesn't have it, and UserService doesn't depend on it)
- Remove private fields with backing properties pattern
- Add `public int Id { get; set; }` auto-increment PK
- Keep `Username` as string property (unique index, no PK)
- Remove `UserList` class (used only by JSON repo)
- Inherit properly from `BaseEntity` — remove duplicate `CreateAt/UpdateAt` if any

```csharp
using System;
using TripleDetection.Data;

namespace TripleDetection.Data.Entities
{
    public class User : BaseEntity
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string RealName { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public string StatusText
        {
            get
            {
                if (!IsEnabled) return "已禁用";
                if (IsLocked) return "已锁定";
                return "正常";
            }
        }
    }
}
```

---

### Task 2: Update UserConfiguration - Map Id PK

**Files:**
- Modify: `TripleDetection.Data/Repositories/Configuration/UserConfiguration.cs`

**Changes:**
- Change `HasKey(u => u.Username)` to `HasKey(u => u.Id)`
- Add `Property(u => u.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)`
- Keep `Property(u => u.Username).HasMaxLength(100).IsRequired()` (unique index)
- Add `Property(u => u.RealName).HasMaxLength(100)`
- Add `Property(u => u.Role).HasMaxLength(50).IsRequired()`
- Ignore `StatusText` (computed property)

```csharp
using System.Data.Entity.ModelConfiguration;
using System.ComponentModel.DataAnnotations.Schema;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories.Configuration
{
    public class UserConfiguration : EntityTypeConfiguration<User>
    {
        public UserConfiguration()
        {
            ToTable("Users");

            HasKey(u => u.Id);

            Property(u => u.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(u => u.Username).HasMaxLength(100).IsRequired();
            Property(u => u.RealName).HasMaxLength(100);
            Property(u => u.Password).HasMaxLength(256).IsRequired();
            Property(u => u.Role).HasMaxLength(50).IsRequired();

            Ignore(u => u.StatusText);
        }
    }
}
```

---

### Task 3: Update UserQuery - Add ExactUsername

**Files:**
- Modify: `TripleDetection.Data/Entities/UserQuery.cs`

**Changes:**
- Add `ExactUsername` for exact match (login authentication)
- Keep `Username` for fuzzy search (user management UI)

```csharp
using System;
using TripleDetection.Data.Repositories;

namespace TripleDetection.Data.Entities
{
    public class UserQuery : PagedQuery
    {
        public string ExactUsername { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string StatusText { get; set; }
    }
}
```

---

### Task 4: Update SqliteRepository - Add UserQuery Support

**Files:**
- Modify: `TripleDetection.Data/Repositories/Sqlite/SqliteRepository.cs`

**Changes:** Add a `Query(UserQuery)` method that handles User-specific filters:

```csharp
public IPagedResult<T> Query(UserQuery query)
{
    var q = _dbSet.Where(x => !x.IsDeleted).AsEnumerable();

    if (query is UserQuery uq)
    {
        if (!string.IsNullOrEmpty(uq.ExactUsername))
            q = q.Where(x => (x as User)?.Username == uq.ExactUsername);
        if (!string.IsNullOrEmpty(uq.Username))
            q = q.Where(x => (x as User)?.Username.Contains(uq.Username) == true);
        if (!string.IsNullOrEmpty(uq.Role))
            q = q.Where(x => (x as User)?.Role == uq.Role);
        if (!string.IsNullOrEmpty(uq.StatusText))
            q = q.Where(x => (x as User)?.StatusText == uq.StatusText);
    }

    if (!string.IsNullOrEmpty(query.SortBy))
    {
        var prop = typeof(T).GetProperty(query.SortBy);
        if (prop != null)
        {
            q = query.SortDescending
                ? q.OrderByDescending(x => prop.GetValue(x))
                : q.OrderBy(x => prop.GetValue(x));
        }
    }

    var total = q.Count();
    var items = q.Skip(query.PageIndex * query.PageSize)
                 .Take(query.PageSize)
                 .ToList();

    return new PagedResult<T>(items, total, query.PageIndex, query.PageSize);
}
```

---

### Task 5: Delete IUserRepository

**Files:**
- Delete: `TripleDetection.Data/Repositories/IUserRepository.cs`

---

### Task 6: Delete JSON-based UserRepository

**Files:**
- Delete: `TripleDetection.Data/Repositories/UserRepository.cs`

---

### Task 7: Delete SqliteUserRepository

**Files:**
- Delete: `TripleDetection.Data/Repositories/Sqlite/SqliteUserRepository.cs`

---

### Task 8: Update IRepositoryFactory - Remove CreateUserRepository

**Files:**
- Modify: `TripleDetection.Data/Repositories/Contracts/IRepositoryFactory.cs`

**Changes:** Remove `CreateUserRepository()` method and `IUserRepository` using.

```csharp
using System;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories.Contracts
{
    public enum DatabaseProviderType
    {
        InMemory,
        Sqlite,
        MySql,
        PostgreSql,
        SqlServer
    }

    public interface IRepositoryFactory
    {
        IUnitOfWork CreateUnitOfWork();
        IRepository<T> CreateRepository<T>() where T : BaseEntity;
        DatabaseProviderType ProviderType { get; }
    }
}
```

---

### Task 9: Update IUnitOfWork - Remove GetUserRepository

**Files:**
- Modify: `TripleDetection.Data/Repositories/Contracts/IUnitOfWork.cs`

**Changes:** Remove `GetUserRepository()` method.

```csharp
using System;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories.Contracts
{
    public interface IUnitOfWork : IDisposable
    {
        void BeginTransaction();
        void Commit();
        void Rollback();
        IRepository<T> GetRepository<T>() where T : BaseEntity;
        int SaveChanges();
        bool IsInTransaction { get; }
    }
}
```

---

### Task 10: Update SqliteRepositoryFactory - Remove CreateUserRepository

**Files:**
- Modify: `TripleDetection.Data/Repositories/Sqlite/SqliteRepositoryFactory.cs`

**Changes:** Remove `CreateUserRepository()` method and `IUserRepository` using/field if present.

```csharp
using System;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories.Contracts;

namespace TripleDetection.Data.Repositories.Sqlite
{
    public class SqliteRepositoryFactory : IRepositoryFactory
    {
        private readonly SqliteDbContext _context;

        public SqliteRepositoryFactory()
        {
            _context = new SqliteDbContext();
        }

        public SqliteRepositoryFactory(string connectionString)
        {
            _context = new SqliteDbContext(connectionString);
        }

        public IUnitOfWork CreateUnitOfWork()
        {
            return new SqliteUnitOfWork(_context);
        }

        public IRepository<T> CreateRepository<T>() where T : BaseEntity
        {
            return new SqliteRepository<T>(_context);
        }

        public DatabaseProviderType ProviderType => DatabaseProviderType.Sqlite;
    }
}
```

---

### Task 11: Update SqliteUnitOfWork - Remove GetUserRepository

**Files:**
- Modify: `TripleDetection.Data/Repositories/Sqlite/SqliteUnitOfWork.cs`

**Changes:** Remove `GetUserRepository()` method and all `IUserRepository` references.

---

### Task 12: Update UserService - Use IRepository<User>

**Files:**
- Modify: `TripleDetection.Services/UserService.cs`

**Changes:**
- Change `IUserRepository` to `IRepository<User>`
- Update constructor to use `IRepository<User>`
- Update `GetByUsername` to use `Find` with expression filter
- Replace `Delete(username)` with `Delete(id)` — but since we need username for lookup, add a helper method

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories;
using TripleDetection.Services.Audit;

namespace TripleDetection.Services
{
    public interface IUserService
    {
        User Authenticate(string username, string password);
        IEnumerable<User> GetAll();
        User GetByUsername(string username);
        User GetById(int id);
        void Create(User user, string createBy, int currentUserId);
        void Update(User user, string updateBy, int currentUserId);
        void Delete(string username, string updateBy, int currentUserId);
        void Enable(string username, string updateBy, int currentUserId);
        void Disable(string username, string updateBy, int currentUserId);
        void Lock(string username, string updateBy, int currentUserId);
        void Unlock(string username, string updateBy, int currentUserId);
        PagedResult<User> Query(UserQuery query);
    }

    public class UserService : IUserService
    {
        private readonly IRepository<User> _repository;
        private readonly IAuditLogService _auditLog;

        public UserService() : this(new SqliteRepositoryFactory().CreateRepository<User>(), null)
        {
        }

        public UserService(IRepository<User> repository, IAuditLogService auditLogService)
        {
            _repository = repository;
            _auditLog = auditLogService;
        }

        public User Authenticate(string username, string password)
        {
            var user = _repository.Find(u => u.Username == username && u.IsEnabled && !u.IsLocked)
                                .FirstOrDefault();
            if (user != null && user.Password == password)
            {
                _auditLog?.Log(user.Id, "登录", "User", user.Id, $"用户登录: {username}");
                return user;
            }
            return null;
        }

        public IEnumerable<User> GetAll()
        {
            return _repository.GetAll();
        }

        public User GetByUsername(string username)
        {
            return _repository.Find(u => u.Username == username).FirstOrDefault();
        }

        public User GetById(int id)
        {
            return _repository.GetById(id);
        }

        public void Create(User user, string createBy, int currentUserId)
        {
            if (string.IsNullOrEmpty(user.Username))
                throw new ArgumentException("Username is required");

            var existing = GetByUsername(user.Username);
            if (existing != null)
                throw new InvalidOperationException($"User '{user.Username}' already exists");

            user.CreateBy = createBy;
            _repository.Add(user);
            _auditLog?.Log(currentUserId, "创建", "User", user.Id, $"创建用户: {user.Username}");
        }

        public void Update(User user, string updateBy, int currentUserId)
        {
            var existing = GetByUsername(user.Username);
            if (existing == null)
                throw new InvalidOperationException($"User '{user.Username}' not found");

            user.Id = existing.Id;
            user.UpdateBy = updateBy;
            _repository.Update(user);
            _auditLog?.Log(currentUserId, "修改", "User", user.Id, $"修改用户: {user.Username}");
        }

        public void Delete(string username, string updateBy, int currentUserId)
        {
            var existing = GetByUsername(username);
            if (existing == null)
                throw new InvalidOperationException($"User '{username}' not found");

            _repository.Delete(existing.Id);
            _auditLog?.Log(currentUserId, "删除", "User", existing.Id, $"删除用户: {username}");
        }

        public void Enable(string username, string updateBy, int currentUserId)
        {
            var user = GetByUsername(username);
            if (user == null)
                throw new InvalidOperationException($"User '{username}' not found");

            user.IsEnabled = true;
            user.UpdateBy = updateBy;
            _repository.Update(user);
            _auditLog?.Log(currentUserId, "启用", "User", user.Id, $"启用用户: {username}");
        }

        public void Disable(string username, string updateBy, int currentUserId)
        {
            var user = GetByUsername(username);
            if (user == null)
                throw new InvalidOperationException($"User '{username}' not found");

            user.IsEnabled = false;
            user.UpdateBy = updateBy;
            _repository.Update(user);
            _auditLog?.Log(currentUserId, "禁用", "User", user.Id, $"禁用用户: {username}");
        }

        public void Lock(string username, string updateBy, int currentUserId)
        {
            var user = GetByUsername(username);
            if (user == null)
                throw new InvalidOperationException($"User '{username}' not found");

            user.IsLocked = true;
            user.UpdateBy = updateBy;
            _repository.Update(user);
            _auditLog?.Log(currentUserId, "锁定", "User", user.Id, $"锁定用户: {username}");
        }

        public void Unlock(string username, string updateBy, int currentUserId)
        {
            var user = GetByUsername(username);
            if (user == null)
                throw new InvalidOperationException($"User '{username}' not found");

            user.IsLocked = false;
            user.UpdateBy = updateBy;
            _repository.Update(user);
            _auditLog?.Log(currentUserId, "解锁", "User", user.Id, $"解锁用户: {username}");
        }

        public PagedResult<User> Query(UserQuery query)
        {
            return _repository.Query(query);
        }
    }
}
```

---

### Task 13: Update init.sql - Add Id Column

**Files:**
- Modify: `docs/database/init.sql`

**Changes:**
- Add `Id` column to Users INSERT (NULL for auto-increment)
- Add `Id` column to Products/Tasks if they don't have it (they should from existing schema)

```sql
INSERT INTO Users (Id, Username, RealName, Password, Role, IsEnabled, IsLocked, LastLoginAt, IsDeleted, CreateBy, UpdateBy, CreateAt, UpdateAt)
VALUES (
    NULL,  -- Id auto-increment
    'admin',
    'Administrator',
    'admin123',  -- Plain password for default admin
    'Admin',
    1,  -- IsEnabled = true
    0,  -- IsLocked = false
    NULL,
    0,  -- IsDeleted = false
    'system',
    'system',
    '2025-01-01T00:00:00',
    '2025-01-01T00:00:00'
);
```

---

### Task 14: Build and Verify

**Command:**
```
powershell.exe -Command "& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' 'd:\xcm\Triple-Detection\TripleDetection.App\TripleDetection.App.csproj' /t:Rebuild /p:Configuration=Debug 2>&1 | Select-Object -Last 20"
```

**Expected:** Build succeeds with 0 errors

---

### Task 15: Verify All Usings/References

**Files to check:**
- `TripleDetection.App/ViewModels/Auth/UserManagementViewModel.cs` — uses `UserService` only (no direct UserRepository)
- `TripleDetection.App/ViewModels/Auth/UserEditViewModel.cs` — uses `UserService` only
- Any other file using `IUserRepository` or `UserRepository` directly

**Verify no remaining references to:**
- `IUserRepository`
- `UserRepository` (JSON-based, not `SqliteRepository<User>`)
- `SqliteUserRepository`

---

## Verification Steps

After each task, verify build passes. Final verification:

1. Build: `MSBuild ... /t:Rebuild /p:Configuration=Debug` → 0 errors
2. User login: `UserService.Authenticate("admin", "admin123")` returns User
3. User CRUD: Create, Update, Delete, Enable, Disable, Lock, Unlock all work
4. User query: `UserService.Query(new UserQuery { Username = "admin" })` returns matching users
5. User management UI: Load UserManagement page, search users, verify data displays

---

## Implementation Order

1. Task 1: Update User entity
2. Task 2: Update UserConfiguration
3. Task 3: Update UserQuery
4. Task 4: Update SqliteRepository
5. Task 5: Delete IUserRepository
6. Task 6: Delete UserRepository (JSON)
7. Task 7: Delete SqliteUserRepository
8. Task 8: Update IRepositoryFactory
9. Task 9: Update IUnitOfWork
10. Task 10: Update SqliteRepositoryFactory
11. Task 11: Update SqliteUnitOfWork
12. Task 12: Update UserService
13. Task 13: Update init.sql
14. Task 14: Build and verify
15. Task 15: Verify references