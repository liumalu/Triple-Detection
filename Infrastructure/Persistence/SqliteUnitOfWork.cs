using System;
using System.Collections.Generic;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Repositories;
using TripleDetection.Infrastructure.Repositories;

namespace TripleDetection.Infrastructure.Persistence
{

public class SqliteUnitOfWork : IUnitOfWork
{
    private readonly string _connectionString;
    private bool _disposed;
    private readonly Dictionary<Type, object> _repositories = new Dictionary<Type, object>();

    public SqliteUnitOfWork(string connectionString)
    {
        _connectionString = connectionString;
    }

    public bool IsInTransaction => false;

    public void BeginTransaction()
    {
        throw new NotImplementedException("Transaction not supported in raw ADO.NET implementation");
    }

    public void Commit()
    {
        throw new NotImplementedException("Transaction not supported in raw ADO.NET implementation");
    }

    public void Rollback()
    {
        throw new NotImplementedException("Transaction not supported in raw ADO.NET implementation");
    }

    public int SaveChanges()
    {
        return 0;
    }

    public IRepository<T> GetRepository<T>() where T : BaseEntity
    {
        var entityType = typeof(T);
        if (!_repositories.ContainsKey(entityType))
        {
            _repositories[entityType] = new SqliteRepository<T>(_connectionString);
        }
        return (IRepository<T>)_repositories[entityType];
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
            }
            _disposed = true;
        }
    }
}
}