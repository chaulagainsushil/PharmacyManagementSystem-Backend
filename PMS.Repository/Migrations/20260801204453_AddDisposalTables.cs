using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PMS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddDisposalTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpiredMedicineDisposals",
                columns: table => new
                {
                    DisposalId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DisposalNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PharmacistId = table.Column<int>(type: "integer", nullable: false),
                    DisposalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpiredMedicineDisposals", x => x.DisposalId);
                    table.ForeignKey(
                        name: "FK_ExpiredMedicineDisposals_AppUsers_PharmacistId",
                        column: x => x.PharmacistId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpiredMedicineDisposals_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ExpiredMedicineDisposalItems",
                columns: table => new
                {
                    DisposalItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DisposalId = table.Column<int>(type: "integer", nullable: false),
                    MedicineId = table.Column<int>(type: "integer", nullable: false),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    QuantityInTablets = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpiredMedicineDisposalItems", x => x.DisposalItemId);
                    table.ForeignKey(
                        name: "FK_ExpiredMedicineDisposalItems_ExpiredMedicineDisposals_Dispo~",
                        column: x => x.DisposalId,
                        principalTable: "ExpiredMedicineDisposals",
                        principalColumn: "DisposalId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExpiredMedicineDisposalItems_MedicineBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "MedicineBatches",
                        principalColumn: "BatchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpiredMedicineDisposalItems_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "MedicineId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpiredMedicineDisposalItems_BatchId",
                table: "ExpiredMedicineDisposalItems",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpiredMedicineDisposalItems_DisposalId",
                table: "ExpiredMedicineDisposalItems",
                column: "DisposalId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpiredMedicineDisposalItems_MedicineId",
                table: "ExpiredMedicineDisposalItems",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpiredMedicineDisposals_DisposalNumber",
                table: "ExpiredMedicineDisposals",
                column: "DisposalNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpiredMedicineDisposals_PharmacistId",
                table: "ExpiredMedicineDisposals",
                column: "PharmacistId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpiredMedicineDisposals_UserId",
                table: "ExpiredMedicineDisposals",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpiredMedicineDisposalItems");

            migrationBuilder.DropTable(
                name: "ExpiredMedicineDisposals");
        }
    }
}
