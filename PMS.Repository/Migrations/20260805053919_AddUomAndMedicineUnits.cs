using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PMS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddUomAndMedicineUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. New columns on SaleItems (nullable first so existing rows don't break) ──
            migrationBuilder.AddColumn<int>(
                name: "BaseQuantityDeducted",
                table: "SaleItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MedicineUnitId",
                table: "SaleItems",
                type: "integer",
                nullable: true,   // temporarily nullable so we can backfill
                defaultValue: null);

            // ── 2. Create UnitsOfMeasure master table ──────────────────────────
            migrationBuilder.CreateTable(
                name: "UnitsOfMeasure",
                columns: table => new
                {
                    UnitOfMeasureId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name     = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Symbol   = table.Column<string>(type: "character varying(20)",  maxLength: 20,  nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitsOfMeasure", x => x.UnitOfMeasureId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnitsOfMeasure_Name",
                table: "UnitsOfMeasure",
                column: "Name",
                unique: true);

            // ── 3. Seed UoM master rows (fixed IDs so the data-migration below can reference them) ──
            // Tablet = 1, Strip = 2, Bottle = 3, Sachet = 4, Packet = 5, Piece = 6, Box = 7
            migrationBuilder.Sql(@"
                INSERT INTO ""UnitsOfMeasure"" (""UnitOfMeasureId"", ""Name"", ""Symbol"", ""IsActive"")
                OVERRIDING SYSTEM VALUE
                VALUES
                  (1, 'Tablet',  'tab',    true),
                  (2, 'Strip',   'strip',  true),
                  (3, 'Bottle',  'btl',    true),
                  (4, 'Sachet',  'sac',    true),
                  (5, 'Packet',  'pkt',    true),
                  (6, 'Piece',   'pc',     true),
                  (7, 'Box',     'box',    true);

                -- Advance the identity sequence past the seeded IDs
                SELECT setval(
                    pg_get_serial_sequence('""UnitsOfMeasure""', 'UnitOfMeasureId'),
                    (SELECT MAX(""UnitOfMeasureId"") FROM ""UnitsOfMeasure"")
                );
            ");

            // ── 4. Create MedicineUnits join table ─────────────────────────────
            migrationBuilder.CreateTable(
                name: "MedicineUnits",
                columns: table => new
                {
                    MedicineUnitId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MedicineId              = table.Column<int>(type: "integer", nullable: false),
                    UnitOfMeasureId         = table.Column<int>(type: "integer", nullable: false),
                    ConversionFactorToBase  = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    IsBaseUnit              = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault               = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive                = table.Column<bool>(type: "boolean", nullable: false),
                    UnitPrice               = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineUnits", x => x.MedicineUnitId);
                    table.ForeignKey(
                        name: "FK_MedicineUnits_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "MedicineId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicineUnits_UnitsOfMeasure_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalTable: "UnitsOfMeasure",
                        principalColumn: "UnitOfMeasureId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicineUnits_MedicineId_UnitOfMeasureId",
                table: "MedicineUnits",
                columns: new[] { "MedicineId", "UnitOfMeasureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicineUnits_MedicineId_IsBaseUnit_Unique",
                table: "MedicineUnits",
                columns: new[] { "MedicineId", "IsBaseUnit" },
                unique: true,
                filter: "\"IsBaseUnit\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineUnits_MedicineId_IsDefault_Unique",
                table: "MedicineUnits",
                columns: new[] { "MedicineId", "IsDefault" },
                unique: true,
                filter: "\"IsDefault\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineUnits_UnitOfMeasureId",
                table: "MedicineUnits",
                column: "UnitOfMeasureId");

            // ── 5. Data migration: create MedicineUnit rows for every existing Medicine ──
            //
            // Strategy:
            //   • Tablet unit (UomId=1): IsBaseUnit=true, IsDefault=true,  ConversionFactor=1,
            //                            UnitPrice = medicine.TabletPrice
            //   • Strip  unit (UomId=2): IsBaseUnit=false, IsDefault=false, ConversionFactor=TabletsPerStrip,
            //                            UnitPrice = medicine.StripPrice
            //     (only inserted when TabletsPerStrip > 1 so single-tablet medicines don't get a Strip unit)
            //
            migrationBuilder.Sql(@"
                -- Tablet unit (base + default) for every medicine
                INSERT INTO ""MedicineUnits""
                    (""MedicineId"", ""UnitOfMeasureId"", ""ConversionFactorToBase"",
                     ""IsBaseUnit"", ""IsDefault"", ""IsActive"", ""UnitPrice"")
                SELECT
                    m.""MedicineId"",
                    1,           -- Tablet UoM
                    1.0,         -- 1 tablet = 1 base unit
                    true,        -- IsBaseUnit
                    true,        -- IsDefault
                    true,        -- IsActive
                    m.""TabletPrice""
                FROM ""Medicines"" m;

                -- Strip unit for medicines that have more than 1 tablet per strip
                INSERT INTO ""MedicineUnits""
                    (""MedicineId"", ""UnitOfMeasureId"", ""ConversionFactorToBase"",
                     ""IsBaseUnit"", ""IsDefault"", ""IsActive"", ""UnitPrice"")
                SELECT
                    m.""MedicineId"",
                    2,                          -- Strip UoM
                    m.""TabletsPerStrip"",      -- 1 strip = N tablets
                    false,                      -- IsBaseUnit
                    false,                      -- IsDefault
                    true,                       -- IsActive
                    m.""StripPrice""
                FROM ""Medicines"" m
                WHERE m.""TabletsPerStrip"" > 1;
            ");

            // ── 6. Backfill SaleItems.MedicineUnitId using the legacy SaleUnitType column ──
            //
            // For old Strip sales  → point at the Strip MedicineUnit for that medicine (if it exists),
            //                         else fall back to the Tablet unit.
            // For old Tablet sales → point at the Tablet MedicineUnit.
            // Also back-fill BaseQuantityDeducted from the stored Quantity + conversion factor.
            //
            migrationBuilder.Sql(@"
                -- Tablet sales: link to the Tablet MedicineUnit
                UPDATE ""SaleItems"" si
                SET
                    ""MedicineUnitId"" = mu.""MedicineUnitId"",
                    ""BaseQuantityDeducted"" = si.""Quantity""
                FROM ""MedicineUnits"" mu
                WHERE mu.""MedicineId""      = si.""MedicineId""
                  AND mu.""UnitOfMeasureId"" = 1   -- Tablet
                  AND si.""SaleUnitType""    = 'Tablet';

                -- Strip sales: link to the Strip MedicineUnit (conversion already in Quantity×TabletsPerStrip)
                UPDATE ""SaleItems"" si
                SET
                    ""MedicineUnitId"" = mu.""MedicineUnitId"",
                    ""BaseQuantityDeducted"" = CAST(
                        si.""Quantity"" * mu.""ConversionFactorToBase"" AS integer
                    )
                FROM ""MedicineUnits"" mu
                JOIN ""Medicines"" m ON m.""MedicineId"" = si.""MedicineId""
                WHERE mu.""MedicineId""      = si.""MedicineId""
                  AND mu.""UnitOfMeasureId"" = 2   -- Strip
                  AND si.""SaleUnitType""    = 'Strip';

                -- Safety fallback: any row still NULL gets the Tablet unit
                UPDATE ""SaleItems"" si
                SET
                    ""MedicineUnitId"" = mu.""MedicineUnitId"",
                    ""BaseQuantityDeducted"" = COALESCE(si.""BaseQuantityDeducted"", si.""Quantity"")
                FROM ""MedicineUnits"" mu
                WHERE mu.""MedicineId""      = si.""MedicineId""
                  AND mu.""UnitOfMeasureId"" = 1   -- Tablet
                  AND si.""MedicineUnitId""  IS NULL;
            ");

            // ── 7. Now make MedicineUnitId NOT NULL and drop the old SaleUnitType column ──
            migrationBuilder.AlterColumn<int>(
                name: "MedicineUnitId",
                table: "SaleItems",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "SaleUnitType",
                table: "SaleItems");

            // ── 8. Add FK from SaleItems → MedicineUnits ──────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_MedicineUnitId",
                table: "SaleItems",
                column: "MedicineUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_MedicineUnits_MedicineUnitId",
                table: "SaleItems",
                column: "MedicineUnitId",
                principalTable: "MedicineUnits",
                principalColumn: "MedicineUnitId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_MedicineUnits_MedicineUnitId",
                table: "SaleItems");

            migrationBuilder.DropIndex(
                name: "IX_SaleItems_MedicineUnitId",
                table: "SaleItems");

            migrationBuilder.DropTable(
                name: "MedicineUnits");

            migrationBuilder.DropTable(
                name: "UnitsOfMeasure");

            migrationBuilder.DropColumn(
                name: "BaseQuantityDeducted",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "MedicineUnitId",
                table: "SaleItems");

            migrationBuilder.AddColumn<string>(
                name: "SaleUnitType",
                table: "SaleItems",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Tablet");
        }
    }
}
