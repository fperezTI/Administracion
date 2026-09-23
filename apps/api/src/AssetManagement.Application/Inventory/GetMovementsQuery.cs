using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

public sealed record GetMovementsQuery(
    Guid CompanyId,
    int PageNumber = 1,
    int PageSize = 50,
    Guid? AssetId = null,
    MovementType? Type = null,
    /// <summary>effectiveAtUtc descendente (default) | folioNumber | assetFolio | type | notes</summary>
    string? SortBy = null,
    bool SortDescending = false)
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

        var joined =
            from m in query
            join asset in db.Assets.AsNoTracking() on m.AssetId equals asset.Id
            select new { Movement = m, AssetFolio = asset.InternalFolio };

        // Notes es nullable: se ordena siempre al final sin importar la dirección (mismo criterio que
        // Description/SerialNumber en GetAssetsQuery).
        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "folioNumber" => descending
                ? joined.OrderByDescending(x => x.Movement.FolioNumber)
                : joined.OrderBy(x => x.Movement.FolioNumber),
            "assetFolio" => descending
                ? joined.OrderByDescending(x => x.AssetFolio)
                : joined.OrderBy(x => x.AssetFolio),
            "type" => descending
                ? joined.OrderByDescending(x => x.Movement.Type)
                : joined.OrderBy(x => x.Movement.Type),
            "notes" => descending
                ? joined.OrderBy(x => x.Movement.Notes == null).ThenByDescending(x => x.Movement.Notes)
                : joined.OrderBy(x => x.Movement.Notes == null).ThenBy(x => x.Movement.Notes),
            "effectiveAtUtc" => descending
                ? joined.OrderByDescending(x => x.Movement.EffectiveAtUtc)
                : joined.OrderBy(x => x.Movement.EffectiveAtUtc),
            // Default histórico (sin SortBy): más reciente primero, sin importar SortDescending.
            _ => joined.OrderByDescending(x => x.Movement.EffectiveAtUtc),
        };

        var projected = ordered.Select(x => new MovementSummary(
            x.Movement.Id, x.Movement.AssetId, x.AssetFolio, x.Movement.Type, x.Movement.FolioNumber,
            x.Movement.Status, x.Movement.FromOrgUnitId, x.Movement.ToOrgUnitId, x.Movement.FromUserId,
            x.Movement.ToUserId, x.Movement.Notes, x.Movement.EffectiveAtUtc));

        return PagedResult<MovementSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
