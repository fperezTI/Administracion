using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Domain.Approvals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Approvals;

/// <summary>Self-service — only the original requester may cancel their own pending request (pedido C4:
/// "rechazo/cancelación").</summary>
public sealed record CancelApprovalInstanceCommand(Guid ApprovalInstanceId) : IRequest
{
}

public sealed class CancelApprovalInstanceCommandHandler(
    IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<CancelApprovalInstanceCommand>
{
    public async Task Handle(CancelApprovalInstanceCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Se requiere iniciar sesión.");
        }

        var instance = await db.ApprovalInstances.FirstOrDefaultAsync(i => i.Id == request.ApprovalInstanceId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApprovalInstance), request.ApprovalInstanceId);

        instance.Cancel(userId, clock.UtcNow, userId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
