using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F3InventoryOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.EnsureSchema(
                name: "signature");

            migrationBuilder.CreateTable(
                name: "Movements",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FolioNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FromOrgUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToOrgUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FromUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EffectiveAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Movements_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "assets",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SignatureRecords",
                schema: "signature",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContextType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContextId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignerDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Mechanism = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignatureRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assignments",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SignatureRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AcceptedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReturnedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReturnMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReturnSignatureRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assignments_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "assets",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_Movements_MovementId",
                        column: x => x.MovementId,
                        principalSchema: "inventory",
                        principalTable: "Movements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Loans",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BorrowerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LoanedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReturnedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReturnMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Loans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Loans_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "assets",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loans_Movements_MovementId",
                        column: x => x.MovementId,
                        principalSchema: "inventory",
                        principalTable: "Movements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AssetId",
                schema: "inventory",
                table: "Assignments",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_CompanyId_AssetId",
                schema: "inventory",
                table: "Assignments",
                columns: new[] { "CompanyId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_CompanyId_AssignedToUserId",
                schema: "inventory",
                table: "Assignments",
                columns: new[] { "CompanyId", "AssignedToUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_MovementId",
                schema: "inventory",
                table: "Assignments",
                column: "MovementId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_AssetId",
                schema: "inventory",
                table: "Loans",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_CompanyId_AssetId",
                schema: "inventory",
                table: "Loans",
                columns: new[] { "CompanyId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_CompanyId_BorrowerUserId",
                schema: "inventory",
                table: "Loans",
                columns: new[] { "CompanyId", "BorrowerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_MovementId",
                schema: "inventory",
                table: "Loans",
                column: "MovementId");

            migrationBuilder.CreateIndex(
                name: "IX_Movements_AssetId",
                schema: "inventory",
                table: "Movements",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Movements_CompanyId_AssetId_EffectiveAtUtc",
                schema: "inventory",
                table: "Movements",
                columns: new[] { "CompanyId", "AssetId", "EffectiveAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Movements_CompanyId_FolioNumber",
                schema: "inventory",
                table: "Movements",
                columns: new[] { "CompanyId", "FolioNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignatureRecords_ContextType_ContextId",
                schema: "signature",
                table: "SignatureRecords",
                columns: new[] { "ContextType", "ContextId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assignments",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "Loans",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "SignatureRecords",
                schema: "signature");

            migrationBuilder.DropTable(
                name: "Movements",
                schema: "inventory");
        }
    }
}
