using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardsPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "identity",
                table: "Permissions",
                columns: new[] { "Id", "Action", "Description", "Module" },
                values: new object[] { new Guid("dcf1a95f-3f4f-9596-44a2-0ee949c3c69a"), "ViewExecutive", "Ver el dashboard ejecutivo", "Dashboards" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("dcf1a95f-3f4f-9596-44a2-0ee949c3c69a"));
        }
    }
}
