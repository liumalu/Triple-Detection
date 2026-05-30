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
    /// 任务状态枚举
    /// </summary>
    public enum TaskStatus
    {
        Pending = 0,    // 待审核
        Approved = 1,   // 已审核
        Running = 2,    // 执行中
        Completed = 3   // 已完成
    }
}