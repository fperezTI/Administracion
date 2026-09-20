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
public sealed record GetTransfersQuery(Guid CompanyId, int PageNumber = 1, int PageSize = 50, TransferStatus? Status = null)
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

        var projected =
            from t in query
            join asset in db.Assets.AsNoTracking().IgnoreQueryFilters() on t.AssetId equals asset.Id
            join fromCompany in db.Companies.AsNoTracking() on t.FromCompanyId equals fromCompany.Id
            join toCompany in db.Companies.AsNoTracking() on t.ToCompanyId equals toCompany.Id
            orderby t.RequestedAtUtc descending
            select new TransferSummary(
                t.Id, t.AssetId, asset.InternalFolio, t.FromCompanyId, fromCompany.TradeName, t.ToCompanyId,
                toCompany.TradeName, t.Status, t.RequestedAtUtc);

        return PagedResult<TransferSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
