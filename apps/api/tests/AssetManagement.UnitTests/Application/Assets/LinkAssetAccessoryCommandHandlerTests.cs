using AssetManagement.Application.Assets;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Domain.Assets;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.Assets;

public class LinkAssetAccessoryCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    private static Asset CreateAsset(Guid companyId, string folio) => Asset.Create(
        companyId, Guid.NewGuid(), folio, "Dell", "Latitude 5450", null, null, PhysicalCondition.Excellent, Now, null);

    [Fact]
    public async Task Links_the_accessory_to_the_primary_asset()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var primary = CreateAsset(companyId, "ASSET-000001");
        var accessory = CreateAsset(companyId, "ASSET-000002");
        db.Assets.AddRange(primary, accessory);
        await db.SaveChangesAsync();

        var handler = new LinkAssetAccessoryCommandHandler(db, companyContext, new FakeCurrentUserContext(), new FakeClock(Now));
        await handler.Handle(new LinkAssetAccessoryCommand(primary.Id, accessory.Id), CancellationToken.None);

        var reloaded = await db.Assets.SingleAsync(a => a.Id == accessory.Id);
        reloaded.AccessoryOfAssetId.Should().Be(primary.Id);
    }

    [Fact]
    public async Task Rejects_linking_an_asset_to_itself()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var asset = CreateAsset(companyId, "ASSET-000001");
        db.Assets.Add(asset);
        await db.SaveChangesAsync();

        var handler = new LinkAssetAccessoryCommandHandler(db, companyContext, new FakeCurrentUserContext(), new FakeClock(Now));
        var act = () => handler.Handle(new LinkAssetAccessoryCommand(asset.Id, asset.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Rejects_when_the_primary_is_itself_an_accessory_of_another_asset()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var grandparent = CreateAsset(companyId, "ASSET-000001");
        var primary = CreateAsset(companyId, "ASSET-000002");
        primary.LinkAsAccessoryOf(grandparent.Id, Now, null);
        var candidateAccessory = CreateAsset(companyId, "ASSET-000003");
        db.Assets.AddRange(grandparent, primary, candidateAccessory);
        await db.SaveChangesAsync();

        var handler = new LinkAssetAccessoryCommandHandler(db, companyContext, new FakeCurrentUserContext(), new FakeClock(Now));
        var act = () => handler.Handle(new LinkAssetAccessoryCommand(primary.Id, candidateAccessory.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Rejects_when_the_candidate_already_has_its_own_accessories()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var primary = CreateAsset(companyId, "ASSET-000001");
        var candidate = CreateAsset(companyId, "ASSET-000002");
        var candidatesOwnAccessory = CreateAsset(companyId, "ASSET-000003");
        candidatesOwnAccessory.LinkAsAccessoryOf(candidate.Id, Now, null);
        db.Assets.AddRange(primary, candidate, candidatesOwnAccessory);
        await db.SaveChangesAsync();

        var handler = new LinkAssetAccessoryCommandHandler(db, companyContext, new FakeCurrentUserContext(), new FakeClock(Now));
        var act = () => handler.Handle(new LinkAssetAccessoryCommand(primary.Id, candidate.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Rejects_a_primary_asset_from_a_different_company()
    {
        // The global company query filter on Asset hides the other company's row entirely before the
        // handler's own explicit check would run — same "not found, not forbidden" defense-in-depth
        // precedent as CreateAssignmentCommandHandlerTests.
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var primary = CreateAsset(companyId, "ASSET-000001");
        var accessory = CreateAsset(Guid.NewGuid(), "ASSET-000002");
        db.Assets.AddRange(primary, accessory);
        await db.SaveChangesAsync();

        var handler = new LinkAssetAccessoryCommandHandler(db, companyContext, new FakeCurrentUserContext(), new FakeClock(Now));
        var act = () => handler.Handle(new LinkAssetAccessoryCommand(primary.Id, accessory.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Unlink_clears_the_relationship()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var primary = CreateAsset(companyId, "ASSET-000001");
        var accessory = CreateAsset(companyId, "ASSET-000002");
        accessory.LinkAsAccessoryOf(primary.Id, Now, null);
        db.Assets.AddRange(primary, accessory);
        await db.SaveChangesAsync();

        var handler = new UnlinkAssetAccessoryCommandHandler(db, companyContext, new FakeCurrentUserContext(), new FakeClock(Now));
        await handler.Handle(new UnlinkAssetAccessoryCommand(accessory.Id), CancellationToken.None);

        var reloaded = await db.Assets.SingleAsync(a => a.Id == accessory.Id);
        reloaded.AccessoryOfAssetId.Should().BeNull();
    }
}
