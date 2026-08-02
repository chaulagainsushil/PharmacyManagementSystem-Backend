using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddStrapsPerBox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StrapsPerBox",
                table: "Medicines",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Back-fill any existing rows that got the default of 0
            migrationBuilder.Sql("UPDATE \"Medicines\" SET \"StrapsPerBox\" = 1 WHERE \"StrapsPerBox\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StrapsPerBox",
                table: "Medicines");
        }
    }
}
