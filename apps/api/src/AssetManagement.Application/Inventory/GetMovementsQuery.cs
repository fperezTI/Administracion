using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

public sealed record GetMovementsQuery(
    Guid CompanyId, int PageNumber = 1, int PageSize = 50, Guid? AssetId = null, MovementType? Type = null)
    : IRequest<PagedResult<MovementSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Movements.Read;
}

public sealed record MovementSummary(
    Guid Id,
    Guid AssetId,
    string AssetFolio,
    MovementType Type,
    string FolioNumber,
    MovementStatus Status,
    Guid? FromOrgUnitId,
    Guid? ToOrgUnitId,
    Guid? FromUserId,
    Guid? ToUserId,
    string? Notes,
    DateTimeOffset EffectiveAtUtc);

public sealed class GetMovementsQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetMovementsQuery, PagedResult<MovementSummary>>
{
    public Task<PagedResult<MovementSummary>> Handle(GetMovementsQuery request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var query = db.Movements.AsNoTracking().Where(m => m.CompanyId == request.CompanyId);

        if (request.AssetId is { } assetId)
        {
            query = query.Where(m => m.AssetId == assetId);
        }

        if (request.Type is { } type)
        {
            query = query.Where(m => m.Type == type);
        }

        var projected =
            from m in query
            join asset in db.Assets.AsNoTracking() on m.AssetId equals asset.Id
            orderby m.EffectiveAtUtc descending
            select new MovementSummary(
                m.Id, m.AssetId, asset.InternalFolio, m.Type, m.FolioNumber, m.Status, m.FromOrgUnitId,
                m.ToOrgUnitId, m.FromUserId, m.ToUserId, m.Notes, m.EffectiveAtUtc);

        return PagedResult<MovementSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
