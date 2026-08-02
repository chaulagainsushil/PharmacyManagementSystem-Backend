using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Application.Application.Repository;
using PMS.Application.Application.Service;

namespace PMS.DependencyHandler;

public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Registers all CRUD repositories and services.
    /// Call this from Program.cs: builder.Services.AddCrudServices();
    /// </summary>
    public static IServiceCollection AddCrudServices(this IServiceCollection services)
    {
        // ── Category ──────────────────────────────────────────────────────────
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryService, CategoryService>();

        // ── Medicine ──────────────────────────────────────────────────────────
        services.AddScoped<IMedicineRepository, MedicineRepository>();
        services.AddScoped<IMedicineService, MedicineService>();

        // ── Supplier ──────────────────────────────────────────────────────────
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ISupplierService, SupplierService>();

        // ── Manufacturer ──────────────────────────────────────────────────────
        services.AddScoped<IManufacturerRepository, ManufacturerRepository>();
        services.AddScoped<IManufacturerService, ManufacturerService>();

        // ── Customer ──────────────────────────────────────────────────────────
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerService, CustomerService>();

        // ── MedicineBatch ─────────────────────────────────────────────────────
        services.AddScoped<IMedicineBatchRepository, MedicineBatchRepository>();
        services.AddScoped<IMedicineBatchService, MedicineBatchService>();

        // ── Sale ──────────────────────────────────────────────────────────────
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ISaleService, SaleService>();

        // ── Disposal ──────────────────────────────────────────────────────────
        services.AddScoped<IDisposalRepository, DisposalRepository>();
        services.AddScoped<IDisposalService, DisposalService>();

        // ── Report ────────────────────────────────────────────────────────────
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
