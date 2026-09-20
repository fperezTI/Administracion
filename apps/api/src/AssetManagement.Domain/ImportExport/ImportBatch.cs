using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.ImportExport;

/// <summary>
/// A bulk asset-import job (pedido: hasta 10,000 filas por lote, con vista previa y modo transaccional
/// completo o solo-filas-válidas). Processed asynchronously and idempotently by a background worker (see
/// ADR 0003/0011): uploading only creates the batch in <see cref="ImportBatchStatus.Queued"/> — nothing is
/// validated or written until the worker picks it up. Validation never writes an <see cref="Assets.Asset"/>;
/// only an explicit <see cref="RequestCommit"/> does, and only for rows still valid at that moment (the
/// worker re-validates — the same risk F4/F5/F7 already accept: state can drift between preview and
/// confirmation).
/// </summary>
public sealed class ImportBatch : AuditableAggregateRoot<Guid>
{
    public Guid CompanyId { get; private set; }
    public string FileName { get; private set; } = null!;
    public string BlobPath { get; private set; } = null!;
    public ImportBatchStatus Status { get; private set; }
    public ImportCommitMode? CommitMode { get; private set; }

    public int? TotalRows { get; private set; }
    public int? ValidRows { get; private set; }
    public int? InvalidRows { get; private set; }
    public int? SucceededRows { get; private set; }
    public int? FailedRows { get; private set; }

    /// <summary>Serialized <c>IReadOnlyList&lt;ImportRowResult&gt;</c> — same "raw JSON blob" shape as
    /// <see cref="Audit.AuditEntry.DetailsJson"/>, not a normalized child table: a report is read as a
    /// whole, never queried row-by-row server-side.</summary>
    public string? ReportJson { get; private set; }

    public string? ErrorMessage { get; private set; }

    private ImportBatch()
    {
    }

    private ImportBatch(Guid id, Guid companyId, string fileName, string blobPath, DateTimeOffset nowUtc, Guid createdByUserId)
        : base(id, nowUtc, createdByUserId)
    {
        CompanyId = companyId;
        FileName = fileName;
        BlobPath = blobPath;
        Status = ImportBatchStatus.Queued;
    }

    public static ImportBatch Create(Guid companyId, string fileName, string blobPath, DateTimeOffset nowUtc, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainException("El nombre del archivo es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(blobPath))
        {
            throw new DomainException("La ruta de almacenamiento del archivo es obligatoria.");
        }

        return new ImportBatch(Guid.NewGuid(), companyId, fileName.Trim(), blobPath.Trim(), nowUtc, createdByUserId);
    }

    public void BeginValidation(DateTimeOffset nowUtc)
    {
        if (Status != ImportBatchStatus.Queued)
        {
            throw new DomainException("Solo un lote en cola puede comenzar a validarse.");
        }

        Status = ImportBatchStatus.Validating;
        RecordUpdate(nowUtc, updatedByUserId: null);
    }

    public void CompleteValidation(int totalRows, int validRows, int invalidRows, string reportJson, DateTimeOffset nowUtc)
    {
        if (Status != ImportBatchStatus.Validating)
        {
            throw new DomainException("Solo un lote en validación puede completar la validación.");
        }

        Status = ImportBatchStatus.Validated;
        TotalRows = totalRows;
        ValidRows = validRows;
        InvalidRows = invalidRows;
        ReportJson = reportJson;
        RecordUpdate(nowUtc, updatedByUserId: null);
    }

    public void FailValidation(string errorMessage, DateTimeOffset nowUtc)
    {
        if (Status != ImportBatchStatus.Validating)
        {
            throw new DomainException("Solo un lote en validación puede fallar la validación.");
        }

        Status = ImportBatchStatus.Failed;
        ErrorMessage = errorMessage;
        RecordUpdate(nowUtc, updatedByUserId: null);
    }

    public void RequestCommit(ImportCommitMode mode, DateTimeOffset nowUtc, Guid requestedByUserId)
    {
        if (Status != ImportBatchStatus.Validated)
        {
            throw new DomainException("Solo un lote validado puede confirmarse.");
        }

        if (InvalidRows > 0 && mode == ImportCommitMode.AllOrNothing)
        {
            throw new DomainException(
                "El lote tiene filas inválidas — el modo 'todo o nada' requiere que todas las filas sean válidas.");
        }

        Status = ImportBatchStatus.Processing;
        CommitMode = mode;
        RecordUpdate(nowUtc, requestedByUserId);
    }

    public void CompleteProcessing(int succeededRows, int failedRows, string reportJson, DateTimeOffset nowUtc)
    {
        if (Status != ImportBatchStatus.Processing)
        {
            throw new DomainException("Solo un lote en procesamiento puede completar el procesamiento.");
        }

        Status = failedRows > 0 ? ImportBatchStatus.CompletedWithErrors : ImportBatchStatus.Completed;
        SucceededRows = succeededRows;
        FailedRows = failedRows;
        ReportJson = reportJson;
        RecordUpdate(nowUtc, updatedByUserId: null);
    }

    public void FailProcessing(string errorMessage, string? reportJson, DateTimeOffset nowUtc)
    {
        if (Status != ImportBatchStatus.Processing)
        {
            throw new DomainException("Solo un lote en procesamiento puede fallar el procesamiento.");
        }

        Status = ImportBatchStatus.Failed;
        ErrorMessage = errorMessage;
        ReportJson = reportJson ?? ReportJson;
        RecordUpdate(nowUtc, updatedByUserId: null);
    }

    public void Cancel(DateTimeOffset nowUtc, Guid cancelledByUserId)
    {
        if (Status is not (ImportBatchStatus.Queued or ImportBatchStatus.Validating or ImportBatchStatus.Validated))
        {
            throw new DomainException("Solo un lote sin confirmar puede cancelarse.");
        }

        Status = ImportBatchStatus.Cancelled;
        RecordUpdate(nowUtc, cancelledByUserId);
    }
}
