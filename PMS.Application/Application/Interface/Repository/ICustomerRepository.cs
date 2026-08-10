using PMS.Common.DTOs.Customer;
using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface ICustomerRepository
{
    Task<IEnumerable<CustomerListItemDto>> GetAllAsync();
    Task<CustomerResponseDto?> GetByIdAsync(int id);

    /// <summary>Returns the tracked entity for update/delete operations.</summary>
    Task<Customer?> GetEntityByIdAsync(int id);

    Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null);
    Task<Customer> CreateAsync(Customer customer);
    Task<Customer> UpdateAsync(Customer customer);
    Task<bool> DeleteAsync(int id);
    Task<bool> HasSalesAsync(int id);
}
