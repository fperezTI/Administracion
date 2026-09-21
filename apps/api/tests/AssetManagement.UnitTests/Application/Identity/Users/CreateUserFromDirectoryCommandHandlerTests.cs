using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Identity.Users;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Organization;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.Identity.Users;

public class CreateUserFromDirectoryCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    private static Company CreateCompany() =>
        Company.Create("Contoso S.A. de C.V.", "Contoso", "CON010101AAA", "MXN", "America/Mexico_City", Now);

    [Fact]
    public async Task Creates_the_user_with_the_chosen_role_and_company_access()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var role = Role.Create("Técnico de soporte", null, Now);
        var company = CreateCompany();
        db.Roles.Add(role);
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var handler = new CreateUserFromDirectoryCommandHandler(db, new FakeCurrentUserContext(), new FakeClock(Now));
        var entraObjectId = Guid.NewGuid();

        var userId = await handler.Handle(
            new CreateUserFromDirectoryCommand(entraObjectId, "Grace Hopper", "grace@example.com", role.Id, [company.Id]),
            CancellationToken.None);

        var user = await db.Users.Include(u => u.UserRoles).Include(u => u.UserCompanies).SingleAsync(u => u.Id == userId);
        user.EntraObjectId.Should().Be(entraObjectId);
        user.DisplayName.Should().Be("Grace Hopper");
        user.UserRoles.Should().ContainSingle(ur => ur.RoleId == role.Id);
        user.UserCompanies.Should().ContainSingle(uc => uc.CompanyId == company.Id);
    }

    [Fact]
    public async Task Rejects_a_person_who_already_has_a_local_profile()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var role = Role.Create("Técnico de soporte", null, Now);
        db.Roles.Add(role);
        var entraObjectId = Guid.NewGuid();
        db.Users.Add(User.Provision(entraObjectId, "Grace Hopper", "grace@example.com", Now));
        await db.SaveChangesAsync();

        var handler = new CreateUserFromDirectoryCommandHandler(db, new FakeCurrentUserContext(), new FakeClock(Now));

        var act = () => handler.Handle(
            new CreateUserFromDirectoryCommand(entraObjectId, "Grace Hopper", "grace@example.com", role.Id, []),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Rejects_an_inactive_role()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var role = Role.Create("Rol desactivado", null, Now);
        role.Deactivate();
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var handler = new CreateUserFromDirectoryCommandHandler(db, new FakeCurrentUserContext(), new FakeClock(Now));

        var act = () => handler.Handle(
            new CreateUserFromDirectoryCommand(Guid.NewGuid(), "Grace Hopper", "grace@example.com", role.Id, []),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Rejects_a_company_that_does_not_exist()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var role = Role.Create("Técnico de soporte", null, Now);
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var handler = new CreateUserFromDirectoryCommandHandler(db, new FakeCurrentUserContext(), new FakeClock(Now));

        var act = () => handler.Handle(
            new CreateUserFromDirectoryCommand(Guid.NewGuid(), "Grace Hopper", "grace@example.com", role.Id, [Guid.NewGuid()]),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
