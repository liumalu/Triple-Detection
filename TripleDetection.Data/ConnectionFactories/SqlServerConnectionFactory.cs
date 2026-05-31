using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace TripleDetection.Data.ConnectionFactories
{
    /// <summary>
    /// SqlServer 数据库连接工厂（未来扩展用）
    /// 切换数据库时：替换 DI 注册行即可，业务层代码无需改动
    /// </summary>
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
}