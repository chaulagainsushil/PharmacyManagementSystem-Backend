using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null);
    Task<Customer> CreateAsync(Customer customer);
    Task<Customer> UpdateAsync(Customer customer);
    Task<bool> DeleteAsync(int id);
    Task<bool> HasSalesAsync(int id);
}
