using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Users;

/// <summary>Permanent PII removal (pedido: "anonimización", ver docs/privacy-retention.md) — irreversible,
/// distinto de una simple desactivación.</summary>
public sealed record AnonymizeUserCommand(Guid UserId) : IRequest, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Users.Update;
}

public sealed class AnonymizeUserCommandHandler(IApplicationDbContext db, IClock clock)
    : IRequestHandler<AnonymizeUserCommand>
{
    public async Task Handle(AnonymizeUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.Anonymize(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }
}
