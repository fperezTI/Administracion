using AssetManagement.Application.Assets;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Domain.Assets;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.Assets;

public class CreateAssetCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Creates_the_asset_in_InWarehouse_with_a_generated_folio_and_tag()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);
        db.AssetCategories.Add(category);
        await db.SaveChangesAsync();

        var handler = new CreateAssetCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));

        var result = await handler.Handle(
            new CreateAssetCommand(
                companyId, category.Id, "Dell", "Latitude 5450", "SN-1", "Laptop nueva",
                PhysicalCondition.Excellent, null, null, null),
            CancellationToken.None);

        result.InternalFolio.Should().Be("ASSET-000001");
        result.TagCode.Should().NotBeNullOrWhiteSpace();
        result.TagCode.Should().NotBe(result.InternalFolio, "the tag code must be globally unique, unlike the per-company folio");

        var asset = await db.Assets.Include(a => a.Tag).SingleAsync(a => a.Id == result.AssetId);
        asset.Status.Should().Be(AssetStatus.InWarehouse);
        asset.Tag!.Technology.Should().Be(IdentificationTechnology.Qr);
    }

    [Fact]
    public async Task Rejects_creation_for_a_company_the_caller_does_not_belong_to()
    {
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var handler = new CreateAssetCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));

        var act = () => handler.Handle(
            new CreateAssetCommand(
                Guid.NewGuid(), Guid.NewGuid(), "Dell", "Latitude", null, null, PhysicalCondition.Good, null, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Rejects_creation_when_a_required_custom_field_is_missing()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);
        category.AddCustomField("Número de serie de RAM", "RAM_SN", CustomFieldDataType.Text, isRequired: true, options: null);
        db.AssetCategories.Add(category);
        await db.SaveChangesAsync();

        var handler = new CreateAssetCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));

        var act = () => handler.Handle(
            new CreateAssetCommand(
                companyId, category.Id, "Dell", "Latitude", null, null, PhysicalCondition.Good, null, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Rejects_creation_for_a_deactivated_category()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);
        category.Deactivate();
        db.AssetCategories.Add(category);
        await db.SaveChangesAsync();

        var handler = new CreateAssetCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));

        var act = () => handler.Handle(
            new CreateAssetCommand(
                companyId, category.Id, "Dell", "Latitude", null, null, PhysicalCondition.Good, null, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
