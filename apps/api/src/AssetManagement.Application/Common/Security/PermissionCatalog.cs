namespace AssetManagement.Application.Common.Security;

/// <summary>
/// The full permission catalog required by pedido §6 ("al menos" 24 modules), seeded once via
/// migration (see Infrastructure/Persistence/Seed) and never created through the API in V1. Only the
/// modules built so far (Companies, Structure, Roles, Permissions, Users) are actually enforced by any
/// handler today; the rest exist so later phases only need to add enforcement, not create permissions
/// retroactively. Codes are "{Module}.{Action}", matching Permission.Code.
/// </summary>
public static class PermissionCatalog
{
    public static class Companies
    {
        public const string Read = "Companies.Read";
        public const string Create = "Companies.Create";
        public const string Update = "Companies.Update";
    }

    public static class Structure
    {
        public const string Read = "Structure.Read";
        public const string Create = "Structure.Create";
        public const string Update = "Structure.Update";
    }

    public static class Roles
    {
        public const string Read = "Roles.Read";
        public const string Create = "Roles.Create";
        public const string Update = "Roles.Update";
        public const string Duplicate = "Roles.Duplicate";
    }

    public static class Permissions
    {
        public const string Read = "Permissions.Read";
        public const string Manage = "Permissions.Manage";
    }

    public static class Users
    {
        public const string Read = "Users.Read";
        public const string Update = "Users.Update";
        public const string ManageRoles = "Users.ManageRoles";
        public const string ManageCompanies = "Users.ManageCompanies";
    }

    public static class Assets
    {
        public const string Read = "Assets.Read";
        public const string Create = "Assets.Create";
        public const string Update = "Assets.Update";
        public const string Decommission = "Assets.Decommission";
    }

    public static class Assignments
    {
        public const string Read = "Assignments.Read";
        public const string Create = "Assignments.Create";
        public const string Update = "Assignments.Update";
    }

    public static class Returns
    {
        public const string Read = "Returns.Read";
        public const string Create = "Returns.Create";
    }

    public static class Loans
    {
        public const string Read = "Loans.Read";
        public const string Create = "Loans.Create";
        public const string Update = "Loans.Update";
    }

    public static class Transfers
    {
        public const string Read = "Transfers.Read";
        public const string Create = "Transfers.Create";
        public const string Update = "Transfers.Update";
    }

    public static class Movements
    {
        public const string Read = "Movements.Read";
    }

    public static class Requests
    {
        public const string Read = "Requests.Read";
        public const string Create = "Requests.Create";
        public const string Update = "Requests.Update";
    }

    public static class Maintenance
    {
        public const string Read = "Maintenance.Read";
        public const string Create = "Maintenance.Create";
        public const string Update = "Maintenance.Update";
    }

    public static class Warranties
    {
        public const string Read = "Warranties.Read";
        public const string Create = "Warranties.Create";
        public const string Update = "Warranties.Update";
    }

    public static class SpareParts
    {
        public const string Read = "SpareParts.Read";
        public const string Create = "SpareParts.Create";
        public const string Update = "SpareParts.Update";
    }

    public static class Consumables
    {
        public const string Read = "Consumables.Read";
        public const string Create = "Consumables.Create";
        public const string Update = "Consumables.Update";
    }

    public static class Documents
    {
        public const string Read = "Documents.Read";
        public const string Create = "Documents.Create";
    }

    public static class Templates
    {
        public const string Read = "Templates.Read";
        public const string Create = "Templates.Create";
        public const string Update = "Templates.Update";
    }

    public static class Reports
    {
        public const string Read = "Reports.Read";
        public const string ReadConsolidated = "Reports.ReadConsolidated";
        public const string Export = "Reports.Export";
    }

    public static class Audit
    {
        public const string Read = "Audit.Read";
    }

    public static class Catalogs
    {
        public const string Read = "Catalogs.Read";
        public const string Create = "Catalogs.Create";
        public const string Update = "Catalogs.Update";
    }

    public static class Imports
    {
        public const string Read = "Imports.Read";
        public const string Create = "Imports.Create";
    }

    public static class Exports
    {
        public const string Create = "Exports.Create";
    }

    public static class Approvals
    {
        public const string Read = "Approvals.Read";
        public const string Configure = "Approvals.Configure";
        public const string Approve = "Approvals.Approve";
        public const string Reject = "Approvals.Reject";
    }

    public static class Configuration
    {
        public const string Read = "Configuration.Read";
        public const string Update = "Configuration.Update";
    }

