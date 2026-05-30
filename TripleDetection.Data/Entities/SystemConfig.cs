using System;

namespace TripleDetection.Data.Entities
{
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
}