using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Organization;

/// <summary>
/// Configurable catalog of organizational unit kinds (business unit, department, warehouse, data
/// center, project, ...). See ADR 0002 for why a single generic OrgUnit + this catalog replaces 11
/// near-identical tables.
/// </summary>
public sealed class OrgUnitType : Entity<Guid>
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    private OrgUnitType()
    {
    }

    public OrgUnitType(Guid id, string code, string name)
        : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
    }
}
