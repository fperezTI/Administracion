using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Organization;

/// <summary>
/// A node in a company's organizational hierarchy (business unit, department, warehouse, ...). See
/// ADR 0002. This aggregate protects only its own local invariant (it cannot parent itself); the
/// tree-wide invariant that a move must not create a cycle spans multiple aggregate instances and is
/// therefore enforced by the application layer (MoveOrgUnitCommandHandler), not here.
/// </summary>
public sealed class OrgUnit : AggregateRoot<Guid>
{
    public Guid CompanyId { get; private set; }
    public Guid OrgUnitTypeId { get; private set; }
    public Guid? ParentOrgUnitId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private OrgUnit()
    {
    }

    private OrgUnit(
        Guid id,
        Guid companyId,
        Guid orgUnitTypeId,
        Guid? parentOrgUnitId,
        string name,
        string code,
        DateTimeOffset nowUtc)
        : base(id)
    {
        CompanyId = companyId;
        OrgUnitTypeId = orgUnitTypeId;
        ParentOrgUnitId = parentOrgUnitId;
        Name = name;
        Code = code;
        CreatedAtUtc = nowUtc;
        IsActive = true;
    }

    public static OrgUnit Create(
        Guid companyId,
        Guid orgUnitTypeId,
        Guid? parentOrgUnitId,
        string name,
        string code,
        DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("El nombre de la unidad organizacional es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("El código de la unidad organizacional es obligatorio.");
        }

        return new OrgUnit(Guid.NewGuid(), companyId, orgUnitTypeId, parentOrgUnitId, name.Trim(), code.Trim(), nowUtc);
    }

    public void Rename(string name, string code)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("El nombre de la unidad organizacional es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("El código de la unidad organizacional es obligatorio.");
        }

        Name = name.Trim();
        Code = code.Trim();
    }

    public void MoveTo(Guid? newParentOrgUnitId)
    {
        if (newParentOrgUnitId.HasValue && newParentOrgUnitId.Value == Id)
        {
            throw new DomainException("Una unidad organizacional no puede ser su propio padre.");
        }

        ParentOrgUnitId = newParentOrgUnitId;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
