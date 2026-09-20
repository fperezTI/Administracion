using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Organization.OrgUnits;
using AssetManagement.Domain.Organization;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Application.Organization;

public class MoveOrgUnitCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Rejects_a_move_that_would_create_a_cycle()
    {
        var companyId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);

        var root = OrgUnit.Create(companyId, typeId, null, "Dirección", "DIR", Now);
        var child = OrgUnit.Create(companyId, typeId, root.Id, "Gerencia", "GER", Now);
        var grandchild = OrgUnit.Create(companyId, typeId, child.Id, "Departamento", "DEP", Now);
        db.OrgUnits.AddRange(root, child, grandchild);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new MoveOrgUnitCommandHandler(db);

        // Moving the root under its own grandchild would create a cycle.
        var act = () => handler.Handle(new MoveOrgUnitCommand(root.Id, grandchild.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Allows_moving_to_a_valid_new_parent_in_the_same_company()
    {
        var companyId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);

        var branchA = OrgUnit.Create(companyId, typeId, null, "Sucursal A", "SUC-A", Now);
        var branchB = OrgUnit.Create(companyId, typeId, null, "Sucursal B", "SUC-B", Now);
        var warehouse = OrgUnit.Create(companyId, typeId, branchA.Id, "Almacén", "ALM", Now);
        db.OrgUnits.AddRange(branchA, branchB, warehouse);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new MoveOrgUnitCommandHandler(db);
        await handler.Handle(new MoveOrgUnitCommand(warehouse.Id, branchB.Id), CancellationToken.None);

        (await db.OrgUnits.FindAsync(warehouse.Id))!.ParentOrgUnitId.Should().Be(branchB.Id);
    }
}
