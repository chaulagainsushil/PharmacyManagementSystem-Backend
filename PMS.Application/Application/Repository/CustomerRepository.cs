using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _db;

    public CustomerRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Customer>> GetAllAsync() =>
        await _db.Customers
            .Include(c => c.Sales)
            .AsNoTracking()
            .OrderBy(c => c.FullName)
            .ToListAsync();

    public async Task<Customer?> GetByIdAsync(int id) =>
        await _db.Customers
            .Include(c => c.Sales)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == id);

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
