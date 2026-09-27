using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity;

/// <summary>Sets the caller's own personal theme preference, or clears it (ThemeCode null) to restore
/// "usar tema de la empresa". Always the current authenticated user — never accepts a target user id,
/// so a caller can't modify anyone else's preference (pedido: sistema de temas visuales). Idempotent:
/// setting the same code again is a no-op write, same as SetCompanyActiveCommand's pattern.</summary>
public sealed record SetMyThemePreferenceCommand(string? ThemeCode) : IRequest;

public sealed class SetMyThemePreferenceCommandHandler(IApplicationDbContext db, ICurrentUserContext currentUser)
    : IRequestHandler<SetMyThemePreferenceCommand>
{
    public async Task Handle(SetMyThemePreferenceCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Authentication is required to set a theme preference.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Identity.User), userId);

        user.SetThemePreference(request.ThemeCode);

        await db.SaveChangesAsync(cancellationToken);
    }
}
