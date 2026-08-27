using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.Customer;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repo;

    public CustomerService(ICustomerRepository repo) => _repo = repo;

    public async Task<IEnumerable<CustomerListItemDto>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
    }

    public async Task<IEnumerable<CustomerListItemDto>> SearchAsync(string query)
    {
        return await _repo.SearchAsync(query);
    }

    public async Task<CustomerResponseDto?> GetByIdAsync(int id)
    {
        return await _repo.GetByIdAsync(id);
    }

    public async Task<CustomerResponseDto?> GetDefaultAsync()
    {
        var defaultCustomer = await _repo.GetDefaultCustomerAsync();
        return defaultCustomer == null ? null : await _repo.GetByIdAsync(defaultCustomer.CustomerId);
    }

    public async Task<(bool Success, string Message, CustomerResponseDto? Data)> CreateAsync(
        CreateCustomerDto dto)
    {
        if (await _repo.ExistsByPhoneAsync(dto.PhoneNumber))
            return (false, $"A customer with phone '{dto.PhoneNumber}' already exists.", null);

        if (dto.IsDefault)
        {
            await _repo.ClearOtherDefaultsAsync();
        }

        var customer = new Customer
        {
            FullName    = dto.FullName.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            Address     = dto.Address?.Trim(),
            IsDefault   = dto.IsDefault,
            CreatedAt   = DateTime.UtcNow
        };

        var created  = await _repo.CreateAsync(customer);
        var response = await _repo.GetByIdAsync(created.CustomerId);
        return (true, "Customer created successfully.", response);
    }

    public async Task<(bool Success, string Message, bool IsConflict, CustomerResponseDto? Data)> UpdateAsync(
        int id, UpdateCustomerDto dto)
    {
        // 1. Read current DTO (includes UpdatedAt)
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            return (false, $"Customer with ID {id} not found.", false, null);

        // 2. Optimistic concurrency check
        if (!ConcurrencyHelper.Matches(existing.UpdatedAt, dto.UpdatedAt))
            return (false, "The record was modified by another user. Please refresh and try again.", true, null);

        // 3. Business rule: duplicate phone
        if (await _repo.ExistsByPhoneAsync(dto.PhoneNumber, excludeId: id))
            return (false, $"A customer with phone '{dto.PhoneNumber}' already exists.", false, null);

        if (dto.IsDefault)
        {
            await _repo.ClearOtherDefaultsAsync(excludeId: id);
        }

        // 4. Fetch tracked entity and mutate
        var entity = await _repo.GetEntityByIdAsync(id);
        if (entity is null)
            return (false, $"Customer with ID {id} not found.", false, null);

        entity.FullName    = dto.FullName.Trim();
        entity.PhoneNumber = dto.PhoneNumber.Trim();
        entity.Address     = dto.Address?.Trim();
        entity.IsDefault   = dto.IsDefault;

        await _repo.UpdateAsync(entity);

        var updated = await _repo.GetByIdAsync(id);
        return (true, "Customer updated successfully.", false, updated);
    }

    public async Task<(bool Success, string Message, CustomerResponseDto? Data)> SetDefaultAsync(int id)
    {
        var entity = await _repo.GetEntityByIdAsync(id);
        if (entity is null)
            return (false, $"Customer with ID {id} not found.", null);

        await _repo.ClearOtherDefaultsAsync(excludeId: id);
        entity.IsDefault = true;
        await _repo.UpdateAsync(entity);

        var updated = await _repo.GetByIdAsync(id);
        return (true, $"{entity.FullName} is now set as the default billing customer.", updated);
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
}
