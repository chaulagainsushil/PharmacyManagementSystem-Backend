using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PMS.Models.Models;

namespace PMS.Repository.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

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

    // ── Disposal ──────────────────────────────────────────────────────────────
    public DbSet<ExpiredMedicineDisposal> ExpiredMedicineDisposals => Set<ExpiredMedicineDisposal>();
    public DbSet<ExpiredMedicineDisposalItem> ExpiredMedicineDisposalItems => Set<ExpiredMedicineDisposalItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Manufacturer ─────────────────────────────────────────────────────
        modelBuilder.Entity<Manufacturer>(e =>
        {
            e.HasKey(m => m.ManufacturerId);
            e.Property(m => m.Name).IsRequired().HasMaxLength(200);
        });

        // ── Category ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.CategoryName).IsRequired().HasMaxLength(100);
        });

        // ── Supplier ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Supplier>(e =>
        {
            e.HasKey(s => s.SupplierId);
            e.Property(s => s.Name).IsRequired().HasMaxLength(200);
            e.Property(s => s.Phone).HasMaxLength(20);
            e.Property(s => s.Address).HasMaxLength(500);
        });

        // ── Medicine ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Medicine>(e =>
        {
            e.HasKey(m => m.MedicineId);
            e.Property(m => m.Name).IsRequired().HasMaxLength(300);
            e.Property(m => m.GenericName).HasMaxLength(300);
            e.Property(m => m.StripPrice).HasColumnType("numeric(18,4)");
            e.Property(m => m.TabletPrice).HasColumnType("numeric(18,4)");

            // Ignore computed property — not mapped to a column
            e.Ignore(m => m.TotalStockInTablets);

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

            // Ignore computed property
            e.Ignore(b => b.IsExpired);

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
            e.HasIndex(c => c.PhoneNumber).IsUnique();
            e.Property(c => c.Address).HasMaxLength(500);
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
            e.HasIndex(s => s.InvoiceNumber).IsUnique();

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

            e.Property(si => si.UnitPrice).HasColumnType("numeric(18,4)");
            e.Property(si => si.DiscountPercent).HasColumnType("numeric(5,2)");
            e.Property(si => si.DiscountAmount).HasColumnType("numeric(18,4)");
            e.Property(si => si.LineTotal).HasColumnType("numeric(18,4)");

            e.Property(si => si.SaleUnitType)
                .HasConversion<string>()
                .HasMaxLength(10);

            // Ignore computed property
            e.Ignore(si => si.TabletsDeducted);

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
        });

        // ── ExpiredMedicineDisposal ────────────────────────────────────────────
        modelBuilder.Entity<ExpiredMedicineDisposal>(e =>
        {
            e.HasKey(d => d.DisposalId);
            e.Property(d => d.DisposalNumber).IsRequired().HasMaxLength(50);
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
    }
}
