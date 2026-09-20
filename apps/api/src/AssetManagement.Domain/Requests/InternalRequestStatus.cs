namespace AssetManagement.Domain.Requests;

public enum InternalRequestStatus
{
    PendingApproval,
    Rejected,
    Cancelled,
    Fulfilled,
}
