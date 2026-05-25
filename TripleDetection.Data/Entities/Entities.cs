using System;

namespace TripleDetection.Data.Entities
{
    /// <summary>
    /// 有效期类型枚举
    /// </summary>
    public enum ValidType
    {
        Year = 0,   // 年
        Month = 1,  // 月
        Day = 2     // 日
    }

    /// <summary>
    /// 产品状态枚举
    /// </summary>
    public enum ProductStatus
    {
        Inactive = 0,  // 停用
        Active = 1     // 启用
    }

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

    /// <summary>
    /// 任务状态枚举
    /// </summary>
    public enum TaskStatus
    {
        Pending = 0,    // 待审核
        Approved = 1,   // 已审核
        Running = 2,    // 执行中
        Completed = 3   // 已完成
    }

    /// <summary>
    /// 任务实体
    /// </summary>
    public class Task : BaseEntity
    {
        public string Name { get; set; }                   // 任务名称
        public int ProductId { get; set; }                 // 关联产品ID
        public TaskStatus Status { get; set; }              // 任务状态
        public string CreatedBy { get; set; }               // 提交人
        public string ReviewedBy { get; set; }             // 审核人
        public DateTime? ReviewedAt { get; set; }          // 审核时间
        public DateTime ProductionDate { get; set; }       // 生产日期
        public DateTime? ExpirationDate { get; set; }      // 有效期至
        public string BatchNumber { get; set; }             // 批次号

        // 导航属性
        public virtual Product Product { get; set; }
    }

    /// <summary>
    /// 用户角色枚举
    /// </summary>
    public enum UserRole
    {
        Admin = 0,      // 管理员
        Operator = 1   // 操作员
    }

    /// <summary>
    /// 用户实体
    /// </summary>
    public class User : BaseEntity
    {
        public string Username { get; set; }               // 用户名
        public string PasswordHash { get; set; }           // 密码哈希
        public UserRole Role { get; set; }                 // 角色
    }

    /// <summary>
    /// 检测记录实体
    /// </summary>
    public class DetectionRecord : BaseEntity
    {
        public int TaskId { get; set; }                   // 关联任务ID
        public string Result { get; set; }                 // 检测结果（OK/NG）
        public double Confidence { get; set; }              // 置信度
        public int CharCount { get; set; }                 // 字符数量
        public string CodeInfo { get; set; }                // 编码信息
        public string ImagePath { get; set; }              // 图像存储路径
        public DateTime DetectionTime { get; set; }         // 检测时间

        // 导航属性
        public virtual Task Task { get; set; }
    }

    /// <summary>
    /// 系统配置实体
    /// </summary>
    public class SystemConfig : BaseEntity
    {
        public string Category { get; set; }               // 配置分类（VM/相机/PLC/图像存储等）
        public string Key { get; set; }                    // 配置键
        public string Value { get; set; }                   // 配置值
        public string Description { get; set; }             // 配置描述
    }

    /// <summary>
    /// 操作审计日志实体
    /// </summary>
    public class AuditLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }                    // 用户ID
        public string Action { get; set; }                 // 操作类型
        public string Details { get; set; }                // 操作详情
        public string IpAddress { get; set; }              // IP地址
        public DateTime CreateAt { get; set; }             // 创建时间

        // 导航属性
        public virtual User User { get; set; }
    }
}