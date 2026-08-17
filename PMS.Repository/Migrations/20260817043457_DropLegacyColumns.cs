using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TabletsPerStrip",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "TabletPrice",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "StripPrice",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "StrapsPerBox",
                table: "Medicines");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TabletsPerStrip",
                table: "Medicines",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "TabletPrice",
                table: "Medicines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StripPrice",
                table: "Medicines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StrapsPerBox",
                table: "Medicines",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }
    }
}
