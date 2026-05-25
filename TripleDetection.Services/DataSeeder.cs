using System;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories;

namespace TripleDetection.Services
{
    /// <summary>
    /// 测试数据初始化器 - 应用启动时自动填充测试数据
    /// </summary>
    public static class DataSeeder
    {
        private static bool _isSeeded = false;
        private static readonly object _lock = new object();

        public static void SeedIfNeeded()
        {
            if (_isSeeded) return;

            lock (_lock)
            {
                if (_isSeeded) return;
                Seed();
                _isSeeded = true;
            }
        }

        public static void Seed()
        {
            var productRepo = new InMemoryRepository<Product>();
            var taskRepo = new InMemoryRepository<Task>();

            // 如果已经有数据就不再添加
            if (productRepo.Count() > 0) return;

            // 添加测试产品
            var product1 = new Product
            {
                Code = "P001",
                Name = "OCR检测产品A",
                Description = "用于OCR文字识别检测",
                SolFilePath = @"D:\xcm\ApplicationDemo\OCRDemoCs\OCRDemoChinese.sol",
                ValidType = ValidType.Month,
                ValidPeriod = 6,
                Status = ProductStatus.Active
            };

            var product2 = new Product
            {
                Code = "P002",
                Name = "缺陷检测产品B",
                Description = "用于表面缺陷检测",
                SolFilePath = @"D:\xcm\ApplicationDemo\OCRDemoCs\OCRDemoChinese.sol",
                ValidType = ValidType.Year,
                ValidPeriod = 1,
                Status = ProductStatus.Active
            };

            var product3 = new Product
            {
                Code = "P003",
                Name = "尺寸测量产品C",
                Description = "用于尺寸测量",
                SolFilePath = @"D:\xcm\ApplicationDemo\OCRDemoCs\OCRDemoChinese.sol",
                ValidType = ValidType.Day,
                ValidPeriod = 30,
                Status = ProductStatus.Inactive
            };

            productRepo.Add(product1);
            productRepo.Add(product2);
            productRepo.Add(product3);

            // 添加测试任务（关联到产品，状态为已审核 Approved=1）
            var task1 = new Task
            {
                Name = "OCR检测任务-2025-05-01",
                ProductId = 1,
                Status = TaskStatus.Approved,
                CreatedBy = "admin",
                ReviewedBy = "admin",
                ReviewedAt = DateTime.Now.AddDays(-1),
                ProductionDate = DateTime.Today.AddDays(-30),
                ExpirationDate = DateTime.Today.AddDays(150),
                BatchNumber = "BATCH20250501"
            };

            var task2 = new Task
            {
                Name = "缺陷检测任务-2025-05-02",
                ProductId = 2,
                Status = TaskStatus.Approved,
                CreatedBy = "admin",
                ReviewedBy = "admin",
                ReviewedAt = DateTime.Now.AddDays(-2),
                ProductionDate = DateTime.Today.AddDays(-20),
                ExpirationDate = DateTime.Today.AddDays(340),
                BatchNumber = "BATCH20250502"
            };

            var task3 = new Task
            {
                Name = "尺寸测量任务-2025-05-03",
                ProductId = 1,
                Status = TaskStatus.Approved,
                CreatedBy = "operator",
                ReviewedBy = "admin",
                ReviewedAt = DateTime.Now.AddHours(-5),
                ProductionDate = DateTime.Today.AddDays(-10),
                ExpirationDate = DateTime.Today.AddDays(180),
                BatchNumber = "BATCH20250503"
            };

            var task4 = new Task
            {
                Name = "备料任务-待审核",
                ProductId = 3,
                Status = TaskStatus.Pending,
                CreatedBy = "operator",
                ProductionDate = DateTime.Today,
                BatchNumber = "BATCH20250504"
            };

            taskRepo.Add(task1);
            taskRepo.Add(task2);
            taskRepo.Add(task3);
            taskRepo.Add(task4);
        }
    }
}