    /// <summary>Every (module, action, description) triple to seed. See Infrastructure's seed consumer.</summary>
    public static IReadOnlyList<(string Module, string Action, string Description)> SeedEntries { get; } =
    [
        ("Companies", "Read", "Consultar empresas"),
        ("Companies", "Create", "Crear empresas"),
        ("Companies", "Update", "Editar y activar/desactivar empresas"),

        ("Structure", "Read", "Consultar la estructura organizacional"),
        ("Structure", "Create", "Crear unidades organizacionales"),
        ("Structure", "Update", "Editar, mover y activar/desactivar unidades organizacionales"),

        ("Roles", "Read", "Consultar roles"),
        ("Roles", "Create", "Crear roles"),
        ("Roles", "Update", "Editar y activar/desactivar roles"),
        ("Roles", "Duplicate", "Duplicar roles"),

        ("Permissions", "Read", "Consultar el catálogo de permisos"),
        ("Permissions", "Manage", "Administrar la matriz de permisos por rol"),

        ("Users", "Read", "Consultar usuarios"),
        ("Users", "Update", "Editar el perfil de un usuario"),
        ("Users", "ManageRoles", "Asignar y quitar roles a usuarios"),
        ("Users", "ManageCompanies", "Otorgar y revocar acceso de usuarios a empresas"),

        ("Assets", "Read", "Consultar activos"),
        ("Assets", "Create", "Dar de alta activos"),
        ("Assets", "Update", "Editar activos"),
        ("Assets", "Decommission", "Dar de baja activos"),

        ("Assignments", "Read", "Consultar asignaciones"),
        ("Assignments", "Create", "Crear asignaciones"),
        ("Assignments", "Update", "Editar asignaciones"),

        ("Returns", "Read", "Consultar devoluciones"),
        ("Returns", "Create", "Registrar devoluciones"),

        ("Loans", "Read", "Consultar préstamos"),
        ("Loans", "Create", "Registrar préstamos"),
        ("Loans", "Update", "Editar préstamos"),

        ("Transfers", "Read", "Consultar transferencias"),
        ("Transfers", "Create", "Crear transferencias"),
        ("Transfers", "Update", "Editar transferencias"),

        ("Movements", "Read", "Consultar movimientos"),

        ("Requests", "Read", "Consultar solicitudes"),
        ("Requests", "Create", "Crear solicitudes"),
        ("Requests", "Update", "Editar solicitudes"),

        ("Maintenance", "Read", "Consultar mantenimientos"),
        ("Maintenance", "Create", "Crear mantenimientos"),
        ("Maintenance", "Update", "Editar y cerrar mantenimientos"),

        ("Warranties", "Read", "Consultar garantías"),
        ("Warranties", "Create", "Registrar garantías"),
        ("Warranties", "Update", "Editar garantías"),

        ("SpareParts", "Read", "Consultar refacciones"),
        ("SpareParts", "Create", "Registrar refacciones"),
        ("SpareParts", "Update", "Editar refacciones"),

        ("Consumables", "Read", "Consultar consumibles"),
        ("Consumables", "Create", "Registrar consumibles"),
        ("Consumables", "Update", "Editar consumibles"),

        ("Documents", "Read", "Consultar documentos"),
        ("Documents", "Create", "Cargar documentos"),

        ("Templates", "Read", "Consultar plantillas"),
        ("Templates", "Create", "Crear plantillas"),
        ("Templates", "Update", "Editar y publicar plantillas"),

        ("Reports", "Read", "Consultar reportes por empresa"),
        ("Reports", "ReadConsolidated", "Consultar reportes consolidados entre empresas"),
        ("Reports", "Export", "Exportar reportes"),

        ("Audit", "Read", "Consultar el registro de auditoría"),

        ("Catalogs", "Read", "Consultar catálogos"),
        ("Catalogs", "Create", "Crear entradas de catálogo"),
        ("Catalogs", "Update", "Editar entradas de catálogo"),

        ("Imports", "Read", "Consultar lotes de importación"),
        ("Imports", "Create", "Iniciar importaciones masivas"),

        ("Exports", "Create", "Generar exportaciones"),

        ("Approvals", "Read", "Consultar flujos y solicitudes de aprobación"),
        ("Approvals", "Configure", "Configurar flujos de aprobación"),
        ("Approvals", "Approve", "Aprobar o rechazar solicitudes"),
        ("Approvals", "Reject", "Rechazar solicitudes"),

        ("Configuration", "Read", "Consultar configuración del sistema"),
        ("Configuration", "Update", "Editar configuración del sistema"),
    ];
}
