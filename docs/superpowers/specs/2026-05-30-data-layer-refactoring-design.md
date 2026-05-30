# Data Layer Architecture Refactoring - Design Spec

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan.

**Goal:** Unify User persistence with other entities using SQLite + generic Repository pattern. Remove dual JSON/SQLite persistence for User.

**Architecture:**
- User entity gets `Id int` auto-increment PK (like other entities)
- `Username` remains as unique index for authentication lookups
- `IUserRepository` deleted, replaced by `IRepository<User>`
- JSON-based `UserRepository.cs` deleted
- `UserService` updated to use `IRepository<User>`

**Tech Stack:** .NET Framework 4.8, Entity Framework 6, SQLite, C# 8.0

---

## Current State Problems

| Issue | Location | Description |
|-------|----------|-------------|
| Dual persistence | `UserRepository.cs` (JSON) + `SqliteUserRepository.cs` (SQLite) | Duplicate code paths, confusing |
| Separate interface | `IUserRepository.cs` | Needed only because Username is PK instead of Id |
| Service uses wrong default | `UserService.cs:36` | `new UserRepository()` defaults to JSON, not SQLite |
| IsDeleted filter inconsistency | `SqliteUserRepository` vs `SqliteRepository<T>` | User repo doesn't filter deleted records |

## Target State

All entities (Product, ProdTask, User, DetectionRecord, SystemConfig, AuditLog) use:
- `Id int` auto-increment primary key
- `SqliteRepository<T>` generic implementation
- `IRepository<T>` interface
- Logical deletion (`IsDeleted` flag)

## Migration Plan

### User Entity Changes

```csharp
// TripleDetection.Data/Entities/User.cs
public class User : BaseEntity
{
    public int Id { get; set; }              // NEW: auto-increment PK
    public string Username { get; set; }      // UNIQUE index (kept for auth lookups)
    public string RealName { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsLocked { get; set; }
    // ... other fields inherited from BaseEntity (CreateAt, UpdateAt, IsDeleted, etc.)
}
```

### Files to Delete

| File | Reason |
|------|--------|
| `TripleDetection.Data/Repositories/UserRepository.cs` | JSON-based persistence, obsolete |
| `TripleDetection.Data/Repositories/IUserRepository.cs` | Replaced by `IRepository<User>` |
| `TripleDetection.Data/Repositories/Sqlite/SqliteUserRepository.cs` | Replaced by `SqliteRepository<User>` |

### Files to Modify

| File | Changes |
|------|---------|
| `TripleDetection.Data/Entities/User.cs` | Add `Id int` auto-increment, remove `int Id` setter override if exists |
| `TripleDetection.Data/Entities/UserQuery.cs` | Rename `GetByUsername` if needed, or add `GetById` |
| `TripleDetection.Services/UserService.cs` | Change `IUserRepository` to `IRepository<User>`, update method signatures |
| `TripleDetection.Data/Repositories/Sqlite/SqliteRepositoryFactory.cs` | Remove `CreateUserRepository()`, use generic `CreateRepository<User>()` |
| `TripleDetection.Data/Repositories/Sqlite/SqliteUnitOfWork.cs` | Remove `GetUserRepository()`, use generic `GetRepository<User>()` |
| `TripleDetection.Data/Repositories/Contracts/IRepositoryFactory.cs` | Remove `CreateUserRepository()` |
| `TripleDetection.Data/Repositories/Contracts/IUnitOfWork.cs` | Remove `GetUserRepository()` |
| `TripleDetection.Data/Repositories/Configuration/UserConfiguration.cs` | Add `Id` column mapping, keep `Username` unique |
| `docs/database/init.sql` | Add admin user with Id, Username, hashed password |
| `TripleDetection.App/DatabaseConfig.cs` | Update if needed for User entity changes |

### EF Migration / Schema Update

The existing SQLite database needs a schema change:

```sql
-- For existing users, Id will be auto-assigned
-- Username unique constraint already exists
ALTER TABLE Users ADD COLUMN Id INTEGER PRIMARY KEY AUTOINCREMENT;
```

Or use EF code-first migration approach.

### UserQuery Changes

Rename query parameter from `Username` to `ExactUsername` for exact match (vs `Contains`模糊查询 in other fields):

```csharp
public class UserQuery : PagedQuery
{
    public string ExactUsername { get; set; }  // exact match for auth
    public string Username { get; set; }       // fuzzy search (optional)
    public string Role { get; set; }
    public string StatusText { get; set; }
    // ...
}
```

## Implementation Order

1. Update `User.cs` entity — add `Id` PK
2. Update `UserConfiguration.cs` — map `Id` column, keep `Username` unique
3. Delete `IUserRepository.cs`
4. Delete `UserRepository.cs` (JSON-based)
5. Delete `SqliteUserRepository.cs`
6. Update `UserService.cs` — use `IRepository<User>`
7. Update `IRepositoryFactory.cs` — remove `CreateUserRepository()`
8. Update `IUnitOfWork.cs` — remove `GetUserRepository()`
9. Update `SqliteRepositoryFactory.cs` — remove `CreateUserRepository()`
10. Update `SqliteUnitOfWork.cs` — remove `GetUserRepository()`
11. Update `UserQuery.cs` — add `ExactUsername` for auth lookups
12. Update `SqliteRepository<User>` query — handle `ExactUsername` filter
13. Update `docs/database/init.sql` — add admin user with `Id`
14. Build and verify

## Verification Criteria

- [ ] `dotnet build` passes with 0 errors
- [ ] User login works (username + password authentication)
- [ ] User CRUD operations from UI work correctly
- [ ] UserManagement page shows all users from SQLite
- [ ] UserQuery with `ExactUsername` returns correct user for login
- [ ] Existing users in `users.json` are NOT automatically migrated (fresh init via SQL)