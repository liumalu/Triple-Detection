using System.Data.Common;

namespace TripleDetection.Data
{
    /// <summary>
    /// 数据库连接工厂接口
    /// 实现此接口以支持不同数据库（SQLite、SqlServer、MySQL 等）
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// 创建并返回一个数据库连接
        /// </summary>
        DbConnection CreateConnection();
    }
}