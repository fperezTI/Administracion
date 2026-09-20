using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Security;
using AssetManagement.Application.Search;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Inventory;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Application.Search;

public class GlobalSearchQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    private static GlobalSearchQueryHandler CreateHandler(
        AppDbContext db, FakeCurrentCompanyContext companyContext, HashSet<string> grantedPermissions) =>
        new(db, new FakeCurrentUserContext(), companyContext, new FakePermissionChecker { GrantedPermissionCodes = grantedPermissions });

    [Fact]
    public async Task Asset_result_appears_only_when_the_caller_has_Assets_Read()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);

        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);
        db.AssetCategories.Add(category);
        db.Assets.Add(Asset.Create(companyId, category.Id, "ASSET-SRCH-001", "Dell", "Latitude", null, null, PhysicalCondition.Good, Now, null));
        await db.SaveChangesAsync();

        var withPermission = CreateHandler(db, companyContext, [PermissionCatalog.Assets.Read]);
        var resultsWith = await withPermission.Handle(new GlobalSearchQuery("SRCH", companyId), CancellationToken.None);
        resultsWith.Should().ContainSingle(r => r.EntityType == "Asset" && r.Title == "ASSET-SRCH-001");

        var withoutPermission = CreateHandler(db, companyContext, []);
        var resultsWithout = await withoutPermission.Handle(new GlobalSearchQuery("SRCH", companyId), CancellationToken.None);
        resultsWithout.Should().BeEmpty();
    }

    [Fact]
    public async Task Term_shorter_than_two_characters_returns_empty_without_error()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var handler = CreateHandler(db, companyContext, [PermissionCatalog.Assets.Read]);

        var results = await handler.Handle(new GlobalSearchQuery("A", companyId), CancellationToken.None);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Unauthenticated_caller_throws_ForbiddenAccessException()
    {
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var handler = new GlobalSearchQueryHandler(
            db, new FakeCurrentUserContext { IsAuthenticated = false, UserId = null }, companyContext,
            new FakePermissionChecker());

        var act = () => handler.Handle(new GlobalSearchQuery("laptop", null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Movement_result_links_to_its_asset_not_to_itself()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);

        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);
        db.AssetCategories.Add(category);
        var asset = Asset.Create(companyId, category.Id, "ASSET-000001", "Dell", "Latitude", null, null, PhysicalCondition.Good, Now, null);
        db.Assets.Add(asset);
        var movement = Movement.Create(companyId, asset.Id, MovementType.Relocation, "MOV-REL-000042", startsCompleted: true, null, null, null, null, null, Now, null);
        db.Movements.Add(movement);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, companyContext, [PermissionCatalog.Movements.Read]);
        var results = await handler.Handle(new GlobalSearchQuery("MOV-REL", companyId), CancellationToken.None);

        var result = results.Should().ContainSingle().Subject;
        result.EntityType.Should().Be("Movement");
        result.EntityId.Should().Be(movement.Id);
        result.LinkEntityType.Should().Be("Asset");
        result.LinkEntityId.Should().Be(asset.Id);
    }

    [Fact]
    public async Task Company_scoped_search_excludes_results_from_other_companies()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyA, companyB] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);

        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);
        db.AssetCategories.Add(category);
        db.Assets.Add(Asset.Create(companyA, category.Id, "ASSET-ONLY-A", "Dell", "Latitude", null, null, PhysicalCondition.Good, Now, null));
        db.Assets.Add(Asset.Create(companyB, category.Id, "ASSET-ONLY-B", "Dell", "Latitude", null, null, PhysicalCondition.Good, Now, null));
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, companyContext, [PermissionCatalog.Assets.Read]);
        var scopedToA = await handler.Handle(new GlobalSearchQuery("ASSET-ONLY", companyA), CancellationToken.None);
        var consolidated = await handler.Handle(new GlobalSearchQuery("ASSET-ONLY", null), CancellationToken.None);

        scopedToA.Should().ContainSingle(r => r.Title == "ASSET-ONLY-A");
        consolidated.Should().HaveCount(2);
    }
}
