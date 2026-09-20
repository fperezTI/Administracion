using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F6MaintenanceAndSpareParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "spareparts");

            migrationBuilder.EnsureSchema(
                name: "maintenance");

            migrationBuilder.CreateTable(
                name: "Consumables",
                schema: "spareparts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinimumStock = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CurrentStock = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consumables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceChecklistDefinitions",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssetCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceChecklistDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceOrders",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Folio = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ChecklistDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChecklistVersionNumber = table.Column<int>(type: "int", nullable: true),
                    OpenedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResultStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ResultNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceOrders_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "assets",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpareParts",
                schema: "spareparts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PartNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CurrentAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentWarehouseOrgUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpareParts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warranties",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Terms = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warranties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warranties_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "assets",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsumableStockMovements",
                schema: "spareparts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsumableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Folio = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarehouseOrgUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReferenceMaintenanceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumableStockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumableStockMovements_Consumables_ConsumableId",
                        column: x => x.ConsumableId,
                        principalSchema: "spareparts",
                        principalTable: "Consumables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceChecklistVersions",
                schema: "maintenance",
                columns: table => new
                {
                    ChecklistDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Items = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceChecklistVersions", x => new { x.ChecklistDefinitionId, x.VersionNumber });
                    table.ForeignKey(
                        name: "FK_MaintenanceChecklistVersions_MaintenanceChecklistDefinitions_ChecklistDefinitionId",
                        column: x => x.ChecklistDefinitionId,
                        principalSchema: "maintenance",
                        principalTable: "MaintenanceChecklistDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceOrderChecklistResults",
                schema: "maintenance",
                columns: table => new
                {
                    MaintenanceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemIndex = table.Column<int>(type: "int", nullable: false),
                    ItemText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceOrderChecklistResults", x => new { x.MaintenanceOrderId, x.ItemIndex });
                    table.ForeignKey(
                        name: "FK_MaintenanceOrderChecklistResults_MaintenanceOrders_MaintenanceOrderId",
                        column: x => x.MaintenanceOrderId,
                        principalSchema: "maintenance",
                        principalTable: "MaintenanceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SparePartInstallations",
                schema: "spareparts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SparePartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InstalledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RemovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SparePartInstallations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SparePartInstallations_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "assets",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SparePartInstallations_SpareParts_SparePartId",
                        column: x => x.SparePartId,
                        principalSchema: "spareparts",
                        principalTable: "SpareParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Consumables_CompanyId_Name",
                schema: "spareparts",
                table: "Consumables",
                columns: new[] { "CompanyId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableStockMovements_CompanyId_Folio",
                schema: "spareparts",
                table: "ConsumableStockMovements",
                columns: new[] { "CompanyId", "Folio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableStockMovements_ConsumableId_WarehouseOrgUnitId_OccurredAtUtc",
                schema: "spareparts",
                table: "ConsumableStockMovements",
                columns: new[] { "ConsumableId", "WarehouseOrgUnitId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceChecklistDefinitions_Key",
                schema: "maintenance",
                table: "MaintenanceChecklistDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceOrders_AssetId",
                schema: "maintenance",
                table: "MaintenanceOrders",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceOrders_CompanyId_Folio",
                schema: "maintenance",
                table: "MaintenanceOrders",
                columns: new[] { "CompanyId", "Folio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SparePartInstallations_AssetId",
                schema: "spareparts",
                table: "SparePartInstallations",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_SparePartInstallations_SparePartId",
                schema: "spareparts",
                table: "SparePartInstallations",
                column: "SparePartId");

            migrationBuilder.CreateIndex(
                name: "IX_SpareParts_CompanyId_SerialNumber",
                schema: "spareparts",
                table: "SpareParts",
                columns: new[] { "CompanyId", "SerialNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_AssetId",
                schema: "maintenance",
                table: "Warranties",
                column: "AssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumableStockMovements",
                schema: "spareparts");

            migrationBuilder.DropTable(
                name: "MaintenanceChecklistVersions",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "MaintenanceOrderChecklistResults",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "SparePartInstallations",
                schema: "spareparts");

            migrationBuilder.DropTable(
                name: "Warranties",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "Consumables",
                schema: "spareparts");

            migrationBuilder.DropTable(
                name: "MaintenanceChecklistDefinitions",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "MaintenanceOrders",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "SpareParts",
                schema: "spareparts");
        }
    }
}
