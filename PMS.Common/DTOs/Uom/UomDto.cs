namespace PMS.Common.DTOs.Uom;

/// <summary>Response DTO for UnitOfMeasure master records.</summary>
public class UomDto
{
    public int UnitOfMeasureId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
