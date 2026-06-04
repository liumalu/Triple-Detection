using System;
using System.Data.Common;
using System.Data.SQLite;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Repositories;
using TripleDetection.Infrastructure.Persistence;

namespace TripleDetection.Infrastructure.Repositories
{

public class SqliteRepositoryFactory : IRepositoryFactory
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteRepositoryFactory(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public DatabaseProviderType ProviderType => DatabaseProviderType.Sqlite;

    public IUnitOfWork CreateUnitOfWork()
    {
        var connectionString = GetConnectionString();
        return new SqliteUnitOfWork(connectionString);
    }

    public IRepository<T> CreateRepository<T>() where T : BaseEntity
    {
        var connection = _connectionFactory.CreateConnection();
        return new SqliteRepository<T>(connection.ConnectionString);
    }

    public IAuditLogRepository CreateAuditLogRepository()
    {
        var connection = _connectionFactory.CreateConnection();
        return new AuditLogRepository(connection.ConnectionString);
    }

    public IDetectionRecordRepository CreateDetectionRecordRepository()
    {
        var connection = _connectionFactory.CreateConnection();
        return new DetectionRecordRepository(connection.ConnectionString);
    }

    private string GetConnectionString()
    {
        using (var conn = _connectionFactory.CreateConnection())
        {
            return conn.ConnectionString;
        }
    }
}
}