using System;

namespace TripleDetection.Domain.Entities
{

public abstract class BaseEntity
{
    public int Id { get; set; }
    public string CreateBy { get; set; } = string.Empty;
    public string UpdateBy { get; set; } = string.Empty;
    public DateTime CreateAt { get; set; } = DateTime.Now;
    public DateTime UpdateAt { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; } = false;
}
}