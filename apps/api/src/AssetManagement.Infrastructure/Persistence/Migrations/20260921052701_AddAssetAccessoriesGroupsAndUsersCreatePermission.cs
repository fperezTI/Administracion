using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetAccessoriesGroupsAndUsersCreatePermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignmentGroupId",
                schema: "inventory",
                table: "Assignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AccessoryOfAssetId",
                schema: "assets",
                table: "Assets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AssignmentGroupId",
                schema: "inventory",
                table: "Assignments",
                column: "AssignmentGroupId",
                filter: "[AssignmentGroupId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AccessoryOfAssetId",
                schema: "assets",
                table: "Assets",
                column: "AccessoryOfAssetId");

            // Restrict (NO ACTION), not SetNull — SQL Server refuses SetNull on this self-referencing FK
            // ("may cause cycles or multiple cascade paths") because Assets already has other cascading
            // children (AssetTag, AssetCustomFieldValue). Assets are never hard-deleted in this app anyway
            // (decommissioned instead), so this only ever matters for application-level unlinking
            // (UnlinkAssetAccessoryCommand), never a real delete — see AssetConfiguration.cs.
            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Assets_AccessoryOfAssetId",
                schema: "assets",
                table: "Assets",
                column: "AccessoryOfAssetId",
                principalSchema: "assets",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.InsertData(
                schema: "identity",
                table: "Permissions",
                columns: new[] { "Id", "Action", "Description", "Module" },
                values: new object[] { new Guid("e79d8cef-c806-25f1-6c8a-5d665fce4efe"), "Create", "Agregar usuarios desde el directorio de Entra ID", "Users" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("e79d8cef-c806-25f1-6c8a-5d665fce4efe"));

            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Assets_AccessoryOfAssetId",
                schema: "assets",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_AssignmentGroupId",
                schema: "inventory",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assets_AccessoryOfAssetId",
                schema: "assets",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "AssignmentGroupId",
                schema: "inventory",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "AccessoryOfAssetId",
                schema: "assets",
                table: "Assets");
        }
    }
}
