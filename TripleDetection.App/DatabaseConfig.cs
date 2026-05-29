using System;
using TripleDetection.Data;
using TripleDetection.Data.Repositories.Contracts;
using TripleDetection.Data.Repositories.Sqlite;

namespace TripleDetection.App
{
    /// <summary>
    /// 数据库配置 - 应用启动时初始化数据库
    /// </summary>
    public static class DatabaseConfig
    {
        private static IRepositoryFactory _factory;

        /// <summary>
        /// 获取当前仓储工厂实例
        /// </summary>
        public static IRepositoryFactory Factory
        {
            get
            {
                if (_factory == null)
                {
                    _factory = new SqliteRepositoryFactory();
                }
                return _factory;
            }
        }

        /// <summary>
        /// 初始化数据库 - 应在应用启动时调用
        /// </summary>
        public static void Initialize()
        {
            DatabaseInitializer.Initialize();
        }
    }
}