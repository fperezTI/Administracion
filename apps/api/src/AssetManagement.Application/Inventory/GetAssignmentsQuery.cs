using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

public sealed record GetAssignmentsQuery(
    Guid CompanyId, int PageNumber = 1, int PageSize = 50, AssignmentStatus? Status = null, Guid? AssetId = null)
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

        var projected =
            from a in query
            join asset in db.Assets.AsNoTracking() on a.AssetId equals asset.Id
            join user in db.Users.AsNoTracking() on a.AssignedToUserId equals user.Id
            orderby a.AssignedAtUtc descending
            select new AssignmentSummary(
                a.Id, a.AssetId, asset.InternalFolio, a.AssignedToUserId, user.DisplayName, a.Status,
                a.AssignedAtUtc, a.AcceptedAtUtc, a.ReturnedAtUtc, a.AssignmentGroupId);

        return PagedResult<AssignmentSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
