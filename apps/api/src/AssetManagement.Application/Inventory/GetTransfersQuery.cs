using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

/// <summary>Lists transfers where the caller's active company is either the source or the destination —
/// the one query in the system that legitimately spans two companies (see the OR query filter on
/// Transfer in AppDbContext).</summary>
public sealed record GetTransfersQuery(
    Guid CompanyId,
    int PageNumber = 1,
    int PageSize = 50,
    TransferStatus? Status = null,
    /// <summary>requestedAtUtc descendente (default) | assetFolio | fromCompanyName | toCompanyName | status</summary>
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<PagedResult<TransferSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Transfers.Read;
}

public sealed record TransferSummary(
    Guid Id,
    Guid AssetId,
    string AssetFolio,
    Guid FromCompanyId,
    string FromCompanyName,
    Guid ToCompanyId,
    string ToCompanyName,
    TransferStatus Status,
    DateTimeOffset RequestedAtUtc);

public sealed class GetTransfersQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetTransfersQuery, PagedResult<TransferSummary>>
{
    public Task<PagedResult<TransferSummary>> Handle(GetTransfersQuery request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var query = db.Transfers.AsNoTracking()
            .Where(t => t.FromCompanyId == request.CompanyId || t.ToCompanyId == request.CompanyId);

        if (request.Status is { } status)
        {
            query = query.Where(t => t.Status == status);
        }

        var joined =
            from t in query
            join asset in db.Assets.AsNoTracking().IgnoreQueryFilters() on t.AssetId equals asset.Id
            join fromCompany in db.Companies.AsNoTracking() on t.FromCompanyId equals fromCompany.Id
            join toCompany in db.Companies.AsNoTracking() on t.ToCompanyId equals toCompany.Id
            select new { Transfer = t, AssetFolio = asset.InternalFolio, FromCompanyName = fromCompany.TradeName, ToCompanyName = toCompany.TradeName };

        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "assetFolio" => descending ? joined.OrderByDescending(x => x.AssetFolio) : joined.OrderBy(x => x.AssetFolio),
            "fromCompanyName" => descending
                ? joined.OrderByDescending(x => x.FromCompanyName)
                : joined.OrderBy(x => x.FromCompanyName),
            "toCompanyName" => descending
                ? joined.OrderByDescending(x => x.ToCompanyName)
                : joined.OrderBy(x => x.ToCompanyName),
            "status" => descending ? joined.OrderByDescending(x => x.Transfer.Status) : joined.OrderBy(x => x.Transfer.Status),
            "requestedAtUtc" => descending
                ? joined.OrderByDescending(x => x.Transfer.RequestedAtUtc)
                : joined.OrderBy(x => x.Transfer.RequestedAtUtc),
            // Default histórico (sin SortBy): más reciente primero, sin importar SortDescending.
            _ => joined.OrderByDescending(x => x.Transfer.RequestedAtUtc),
        };

        var projected = ordered.Select(x => new TransferSummary(
            x.Transfer.Id, x.Transfer.AssetId, x.AssetFolio, x.Transfer.FromCompanyId, x.FromCompanyName,
            x.Transfer.ToCompanyId, x.ToCompanyName, x.Transfer.Status, x.Transfer.RequestedAtUtc));

        return PagedResult<TransferSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
