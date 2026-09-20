namespace AssetManagement.Application.Common.Organization;

/// <summary>
/// The 11 organizational unit kinds required by pedido §9, seeded once via migration. The catalog
/// itself stays editable later (ADR 0002); this is only the initial configuration.
/// </summary>
public static class OrgUnitTypeCatalog
{
    public static class Codes
    {
        public const string BusinessUnit = "BusinessUnit";
        public const string Direction = "Direction";
        public const string Management = "Management";
        public const string Department = "Department";
        public const string Area = "Area";
        public const string Team = "Team";
        public const string Branch = "Branch";
        public const string Location = "Location";
        public const string Warehouse = "Warehouse";
        public const string DataCenter = "DataCenter";
        public const string Project = "Project";
    }

    public static IReadOnlyList<(string Code, string Name)> SeedEntries { get; } =
    [
        (Codes.BusinessUnit, "Unidad de negocio"),
        (Codes.Direction, "Dirección"),
        (Codes.Management, "Gerencia"),
        (Codes.Department, "Departamento"),
        (Codes.Area, "Área"),
        (Codes.Team, "Equipo"),
        (Codes.Branch, "Sucursal"),
        (Codes.Location, "Ubicación física"),
        (Codes.Warehouse, "Almacén"),
        (Codes.DataCenter, "Centro de datos"),
        (Codes.Project, "Proyecto"),
    ];
}
