using System;
using System.IO;
using System.Linq;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories;
using TripleDetection.Data.Repositories.Sqlite;

namespace TripleDetection.Data
{
    /// <summary>
    /// 数据库初始化器 - 创建数据库并导入初始数据
    /// </summary>
    public static class DatabaseInitializer
    {
        private static readonly string DataDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Data");

        private static readonly string DbPath = Path.Combine(
            DataDirectory, "tripledetection.db");

        private static readonly string ConfigDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Config");

        private static readonly string UsersJsonPath = Path.Combine(
            ConfigDirectory, "users.json");

        /// <summary>
        /// 初始化数据库 - 调用此方法启动应用时
        /// </summary>
        public static void Initialize()
        {
            // 确保 Data 目录存在
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            // 创建数据库和表
            EnsureDatabaseCreated();

            // 导入初始数据（如果数据库为空）
            SeedInitialData();
        }

        /// <summary>
        /// 确保数据库和表已创建
        /// </summary>
        public static void EnsureDatabaseCreated()
        {
            using (var context = new SqliteDbContext())
            {
                // 创建数据库如果不存在
                if (!context.Database.Exists())
                {
                    context.Database.Create();
                }
            }
        }

        /// <summary>
        /// 导入初始数据（仅当数据库为空时）
        /// </summary>
        public static void SeedInitialData()
        {
            using (var context = new SqliteDbContext())
            {
                // 检查是否已有用户数据
                if (context.Users.Any())
                {
                    return; // 已有数据，跳过
                }

                // 从 JSON 文件读取初始用户
                if (File.Exists(UsersJsonPath))
                {
                    try
                    {
                        var json = File.ReadAllText(UsersJsonPath);
                        var userList = SimpleJsonHelper.Deserialize<UserList>(json);

                        if (userList?.Users != null)
                        {
                            foreach (var user in userList.Users)
                            {
                                context.Users.Add(user);
                            }
                            context.SaveChanges();
                        }
                    }
                    catch
                    {
                        // 如果 JSON 解析失败，创建默认管理员
                        CreateDefaultAdmin(context);
                    }
                }
                else
                {
                    // 没有 JSON 文件，创建默认管理员
                    CreateDefaultAdmin(context);
                }
            }
        }

        private static void CreateDefaultAdmin(SqliteDbContext context)
        {
            var admin = new User
            {
                Username = "admin",
                RealName = "管理员",
                Password = "admin123",
                Role = "Admin",
                IsEnabled = true,
                IsLocked = false,
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            };
            context.Users.Add(admin);
            context.SaveChanges();
        }
    }
}