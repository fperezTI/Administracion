using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F12PrivacyRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnonymizedAtUtc",
                schema: "identity",
                table: "Users",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnonymizedAtUtc",
                schema: "identity",
                table: "Users");
        }
    }
}
