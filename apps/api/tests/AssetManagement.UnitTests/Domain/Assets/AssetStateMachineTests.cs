using AssetManagement.Domain.Assets;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Assets;

public class AssetStateMachineTests
{
    [Theory]
    [InlineData(AssetStatus.InWarehouse, AssetStatus.Assigned)]
    [InlineData(AssetStatus.Assigned, AssetStatus.InWarehouse)]
    [InlineData(AssetStatus.Assigned, AssetStatus.Lost)]
    [InlineData(AssetStatus.InMaintenance, AssetStatus.UnderWarranty)]
    [InlineData(AssetStatus.PendingDecommission, AssetStatus.Decommissioned)]
    [InlineData(AssetStatus.PendingDecommission, AssetStatus.InWarehouse)]
    [InlineData(AssetStatus.Decommissioned, AssetStatus.Sold)]
    public void CanTransition_allows_documented_transitions(AssetStatus from, AssetStatus to)
    {
        AssetStateMachine.CanTransition(from, to).Should().BeTrue();
    }

    [Theory]
    [InlineData(AssetStatus.Damaged, AssetStatus.Assigned)]
    [InlineData(AssetStatus.Sold, AssetStatus.InWarehouse)]
    [InlineData(AssetStatus.Decommissioned, AssetStatus.InWarehouse)]
    [InlineData(AssetStatus.Lost, AssetStatus.InWarehouse)]
    public void CanTransition_rejects_undocumented_transitions(AssetStatus from, AssetStatus to)
    {
        AssetStateMachine.CanTransition(from, to).Should().BeFalse();
    }

    [Fact]
    public void EnsureCanTransition_throws_for_an_invalid_transition()
    {
        var act = () => AssetStateMachine.EnsureCanTransition(AssetStatus.Sold, AssetStatus.InWarehouse);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Terminal_states_have_no_outgoing_transitions()
    {
        foreach (var terminal in new[] { AssetStatus.Sold, AssetStatus.Donated, AssetStatus.Destroyed })
        {
            foreach (AssetStatus candidate in Enum.GetValues<AssetStatus>())
            {
                AssetStateMachine.CanTransition(terminal, candidate).Should().BeFalse();
            }
        }
    }
}
