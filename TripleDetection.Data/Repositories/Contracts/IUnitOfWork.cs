using System;

namespace TripleDetection.Data.Repositories.Contracts
{
    /// <summary>
    /// 工作单元接口 - 管理事务和仓储
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// 开始事务
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// 提交事务
        /// </summary>
        void Commit();

        /// <summary>
        /// 回滚事务
        /// </summary>
        void Rollback();

        /// <summary>
        /// 获取指定实体类型的仓储
        /// </summary>
        IRepository<T> GetRepository<T>() where T : BaseEntity;

        /// <summary>
        /// 保存所有更改
        /// </summary>
        int SaveChanges();

        /// <summary>
        /// 是否处于事务中
        /// </summary>
        bool IsInTransaction { get; }
    }
}