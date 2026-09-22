using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

/// <summary>Self-service: the report behind the "confirm what was assigned to me" email link — same
/// content as <see cref="GetAssignmentByIdQuery"/> (via <see cref="AssignmentDetailBuilder"/>), but gated
/// by ownership instead of `Assignments.Read`, same self-scoping precedent as
/// <see cref="GetMyAssignmentsQuery"/>/<see cref="SignAssignmentCommand"/> — a plain recipient with no RBAC
/// permission at all must still be able to see and accept their own assignment.</summary>
public sealed record GetMyAssignmentByIdQuery(Guid AssignmentId) : IRequest<AssignmentDetail>
{
}

public sealed class GetMyAssignmentByIdQueryHandler(IApplicationDbContext db, ICurrentUserContext currentUser)
    : IRequestHandler<GetMyAssignmentByIdQuery, AssignmentDetail>
{
    public async Task<AssignmentDetail> Handle(GetMyAssignmentByIdQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Se requiere iniciar sesión.");
        }

        var assignedToUserId = await db.Assignments.AsNoTracking()
            .Where(a => a.Id == request.AssignmentId)
            .Select(a => (Guid?)a.AssignedToUserId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Assignment), request.AssignmentId);

        if (assignedToUserId != userId)
        {
            throw new ForbiddenAccessException("Solo el destinatario de la asignación puede consultarla.");
        }

        return await AssignmentDetailBuilder.BuildAsync(db, request.AssignmentId, cancellationToken);
    }
}
