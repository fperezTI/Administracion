using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Requests;

/// <summary>Self-service — only the original requester may cancel their own pending request, same pattern
/// as <c>CancelTransferCommand</c> (F5) / <c>CancelApprovalInstanceCommand</c> (F4). Does not also cancel
/// the associated <c>ApprovalInstance</c> — same precedent <c>CancelTransferCommand</c> already set; the
/// instance is simply left <c>Pending</c> with nothing left to decide on.</summary>
public sealed record CancelInternalRequestCommand(Guid InternalRequestId) : IRequest, IAuditableCommand
{
}

public sealed class CancelInternalRequestCommandHandler(IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<CancelInternalRequestCommand>
{
    public async Task Handle(CancelInternalRequestCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Se requiere iniciar sesión.");
        }

        var internalRequest = await db.InternalRequests.FirstOrDefaultAsync(r => r.Id == request.InternalRequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(InternalRequest), request.InternalRequestId);

        internalRequest.Cancel(userId, clock.UtcNow, userId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
