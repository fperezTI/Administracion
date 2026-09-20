namespace AssetManagement.Domain.Inventory;

public enum TransferStatus
{
    PendingApproval,
    Rejected,
    Cancelled,
    InTransit,
    Completed,
}
