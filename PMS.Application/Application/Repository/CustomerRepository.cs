using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Common.DTOs.Customer;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _db;

    public CustomerRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<CustomerListItemDto>> GetAllAsync() =>
        await _db.Customers
            .AsNoTracking()
            .OrderBy(c => c.FullName)
            .Select(c => new CustomerListItemDto
            {
                CustomerId   = c.CustomerId,
                FullName     = c.FullName,
                PhoneNumber  = c.PhoneNumber,
                Address      = c.Address,
                TotalSales   = c.Sales.Count(),
                CreatedAt    = c.CreatedAt,
                UpdatedAt    = c.UpdatedAt
            })
            .ToListAsync();

    public async Task<CustomerResponseDto?> GetByIdAsync(int id) =>
        await _db.Customers
            .AsNoTracking()
            .Where(c => c.CustomerId == id)
            .Select(c => new CustomerResponseDto
            {
                CustomerId  = c.CustomerId,
                FullName    = c.FullName,
                PhoneNumber = c.PhoneNumber,
                Address     = c.Address,
                TotalSales  = c.Sales.Count(),
                CreatedAt   = c.CreatedAt,
                UpdatedAt   = c.UpdatedAt
            })
            .FirstOrDefaultAsync();

    public async Task<Customer?> GetEntityByIdAsync(int id) =>
        await _db.Customers.FindAsync(id);

    public async Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null) =>
        await _db.Customers
            .AnyAsync(c => c.PhoneNumber == phone
                          && (excludeId == null || c.CustomerId != excludeId));

    public async Task<Customer> CreateAsync(Customer customer)
    {
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer> UpdateAsync(Customer customer)
    {
        _db.Customers.Update(customer);
        await _db.SaveChangesAsync();
        return customer;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return false;
        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasSalesAsync(int id) =>
        await _db.Sales.AnyAsync(s => s.CustomerId == id);
}
