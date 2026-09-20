using AssetManagement.Domain.Organization;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.Organization;

/// <summary>Verifies the defense-in-depth global query filter on AppDbContext (see
/// docs/multi-company.md): a query only ever sees rows for companies the caller belongs to, even if a
/// handler's own explicit check were somehow bypassed.</summary>
public class MultiCompanyQueryFilterTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OrgUnits_from_a_company_the_caller_does_not_belong_to_are_invisible()
    {
        var accessibleCompanyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        // Seed both companies' org units using an unrestricted context (simulates data already in the DB).
        var seedContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [accessibleCompanyId, otherCompanyId] };
        using (var seedDb = InMemoryAppDbContextFactory.Create(seedContext, dbName))
        {
            seedDb.OrgUnits.Add(OrgUnit.Create(accessibleCompanyId, typeId, null, "Mío", "MIO", Now));
            seedDb.OrgUnits.Add(OrgUnit.Create(otherCompanyId, typeId, null, "Ajeno", "AJENO", Now));
            await seedDb.SaveChangesAsync();
        }

        var scopedContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [accessibleCompanyId] };
        using var scopedDb = InMemoryAppDbContextFactory.Create(scopedContext, dbName);

        var visible = await scopedDb.OrgUnits.ToListAsync();

        visible.Should().ContainSingle(o => o.Name == "Mío");
    }
}
