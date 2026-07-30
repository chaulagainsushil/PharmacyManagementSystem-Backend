namespace PMS.Models.Models;

public class Manufacturer
{
    public int ManufacturerId { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
}
