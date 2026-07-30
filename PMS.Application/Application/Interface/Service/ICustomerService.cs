using PMS.Common.DTOs.Customer;

namespace PMS.Application.Application.Interface.Service;

public interface ICustomerService
{
    Task<IEnumerable<CustomerListItemDto>> GetAllAsync();
    Task<CustomerResponseDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message, CustomerResponseDto? Data)> CreateAsync(CreateCustomerDto dto);
    Task<(bool Success, string Message, CustomerResponseDto? Data)> UpdateAsync(int id, UpdateCustomerDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}
