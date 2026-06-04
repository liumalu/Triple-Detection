using System;

namespace TripleDetection.Domain.Entities
{

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
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