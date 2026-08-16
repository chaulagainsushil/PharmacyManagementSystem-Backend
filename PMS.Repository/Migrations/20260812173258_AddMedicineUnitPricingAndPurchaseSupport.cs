using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicineUnitPricingAndPurchaseSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MedicineBatches_QuantityInTablets_NonNegative",
                table: "MedicineBatches");

            migrationBuilder.RenameColumn(
                name: "QuantityInTablets",
                table: "MedicineBatches",
                newName: "QuantityInBaseUnit");

            migrationBuilder.RenameColumn(
                name: "PurchasePricePerTablet",
                table: "MedicineBatches",
                newName: "CostPricePerBaseUnit");

            migrationBuilder.AddColumn<bool>(
                name: "CanPurchase",
                table: "MedicineUnits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanSell",
                table: "MedicineUnits",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "MedicineUnits",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Mrp",
                table: "MedicineUnits",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PurchaseMedicineUnitId",
                table: "MedicineBatches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PurchaseQuantityInPurchaseUnit",
                table: "MedicineBatches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicineBatches_PurchaseMedicineUnitId",
                table: "MedicineBatches",
                column: "PurchaseMedicineUnitId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MedicineBatches_QuantityInBaseUnit_NonNegative",
                table: "MedicineBatches",
                sql: "\"QuantityInBaseUnit\" >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicineBatches_MedicineUnits_PurchaseMedicineUnitId",
                table: "MedicineBatches",
                column: "PurchaseMedicineUnitId",
                principalTable: "MedicineUnits",
                principalColumn: "MedicineUnitId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicineBatches_MedicineUnits_PurchaseMedicineUnitId",
                table: "MedicineBatches");

            migrationBuilder.DropIndex(
                name: "IX_MedicineBatches_PurchaseMedicineUnitId",
                table: "MedicineBatches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MedicineBatches_QuantityInBaseUnit_NonNegative",
                table: "MedicineBatches");

            migrationBuilder.DropColumn(
                name: "CanPurchase",
                table: "MedicineUnits");

            migrationBuilder.DropColumn(
                name: "CanSell",
                table: "MedicineUnits");

            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "MedicineUnits");

            migrationBuilder.DropColumn(
                name: "Mrp",
                table: "MedicineUnits");

            migrationBuilder.DropColumn(
                name: "PurchaseMedicineUnitId",
                table: "MedicineBatches");

            migrationBuilder.DropColumn(
                name: "PurchaseQuantityInPurchaseUnit",
                table: "MedicineBatches");

            migrationBuilder.RenameColumn(
                name: "QuantityInBaseUnit",
                table: "MedicineBatches",
                newName: "QuantityInTablets");

            migrationBuilder.RenameColumn(
                name: "CostPricePerBaseUnit",
                table: "MedicineBatches",
                newName: "PurchasePricePerTablet");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MedicineBatches_QuantityInTablets_NonNegative",
                table: "MedicineBatches",
                sql: "\"QuantityInTablets\" >= 0");
        }
    }
}
