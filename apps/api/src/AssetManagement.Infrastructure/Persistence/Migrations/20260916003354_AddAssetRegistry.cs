using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AssetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "assets");

            migrationBuilder.CreateTable(
                name: "AssetCategories",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DefaultIdentificationTechnology = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FolioSequences",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NextValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolioSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InternalFolio = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PatrimonialFolio = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PhysicalCondition = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CurrentOrgUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AcquisitionCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    Supplier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Invoice = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PurchaseOrder = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WarrantyStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    WarrantyEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SupportContract = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SupportProvider = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_AssetCategories_AssetCategoryId",
                        column: x => x.AssetCategoryId,
                        principalSchema: "assets",
                        principalTable: "AssetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomFieldDefinitions",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomFieldDefinitions_AssetCategories_AssetCategoryId",
                        column: x => x.AssetCategoryId,
                        principalSchema: "assets",
                        principalTable: "AssetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetTags",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Technology = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PrintCount = table.Column<int>(type: "int", nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastPrintedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetTags_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "assets",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetCustomFieldValues",
                schema: "assets",
                columns: table => new
                {
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomFieldDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCustomFieldValues", x => new { x.AssetId, x.CustomFieldDefinitionId });
                    table.ForeignKey(
                        name: "FK_AssetCustomFieldValues_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "assets",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetCustomFieldValues_CustomFieldDefinitions_CustomFieldDefinitionId",
                        column: x => x.CustomFieldDefinitionId,
                        principalSchema: "assets",
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "assets",
                table: "AssetCategories",
                columns: new[] { "Id", "Code", "CreatedAtUtc", "DefaultIdentificationTechnology", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("33b4d8fc-7f60-42f8-5693-195084101d63"), "MONITOR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Qr", true, "Monitores" },
                    { new Guid("3956a83c-d044-8d87-dcab-998944fb9bd7"), "CHARGER", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Qr", true, "Cargadores" },
                    { new Guid("5f9206f8-e0f1-5881-0908-a12f144dbdce"), "SWITCH", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Qr", true, "Switches" },
                    { new Guid("6bd5059f-64a3-1b70-372d-26af75fe1efe"), "LAPTOP", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Qr", true, "Laptops" },
                    { new Guid("86c4ecbe-46c3-274e-9735-1a9cd405b528"), "PHONE", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Qr", true, "Celulares" },
                    { new Guid("88203046-86c5-c05a-b570-98800696b882"), "PRINTER", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Qr", true, "Impresoras" },
                    { new Guid("a0a9f650-9801-608a-1d88-4fc8c80a9f8e"), "ACCESS_POINT", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Qr", true, "Access points" },
                    { new Guid("ddd29821-9b3f-ee4a-1cbf-e40ac43c5cc2"), "SERVER", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "QrAndBarcode", true, "Servidores" },
                    { new Guid("ed93ce95-931a-deb0-14a6-074cac9837f5"), "DOCK", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Qr", true, "Docks" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetCategories_Code",
                schema: "assets",
                table: "AssetCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetCustomFieldValues_CustomFieldDefinitionId",
                schema: "assets",
                table: "AssetCustomFieldValues",
                column: "CustomFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetCategoryId",
                schema: "assets",
                table: "Assets",
                column: "AssetCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_InternalFolio",
                schema: "assets",
                table: "Assets",
                columns: new[] { "CompanyId", "InternalFolio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_SerialNumber",
                schema: "assets",
                table: "Assets",
                columns: new[] { "CompanyId", "SerialNumber" },
                unique: true,
                filter: "[SerialNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_Status",
                schema: "assets",
                table: "Assets",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetTags_AssetId",
                schema: "assets",
                table: "AssetTags",
                column: "AssetId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetTags_Code",
                schema: "assets",
                table: "AssetTags",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldDefinitions_AssetCategoryId_Code",
                schema: "assets",
                table: "CustomFieldDefinitions",
                columns: new[] { "AssetCategoryId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FolioSequences_CompanyId_DocumentType",
                schema: "organization",
                table: "FolioSequences",
                columns: new[] { "CompanyId", "DocumentType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetCustomFieldValues",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "AssetTags",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "FolioSequences",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "CustomFieldDefinitions",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "Assets",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "AssetCategories",
                schema: "assets");
        }
    }
}
