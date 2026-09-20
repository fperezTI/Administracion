namespace AssetManagement.Domain.Identity;

/// <summary>
/// Join record: a user may operate on a given company's data. Membership, not authorization — see
/// docs/multi-company.md ("la empresa activa funciona como contexto y filtro operativo, no como
/// frontera de autorización").
/// </summary>
public sealed class UserCompany
{
    public Guid UserId { get; private set; }
    public Guid CompanyId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }

    private UserCompany()
    {
    }

    internal static UserCompany Create(Guid userId, Guid companyId, DateTimeOffset nowUtc) =>
        new() { UserId = userId, CompanyId = companyId, GrantedAtUtc = nowUtc };
}
