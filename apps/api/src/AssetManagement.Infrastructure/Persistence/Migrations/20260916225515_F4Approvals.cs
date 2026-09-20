using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F4Approvals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "approvals");

            migrationBuilder.EnsureSchema(
                name: "templates");

            migrationBuilder.AddColumn<string>(
                name: "SignatureImageDataUrl",
                schema: "signature",
                table: "SignatureRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApprovalFlowDefinitions",
                schema: "approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApproverRoleIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredApprovals = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequiresComment = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalFlowDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalInstances",
                schema: "approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlowDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContextType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContextId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApproverRoleIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredApprovals = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalInstances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                schema: "templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalSteps",
                schema: "approvals",
                columns: table => new
                {
                    ApprovalInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApproverUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalSteps", x => new { x.ApprovalInstanceId, x.ApproverUserId });
                    table.ForeignKey(
                        name: "FK_ApprovalSteps_ApprovalInstances_ApprovalInstanceId",
                        column: x => x.ApprovalInstanceId,
                        principalSchema: "approvals",
                        principalTable: "ApprovalInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateVersions",
                schema: "templates",
                columns: table => new
                {
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateVersions", x => new { x.TemplateId, x.VersionNumber });
                    table.ForeignKey(
                        name: "FK_TemplateVersions_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "templates",
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalFlowDefinitions_Key_CompanyId_IsActive",
                schema: "approvals",
                table: "ApprovalFlowDefinitions",
                columns: new[] { "Key", "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalInstances_CompanyId_Status",
                schema: "approvals",
                table: "ApprovalInstances",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalInstances_ContextType_ContextId",
                schema: "approvals",
                table: "ApprovalInstances",
                columns: new[] { "ContextType", "ContextId" });

            migrationBuilder.CreateIndex(
                name: "IX_Templates_Key",
                schema: "templates",
                table: "Templates",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalFlowDefinitions",
                schema: "approvals");

            migrationBuilder.DropTable(
                name: "ApprovalSteps",
                schema: "approvals");

            migrationBuilder.DropTable(
                name: "TemplateVersions",
                schema: "templates");

            migrationBuilder.DropTable(
                name: "ApprovalInstances",
                schema: "approvals");

            migrationBuilder.DropTable(
                name: "Templates",
                schema: "templates");

            migrationBuilder.DropColumn(
                name: "SignatureImageDataUrl",
                schema: "signature",
                table: "SignatureRecords");
        }
    }
}
