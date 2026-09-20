namespace AssetManagement.Domain.ImportExport;

public enum ImportBatchStatus
{
    Queued,
    Validating,
    Validated,
    Processing,
    Completed,
    CompletedWithErrors,
    Failed,
    Cancelled,
}
