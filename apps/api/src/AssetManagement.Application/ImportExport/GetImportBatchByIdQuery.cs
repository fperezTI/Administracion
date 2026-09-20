using System.Text.Json;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.ImportExport;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.ImportExport;

public sealed record GetImportBatchByIdQuery(Guid ImportBatchId) : IRequest<ImportBatchDetail>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Imports.Read;
}

public sealed record ImportBatchDetail(
    Guid Id, Guid CompanyId, string FileName, ImportBatchStatus Status, ImportCommitMode? CommitMode,
    int? TotalRows, int? ValidRows, int? InvalidRows, int? SucceededRows, int? FailedRows, string? ErrorMessage,
    IReadOnlyList<ImportRowResult> Rows, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed class GetImportBatchByIdQueryHandler(IApplicationDbContext db) : IRequestHandler<GetImportBatchByIdQuery, ImportBatchDetail>
{
    public async Task<ImportBatchDetail> Handle(GetImportBatchByIdQuery request, CancellationToken cancellationToken)
    {
        var batch = await db.ImportBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == request.ImportBatchId, cancellationToken)
            ?? throw new NotFoundException(nameof(ImportBatch), request.ImportBatchId);

        var rows = batch.ReportJson is null
            ? []
            : JsonSerializer.Deserialize<IReadOnlyList<ImportRowResult>>(batch.ReportJson) ?? [];

        return new ImportBatchDetail(
            batch.Id, batch.CompanyId, batch.FileName, batch.Status, batch.CommitMode, batch.TotalRows, batch.ValidRows,
            batch.InvalidRows, batch.SucceededRows, batch.FailedRows, batch.ErrorMessage, rows, batch.CreatedAtUtc, batch.UpdatedAtUtc);
    }
}
