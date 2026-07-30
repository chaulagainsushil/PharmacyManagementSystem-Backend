using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.Customer;

public class CreateCustomerDto
{
    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(200, ErrorMessage = "Full name must not exceed 200 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [MaxLength(20, ErrorMessage = "Phone number must not exceed 20 characters.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Address must not exceed 500 characters.")]
    public string? Address { get; set; }
}
