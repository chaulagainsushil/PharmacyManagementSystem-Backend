namespace PMS.Models.Models;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<ExpiredMedicineDisposal> Disposals { get; set; } = new List<ExpiredMedicineDisposal>();
}
