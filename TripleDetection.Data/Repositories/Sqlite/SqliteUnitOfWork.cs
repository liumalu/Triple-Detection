using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
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
                _repositories[entityType] = new SqliteRepository<T>(_context);
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
                    _transaction?.Dispose();
                    _context?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}