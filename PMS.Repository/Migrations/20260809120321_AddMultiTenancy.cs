using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PMS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnitsOfMeasure_Name",
                table: "UnitsOfMeasure");

            migrationBuilder.DropIndex(
                name: "IX_Sales_InvoiceNumber",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Customers_PhoneNumber",
                table: "Customers");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "UnitsOfMeasure",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Suppliers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Sales",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "SaleItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PharmacyProfiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "MedicineUnits",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Medicines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "MedicineBatches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Manufacturers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ExpiredMedicineDisposals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ExpiredMedicineDisposalItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Customers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Categories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PharmacyProfiles_TenantId",
                table: "PharmacyProfiles",
                column: "TenantId");

            migrationBuilder.CreateTable(
                name: "SubscriptionPlanDefinitions",
                columns: table => new
                {
                    SubscriptionPlanDefinitionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DurationInDays = table.Column<int>(type: "integer", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlanDefinitions", x => x.SubscriptionPlanDefinitionId);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    SubscriptionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaymentReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "FK_Subscriptions_PharmacyProfiles_TenantId",
                        column: x => x.TenantId,
                        principalTable: "PharmacyProfiles",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnitsOfMeasure_TenantId_Name",
                table: "UnitsOfMeasure",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_InvoiceNumber",
                table: "Sales",
                columns: new[] { "TenantId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyProfiles_TenantId",
                table: "PharmacyProfiles",
                column: "TenantId",
                unique: true);

            // Idempotent — a previous partial run may have already added this constraint
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'CK_MedicineBatches_QuantityInTablets_NonNegative'
    ) THEN
        ALTER TABLE ""MedicineBatches""
            ADD CONSTRAINT ""CK_MedicineBatches_QuantityInTablets_NonNegative""
            CHECK (""QuantityInTablets"" >= 0);
    END IF;
END $$;
");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_PhoneNumber",
                table: "Customers",
                columns: new[] { "TenantId", "PhoneNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlanDefinitions_PlanType",
                table: "SubscriptionPlanDefinitions",
                column: "PlanType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId",
                table: "Subscriptions",
                column: "TenantId");

            // ── Backfill: assign existing single pharmacy's TenantId to all rows ──
            // Runs AFTER SubscriptionPlanDefinitions and Subscriptions tables are created.
            migrationBuilder.Sql(@"
DO $$
DECLARE
    v_tenant_id UUID;
BEGIN
    SELECT gen_random_uuid() INTO v_tenant_id;

    IF EXISTS (SELECT 1 FROM ""PharmacyProfiles"" LIMIT 1) THEN
        UPDATE ""PharmacyProfiles""
        SET    ""TenantId"" = v_tenant_id
        WHERE  ""PharmacyProfileId"" = (SELECT MIN(""PharmacyProfileId"") FROM ""PharmacyProfiles"");
    ELSE
        RETURN;
    END IF;

    UPDATE ""Categories""                   SET ""TenantId"" = v_tenant_id WHERE ""TenantId"" = '00000000-0000-0000-0000-000000000000';
    UPDATE ""Manufacturers""                SET ""TenantId"" = v_tenant_id WHERE ""TenantId"" = '00000000-0000-0000-0000-000000000000';
    UPDATE ""Suppliers""                    SET ""TenantId"" = v_tenant_id WHERE ""TenantId"" = '00000000-0000-0000-0000-000000000000';
    UPDATE ""UnitsOfMeasure""               SET ""TenantId"" = v_tenant_id WHERE ""TenantId"" = '00000000-0000-0000-0000-000000000000';
    UPDATE ""Medicines""                    SET ""TenantId"" = v_tenant_id WHERE ""TenantId"" = '00000000-0000-0000-0000-000000000000';
    UPDATE ""MedicineUnits""                SET ""TenantId"" = v_tenant_id WHERE ""TenantId"" = '00000000-0000-0000-0000-000000000000';
    UPDATE ""MedicineBatches""              SET ""TenantId"" = v_tenant_id WHERE ""TenantId"" = '00000000-0000-0000-0000-000000000000';
    UPDATE ""Customers""                    SET ""TenantId"" = v_tenant_id WHERE ""TenantId"" = '00000000-0000-0000-0000-000000000000';
    UPDATE ""Sales""                        SET ""TenantId"" = v_tenant_id WHERE ""TenantId"" = '00000000-0000-0000-0000-000000000000';
    UPDATE ""SaleItems""                    SET ""TenantId"" = v_tenant_id WHERE ""TenantId"" = '00000000-0000-0000-0000-000000000000';
    UPDATE ""ExpiredMedicineDisposals""     SET ""TenantId"" = v_tenant_id WHERE ""TenantId"" = '00000000-0000-0000-0000-000000000000';
    UPDATE ""ExpiredMedicineDisposalItems"" SET ""TenantId"" = v_tenant_id WHERE ""TenantId"" = '00000000-0000-0000-0000-000000000000';

    INSERT INTO ""SubscriptionPlanDefinitions"" (""PlanType"", ""DurationInDays"", ""Price"", ""Description"")
    VALUES
        ('Silver',  180,  999.00, '6-month Silver plan'),
        ('Gold',    365, 1799.00, '1-year Gold plan'),
        ('Diamond', NULL, 4999.00, 'Lifetime Diamond plan')
    ON CONFLICT DO NOTHING;

    INSERT INTO ""Subscriptions"" (""TenantId"", ""PlanType"", ""StartDate"", ""EndDate"", ""Status"", ""PaymentReference"", ""CreatedAt"", ""UpdatedAt"")
    VALUES (
        v_tenant_id,
        'Silver',
        NOW(),
        NOW() + INTERVAL '180 days',
        'Active',
        'legacy-backfill',
        NOW(),
        NOW()
    );
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionPlanDefinitions");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UnitsOfMeasure_TenantId_Name",
                table: "UnitsOfMeasure");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_InvoiceNumber",
                table: "Sales");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PharmacyProfiles_TenantId",
                table: "PharmacyProfiles");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyProfiles_TenantId",
                table: "PharmacyProfiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MedicineBatches_QuantityInTablets_NonNegative",
                table: "MedicineBatches");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId_PhoneNumber",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "UnitsOfMeasure");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PharmacyProfiles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MedicineUnits");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MedicineBatches");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Manufacturers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ExpiredMedicineDisposals");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ExpiredMedicineDisposalItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_UnitsOfMeasure_Name",
                table: "UnitsOfMeasure",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_InvoiceNumber",
                table: "Sales",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PhoneNumber",
                table: "Customers",
                column: "PhoneNumber",
                unique: true);
        }
    }
}
