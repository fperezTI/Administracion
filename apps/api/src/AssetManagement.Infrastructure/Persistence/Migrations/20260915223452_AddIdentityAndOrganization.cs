using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AssetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityAndOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "organization");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "Companies",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TradeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BaseCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrgUnitTypes",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgUnitTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntraObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastLoginAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrgUnits",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgUnitTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentOrgUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrgUnits_OrgUnitTypes_OrgUnitTypeId",
                        column: x => x.OrgUnitTypeId,
                        principalSchema: "organization",
                        principalTable: "OrgUnitTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrgUnits_OrgUnits_ParentOrgUnitId",
                        column: x => x.ParentOrgUnitId,
                        principalSchema: "organization",
                        principalTable: "OrgUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "identity",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "identity",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCompanies",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompanies", x => new { x.UserId, x.CompanyId });
                    table.ForeignKey(
                        name: "FK_UserCompanies_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "organization",
                table: "OrgUnitTypes",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("128bf7a4-eaee-c125-4264-41a096400c5d"), "Management", true, "Gerencia" },
                    { new Guid("2a8c40c9-27ba-15e0-84e4-ce2b3058a07b"), "BusinessUnit", true, "Unidad de negocio" },
                    { new Guid("5becbc5a-1874-fe4b-343f-7664304fcd0e"), "DataCenter", true, "Centro de datos" },
                    { new Guid("851df337-7b61-5a0d-c862-7f4d945c0da5"), "Warehouse", true, "Almacén" },
                    { new Guid("879fb8c5-3bed-5ae1-0148-44c616d61c3b"), "Branch", true, "Sucursal" },
                    { new Guid("97350561-7bb1-02e8-7f7d-9a13c1d769f7"), "Team", true, "Equipo" },
                    { new Guid("ac8bc210-f0f6-8e26-81b2-7d66d23365f9"), "Direction", true, "Dirección" },
                    { new Guid("c7376946-92c6-23ba-690e-165ea161977e"), "Project", true, "Proyecto" },
                    { new Guid("d8ac22ee-3164-46dc-42db-b7e2251113f3"), "Location", true, "Ubicación física" },
                    { new Guid("ee2babf1-dc51-68c8-3a7d-c5e2cdf92f96"), "Department", true, "Departamento" },
                    { new Guid("f39ccc0b-dd33-bfeb-2c55-34ff08570cb0"), "Area", true, "Área" }
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "Permissions",
                columns: new[] { "Id", "Action", "Description", "Module" },
                values: new object[,]
                {
                    { new Guid("0857a031-1328-02fd-cdd2-ede1edec414b"), "Create", "Crear transferencias", "Transfers" },
                    { new Guid("0b235011-5ebf-5a6a-039b-e20dd438d3ea"), "Update", "Editar el perfil de un usuario", "Users" },
                    { new Guid("133339b6-9ddf-ed9e-379f-a1944a5a4335"), "Create", "Crear unidades organizacionales", "Structure" },
                    { new Guid("1df2c2f0-ee0e-4444-93de-668ed2227c53"), "Update", "Editar configuración del sistema", "Configuration" },
                    { new Guid("213949d4-f099-2fc0-2d4d-7c36b0e52202"), "Read", "Consultar transferencias", "Transfers" },
                    { new Guid("2320c50e-22d6-4707-7a4a-015dbbf5850e"), "Read", "Consultar devoluciones", "Returns" },
                    { new Guid("268680ee-9be4-a63b-28ed-d2c3d6efdcad"), "Read", "Consultar solicitudes", "Requests" },
                    { new Guid("270ef0a3-09f6-ac45-8785-2e71371ca7dc"), "Update", "Editar refacciones", "SpareParts" },
                    { new Guid("27c19625-05a9-ed04-e019-b3aeaeecf0de"), "Update", "Editar préstamos", "Loans" },
                    { new Guid("2a7474be-e80c-c46f-f853-9b8e966b3e7d"), "Reject", "Rechazar solicitudes", "Approvals" },
                    { new Guid("2dabb61a-3ae8-e7ce-2339-2f8a93c57166"), "Create", "Crear mantenimientos", "Maintenance" },
                    { new Guid("2fc809da-48b5-beb0-db76-a516975167ac"), "Create", "Crear roles", "Roles" },
                    { new Guid("30078032-4b27-50da-0761-d9aa615395b3"), "Create", "Crear solicitudes", "Requests" },
                    { new Guid("30130c59-70e4-7722-0d12-b20ff25b183e"), "Configure", "Configurar flujos de aprobación", "Approvals" },
                    { new Guid("3365539c-176c-9ebd-50e0-a5fcd957ed81"), "Create", "Registrar garantías", "Warranties" },
                    { new Guid("36823748-f420-0165-40b3-e3e9e03e7efc"), "Create", "Crear asignaciones", "Assignments" },
                    { new Guid("378030f9-f874-5516-c964-f51ec5510527"), "Create", "Dar de alta activos", "Assets" },
                    { new Guid("38045db3-c437-2c0d-9b9f-ef5c4754d976"), "Update", "Editar y publicar plantillas", "Templates" },
                    { new Guid("3be03333-5582-c4c2-49ef-412e3ff8a424"), "Update", "Editar entradas de catálogo", "Catalogs" },
                    { new Guid("402e4fc0-8838-b4f0-e420-adb4e3d8e15e"), "ManageCompanies", "Otorgar y revocar acceso de usuarios a empresas", "Users" },
                    { new Guid("40b4fffd-9165-183c-c6bb-59bc8ae3028e"), "Read", "Consultar movimientos", "Movements" },
                    { new Guid("463e11ac-d988-e061-b8d1-e2af274d1408"), "Read", "Consultar reportes por empresa", "Reports" },
                    { new Guid("51efca44-35f2-1107-93bc-f98c348c95ab"), "Update", "Editar y activar/desactivar empresas", "Companies" },
                    { new Guid("52d4b4b1-15c6-1e8c-598c-ad0d8df168cd"), "Create", "Cargar documentos", "Documents" },
                    { new Guid("540d31ee-7573-cb93-c53d-eecb906d879f"), "Create", "Registrar devoluciones", "Returns" },
                    { new Guid("5773bc8f-342d-11cb-d06f-cae7c2ff8955"), "Create", "Crear plantillas", "Templates" },
                    { new Guid("59f420f8-c138-7c15-75ef-16a829e7178f"), "Read", "Consultar asignaciones", "Assignments" },
                    { new Guid("5b88cd52-8151-a321-9948-a0b6605199a8"), "Read", "Consultar activos", "Assets" },
                    { new Guid("5ed43a61-1131-35f9-418e-5d734885f041"), "Create", "Registrar préstamos", "Loans" },
                    { new Guid("62d5f94f-ce24-94a7-58fd-bb725f1c86ed"), "Read", "Consultar garantías", "Warranties" },
                    { new Guid("634dffa1-d1c3-6f18-dec1-c8f7799f0d1d"), "Read", "Consultar préstamos", "Loans" },
                    { new Guid("65ef226f-e2db-3a12-0477-ab17f129d3e8"), "Decommission", "Dar de baja activos", "Assets" },
                    { new Guid("70750688-5a74-757a-9cd8-44bda7b994a5"), "Read", "Consultar el registro de auditoría", "Audit" },
                    { new Guid("724181e6-2f47-16c3-ad02-0a49875b2709"), "Read", "Consultar lotes de importación", "Imports" },
                    { new Guid("72812ef6-fd11-a91e-eacd-e45767c1bf52"), "Update", "Editar consumibles", "Consumables" },
                    { new Guid("76da9d34-92ae-05b8-d138-be710fadc141"), "Update", "Editar activos", "Assets" },
                    { new Guid("795066b7-6317-d5c6-f5af-d2bb4983d9e1"), "Read", "Consultar consumibles", "Consumables" },
                    { new Guid("7a5d5652-a1ac-d274-95b2-44442db58ad8"), "Read", "Consultar empresas", "Companies" },
                    { new Guid("7b9a501b-aca1-782e-2453-4069f9fe6b8e"), "Update", "Editar transferencias", "Transfers" },
                    { new Guid("890418b1-6d85-ef64-7c23-0becce314341"), "Read", "Consultar roles", "Roles" },
                    { new Guid("8a224771-1e2a-23ee-59f6-5bdd1c8571dd"), "Update", "Editar asignaciones", "Assignments" },
                    { new Guid("8b2f8328-2079-f60b-d7d6-95cdd03d56b0"), "Update", "Editar solicitudes", "Requests" },
                    { new Guid("96e2517c-3d27-8a06-7ee1-f62f1a7df249"), "Update", "Editar garantías", "Warranties" },
                    { new Guid("994cea42-d44d-409a-ddb1-757489a9eade"), "ManageRoles", "Asignar y quitar roles a usuarios", "Users" },
                    { new Guid("9abf5b7b-1ba0-e2d1-c8b6-056606a5de5d"), "Read", "Consultar plantillas", "Templates" },
                    { new Guid("9ac2f6ca-bc4c-715c-c77d-cf0c0db277ed"), "Create", "Registrar consumibles", "Consumables" },
                    { new Guid("9b30f027-273b-d2d8-264d-c800598aa857"), "Export", "Exportar reportes", "Reports" },
                    { new Guid("9d3a8093-632b-8b3f-3601-d0b0a51327dd"), "Update", "Editar, mover y activar/desactivar unidades organizacionales", "Structure" },
                    { new Guid("9ef45691-7abd-42c2-3dda-5ffb8bc7eda6"), "Read", "Consultar catálogos", "Catalogs" },
                    { new Guid("b3645805-12fc-7766-134b-4385b7d86956"), "Read", "Consultar el catálogo de permisos", "Permissions" },
                    { new Guid("bb101cae-8dbb-7955-aa73-6ce19633d581"), "Create", "Iniciar importaciones masivas", "Imports" },
                    { new Guid("be9ac938-85f3-2cd7-2541-82e0161b6287"), "Duplicate", "Duplicar roles", "Roles" },
                    { new Guid("c4216206-ff04-228e-79a4-bae418708795"), "Read", "Consultar configuración del sistema", "Configuration" },
                    { new Guid("c63e9245-979e-0353-3f7f-ea5795c10cde"), "Read", "Consultar usuarios", "Users" },
                    { new Guid("c6acddef-defc-66bd-f092-2f4961c55029"), "Create", "Registrar refacciones", "SpareParts" },
                    { new Guid("c6ad17a7-b959-17e1-884b-ce8c45227908"), "Read", "Consultar mantenimientos", "Maintenance" },
                    { new Guid("d046fbc0-f442-19b7-74ca-adbeaf30c6c8"), "Read", "Consultar documentos", "Documents" },
                    { new Guid("d5ee8009-1ae1-1443-53dd-89f6ad932040"), "Read", "Consultar refacciones", "SpareParts" },
                    { new Guid("d8235c70-53e7-80a3-24cf-bd2bb89d0b04"), "Create", "Crear entradas de catálogo", "Catalogs" },
                    { new Guid("d84f3d31-95fb-a3ae-7fa3-b82d08357951"), "Approve", "Aprobar o rechazar solicitudes", "Approvals" },
                    { new Guid("e29fbec3-e214-f53c-8ca0-a2f1b8f7ca89"), "Create", "Crear empresas", "Companies" },
                    { new Guid("e3237316-9a5c-30f2-e041-f06a00de5f8b"), "Create", "Generar exportaciones", "Exports" },
                    { new Guid("e3f62e59-b4b6-58e7-3bd0-426ea1ec4780"), "Update", "Editar y activar/desactivar roles", "Roles" },
                    { new Guid("ed65ea32-ae1f-14e3-ff0b-2a75bfd0a393"), "ReadConsolidated", "Consultar reportes consolidados entre empresas", "Reports" },
                    { new Guid("f09c265e-84ff-7299-fb9c-be48c4a0a743"), "Update", "Editar y cerrar mantenimientos", "Maintenance" },
                    { new Guid("f57b7bb7-caa8-d044-73b6-43d352e308a8"), "Manage", "Administrar la matriz de permisos por rol", "Permissions" },
                    { new Guid("f5af1a2f-635d-0f01-7b27-dc51f0c95192"), "Read", "Consultar la estructura organizacional", "Structure" },
                    { new Guid("fa13fc8c-2340-d2f5-fa21-7f64575be3f9"), "Read", "Consultar flujos y solicitudes de aprobación", "Approvals" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TaxId",
                schema: "organization",
                table: "Companies",
                column: "TaxId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnits_CompanyId_Code",
                schema: "organization",
                table: "OrgUnits",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnits_CompanyId_IsActive",
                schema: "organization",
                table: "OrgUnits",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnits_OrgUnitTypeId",
                schema: "organization",
                table: "OrgUnits",
                column: "OrgUnitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnits_ParentOrgUnitId",
                schema: "organization",
                table: "OrgUnits",
                column: "ParentOrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnitTypes_Code",
                schema: "organization",
                table: "OrgUnitTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Module_Action",
                schema: "identity",
                table: "Permissions",
                columns: new[] { "Module", "Action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "identity",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                schema: "identity",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "identity",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "identity",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EntraObjectId",
                schema: "identity",
                table: "Users",
                column: "EntraObjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Companies",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "OrgUnits",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserCompanies",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "OrgUnitTypes",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "identity");
        }
    }
}
