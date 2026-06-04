using System;
using TripleDetection.Domain.Enums;

namespace TripleDetection.Domain.Entities
{

public class Product : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SolFilePath { get; set; } = string.Empty;
    public ValidType ValidType { get; set; } = ValidType.Year;
    public int ValidPeriod { get; set; } = 1;
    public ProductStatus Status { get; set; } = ProductStatus.Active;
}
}