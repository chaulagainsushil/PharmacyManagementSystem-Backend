using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddNonNegativeStockConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_MedicineBatches_QuantityInTablets_NonNegative",
                table: "MedicineBatches",
                sql: "\"QuantityInTablets\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MedicineBatches_QuantityInTablets_NonNegative",
                table: "MedicineBatches");
        }
    }
}
