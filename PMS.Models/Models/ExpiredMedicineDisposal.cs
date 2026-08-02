namespace PMS.Models.Models;

/// <summary>
/// Records the disposal (destruction/throw-away) of expired or near-expired medicine.
/// </summary>
public class ExpiredMedicineDisposal
{
    public int DisposalId { get; set; }
    public string DisposalNumber { get; set; } = string.Empty; // e.g. DSP-20260801-0001

    public int PharmacistId { get; set; }
    public DateTime DisposalDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Pharmacist { get; set; } = null!;
    public ICollection<ExpiredMedicineDisposalItem> Items { get; set; } = new List<ExpiredMedicineDisposalItem>();
}
