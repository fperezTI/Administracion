using AssetManagement.Domain.Organization;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Organization;

public class OrgUnitTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MoveTo_rejects_becoming_its_own_parent()
    {
        var orgUnit = OrgUnit.Create(Guid.NewGuid(), Guid.NewGuid(), null, "Almacén Central", "ALM-01", Now);

        var act = () => orgUnit.MoveTo(orgUnit.Id);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MoveTo_a_different_parent_updates_ParentOrgUnitId()
    {
        var orgUnit = OrgUnit.Create(Guid.NewGuid(), Guid.NewGuid(), null, "Almacén Central", "ALM-01", Now);
        var newParentId = Guid.NewGuid();

        orgUnit.MoveTo(newParentId);

        orgUnit.ParentOrgUnitId.Should().Be(newParentId);
    }

    [Fact]
    public void MoveTo_null_detaches_from_its_parent()
    {
        var parentId = Guid.NewGuid();
        var orgUnit = OrgUnit.Create(Guid.NewGuid(), Guid.NewGuid(), parentId, "Almacén Central", "ALM-01", Now);

        orgUnit.MoveTo(null);

        orgUnit.ParentOrgUnitId.Should().BeNull();
    }

    [Fact]
    public void Rename_rejects_an_empty_code()
    {
        var orgUnit = OrgUnit.Create(Guid.NewGuid(), Guid.NewGuid(), null, "Almacén Central", "ALM-01", Now);

        var act = () => orgUnit.Rename("Almacén Central", string.Empty);

        act.Should().Throw<DomainException>();
    }
}
