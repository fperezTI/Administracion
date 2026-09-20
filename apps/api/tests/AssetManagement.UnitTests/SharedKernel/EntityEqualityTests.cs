using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.SharedKernel;

file sealed class SampleEntity(Guid id) : Entity<Guid>(id);

file sealed class OtherEntity(Guid id) : Entity<Guid>(id);

public class EntityEqualityTests
{
    [Fact]
    public void Entities_with_the_same_id_and_type_are_equal()
    {
        var id = Guid.NewGuid();
        var first = new SampleEntity(id);
        var second = new SampleEntity(id);

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Entities_with_different_ids_are_not_equal()
    {
        var first = new SampleEntity(Guid.NewGuid());
        var second = new SampleEntity(Guid.NewGuid());

        first.Should().NotBe(second);
    }

    [Fact]
    public void Entities_of_different_types_with_the_same_id_are_not_equal()
    {
        var id = Guid.NewGuid();
        var sample = new SampleEntity(id);
        var other = new OtherEntity(id);

        sample.Equals(other).Should().BeFalse();
    }
}
