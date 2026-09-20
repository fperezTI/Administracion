namespace AssetManagement.Application.Common.Security;

/// <summary>Document type codes used as the folio prefix (pedido §8/§17) — one FolioSequence row per
/// (company, document type). Add a constant here when a future phase introduces a new foliated document;
/// never invent a folio format inline in a handler.</summary>
public static class FolioDocumentTypes
{
    public const string Asset = "ASSET";
    public const string MovementAssignment = "MOV-ASG";
    public const string MovementAssignmentReturn = "MOV-RET";
    public const string MovementLoan = "MOV-LOAN";
    public const string MovementLoanReturn = "MOV-LOANRET";
    public const string MovementRelocation = "MOV-REL";
    public const string MovementCrossCompanyTransferOut = "MOV-XFER-OUT";
    public const string MovementCrossCompanyTransferIn = "MOV-XFER-IN";
    public const string MaintenanceOrder = "MAINT";
    public const string ConsumableStockMovement = "CONS-MOV";
}
