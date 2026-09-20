using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F9ImportsExports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "importexport");

            migrationBuilder.CreateTable(
                name: "ImportBatches",
                schema: "importexport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    BlobPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CommitMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TotalRows = table.Column<int>(type: "int", nullable: true),
                    ValidRows = table.Column<int>(type: "int", nullable: true),
                    InvalidRows = table.Column<int>(type: "int", nullable: true),
                    SucceededRows = table.Column<int>(type: "int", nullable: true),
                    FailedRows = table.Column<int>(type: "int", nullable: true),
                    ReportJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_CompanyId_CreatedAtUtc",
                schema: "importexport",
                table: "ImportBatches",
                columns: new[] { "CompanyId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportBatches",
                schema: "importexport");
        }
    }
}
