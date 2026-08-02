using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class BackfillStrapsPerBox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set all existing medicines where StrapsPerBox was defaulted to 0 to 1
            migrationBuilder.Sql("UPDATE \"Medicines\" SET \"StrapsPerBox\" = 1 WHERE \"StrapsPerBox\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed for this data fix
        }
    }
}
