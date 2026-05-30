using System;

namespace TripleDetection.Data.Entities
{
    /// <summary>
    /// 产品实体
    /// </summary>
    public class Product : BaseEntity
    {
        public string Code { get; set; }       // 产品编码
        public string Name { get; set; }       // 产品名称
        public string Description { get; set; } // 产品描述
        public string SolFilePath { get; set; } // 绑定的 .sol 方案文件路径
        public ValidType ValidType { get; set; } // 有效期类型（年/月/日）
        public int ValidPeriod { get; set; }   // 有效期数量（默认1）
        public ProductStatus Status { get; set; } // 产品状态（停用/启用）
    }
}