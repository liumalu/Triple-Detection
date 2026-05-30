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
            var providerServices = SQLiteProviderFactory.Instance.GetService(typeof(DbProviderServices)) as DbProviderServices;
            if (providerServices != null)
            {
                SetProviderServices("System.Data.SQLite.EF6", providerServices);
                SetProviderFactory("System.Data.SQLite.EF6", SQLiteProviderFactory.Instance);
                SetProviderFactory("System.Data.SQLite", SQLiteProviderFactory.Instance);
            }
        }
    }
}