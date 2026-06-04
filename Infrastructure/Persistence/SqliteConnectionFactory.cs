using System;
using System.Data.Common;
using System.Data.SQLite;
using TripleDetection.Domain.Repositories;

namespace TripleDetection.Infrastructure.Persistence
{

public class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public DbConnection CreateConnection()
    {
        return new SQLiteConnection(_connectionString);
    }
}
}