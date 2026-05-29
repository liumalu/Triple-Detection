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
            var dbPath = System.IO.Path.Combine(
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