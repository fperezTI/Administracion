using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Audit;

/// <summary><see cref="CompanyId"/> is an optional display filter, not a security boundary — same
/// treatment as <c>Companies.Read</c>/<c>Roles.Read</c> (no per-company membership filter either):
/// <c>Audit.Read</c> is a global, tenant-wide admin permission (see AppDbContext's remarks on
/// <c>AuditEntry</c> for why it can't reliably be scoped by "active company" the way other queries are).</summary>
public sealed record GetAuditEntriesQuery(
    Guid? CompanyId = null, int PageNumber = 1, int PageSize = 50, Guid? UserId = null, string? CommandName = null,
    DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null)
    : IRequest<PagedResult<AuditEntrySummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Audit.Read;
}

public sealed record AuditEntrySummary(
    Guid Id, Guid? CompanyId, Guid? UserId, string? UserDisplayName, string CommandName, string? Module, string? Action,
    string? DetailsJson, bool Succeeded, string? ErrorMessage, DateTimeOffset OccurredAtUtc);

public sealed class GetAuditEntriesQueryHandler(IApplicationDbContext db) : IRequestHandler<GetAuditEntriesQuery, PagedResult<AuditEntrySummary>>
{
    public Task<PagedResult<AuditEntrySummary>> Handle(GetAuditEntriesQuery request, CancellationToken cancellationToken)
    {
        var query = db.AuditEntries.AsNoTracking();

        if (request.CompanyId is { } companyId)
        {
            query = query.Where(a => a.CompanyId == companyId);
        }

        if (request.UserId is { } userId)
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(request.CommandName))
        {
            query = query.Where(a => a.CommandName == request.CommandName);
        }

        if (request.FromUtc is { } fromUtc)
        {
            query = query.Where(a => a.OccurredAtUtc >= fromUtc);
        }

        if (request.ToUtc is { } toUtc)
        {
            query = query.Where(a => a.OccurredAtUtc <= toUtc);
        }

        var projected = query.OrderByDescending(a => a.OccurredAtUtc)
            .Select(a => new AuditEntrySummary(
                a.Id, a.CompanyId, a.UserId, a.UserDisplayName, a.CommandName, a.Module, a.Action, a.DetailsJson,
                a.Succeeded, a.ErrorMessage, a.OccurredAtUtc));

        return PagedResult<AuditEntrySummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
