using AssetManagement.Application.Reports;
using AssetManagement.Domain.SparePartsAndConsumables;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Application.Reports;

public class GetLowStockConsumablesQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Includes_at_or_below_minimum_excludes_above_minimum_and_undefined_minimum()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);

        var belowMinimum = Consumable.Create(companyId, "Toner HP 26A", "TNR-26A", "Pieza", 10, Now, null);
        belowMinimum.ApplyStockMovement(ConsumableStockDirection.In, 5, Now, null);

        var atMinimum = Consumable.Create(companyId, "Cable HDMI", "CBL-HDMI", "Pieza", 10, Now, null);
        atMinimum.ApplyStockMovement(ConsumableStockDirection.In, 10, Now, null);

        var aboveMinimum = Consumable.Create(companyId, "Mouse USB", "MSE-USB", "Pieza", 10, Now, null);
        aboveMinimum.ApplyStockMovement(ConsumableStockDirection.In, 20, Now, null);

        var noMinimum = Consumable.Create(companyId, "Teclado USB", "KBD-USB", "Pieza", null, Now, null);
        noMinimum.ApplyStockMovement(ConsumableStockDirection.In, 1, Now, null);

        db.Consumables.AddRange(belowMinimum, atMinimum, aboveMinimum, noMinimum);
        await db.SaveChangesAsync();

        var handler = new GetLowStockConsumablesQueryHandler(db, companyContext);
        var result = await handler.Handle(new GetLowStockConsumablesQuery(companyId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(r => r.ConsumableId == belowMinimum.Id && r.Shortfall == 5);
        result.Should().Contain(r => r.ConsumableId == atMinimum.Id && r.Shortfall == 0);
        result.Should().NotContain(r => r.ConsumableId == aboveMinimum.Id);
        result.Should().NotContain(r => r.ConsumableId == noMinimum.Id);
        result.Should().BeInDescendingOrder(r => r.Shortfall);
    }
}
