using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Infrastructure;
using System.Data.SQLite.EF6;

namespace TripleDetection.Data
{
    /// <summary>
    /// SQLite EF6 配置 - 通过代码初始化，避免配置冲突
    /// </summary>
    public class SQLiteEFConfiguration : DbConfiguration
    {
        public SQLiteEFConfiguration()
        {
            var sqliteServices = SQLiteProviderFactory.Instance.GetService(typeof(DbProviderServices)) as DbProviderServices;

            SetProviderServices("System.Data.SQLite.EF6", sqliteServices);
            SetProviderFactory("System.Data.SQLite.EF6", SQLiteProviderFactory.Instance);
            SetProviderFactory("System.Data.SQLite", SQLiteProviderFactory.Instance);

            // 强制 SQLite 为默认连接工厂，防止 EF6 回退到 SqlServer
            SetDefaultConnectionFactory(new SQLiteConnectionFactory());
        }
    }

    /// <summary>
    /// SQLite 连接工厂 - 确保所有连接都使用 SQLite
    /// </summary>
    public class SQLiteConnectionFactory : IDbConnectionFactory
    {
        public DbConnection CreateConnection(string nameOrConnectionString)
        {
            var cs = nameOrConnectionString;
            if (string.IsNullOrEmpty(cs) || cs.IndexOf("Data Source", StringComparison.OrdinalIgnoreCase) < 0)
            {
                cs = $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tripledetection.db")};providerName=System.Data.SQLite.EF6";
            }
            else
            {
                cs = $"{cs};providerName=System.Data.SQLite.EF6";
            }
            return new System.Data.SQLite.SQLiteConnection(cs);
        }
    }
}