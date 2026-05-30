using System;
using TripleDetection.Data;

namespace TripleDetection.Data.Entities
{
    public class User : BaseEntity
    {
        public string Username { get; set; }
        public string RealName { get; set; }
        public string Password { get; set; }
        public string PasswordSalt { get; set; }
        public string PasswordHash { get; set; }

        public string Role { get; set; } = "Operator";
        public bool IsEnabled { get; set; } = true;
        public bool IsLocked { get; set; } = false;
        public DateTime? LastLoginAt { get; set; }

        public string StatusText
        {
            get
            {
                if (!IsEnabled) return "已禁用";
                if (IsLocked) return "已锁定";
                return "正常";
            }
        }
    }
}
