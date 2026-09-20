using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Domain.Audit;
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
    private const string SuperAdminRoleName = "Super Administrador";

    public async Task<CurrentUserSnapshot> Handle(ProvisionOrUpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == request.EntraObjectId, cancellationToken);
        var now = clock.UtcNow;

        // Solo puede haber un "primer usuario" en la vida del sistema: se verifica antes de dar de
        // alta al nuevo User (todavía no persistido, por lo que no se cuenta a sí mismo).
        var isFirstUserEver = user is null && !await db.Users.AnyAsync(cancellationToken);

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

        if (isFirstUserEver)
        {
            await BootstrapSuperAdminAsync(user, now, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        var permissionCodes = await UserPermissionLookup.GetPermissionCodesAsync(db, user.Id, cancellationToken);

        var companyIds = await db.UserCompanies
            .Where(uc => uc.UserId == user.Id)
            .Select(uc => uc.CompanyId)
            .ToListAsync(cancellationToken);

        return new CurrentUserSnapshot(
            user.Id, user.EntraObjectId, user.DisplayName, user.Email, user.IsActive, permissionCodes, companyIds);
    }

    /// <summary>
    /// Sin esto, un despliegue nuevo (sin cuentas locales ni contraseñas, solo Entra ID) no tendría
    /// forma de arrancar: nadie podría entrar a la pantalla de Roles/Usuarios para darse permisos a sí
    /// mismo. Se ejecuta una única vez por instalación, en la misma transacción que crea al primer
    /// usuario, y queda con un rastro de auditoría explícito por su relevancia de seguridad (CLAUDE.md
    /// regla 5) aunque este comando en sí no pase por AuditBehavior (no lo dispara una acción de usuario).
    /// </summary>
    private async Task BootstrapSuperAdminAsync(User user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var role = Role.Create(
            SuperAdminRoleName,
            "Rol de arranque con todos los permisos, creado automáticamente para el primer usuario del sistema.",
            now);

        var allPermissionIds = await db.Permissions.Select(p => p.Id).ToListAsync(cancellationToken);
        role.SetPermissions(allPermissionIds);
        db.Roles.Add(role);

        user.AssignRole(role.Id, user.Id, now);

        db.AuditEntries.Add(AuditEntry.Create(
            companyId: null,
            userId: user.Id,
            userDisplayName: user.DisplayName,
            commandName: nameof(ProvisionOrUpdateUserCommand),
            module: "Identity",
            action: "BootstrapSuperAdmin",
            detailsJson: null,
            succeeded: true,
            errorMessage: null,
            ipAddress: null,
            userAgent: null,
            correlationId: null,
            nowUtc: now));
    }
}
