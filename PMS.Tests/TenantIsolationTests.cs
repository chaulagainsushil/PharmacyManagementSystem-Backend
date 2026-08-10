using Microsoft.EntityFrameworkCore;
using PMS.Models.Interfaces;
using PMS.Models.Models;
using PMS.Repository.Data;
using Xunit;

namespace PMS.Tests;

/// <summary>
/// Verifies that EF Core's global query filters correctly isolate each tenant's data.
/// Pharmacy A must never see Pharmacy B's medicines, customers, or sales.
/// </summary>
public class TenantIsolationTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an in-memory ApplicationDbContext scoped to the specified tenant.
    /// The shared database name ensures both tenants write to the same in-memory store.
    /// </summary>
    private static ApplicationDbContext CreateContext(
        Guid tenantId, string dbName = "TenantIsolationDb")
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var tenantProvider = new FakeTenantProvider(tenantId);
        return new ApplicationDbContext(options, tenantProvider);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Medicine_QueryFilter_TenantA_CannotSee_TenantB_Medicines()
    {
        var dbName = $"TenantIsolation_{Guid.NewGuid()}";

        // Seed: add medicines for both tenants using separate contexts
        await using (var ctxA = CreateContext(TenantA, dbName))
        {
            ctxA.Medicines.Add(new Medicine { Name = "Aspirin (A)", TenantId = TenantA });
            ctxA.Medicines.Add(new Medicine { Name = "Paracetamol (A)", TenantId = TenantA });
            await ctxA.SaveChangesAsync();
        }

        await using (var ctxB = CreateContext(TenantB, dbName))
        {
            ctxB.Medicines.Add(new Medicine { Name = "Ibuprofen (B)", TenantId = TenantB });
            await ctxB.SaveChangesAsync();
        }

        // Assert: querying as Tenant A returns only Tenant A's medicines
        await using var queryCtxA = CreateContext(TenantA, dbName);
        var medicinesForA = await queryCtxA.Medicines.ToListAsync();

        Assert.Equal(2, medicinesForA.Count);
        Assert.All(medicinesForA, m => Assert.Equal(TenantA, m.TenantId));
        Assert.DoesNotContain(medicinesForA, m => m.Name.Contains("(B)"));
    }

    [Fact]
    public async Task Medicine_QueryFilter_TenantB_CannotSee_TenantA_Medicines()
    {
        var dbName = $"TenantIsolation_{Guid.NewGuid()}";

        await using (var ctxA = CreateContext(TenantA, dbName))
        {
            ctxA.Medicines.Add(new Medicine { Name = "Aspirin (A)", TenantId = TenantA });
            await ctxA.SaveChangesAsync();
        }

        await using (var ctxB = CreateContext(TenantB, dbName))
        {
            ctxB.Medicines.Add(new Medicine { Name = "Ibuprofen (B)", TenantId = TenantB });
            await ctxB.SaveChangesAsync();
        }

        await using var queryCtxB = CreateContext(TenantB, dbName);
        var medicinesForB = await queryCtxB.Medicines.ToListAsync();

        Assert.Single(medicinesForB);
        Assert.Equal(TenantB, medicinesForB[0].TenantId);
        Assert.DoesNotContain(medicinesForB, m => m.Name.Contains("(A)"));
    }

    [Fact]
    public async Task Customer_QueryFilter_IsolatesByTenant()
    {
        var dbName = $"TenantIsolation_{Guid.NewGuid()}";

        await using (var ctxA = CreateContext(TenantA, dbName))
        {
            ctxA.Customers.Add(new Customer
                { FullName = "Alice", PhoneNumber = "9800000001", TenantId = TenantA });
            await ctxA.SaveChangesAsync();
        }

        await using (var ctxB = CreateContext(TenantB, dbName))
        {
            ctxB.Customers.Add(new Customer
                { FullName = "Bob", PhoneNumber = "9800000002", TenantId = TenantB });
            await ctxB.SaveChangesAsync();
        }

        await using var qA = CreateContext(TenantA, dbName);
        var customersA = await qA.Customers.ToListAsync();

        await using var qB = CreateContext(TenantB, dbName);
        var customersB = await qB.Customers.ToListAsync();

        Assert.Single(customersA);
        Assert.Equal("Alice", customersA[0].FullName);

        Assert.Single(customersB);
        Assert.Equal("Bob", customersB[0].FullName);
    }

    [Fact]
    public async Task Category_QueryFilter_IsolatesByTenant()
    {
        var dbName = $"TenantIsolation_{Guid.NewGuid()}";

        await using (var ctx = CreateContext(TenantA, dbName))
        {
            ctx.Categories.Add(new Category { CategoryName = "Antibiotics", TenantId = TenantA });
            ctx.Categories.Add(new Category { CategoryName = "Vitamins", TenantId = TenantA });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(TenantB, dbName))
        {
            ctx.Categories.Add(new Category { CategoryName = "Analgesics", TenantId = TenantB });
            await ctx.SaveChangesAsync();
        }

        await using var qA = CreateContext(TenantA, dbName);
        var catA = await qA.Categories.ToListAsync();

        Assert.Equal(2, catA.Count);
        Assert.All(catA, c => Assert.Equal(TenantA, c.TenantId));
    }

    [Fact]
    public async Task IgnoreQueryFilters_ReturnsAllTenants()
    {
        var dbName = $"TenantIsolation_{Guid.NewGuid()}";

        await using (var ctx = CreateContext(TenantA, dbName))
        {
            ctx.Medicines.Add(new Medicine { Name = "Med-A", TenantId = TenantA });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(TenantB, dbName))
        {
            ctx.Medicines.Add(new Medicine { Name = "Med-B", TenantId = TenantB });
            await ctx.SaveChangesAsync();
        }

        // When the caller explicitly opts out of the global filter they see everything
        await using var qA = CreateContext(TenantA, dbName);
        var allMedicines = await qA.Medicines.IgnoreQueryFilters().ToListAsync();

        Assert.Equal(2, allMedicines.Count);
    }
}

/// <summary>Minimal ITenantProvider stub for testing — returns a fixed Guid.</summary>
public class FakeTenantProvider : ITenantProvider
{
    public FakeTenantProvider(Guid tenantId) => TenantId = tenantId;
    public Guid TenantId { get; }
}
