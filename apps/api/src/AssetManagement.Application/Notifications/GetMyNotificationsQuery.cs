using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Notifications;

/// <summary>Self-service, no permission — mirrors <c>GetMyAssignmentsQuery</c>. No
/// <c>Notifications.*</c> permission was ever seeded (confirmed against <c>PermissionCatalog</c>): a
/// notification belongs to the person it was sent to, not to an RBAC-gated resource.</summary>
public sealed record GetMyNotificationsQuery : IRequest<IReadOnlyList<MyNotificationSummary>>
{
}

public sealed record MyNotificationSummary(
    Guid Id, string Type, string Title, string Body, bool IsRead, DateTimeOffset CreatedAtUtc, DateTimeOffset? ReadAtUtc);

public sealed class GetMyNotificationsQueryHandler(IApplicationDbContext db, ICurrentUserContext currentUser)
    : IRequestHandler<GetMyNotificationsQuery, IReadOnlyList<MyNotificationSummary>>
{
    public async Task<IReadOnlyList<MyNotificationSummary>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Se requiere iniciar sesión.");
        }

        var results =
            from n in db.Notifications.AsNoTracking()
            where n.UserId == userId
            orderby n.IsRead, n.CreatedAtUtc descending
            select new MyNotificationSummary(n.Id, n.Type, n.Title, n.Body, n.IsRead, n.CreatedAtUtc, n.ReadAtUtc);

        return await results.ToListAsync(cancellationToken);
    }
}
