using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PMS.Models.Enums;
using PMS.Models.Interfaces;
using PMS.Models.Models;

namespace PMS.Repository.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    private readonly ITenantProvider? _tenantProvider;

    /// <summary>
    /// Constructor used at runtime — tenant provider is injected from DI.
    /// </summary>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Design-time constructor used by EF Core migrations tooling.
    /// No tenant provider is available — global filters are bypassed (TenantId = Guid.Empty).
    /// </summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        _tenantProvider = null;
    }

    // ── DbSets ────────────────────────────────────────────────────────────────
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<MedicineBatch> MedicineBatches => Set<MedicineBatch>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<User> AppUsers => Set<User>();

    // ── UoM ───────────────────────────────────────────────────────────────────
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<MedicineUnit> MedicineUnits => Set<MedicineUnit>();

    // ── Disposal ──────────────────────────────────────────────────────────────
    public DbSet<ExpiredMedicineDisposal> ExpiredMedicineDisposals => Set<ExpiredMedicineDisposal>();
    public DbSet<ExpiredMedicineDisposalItem> ExpiredMedicineDisposalItems => Set<ExpiredMedicineDisposalItem>();

    // ── Pharmacy Profile (Tenant root) ────────────────────────────────────────
    public DbSet<PharmacyProfile> PharmacyProfiles => Set<PharmacyProfile>();
    public DbSet<PharmacyPhoneNumber> PharmacyPhoneNumbers => Set<PharmacyPhoneNumber>();

    // ── Subscription ──────────────────────────────────────────────────────────
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionPlanDefinition> SubscriptionPlanDefinitions => Set<SubscriptionPlanDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Global tenant query filters (applied via reflection) ──────────────
        // For every entity type that implements IHasTenant, register a filter so
        // all LINQ queries are automatically scoped to the current tenant.
        ApplyTenantQueryFilters(modelBuilder);

        // ── Manufacturer ─────────────────────────────────────────────────────
        modelBuilder.Entity<Manufacturer>(e =>
        {
            e.HasKey(m => m.ManufacturerId);
            e.Property(m => m.Name).IsRequired().HasMaxLength(200);
            e.Property(m => m.TenantId).IsRequired();
        });

        // ── Category ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.CategoryName).IsRequired().HasMaxLength(100);
            e.Property(c => c.TenantId).IsRequired();
        });

        // ── Supplier ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Supplier>(e =>
        {
            e.HasKey(s => s.SupplierId);
            e.Property(s => s.Name).IsRequired().HasMaxLength(200);
            e.Property(s => s.Phone).HasMaxLength(20);
            e.Property(s => s.Address).HasMaxLength(500);
            e.Property(s => s.TenantId).IsRequired();
        });

        // ── UnitOfMeasure ─────────────────────────────────────────────────────
        modelBuilder.Entity<UnitOfMeasure>(e =>
        {
            e.HasKey(u => u.UnitOfMeasureId);
            e.Property(u => u.Name).IsRequired().HasMaxLength(100);
            e.Property(u => u.Symbol).IsRequired().HasMaxLength(20);
            e.Property(u => u.TenantId).IsRequired();
            // Unique name per tenant (replacing the global unique index)
            e.HasIndex(u => new { u.TenantId, u.Name }).IsUnique();
        });

        // ── MedicineUnit ──────────────────────────────────────────────────────
        modelBuilder.Entity<MedicineUnit>(e =>
        {
            e.HasKey(mu => mu.MedicineUnitId);
            e.Property(mu => mu.ConversionFactorToBase).HasColumnType("numeric(18,6)");
            e.Property(mu => mu.UnitPrice).HasColumnType("numeric(18,4)");
            e.Property(mu => mu.TenantId).IsRequired();

            // Each (Medicine, UoM) pair is unique — no duplicate units per medicine
            e.HasIndex(mu => new { mu.MedicineId, mu.UnitOfMeasureId }).IsUnique();

            // Partial unique index: at most one IsBaseUnit=true per medicine
            e.HasIndex(mu => new { mu.MedicineId, mu.IsBaseUnit })
                .IsUnique()
                .HasFilter("is_base_unit = true")
                .HasDatabaseName("IX_MedicineUnits_MedicineId_IsBaseUnit_Unique");

            // Partial unique index: at most one IsDefault=true per medicine
            e.HasIndex(mu => new { mu.MedicineId, mu.IsDefault })
                .IsUnique()
                .HasFilter("is_default = true")
                .HasDatabaseName("IX_MedicineUnits_MedicineId_IsDefault_Unique");

            e.HasOne(mu => mu.Medicine)
                .WithMany(m => m.Units)
                .HasForeignKey(mu => mu.MedicineId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(mu => mu.UnitOfMeasure)
                .WithMany(u => u.MedicineUnits)
                .HasForeignKey(mu => mu.UnitOfMeasureId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Medicine ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Medicine>(e =>
        {
            e.HasKey(m => m.MedicineId);
            e.Property(m => m.Name).IsRequired().HasMaxLength(300);
            e.Property(m => m.GenericName).HasMaxLength(300);
            e.Property(m => m.StripPrice).HasColumnType("numeric(18,4)");
            e.Property(m => m.TabletPrice).HasColumnType("numeric(18,4)");
            e.Property(m => m.TenantId).IsRequired();

            // Ignore computed properties — not mapped to columns
            e.Ignore(m => m.TotalStockInTablets);
            e.Ignore(m => m.TotalTabletsPerBox);

            e.HasOne(m => m.Category)
                .WithMany(c => c.Medicines)
                .HasForeignKey(m => m.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(m => m.Manufacturer)
                .WithMany(mf => mf.Medicines)
                .HasForeignKey(m => m.ManufacturerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── MedicineBatch ─────────────────────────────────────────────────────
        modelBuilder.Entity<MedicineBatch>(e =>
        {
            e.HasKey(b => b.BatchId);
            e.Property(b => b.BatchNumber).IsRequired().HasMaxLength(100);
            e.Property(b => b.PurchasePricePerTablet).HasColumnType("numeric(18,4)");
            e.Property(b => b.TenantId).IsRequired();

            e.Ignore(b => b.IsExpired);

            // Enforce non-negative stock at the database level
            e.ToTable(t => t.HasCheckConstraint(
                "CK_MedicineBatches_QuantityInTablets_NonNegative",
                "\"QuantityInTablets\" >= 0"));

            e.HasOne(b => b.Medicine)
                .WithMany(m => m.Batches)
                .HasForeignKey(b => b.MedicineId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(b => b.Supplier)
                .WithMany(s => s.Batches)
                .HasForeignKey(b => b.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Customer ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.CustomerId);
            e.Property(c => c.FullName).IsRequired().HasMaxLength(200);
            e.Property(c => c.PhoneNumber).IsRequired().HasMaxLength(20);
            e.Property(c => c.TenantId).IsRequired();
            e.Property(c => c.Address).HasMaxLength(500);

            // Phone number is unique within a tenant, not globally
            e.HasIndex(c => new { c.TenantId, c.PhoneNumber }).IsUnique();
        });

        // ── User (Pharmacist / App User) ──────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.UserId);
            e.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
        });

        // ── Sale ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<Sale>(e =>
        {
            e.HasKey(s => s.SaleId);
            e.Property(s => s.InvoiceNumber).IsRequired().HasMaxLength(50);
            e.Property(s => s.TenantId).IsRequired();

            // Invoice number is unique within a tenant
            e.HasIndex(s => new { s.TenantId, s.InvoiceNumber }).IsUnique();

            e.Property(s => s.Subtotal).HasColumnType("numeric(18,4)");
            e.Property(s => s.DiscountPercent).HasColumnType("numeric(5,2)");
            e.Property(s => s.DiscountAmount).HasColumnType("numeric(18,4)");
            e.Property(s => s.TotalAmount).HasColumnType("numeric(18,4)");

            e.Property(s => s.PaymentMode)
                .HasConversion<string>()
                .HasMaxLength(20);

            e.HasOne(s => s.Customer)
                .WithMany(c => c.Sales)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.Pharmacist)
                .WithMany(u => u.Sales)
                .HasForeignKey(s => s.PharmacistId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SaleItem ──────────────────────────────────────────────────────────
        modelBuilder.Entity<SaleItem>(e =>
        {
            e.HasKey(si => si.SaleItemId);
            e.Property(si => si.TenantId).IsRequired();

            e.Property(si => si.UnitPrice).HasColumnType("numeric(18,4)");
            e.Property(si => si.DiscountPercent).HasColumnType("numeric(5,2)");
            e.Property(si => si.DiscountAmount).HasColumnType("numeric(18,4)");
            e.Property(si => si.LineTotal).HasColumnType("numeric(18,4)");

            e.HasOne(si => si.Sale)
                .WithMany(s => s.SaleItems)
                .HasForeignKey(si => si.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(si => si.Medicine)
                .WithMany(m => m.SaleItems)
                .HasForeignKey(si => si.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(si => si.Batch)
                .WithMany(b => b.SaleItems)
                .HasForeignKey(si => si.BatchId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(si => si.MedicineUnit)
                .WithMany()
                .HasForeignKey(si => si.MedicineUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ExpiredMedicineDisposal ────────────────────────────────────────────
        modelBuilder.Entity<ExpiredMedicineDisposal>(e =>
        {
            e.HasKey(d => d.DisposalId);
            e.Property(d => d.DisposalNumber).IsRequired().HasMaxLength(50);
            e.Property(d => d.TenantId).IsRequired();
            e.HasIndex(d => d.DisposalNumber).IsUnique();

            e.HasOne(d => d.Pharmacist)
                .WithMany()
                .HasForeignKey(d => d.PharmacistId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ExpiredMedicineDisposalItem ───────────────────────────────────────
        modelBuilder.Entity<ExpiredMedicineDisposalItem>(e =>
        {
            e.HasKey(i => i.DisposalItemId);
            e.Property(i => i.Reason).IsRequired().HasMaxLength(500);
            e.Property(i => i.TenantId).IsRequired();

            e.HasOne(i => i.Disposal)
                .WithMany(d => d.Items)
                .HasForeignKey(i => i.DisposalId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(i => i.Medicine)
                .WithMany()
                .HasForeignKey(i => i.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.Batch)
                .WithMany()
                .HasForeignKey(i => i.BatchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PharmacyProfile (Tenant root) ─────────────────────────────────────
        modelBuilder.Entity<PharmacyProfile>(e =>
        {
            e.HasKey(p => p.PharmacyProfileId);
            e.Property(p => p.PharmacyName).IsRequired().HasMaxLength(300);
            e.Property(p => p.LogoUrl).HasMaxLength(500);
            e.Property(p => p.SignatureUrl).HasMaxLength(500);
            e.Property(p => p.PanNumber).HasMaxLength(20);
            e.Property(p => p.VatNumber).HasMaxLength(20);
            e.Property(p => p.Location).HasMaxLength(500);
            // TenantId must be unique — one PharmacyProfile per tenant
            e.HasIndex(p => p.TenantId).IsUnique();
        });

        // ── PharmacyPhoneNumber ───────────────────────────────────────────────
        modelBuilder.Entity<PharmacyPhoneNumber>(e =>
        {
            e.HasKey(p => p.PharmacyPhoneNumberId);
            e.Property(p => p.PhoneNumber).IsRequired().HasMaxLength(20);
            e.Property(p => p.Label).HasMaxLength(50);

            e.HasOne(p => p.PharmacyProfile)
                .WithMany(pp => pp.PhoneNumbers)
                .HasForeignKey(p => p.PharmacyProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── SubscriptionPlanDefinition ────────────────────────────────────────
        modelBuilder.Entity<SubscriptionPlanDefinition>(e =>
        {
            e.HasKey(s => s.SubscriptionPlanDefinitionId);
            e.Property(s => s.PlanType)
                .HasConversion<string>()
                .HasMaxLength(20);
            e.HasIndex(s => s.PlanType).IsUnique();
            e.Property(s => s.Price).HasColumnType("numeric(18,4)");
            e.Property(s => s.Description).HasMaxLength(500);
        });

        // ── Subscription ──────────────────────────────────────────────────────
        modelBuilder.Entity<Subscription>(e =>
        {
            e.HasKey(s => s.SubscriptionId);
            e.Property(s => s.TenantId).IsRequired();
            e.Property(s => s.PlanType)
                .HasConversion<string>()
                .HasMaxLength(20);
            e.Property(s => s.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
            e.Property(s => s.PaymentReference).HasMaxLength(200);

            // FK from Subscription.TenantId → PharmacyProfile.TenantId
            e.HasOne(s => s.Tenant)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.TenantId)
                .HasPrincipalKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ── SaveChanges overrides ─────────────────────────────────────────────────

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        StampNewTenantEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        StampNewTenantEntities();
        return base.SaveChanges();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Uses reflection to find every entity type in the model that implements
    /// <see cref="IHasTenant"/> and registers an EF Core global query filter that
    /// restricts queries to the current tenant's <see cref="ITenantProvider.TenantId"/>.
    /// </summary>
    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        var hasTenantType = typeof(IHasTenant);
        var method        = GetType()
            .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!hasTenantType.IsAssignableFrom(clrType)) continue;

            var generic = method.MakeGenericMethod(clrType);
            generic.Invoke(this, [modelBuilder]);
        }
    }

    /// <summary>
    /// Registers the query filter for a single <typeparamref name="TEntity"/>.
    /// Called via reflection from <see cref="ApplyTenantQueryFilters"/>.
    /// </summary>
    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IHasTenant
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e => _tenantProvider == null
                                 || _tenantProvider.TenantId == Guid.Empty
                                 || e.TenantId == _tenantProvider.TenantId);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
            if (property != null)
                property.CurrentValue = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// For newly Added entities that implement <see cref="IHasTenant"/> and have no TenantId set,
    /// stamp the current tenant's ID automatically. This ensures services don't have to set it manually.
    /// </summary>
    private void StampNewTenantEntities()
    {
        if (_tenantProvider is null || _tenantProvider.TenantId == Guid.Empty) return;

        var newEntries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added && e.Entity is IHasTenant);

        foreach (var entry in newEntries)
        {
            var entity = (IHasTenant)entry.Entity;
            if (entity.TenantId == Guid.Empty)
                entity.TenantId = _tenantProvider.TenantId;
        }
    }
}
