using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class BackfillUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill UpdatedAt to current UTC time for all rows that still have the sentinel default.
            // These are existing rows that were created before the AddOptimisticConcurrency migration.
            var tables = new[]
            {
                "UnitsOfMeasure",
                "Suppliers",
                "Sales",
                "SaleItems",
                "PharmacyPhoneNumbers",
                "MedicineUnits",
                "Medicines",
                "MedicineBatches",
                "Manufacturers",
                "ExpiredMedicineDisposals",
                "ExpiredMedicineDisposalItems",
                "Customers",
                "Categories",
                "AppUsers"
            };

            foreach (var table in tables)
            {
                migrationBuilder.Sql(
                    $"UPDATE \"{table}\" SET \"UpdatedAt\" = NOW() AT TIME ZONE 'UTC' WHERE \"UpdatedAt\" < '0001-01-02';"
                );
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed for a data backfill
        }
    }
}
