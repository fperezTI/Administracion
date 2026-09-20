using AssetManagement.Domain.Identity;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Identity;

public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Provision_creates_an_active_user_with_the_given_profile()
    {
        var entraObjectId = Guid.NewGuid();

        var user = User.Provision(entraObjectId, "Ada Lovelace", "ada@example.com", Now);

        user.EntraObjectId.Should().Be(entraObjectId);
        user.DisplayName.Should().Be("Ada Lovelace");
        user.Email.Should().Be("ada@example.com");
        user.IsActive.Should().BeTrue();
        user.CreatedAtUtc.Should().Be(Now);
        user.LastLoginAtUtc.Should().BeNull();
    }

    [Fact]
    public void Provision_rejects_an_empty_Entra_object_id()
    {
        var act = () => User.Provision(Guid.Empty, "Ada Lovelace", "ada@example.com", Now);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("", "ada@example.com")]
    [InlineData("Ada Lovelace", "")]
    public void Provision_rejects_missing_name_or_email(string displayName, string email)
    {
        var act = () => User.Provision(Guid.NewGuid(), displayName, email, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AssignRole_is_idempotent_for_the_same_role()
    {
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        var roleId = Guid.NewGuid();

        user.AssignRole(roleId, assignedByUserId: null, Now);
        user.AssignRole(roleId, assignedByUserId: null, Now);

        user.UserRoles.Should().ContainSingle();
    }

    [Fact]
    public void RemoveRole_removes_only_the_specified_role()
    {
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        var keptRoleId = Guid.NewGuid();
        var removedRoleId = Guid.NewGuid();
        user.AssignRole(keptRoleId, null, Now);
        user.AssignRole(removedRoleId, null, Now);

        user.RemoveRole(removedRoleId);

        user.UserRoles.Should().ContainSingle(ur => ur.RoleId == keptRoleId);
    }

    [Fact]
    public void Deactivate_then_Activate_round_trips_IsActive()
    {
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);

        user.Deactivate();
        user.IsActive.Should().BeFalse();

        user.Activate();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Anonymize_scrubs_display_name_and_email_and_deactivates()
    {
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);

        user.Anonymize(Now);

        user.DisplayName.Should().NotBe("Ada Lovelace");
        user.Email.Should().NotBe("ada@example.com");
        user.IsActive.Should().BeFalse();
        user.AnonymizedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Anonymize_twice_throws()
    {
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        user.Anonymize(Now);

        var act = () => user.Anonymize(Now);

        act.Should().Throw<DomainException>();
    }
}
