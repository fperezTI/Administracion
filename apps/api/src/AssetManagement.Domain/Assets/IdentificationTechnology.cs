namespace AssetManagement.Domain.Assets;

/// <summary>Supported physical identification technologies (pedido §12). QR is the default; the set
/// itself is fixed (these are real, finite technical options, not a business-configurable catalog), but
/// which one applies is configurable per category or per asset.</summary>
public enum IdentificationTechnology
{
    Qr,
    Barcode,
    QrAndBarcode,
    Nfc,
    Rfid,
}
