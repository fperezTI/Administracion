using AssetManagement.Domain.SharedKernel;
using AssetManagement.Domain.SparePartsAndConsumables;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.SparePartsAndConsumables;

public class ConsumableTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    private static Consumable CreateConsumable() =>
        Consumable.Create(Guid.NewGuid(), "Tóner HP 58A", "TN-58A", "Pieza", 5, Now, null);

    [Fact]
    public void Create_starts_with_zero_stock()
    {
        var consumable = CreateConsumable();

        consumable.CurrentStock.Should().Be(0);
    }

    [Fact]
    public void ApplyStockMovement_In_increases_stock()
    {
        var consumable = CreateConsumable();

        consumable.ApplyStockMovement(ConsumableStockDirection.In, 10, Now, null);

        consumable.CurrentStock.Should().Be(10);
    }

    [Fact]
    public void ApplyStockMovement_Out_decreases_stock()
    {
        var consumable = CreateConsumable();
        consumable.ApplyStockMovement(ConsumableStockDirection.In, 10, Now, null);

        consumable.ApplyStockMovement(ConsumableStockDirection.Out, 4, Now, null);

        consumable.CurrentStock.Should().Be(6);
    }

    [Fact]
    public void ApplyStockMovement_that_would_go_negative_throws()
    {
        var consumable = CreateConsumable();
        consumable.ApplyStockMovement(ConsumableStockDirection.In, 3, Now, null);

        var act = () => consumable.ApplyStockMovement(ConsumableStockDirection.Out, 4, Now, null);

        act.Should().Throw<DomainException>();
        consumable.CurrentStock.Should().Be(3);
    }

    [Fact]
    public void ApplyStockMovement_with_zero_quantity_throws()
    {
        var consumable = CreateConsumable();

        var act = () => consumable.ApplyStockMovement(ConsumableStockDirection.In, 0, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_negative_minimum_stock()
    {
        var act = () => Consumable.Create(Guid.NewGuid(), "Tóner", null, "Pieza", -1, Now, null);

        act.Should().Throw<DomainException>();
    }
}
