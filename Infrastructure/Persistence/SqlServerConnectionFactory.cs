using System;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace TripleDetection.Infrastructure.Persistence;

public class SqlServerConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlServerConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public DbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}