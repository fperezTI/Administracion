using AssetManagement.Domain.Identity;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Identity;

public class RoleTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_rejects_an_empty_name()
    {
        var act = () => Role.Create(string.Empty, "desc", Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Duplicate_copies_the_current_permission_set_into_a_new_role()
    {
        var source = Role.Create("Almacenista", "Rol de almacén", Now);
        var permissionIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        source.SetPermissions(permissionIds);

        var copy = source.Duplicate("Almacenista (copia)", Now);

        copy.Id.Should().NotBe(source.Id);
        copy.Name.Should().Be("Almacenista (copia)");
        copy.RolePermissions.Select(rp => rp.PermissionId).Should().BeEquivalentTo(permissionIds);
    }

    [Fact]
    public void SetPermissions_replaces_the_entire_matrix_and_deduplicates()
    {
        var role = Role.Create("Auditor", null, Now);
        var permissionId = Guid.NewGuid();
        role.SetPermissions([permissionId, permissionId, Guid.NewGuid()]);

        var newPermissionId = Guid.NewGuid();
        role.SetPermissions([newPermissionId]);

        role.RolePermissions.Should().ContainSingle(rp => rp.PermissionId == newPermissionId);
    }

    [Fact]
    public void Deactivate_does_not_remove_the_role_or_its_permissions()
    {
        var role = Role.Create("Auditor", null, Now);
        role.SetPermissions([Guid.NewGuid()]);

        role.Deactivate();

        role.IsActive.Should().BeFalse();
        role.RolePermissions.Should().ContainSingle();
    }
}
