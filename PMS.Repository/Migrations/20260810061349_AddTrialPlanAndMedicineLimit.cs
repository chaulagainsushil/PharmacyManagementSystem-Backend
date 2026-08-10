using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddTrialPlanAndMedicineLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MedicineLimit",
                table: "SubscriptionPlanDefinitions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MedicineLimit",
                table: "SubscriptionPlanDefinitions");
        }
    }
}
