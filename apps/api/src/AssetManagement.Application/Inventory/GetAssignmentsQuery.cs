using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

public sealed record GetAssignmentsQuery(
    Guid CompanyId,
    int PageNumber = 1,
    int PageSize = 50,
    AssignmentStatus? Status = null,
    Guid? AssetId = null,
    /// <summary>Coincide contra el nombre de la persona asignada (contains, sin distinguir mayúsculas).</summary>
    string? AssignedToSearch = null,
    /// <summary>Coincide contra folio/marca/modelo del activo asignado (mismo criterio que ExportAssetsQuery).</summary>
    string? Search = null,
    DateOnly? AssignedFrom = null,
    DateOnly? AssignedTo = null,
    Guid? OrgUnitId = null,
    /// <summary>assignedAtUtc descendente (default) | assetFolio | assignedToDisplayName | status</summary>
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<PagedResult<AssignmentSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Assignments.Read;
}

public sealed record AssignmentSummary(
    Guid Id,
    Guid AssetId,
    string AssetFolio,
    Guid AssignedToUserId,
    string AssignedToDisplayName,
    AssignmentStatus Status,
    DateTimeOffset AssignedAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? ReturnedAtUtc,
    Guid? GroupId);

public sealed class GetAssignmentsQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetAssignmentsQuery, PagedResult<AssignmentSummary>>
{
    public Task<PagedResult<AssignmentSummary>> Handle(GetAssignmentsQuery request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var query = db.Assignments.AsNoTracking().Where(a => a.CompanyId == request.CompanyId);

        if (request.Status is { } status)
        {
            query = query.Where(a => a.Status == status);
        }

        if (request.AssetId is { } assetId)
        {
            query = query.Where(a => a.AssetId == assetId);
        }

        if (request.AssignedFrom is { } assignedFrom)
        {
            var fromUtc = assignedFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.AssignedAtUtc >= fromUtc);
        }

        if (request.AssignedTo is { } assignedTo)
        {
            var toUtc = assignedTo.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(a => a.AssignedAtUtc <= toUtc);
        }

        if (request.OrgUnitId is { } orgUnitId)
        {
            query = query.Where(a => a.OrgUnitId == orgUnitId);
        }

        var joined =
            from a in query
            join asset in db.Assets.AsNoTracking() on a.AssetId equals asset.Id
            join user in db.Users.AsNoTracking() on a.AssignedToUserId equals user.Id
            select new { Assignment = a, AssetFolio = asset.InternalFolio, AssetBrand = asset.Brand, AssetModel = asset.Model, AssignedToName = user.DisplayName };

        if (!string.IsNullOrWhiteSpace(request.AssignedToSearch))
        {
            var term = request.AssignedToSearch.Trim();
            joined = joined.Where(x => x.AssignedToName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            joined = joined.Where(x => x.AssetFolio.Contains(term) || x.AssetBrand.Contains(term) || x.AssetModel.Contains(term));
        }

        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "assetFolio" => descending ? joined.OrderByDescending(x => x.AssetFolio) : joined.OrderBy(x => x.AssetFolio),
            "assignedToDisplayName" => descending
                ? joined.OrderByDescending(x => x.AssignedToName)
                : joined.OrderBy(x => x.AssignedToName),
            "status" => descending
                ? joined.OrderByDescending(x => x.Assignment.Status)
                : joined.OrderBy(x => x.Assignment.Status),
            "assignedAtUtc" => descending
                ? joined.OrderByDescending(x => x.Assignment.AssignedAtUtc)
                : joined.OrderBy(x => x.Assignment.AssignedAtUtc),
            // Default histórico (sin SortBy): más reciente primero, sin importar SortDescending.
            _ => joined.OrderByDescending(x => x.Assignment.AssignedAtUtc),
        };

        var projected = ordered.Select(x => new AssignmentSummary(
            x.Assignment.Id, x.Assignment.AssetId, x.AssetFolio, x.Assignment.AssignedToUserId, x.AssignedToName,
            x.Assignment.Status, x.Assignment.AssignedAtUtc, x.Assignment.AcceptedAtUtc, x.Assignment.ReturnedAtUtc,
            x.Assignment.AssignmentGroupId));

        return PagedResult<AssignmentSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
