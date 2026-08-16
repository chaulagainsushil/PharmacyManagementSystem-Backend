using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Application.Application.Repository;
using PMS.Application.Application.Service;
using PMS.Filters;

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

        // ── Unit of Measure ───────────────────────────────────────────────────
        services.AddScoped<IUnitOfMeasureRepository, UnitOfMeasureRepository>();
        services.AddScoped<IUnitOfMeasureService, UnitOfMeasureService>();

        services.AddScoped<IMedicineUnitRepository, MedicineUnitRepository>();
        services.AddScoped<IMedicineUnitService, MedicineUnitService>();

        // ── Product & ProductUnit (Generic dual-unit system) ───────────────────
        services.AddScoped<IProductUnitRepository, ProductUnitRepository>();
        services.AddScoped<IProductUnitService, ProductUnitService>();

        // ── Sale ──────────────────────────────────────────────────────────────
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ISaleService, SaleService>();

        // ── Disposal ──────────────────────────────────────────────────────────
        services.AddScoped<IDisposalRepository, DisposalRepository>();
        services.AddScoped<IDisposalService, DisposalService>();

        // ── Report ────────────────────────────────────────────────────────────
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportService, ReportService>();

        // ── Pharmacy Profile ──────────────────────────────────────────────────
        services.AddScoped<IPharmacyProfileRepository, PharmacyProfileRepository>();
        services.AddScoped<IPharmacyProfileService, PharmacyProfileService>();

        return services;
    }

    /// <summary>
    /// Configures MVC to register the <see cref="SubscriptionGateFilter"/> globally
    /// so it applies to every controller action without requiring a per-controller attribute.
    /// </summary>
    public static IMvcBuilder AddSubscriptionGateFilter(this IMvcBuilder mvcBuilder)
    {
        mvcBuilder.AddMvcOptions(options =>
            options.Filters.AddService<SubscriptionGateFilter>());

        return mvcBuilder;
    }
}
