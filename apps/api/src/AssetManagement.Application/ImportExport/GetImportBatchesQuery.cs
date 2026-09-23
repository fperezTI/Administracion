using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.ImportExport;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.ImportExport;

public sealed record GetImportBatchesQuery(
    Guid CompanyId,
    int PageNumber = 1,
    int PageSize = 50,
    /// <summary>createdAtUtc descendente (default) | fileName | status | totalRows</summary>
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<PagedResult<ImportBatchSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Imports.Read;
}

public sealed record ImportBatchSummary(
    Guid Id, string FileName, ImportBatchStatus Status, int? TotalRows, int? ValidRows, int? InvalidRows,
    int? SucceededRows, int? FailedRows, DateTimeOffset CreatedAtUtc);

public sealed class GetImportBatchesQueryHandler(IApplicationDbContext db) : IRequestHandler<GetImportBatchesQuery, PagedResult<ImportBatchSummary>>
{
    public Task<PagedResult<ImportBatchSummary>> Handle(GetImportBatchesQuery request, CancellationToken cancellationToken)
    {
        var query = db.ImportBatches.AsNoTracking().Where(b => b.CompanyId == request.CompanyId);

        // TotalRows es nullable (todavía procesando el archivo): se ordena siempre al final.
        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "fileName" => descending ? query.OrderByDescending(b => b.FileName) : query.OrderBy(b => b.FileName),
            "status" => descending ? query.OrderByDescending(b => b.Status) : query.OrderBy(b => b.Status),
            "totalRows" => descending
                ? query.OrderBy(b => b.TotalRows == null).ThenByDescending(b => b.TotalRows)
                : query.OrderBy(b => b.TotalRows == null).ThenBy(b => b.TotalRows),
            "createdAtUtc" => descending
                ? query.OrderByDescending(b => b.CreatedAtUtc)
                : query.OrderBy(b => b.CreatedAtUtc),
            // Default histórico (sin SortBy): más reciente primero, sin importar SortDescending.
            _ => query.OrderByDescending(b => b.CreatedAtUtc),
        };

        var projected = ordered.Select(b => new ImportBatchSummary(
            b.Id, b.FileName, b.Status, b.TotalRows, b.ValidRows, b.InvalidRows, b.SucceededRows, b.FailedRows, b.CreatedAtUtc));

        return PagedResult<ImportBatchSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
