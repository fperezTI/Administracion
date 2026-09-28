using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetInsuranceAndMaintenanceSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "InsuranceExpiryDate",
                schema: "assets",
                table: "Assets",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsurancePolicyNumber",
                schema: "assets",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceProvider",
                schema: "assets",
                table: "Assets",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NextMaintenanceDueDate",
                schema: "assets",
                table: "Assets",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InsuranceExpiryDate",
                schema: "assets",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "InsurancePolicyNumber",
                schema: "assets",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "InsuranceProvider",
                schema: "assets",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "NextMaintenanceDueDate",
                schema: "assets",
                table: "Assets");
        }
    }
}
