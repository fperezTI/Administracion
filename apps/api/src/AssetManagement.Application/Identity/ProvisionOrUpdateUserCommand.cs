using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity;

/// <summary>
/// Runs once per authenticated request (see the provisioning middleware in Infrastructure/API). On
/// first login it creates the local profile (pedido §5: "creación controlada del perfil local en el
/// primer acceso"); on every login it records the access time. Never called directly by a user action.
/// </summary>
public sealed record ProvisionOrUpdateUserCommand(Guid EntraObjectId, string DisplayName, string Email)
    : IRequest<CurrentUserSnapshot>;

public sealed record CurrentUserSnapshot(
    Guid UserId,
    Guid EntraObjectId,
    string DisplayName,
    string Email,
    bool IsActive,
    IReadOnlyCollection<string> PermissionCodes,
    IReadOnlyCollection<Guid> CompanyIds);

public sealed class ProvisionOrUpdateUserCommandHandler(IApplicationDbContext db, IClock clock)
    : IRequestHandler<ProvisionOrUpdateUserCommand, CurrentUserSnapshot>
{
    public async Task<CurrentUserSnapshot> Handle(ProvisionOrUpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == request.EntraObjectId, cancellationToken);
        var now = clock.UtcNow;

        if (user is null)
        {
            user = User.Provision(request.EntraObjectId, request.DisplayName, request.Email, now);
            db.Users.Add(user);
        }
        else
        {
            user.UpdateProfile(request.DisplayName, request.Email);
        }

        user.RecordLogin(now);

        await db.SaveChangesAsync(cancellationToken);

        var permissionCodes = await UserPermissionLookup.GetPermissionCodesAsync(db, user.Id, cancellationToken);

        var companyIds = await db.UserCompanies
            .Where(uc => uc.UserId == user.Id)
            .Select(uc => uc.CompanyId)
            .ToListAsync(cancellationToken);

        return new CurrentUserSnapshot(
            user.Id, user.EntraObjectId, user.DisplayName, user.Email, user.IsActive, permissionCodes, companyIds);
    }
}
