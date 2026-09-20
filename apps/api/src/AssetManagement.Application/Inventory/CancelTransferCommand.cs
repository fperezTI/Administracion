using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

/// <summary>Self-service — only the original requester may cancel their own pending transfer, same
/// pattern as CancelPendingAssignmentCommand (F3) / CancelApprovalInstanceCommand (F4).</summary>
public sealed record CancelTransferCommand(Guid TransferId) : IRequest, IAuditableCommand
{
}

public sealed class CancelTransferCommandHandler(IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<CancelTransferCommand>
{
    public async Task Handle(CancelTransferCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Se requiere iniciar sesión.");
        }

        var transfer = await db.Transfers.FirstOrDefaultAsync(t => t.Id == request.TransferId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transfer), request.TransferId);

        transfer.Cancel(userId, clock.UtcNow, userId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
