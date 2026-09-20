using AssetManagement.Domain.Approvals;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Approvals;

public class ApprovalFlowDefinitionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_rejects_zero_approver_roles()
    {
        var act = () => ApprovalFlowDefinition.Create("asset.decommission", null, [], 1, ApprovalMode.Parallel, false, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_allows_parallel_required_approvals_above_the_role_count()
    {
        // Several different people can hold the same role — roles=[Manager], requiredApprovals=2
        // legitimately asks for two distinct managers to each approve.
        var flow = ApprovalFlowDefinition.Create(
            "asset.decommission", null, [Guid.NewGuid()], 2, ApprovalMode.Parallel, false, Now, null);

        flow.RequiredApprovals.Should().Be(2);
    }

    [Fact]
    public void Create_rejects_a_required_approvals_count_below_one()
    {
        var act = () => ApprovalFlowDefinition.Create(
            "asset.decommission", null, [Guid.NewGuid()], 0, ApprovalMode.Parallel, false, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_sequential_mode_when_required_approvals_does_not_match_role_count()
    {
        var act = () => ApprovalFlowDefinition.Create(
            "asset.decommission", null, [Guid.NewGuid(), Guid.NewGuid()], 1, ApprovalMode.Sequential, false, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_accepts_a_valid_sequential_flow()
    {
        var roleIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var flow = ApprovalFlowDefinition.Create("asset.decommission", null, roleIds, 2, ApprovalMode.Sequential, true, Now, null);

        flow.ApproverRoleIds.Should().Equal(roleIds);
        flow.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_then_Activate_round_trips()
    {
        var flow = ApprovalFlowDefinition.Create(
            "asset.decommission", null, [Guid.NewGuid()], 1, ApprovalMode.Parallel, false, Now, null);

        flow.Deactivate(Now, null);
        flow.IsActive.Should().BeFalse();

        flow.Activate(Now, null);
        flow.IsActive.Should().BeTrue();
    }
}
