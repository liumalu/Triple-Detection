using System;

namespace TripleDetection.Data.Entities
{
    /// <summary>
    /// 任务实体
    /// </summary>
    public class ProdTask : BaseEntity
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
}