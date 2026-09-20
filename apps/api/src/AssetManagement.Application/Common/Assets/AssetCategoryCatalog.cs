using AssetManagement.Domain.Assets;

namespace AssetManagement.Application.Common.Assets;

/// <summary>The 9 initial asset categories required by pedido §10, seeded once via migration. The
/// catalog itself stays fully editable afterwards (create/deactivate more categories through the API) —
/// this is only the starting configuration, never a hardcoded enum.</summary>
public static class AssetCategoryCatalog
{
    public static IReadOnlyList<(string Code, string Name, IdentificationTechnology DefaultTechnology)> SeedEntries { get; } =
    [
        ("LAPTOP", "Laptops", IdentificationTechnology.Qr),
        ("MONITOR", "Monitores", IdentificationTechnology.Qr),
        ("DOCK", "Docks", IdentificationTechnology.Qr),
        ("CHARGER", "Cargadores", IdentificationTechnology.Qr),
        ("PHONE", "Celulares", IdentificationTechnology.Qr),
        ("PRINTER", "Impresoras", IdentificationTechnology.Qr),
        ("SWITCH", "Switches", IdentificationTechnology.Qr),
        ("ACCESS_POINT", "Access points", IdentificationTechnology.Qr),
        ("SERVER", "Servidores", IdentificationTechnology.QrAndBarcode),
    ];
}
