using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.ImportExport;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.ImportExport;

public sealed record GetImportBatchesQuery(Guid CompanyId, int PageNumber = 1, int PageSize = 50)
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
        var query = db.ImportBatches.AsNoTracking()
            .Where(b => b.CompanyId == request.CompanyId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => new ImportBatchSummary(
                b.Id, b.FileName, b.Status, b.TotalRows, b.ValidRows, b.InvalidRows, b.SucceededRows, b.FailedRows, b.CreatedAtUtc));

        return PagedResult<ImportBatchSummary>.CreateAsync(query, request.PageNumber, request.PageSize, cancellationToken);
    }
}
