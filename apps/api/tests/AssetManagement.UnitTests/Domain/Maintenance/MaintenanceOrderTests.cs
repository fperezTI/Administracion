using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Maintenance;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Maintenance;

public class MaintenanceOrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    private static MaintenanceOrder OpenOrder(IReadOnlyList<string>? checklistItems = null) =>
        MaintenanceOrder.Open(
            Guid.NewGuid(), Guid.NewGuid(), "MAINT-000001", MaintenanceOrderType.Corrective, "No enciende",
            checklistItems is null ? null : Guid.NewGuid(), checklistItems is null ? null : 1, checklistItems, Now, null);

    [Fact]
    public void Open_starts_in_Open_status()
    {
        var order = OpenOrder();

        order.Status.Should().Be(MaintenanceOrderStatus.Open);
    }

    [Fact]
    public void Open_rejects_empty_description()
    {
        var act = () => MaintenanceOrder.Open(
            Guid.NewGuid(), Guid.NewGuid(), "MAINT-000001", MaintenanceOrderType.Corrective, "  ", null, null, null, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Open_snapshots_checklist_items_as_pending_results()
    {
        var order = OpenOrder(["Revisar batería", "Revisar pantalla"]);

        order.ChecklistResults.Should().HaveCount(2);
        order.ChecklistResults.Should().OnlyContain(r => !r.IsCompleted);
        order.ChecklistResults.Select(r => r.ItemText).Should().BeEquivalentTo(["Revisar batería", "Revisar pantalla"]);
    }

    [Fact]
    public void Close_with_InWarehouse_result_succeeds()
    {
        var order = OpenOrder();

        order.Close(AssetStatus.InWarehouse, "Se reemplazó la fuente de poder.", null, Now.AddHours(2), null);

        order.Status.Should().Be(MaintenanceOrderStatus.Closed);
        order.ResultStatus.Should().Be(AssetStatus.InWarehouse);
        order.ClosedAtUtc.Should().Be(Now.AddHours(2));
    }

    [Fact]
    public void Close_rejects_PendingDecommission_as_a_result()
    {
        var order = OpenOrder();

        var act = () => order.Close(AssetStatus.PendingDecommission, "No se pudo reparar.", null, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Close_rejects_empty_result_notes()
    {
        var order = OpenOrder();

        var act = () => order.Close(AssetStatus.Damaged, "  ", null, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Close_twice_throws()
    {
        var order = OpenOrder();
        order.Close(AssetStatus.InWarehouse, "Reparado.", null, Now, null);

        var act = () => order.Close(AssetStatus.InWarehouse, "Reparado de nuevo.", null, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Close_records_checklist_item_results_by_index()
    {
        var order = OpenOrder(["Revisar batería", "Revisar pantalla"]);
        var results = new Dictionary<int, (bool IsCompleted, string? Notes)> { [0] = (true, "OK"), [1] = (false, "Rota") };

        order.Close(AssetStatus.Damaged, "Pantalla rota, no se pudo reparar en sitio.", results, Now, null);

        order.ChecklistResults.Single(r => r.ItemIndex == 0).IsCompleted.Should().BeTrue();
        order.ChecklistResults.Single(r => r.ItemIndex == 1).IsCompleted.Should().BeFalse();
        order.ChecklistResults.Single(r => r.ItemIndex == 1).Notes.Should().Be("Rota");
    }
}
