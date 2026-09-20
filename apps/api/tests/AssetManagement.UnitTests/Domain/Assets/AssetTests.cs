using AssetManagement.Domain.Assets;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Assets;

public class AssetTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    private static Asset CreateAsset() => Asset.Create(
        Guid.NewGuid(), Guid.NewGuid(), "ASSET-000001", "Dell", "Latitude 5450", "SN-1", "Laptop de prueba",
        PhysicalCondition.Excellent, Now, Guid.NewGuid());

    [Fact]
    public void Create_starts_in_InWarehouse_status()
    {
        var asset = CreateAsset();

        asset.Status.Should().Be(AssetStatus.InWarehouse);
        asset.CreatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Create_rejects_missing_internal_folio()
    {
        var act = () => Asset.Create(
            Guid.NewGuid(), Guid.NewGuid(), "", "Dell", "Latitude", null, null,
            PhysicalCondition.Good, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IssueTag_rejects_a_second_tag_on_the_same_asset()
    {
        var asset = CreateAsset();
        asset.IssueTag("ASSET-000001", IdentificationTechnology.Qr, Now);

        var act = () => asset.IssueTag("ASSET-000001", IdentificationTechnology.Qr, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ReprintTag_increments_print_count_without_changing_the_code()
    {
        var asset = CreateAsset();
        asset.IssueTag("ASSET-000001", IdentificationTechnology.Qr, Now);

        asset.ReprintTag(Now.AddDays(1));

        asset.Tag!.Code.Should().Be("ASSET-000001");
        asset.Tag.PrintCount.Should().Be(2);
    }

    [Fact]
    public void ReprintTag_without_a_tag_throws()
    {
        var asset = CreateAsset();

        var act = () => asset.ReprintTag(Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetCustomFieldValues_replaces_the_entire_set()
    {
        var asset = CreateAsset();
        var fieldA = Guid.NewGuid();
        var fieldB = Guid.NewGuid();
        asset.SetCustomFieldValues([(fieldA, "16GB")]);

        asset.SetCustomFieldValues([(fieldB, "512GB")]);

        asset.CustomFieldValues.Should().ContainSingle(v => v.CustomFieldDefinitionId == fieldB && v.Value == "512GB");
    }

    [Fact]
    public void UpdateProfile_rejects_an_empty_brand()
    {
        var asset = CreateAsset();

        var act = () => asset.UpdateProfile("", "Model", null, null, null, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ChangeStatus_allows_a_valid_transition()
    {
        var asset = CreateAsset();

        asset.ChangeStatus(AssetStatus.Reserved, Now, null);

        asset.Status.Should().Be(AssetStatus.Reserved);
    }

    [Fact]
    public void ChangeStatus_rejects_a_transition_the_state_machine_does_not_allow()
    {
        var asset = CreateAsset();

        var act = () => asset.ChangeStatus(AssetStatus.Sold, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CompleteCrossCompanyTransfer_changes_company_and_folio_without_touching_the_tag_code()
    {
        var asset = CreateAsset();
        asset.IssueTag("GLOBAL-TAG-CODE", IdentificationTechnology.Qr, Now);
        asset.ChangeStatus(AssetStatus.InTransit, Now, null);
        var newCompanyId = Guid.NewGuid();

        asset.CompleteCrossCompanyTransfer(newCompanyId, "ASSET-000099", Now.AddDays(1), null);

        asset.CompanyId.Should().Be(newCompanyId);
        asset.InternalFolio.Should().Be("ASSET-000099");
        asset.Status.Should().Be(AssetStatus.InWarehouse);
        asset.Tag!.Code.Should().Be("GLOBAL-TAG-CODE", "AssetTag.Code is the one identity that survives a transfer, see ADR 0004/0007");
    }

    [Fact]
    public void CompleteCrossCompanyTransfer_clears_the_previous_companys_org_unit()
    {
        var asset = CreateAsset();
        asset.MoveToOrgUnit(Guid.NewGuid(), Now, null);
        asset.ChangeStatus(AssetStatus.InTransit, Now, null);

        asset.CompleteCrossCompanyTransfer(Guid.NewGuid(), "ASSET-000099", Now, null);

        asset.CurrentOrgUnitId.Should().BeNull();
    }

    [Fact]
    public void CompleteCrossCompanyTransfer_rejects_an_asset_not_in_transit()
    {
        var asset = CreateAsset();

        var act = () => asset.CompleteCrossCompanyTransfer(Guid.NewGuid(), "ASSET-000099", Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CompleteCrossCompanyTransfer_rejects_an_empty_folio()
    {
        var asset = CreateAsset();
        asset.ChangeStatus(AssetStatus.InTransit, Now, null);

        var act = () => asset.CompleteCrossCompanyTransfer(Guid.NewGuid(), " ", Now, null);

        act.Should().Throw<DomainException>();
    }
}
