using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Customer;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repo;

    public CustomerService(ICustomerRepository repo) => _repo = repo;

    public async Task<IEnumerable<CustomerListItemDto>> GetAllAsync()
    {
        var customers = await _repo.GetAllAsync();
        return customers.Select(c => new CustomerListItemDto
        {
            CustomerId  = c.CustomerId,
            FullName    = c.FullName,
            PhoneNumber = c.PhoneNumber,
            Address     = c.Address,
            TotalSales  = c.Sales.Count,
            CreatedAt   = c.CreatedAt
        });
    }

    public async Task<CustomerResponseDto?> GetByIdAsync(int id)
    {
        var customer = await _repo.GetByIdAsync(id);
        return customer is null ? null : MapToResponse(customer);
    }

    public async Task<(bool Success, string Message, CustomerResponseDto? Data)> CreateAsync(
        CreateCustomerDto dto)
    {
        if (await _repo.ExistsByPhoneAsync(dto.PhoneNumber))
            return (false, $"A customer with phone '{dto.PhoneNumber}' already exists.", null);

        var customer = new Customer
        {
            FullName    = dto.FullName.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            Address     = dto.Address?.Trim(),
            CreatedAt   = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(customer);
        return (true, "Customer created successfully.", MapToResponse(created));
    }

    public async Task<(bool Success, string Message, CustomerResponseDto? Data)> UpdateAsync(
        int id, UpdateCustomerDto dto)
    {
        var customer = await _repo.GetByIdAsync(id);
        if (customer is null)
            return (false, $"Customer with ID {id} not found.", null);

        if (await _repo.ExistsByPhoneAsync(dto.PhoneNumber, excludeId: id))
            return (false, $"A customer with phone '{dto.PhoneNumber}' already exists.", null);

        customer.FullName    = dto.FullName.Trim();
        customer.PhoneNumber = dto.PhoneNumber.Trim();
        customer.Address     = dto.Address?.Trim();

        var updated = await _repo.UpdateAsync(customer);
        return (true, "Customer updated successfully.", MapToResponse(updated));
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var customer = await _repo.GetByIdAsync(id);
        if (customer is null)
            return (false, $"Customer with ID {id} not found.");

        if (await _repo.HasSalesAsync(id))
            return (false, "Cannot delete customer — they have sales records linked to them.");

        await _repo.DeleteAsync(id);
        return (true, "Customer deleted successfully.");
    }

    private static CustomerResponseDto MapToResponse(Customer c) => new()
    {
        CustomerId  = c.CustomerId,
        FullName    = c.FullName,
        PhoneNumber = c.PhoneNumber,
        Address     = c.Address,
        TotalSales  = c.Sales?.Count ?? 0,
        CreatedAt   = c.CreatedAt
    };
}
