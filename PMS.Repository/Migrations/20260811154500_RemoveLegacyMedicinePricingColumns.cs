using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyMedicinePricingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Safety guard ───────────────────────────────────────────────────
            // Every medicine must have at least one MedicineUnit row before we
            // drop the legacy columns.  If any medicine is missing a unit row the
            // INSERT below will RAISE an exception and the migration will roll back.
            //
            // If this migration fails here, run:
            //   Scripts/backfill_medicine_units_before_drop_legacy_columns.sql
            // then apply the migration again.
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    orphan_count integer;
                BEGIN
                    SELECT COUNT(*) INTO orphan_count
                    FROM ""Medicines"" m
                    WHERE NOT EXISTS (
                        SELECT 1 FROM ""MedicineUnits"" mu
                        WHERE mu.""MedicineId"" = m.""MedicineId""
                    );

                    IF orphan_count > 0 THEN
                        RAISE EXCEPTION
                            'Migration aborted: % medicine(s) have no MedicineUnit rows. '
                            'Run Scripts/backfill_medicine_units_before_drop_legacy_columns.sql first.',
                            orphan_count;
                    END IF;
                END $$;
            ");

            // ── Drop the four legacy columns ───────────────────────────────────
            migrationBuilder.DropColumn(
                name: "StripPrice",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "TabletPrice",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "TabletsPerStrip",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "StrapsPerBox",
                table: "Medicines");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the columns with their original types and safe defaults.
            // Data cannot be recovered from Down() — this is intentional.
            // If you need the data back, restore from a backup taken before Up() ran.

            migrationBuilder.AddColumn<decimal>(
                name: "StripPrice",
                table: "Medicines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TabletPrice",
                table: "Medicines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TabletsPerStrip",
                table: "Medicines",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "StrapsPerBox",
                table: "Medicines",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }
    }
}
