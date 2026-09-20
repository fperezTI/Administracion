using AssetManagement.Domain.SharedKernel;
using AssetManagement.Domain.SparePartsAndConsumables;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.SparePartsAndConsumables;

public class SparePartTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    private static SparePart CreatePart(Guid? warehouseId = null) =>
        SparePart.Create(Guid.NewGuid(), "Memoria RAM 16GB", "RAM-16GB", "SN-12345", warehouseId ?? Guid.NewGuid(), Now, null);

    [Fact]
    public void Create_starts_InStock()
    {
        var part = CreatePart();

        part.Status.Should().Be(SparePartStatus.InStock);
        part.Installations.Should().BeEmpty();
    }

    [Fact]
    public void Create_rejects_empty_serial_number()
    {
        var act = () => SparePart.Create(Guid.NewGuid(), "Memoria RAM", null, "  ", Guid.NewGuid(), Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Install_moves_to_Installed_and_opens_an_installation_record()
    {
        var part = CreatePart();
        var assetId = Guid.NewGuid();

        part.Install(assetId, null, Now, null);

        part.Status.Should().Be(SparePartStatus.Installed);
        part.CurrentAssetId.Should().Be(assetId);
        part.CurrentWarehouseOrgUnitId.Should().BeNull();
        part.Installations.Should().ContainSingle(i => i.AssetId == assetId && i.RemovedAtUtc == null);
    }

    [Fact]
    public void Install_when_already_installed_throws()
    {
        var part = CreatePart();
        part.Install(Guid.NewGuid(), null, Now, null);

        var act = () => part.Install(Guid.NewGuid(), null, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Uninstall_closes_the_open_installation_and_returns_to_stock()
    {
        var part = CreatePart();
        part.Install(Guid.NewGuid(), null, Now, null);
        var warehouseId = Guid.NewGuid();

        part.Uninstall(warehouseId, Now.AddDays(1), null);

        part.Status.Should().Be(SparePartStatus.InStock);
        part.CurrentAssetId.Should().BeNull();
        part.CurrentWarehouseOrgUnitId.Should().Be(warehouseId);
        part.Installations.Single().RemovedAtUtc.Should().Be(Now.AddDays(1));
    }

    [Fact]
    public void Uninstall_when_in_stock_throws()
    {
        var part = CreatePart();

        var act = () => part.Uninstall(Guid.NewGuid(), Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkDisposed_from_stock_succeeds()
    {
        var part = CreatePart();

        part.MarkDisposed(Now, null);

        part.Status.Should().Be(SparePartStatus.Disposed);
    }

    [Fact]
    public void MarkDisposed_while_installed_throws()
    {
        var part = CreatePart();
        part.Install(Guid.NewGuid(), null, Now, null);

        var act = () => part.MarkDisposed(Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Full_install_uninstall_cycle_keeps_history()
    {
        var part = CreatePart();
        var firstAsset = Guid.NewGuid();
        var secondAsset = Guid.NewGuid();

        part.Install(firstAsset, null, Now, null);
        part.Uninstall(Guid.NewGuid(), Now.AddDays(1), null);
        part.Install(secondAsset, null, Now.AddDays(2), null);

        part.Installations.Should().HaveCount(2);
        part.Installations.Count(i => i.RemovedAtUtc is not null).Should().Be(1);
        part.CurrentAssetId.Should().Be(secondAsset);
    }
}
