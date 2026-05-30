using System;

namespace TripleDetection.Data.Entities
{
    /// <summary>
    /// 操作审计日志实体
    /// </summary>
    public class AuditLog : BaseEntity
    {
        public int UserId { get; set; }                    // 用户ID
        public string UserName { get; set; }                // redundant storage for safety when user is deleted
        public string Action { get; set; }                   // operation type (登录/登出/创建/审批/修改/删除)
        public string ObjectType { get; set; }               // object type (User/Task/Product/Config)
        public int ObjectId { get; set; }                   // object ID
        public string Details { get; set; }                // 操作详情
        public string IpAddress { get; set; }              // IP地址

        // 导航属性
        public virtual User User { get; set; }
    }
}