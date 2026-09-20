using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.SharedKernel;

file sealed class Money(decimal amount, string currency) : ValueObject
{
    public decimal Amount { get; } = amount;
    public string Currency { get; } = currency;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

public class ValueObjectEqualityTests
{
    [Fact]
    public void Value_objects_with_the_same_components_are_equal()
    {
        var first = new Money(100m, "MXN");
        var second = new Money(100m, "MXN");

        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Value_objects_with_different_components_are_not_equal()
    {
        var first = new Money(100m, "MXN");
        var second = new Money(100m, "USD");

        first.Should().NotBe(second);
    }
}
