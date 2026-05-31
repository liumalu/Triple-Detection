using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace TripleDetection.Infrastructure.Persistence;

public class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public DbConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }
}