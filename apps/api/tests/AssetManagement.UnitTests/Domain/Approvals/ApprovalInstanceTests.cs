using AssetManagement.Domain.Approvals;
using AssetManagement.Domain.Approvals.Events;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Approvals;

public class ApprovalInstanceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid RequesterId = Guid.NewGuid();
    private static readonly Guid ContextId = Guid.NewGuid();

    private static ApprovalFlowDefinition ParallelFlow(int requiredApprovals, params Guid[] roleIds) =>
        ApprovalFlowDefinition.Create("asset.decommission", null, roleIds, requiredApprovals, ApprovalMode.Parallel, false, Now, null);

    private static ApprovalFlowDefinition SequentialFlow(params Guid[] roleIds) =>
        ApprovalFlowDefinition.Create("asset.decommission", null, roleIds, roleIds.Length, ApprovalMode.Sequential, false, Now, null);

    private static ApprovalInstance CreateInstance(ApprovalFlowDefinition flow) =>
        ApprovalInstance.Create(flow, CompanyId, "AssetDecommission", ContextId, RequesterId, "Justificación", Now, RequesterId);

    [Fact]
    public void Create_requires_a_comment_when_the_flow_demands_one()
    {
        var flow = ApprovalFlowDefinition.Create(
            "asset.decommission", null, [Guid.NewGuid()], 1, ApprovalMode.Parallel, requiresComment: true, Now, null);

        var act = () => ApprovalInstance.Create(flow, CompanyId, "AssetDecommission", ContextId, RequesterId, null, Now, RequesterId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_raises_ApprovalRequested_for_notifications()
    {
        var roleId = Guid.NewGuid();

        var instance = CreateInstance(ParallelFlow(1, roleId));

        instance.DomainEvents.Should().ContainSingle();
        var raised = instance.DomainEvents.Single().Should().BeOfType<ApprovalRequested>().Subject;
        raised.ApproverRoleIds.Should().BeEquivalentTo([roleId]);
        raised.ContextId.Should().Be(ContextId);
    }

    [Fact]
    public void Decide_by_the_requester_is_forbidden_self_approval()
    {
        var roleId = Guid.NewGuid();
        var instance = CreateInstance(ParallelFlow(1, roleId));

        var act = () => instance.Decide(RequesterId, [roleId], ApprovalStepDecision.Approved, null, Now, RequesterId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Decide_by_someone_without_an_eligible_role_is_rejected()
    {
        var instance = CreateInstance(ParallelFlow(1, Guid.NewGuid()));
        var approverId = Guid.NewGuid();

        var act = () => instance.Decide(approverId, [Guid.NewGuid()], ApprovalStepDecision.Approved, null, Now, approverId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Decide_twice_by_the_same_person_is_rejected()
    {
        var roleId = Guid.NewGuid();
        var instance = CreateInstance(ParallelFlow(2, roleId));
        var approverId = Guid.NewGuid();
        instance.Decide(approverId, [roleId], ApprovalStepDecision.Approved, null, Now, approverId);

        var act = () => instance.Decide(approverId, [roleId], ApprovalStepDecision.Approved, null, Now, approverId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rejecting_without_a_comment_is_rejected()
    {
        var roleId = Guid.NewGuid();
        var instance = CreateInstance(ParallelFlow(1, roleId));
        var approverId = Guid.NewGuid();

        var act = () => instance.Decide(approverId, [roleId], ApprovalStepDecision.Rejected, null, Now, approverId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Parallel_mode_completes_once_the_required_approval_count_is_reached_in_any_order()
    {
        var roleA = Guid.NewGuid();
        var roleB = Guid.NewGuid();
        var instance = CreateInstance(ParallelFlow(2, roleA, roleB));
        var approver1 = Guid.NewGuid();
        var approver2 = Guid.NewGuid();

        // Second listed role decides first — parallel mode allows any order.
        instance.Decide(approver1, [roleB], ApprovalStepDecision.Approved, null, Now, approver1);
        instance.Status.Should().Be(ApprovalInstanceStatus.Pending);

        instance.Decide(approver2, [roleA], ApprovalStepDecision.Approved, null, Now, approver2);

        instance.Status.Should().Be(ApprovalInstanceStatus.Approved);
        instance.DomainEvents.Should().ContainSingle(e => e is ApprovalCompleted);
    }

    [Fact]
    public void Sequential_mode_rejects_a_later_role_deciding_out_of_order()
    {
        var roleA = Guid.NewGuid();
        var roleB = Guid.NewGuid();
        var instance = CreateInstance(SequentialFlow(roleA, roleB));
        var approver = Guid.NewGuid();

        var act = () => instance.Decide(approver, [roleB], ApprovalStepDecision.Approved, null, Now, approver);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Sequential_mode_completes_once_every_role_approves_in_order()
    {
        var roleA = Guid.NewGuid();
        var roleB = Guid.NewGuid();
        var instance = CreateInstance(SequentialFlow(roleA, roleB));
        var approver1 = Guid.NewGuid();
        var approver2 = Guid.NewGuid();

        instance.Decide(approver1, [roleA], ApprovalStepDecision.Approved, null, Now, approver1);
        instance.Status.Should().Be(ApprovalInstanceStatus.Pending);

        instance.Decide(approver2, [roleB], ApprovalStepDecision.Approved, null, Now, approver2);

        instance.Status.Should().Be(ApprovalInstanceStatus.Approved);
    }

    [Fact]
    public void A_single_rejection_ends_the_instance_regardless_of_prior_approvals()
    {
        var roleA = Guid.NewGuid();
        var roleB = Guid.NewGuid();
        var instance = CreateInstance(ParallelFlow(2, roleA, roleB));
        var approver1 = Guid.NewGuid();
        var approver2 = Guid.NewGuid();
        instance.Decide(approver1, [roleA], ApprovalStepDecision.Approved, null, Now, approver1);

        instance.Decide(approver2, [roleB], ApprovalStepDecision.Rejected, "No cumple criterios", Now, approver2);

        instance.Status.Should().Be(ApprovalInstanceStatus.Rejected);
        instance.DomainEvents.Should().ContainSingle(e => e is ApprovalRejected);
    }

    [Fact]
    public void Deciding_on_a_no_longer_pending_instance_throws()
    {
        var roleId = Guid.NewGuid();
        var instance = CreateInstance(ParallelFlow(1, roleId));
        var approver1 = Guid.NewGuid();
        instance.Decide(approver1, [roleId], ApprovalStepDecision.Approved, null, Now, approver1);

        var approver2 = Guid.NewGuid();
        var act = () => instance.Decide(approver2, [roleId], ApprovalStepDecision.Approved, null, Now, approver2);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_by_the_requester_succeeds_while_pending()
    {
        var instance = CreateInstance(ParallelFlow(1, Guid.NewGuid()));

        instance.Cancel(RequesterId, Now, RequesterId);

        instance.Status.Should().Be(ApprovalInstanceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_by_someone_other_than_the_requester_is_rejected()
    {
        var instance = CreateInstance(ParallelFlow(1, Guid.NewGuid()));

        var act = () => instance.Cancel(Guid.NewGuid(), Now, Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }
}
