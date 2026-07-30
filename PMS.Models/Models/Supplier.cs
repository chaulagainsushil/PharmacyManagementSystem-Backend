namespace PMS.Models.Models;

public class Supplier
{
    public int SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }

    public ICollection<MedicineBatch> Batches { get; set; } = new List<MedicineBatch>();
}
