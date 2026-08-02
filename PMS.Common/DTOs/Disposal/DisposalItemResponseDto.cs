namespace PMS.Common.DTOs.Disposal;

public class DisposalItemResponseDto
{
    public int DisposalItemId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int QuantityInTablets { get; set; }
    public string Reason { get; set; } = string.Empty;
}
