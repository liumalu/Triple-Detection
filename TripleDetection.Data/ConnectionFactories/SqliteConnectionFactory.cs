using System;
using System.Data.Common;
using System.Data.SQLite;

namespace TripleDetection.Data.ConnectionFactories
{
    /// <summary>
    /// SQLite 数据库连接工厂
    /// </summary>
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