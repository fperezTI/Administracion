using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Identity;

/// <summary>
/// Local profile of an authenticated Microsoft Entra ID principal. Created only on first successful
/// login (see docs/security/authentication.md) — never created directly by an administrator.
/// </summary>
public sealed class User : AggregateRoot<Guid>
{
    private readonly List<UserRole> _userRoles = [];
    private readonly List<UserCompany> _userCompanies = [];

    public Guid EntraObjectId { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }
    public DateTimeOffset? AnonymizedAtUtc { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<UserCompany> UserCompanies => _userCompanies.AsReadOnly();

    private User()
    {
    }

    private User(Guid id, Guid entraObjectId, string displayName, string email, DateTimeOffset nowUtc)
        : base(id)
    {
        EntraObjectId = entraObjectId;
        DisplayName = displayName;
        Email = email;
        CreatedAtUtc = nowUtc;
        IsActive = true;
    }

    public static User Provision(Guid entraObjectId, string displayName, string email, DateTimeOffset nowUtc)
    {
        if (entraObjectId == Guid.Empty)
        {
            throw new DomainException("El identificador de Entra ID es obligatorio para crear el perfil local.");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException("El nombre del usuario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("El correo del usuario es obligatorio.");
        }

        return new User(Guid.NewGuid(), entraObjectId, displayName.Trim(), email.Trim(), nowUtc);
    }

    public void RecordLogin(DateTimeOffset nowUtc) => LastLoginAtUtc = nowUtc;

    public void UpdateProfile(string displayName, string email)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException("El nombre del usuario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("El correo del usuario es obligatorio.");
        }

        DisplayName = displayName.Trim();
        Email = email.Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    /// <summary>Permanent PII removal (pedido: "anonimización", ver docs/privacy-retention.md) — distinto
    /// de <see cref="Deactivate"/> (reversible, solo revoca acceso). Irreversible: no hay "Reidentify".
    /// No toca <c>AuditEntry.UserDisplayName</c> ya escrito — ese campo se denormalizó a propósito desde
    /// F8 (ADR 0010) para no perder el rastro de auditoría ya emitido; anonimizar el perfil no reescribe
    /// el pasado, solo detiene la exposición futura del nombre/correo reales.</summary>
    public void Anonymize(DateTimeOffset nowUtc)
    {
        if (AnonymizedAtUtc is not null)
        {
            throw new DomainException("El usuario ya fue anonimizado.");
        }

        DisplayName = "Usuario eliminado";
        Email = $"anonimizado-{Id:N}@eliminado.invalid";
        IsActive = false;
        AnonymizedAtUtc = nowUtc;
    }

    public void AssignRole(Guid roleId, Guid? assignedByUserId, DateTimeOffset nowUtc)
    {
        if (_userRoles.Any(ur => ur.RoleId == roleId))
        {
            return;
        }

        _userRoles.Add(UserRole.Create(Id, roleId, assignedByUserId, nowUtc));
    }

    public void RemoveRole(Guid roleId) => _userRoles.RemoveAll(ur => ur.RoleId == roleId);

    public void GrantCompanyAccess(Guid companyId, DateTimeOffset nowUtc)
    {
        if (_userCompanies.Any(uc => uc.CompanyId == companyId))
        {
            return;
        }

        _userCompanies.Add(UserCompany.Create(Id, companyId, nowUtc));
    }

    public void RevokeCompanyAccess(Guid companyId) => _userCompanies.RemoveAll(uc => uc.CompanyId == companyId);
}
