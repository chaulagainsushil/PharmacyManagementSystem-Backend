namespace PMS.Models.Models;

/// <summary>
/// One medicine+batch line within a disposal record.
/// </summary>
public class ExpiredMedicineDisposalItem
{
    public int DisposalItemId { get; set; }
    public int DisposalId { get; set; }
    public int MedicineId { get; set; }
    public int BatchId { get; set; }

    /// <summary>Number of tablets being disposed.</summary>
    public int QuantityInTablets { get; set; }

    /// <summary>Reason for disposal (e.g. "Expired", "Damaged", "Near expiry recall").</summary>
    public string Reason { get; set; } = string.Empty;

    public ExpiredMedicineDisposal Disposal { get; set; } = null!;
    public Medicine Medicine { get; set; } = null!;
    public MedicineBatch Batch { get; set; } = null!;
}
