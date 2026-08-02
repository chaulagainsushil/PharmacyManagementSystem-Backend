namespace PMS.Common.DTOs.Disposal;

public class DisposalResponseDto
{
    public int DisposalId { get; set; }
    public string DisposalNumber { get; set; } = string.Empty;
    public int PharmacistId { get; set; }
    public string PharmacistName { get; set; } = string.Empty;
    public DateTime DisposalDate { get; set; }
    public List<DisposalItemResponseDto> Items { get; set; } = new();
}
