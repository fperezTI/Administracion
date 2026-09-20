using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F5CrossCompanyTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FromCompanyId",
                schema: "inventory",
                table: "Movements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ToCompanyId",
                schema: "inventory",
                table: "Movements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Transfers",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DepartureMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiptMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiptSignatureRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DepartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transfers_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "assets",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_AssetId",
                schema: "inventory",
                table: "Transfers",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_FromCompanyId_Status",
                schema: "inventory",
                table: "Transfers",
                columns: new[] { "FromCompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_ToCompanyId_Status",
                schema: "inventory",
                table: "Transfers",
                columns: new[] { "ToCompanyId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transfers",
                schema: "inventory");

            migrationBuilder.DropColumn(
                name: "FromCompanyId",
                schema: "inventory",
                table: "Movements");

            migrationBuilder.DropColumn(
                name: "ToCompanyId",
                schema: "inventory",
                table: "Movements");
        }
    }
}
