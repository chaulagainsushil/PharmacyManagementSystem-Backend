using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSaleSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MedicineUnitId",
                table: "SaleItems",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "MedicineId",
                table: "SaleItems",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "BatchId",
                table: "SaleItems",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                table: "SaleItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Medicine");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "SaleItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductUnitId",
                table: "SaleItems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_ProductId",
                table: "SaleItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_ProductUnitId",
                table: "SaleItems",
                column: "ProductUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_ProductUnits_ProductUnitId",
                table: "SaleItems",
                column: "ProductUnitId",
                principalTable: "ProductUnits",
                principalColumn: "ProductUnitId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_Products_ProductId",
                table: "SaleItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_ProductUnits_ProductUnitId",
                table: "SaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_Products_ProductId",
                table: "SaleItems");

            migrationBuilder.DropIndex(
                name: "IX_SaleItems_ProductId",
                table: "SaleItems");

            migrationBuilder.DropIndex(
                name: "IX_SaleItems_ProductUnitId",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "ProductUnitId",
                table: "SaleItems");

            migrationBuilder.AlterColumn<int>(
                name: "MedicineUnitId",
                table: "SaleItems",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MedicineId",
                table: "SaleItems",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BatchId",
                table: "SaleItems",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
