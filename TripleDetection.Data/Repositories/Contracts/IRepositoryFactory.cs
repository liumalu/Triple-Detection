using System;

namespace TripleDetection.Data.Repositories.Contracts
{
    /// <summary>
    /// 支持的数据库提供者类型
    /// </summary>
    public enum DatabaseProviderType
    {
        InMemory,
        Sqlite,
        MySql,       // 未来扩展
        PostgreSql,  // 未来扩展
        SqlServer    // 未来扩展
    }

    /// <summary>
    /// 仓储工厂接口 - 创建仓储实例
    /// </summary>
    public interface IRepositoryFactory
    {
        /// <summary>
        /// 创建工作单元
        /// </summary>
        IUnitOfWork CreateUnitOfWork();

        /// <summary>
        /// 创建独立仓储
        /// </summary>
        IRepository<T> CreateRepository<T>() where T : BaseEntity;

        /// <summary>
        /// 创建用户仓储
        /// </summary>
        IUserRepository CreateUserRepository();

        /// <summary>
        /// 当前数据库提供者类型
        /// </summary>
        DatabaseProviderType ProviderType { get; }
    }
}