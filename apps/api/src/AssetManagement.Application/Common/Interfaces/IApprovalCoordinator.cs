namespace AssetManagement.Application.Common.Interfaces;

/// <summary>
/// How any business module requests an approval (pedido §10) without knowing anything about how
/// approvals work — it just gets back the id of the <see cref="Domain.Approvals.ApprovalInstance"/>
/// created, and later reacts to <see cref="Domain.Approvals.Events.ApprovalCompleted"/>/
/// <see cref="Domain.Approvals.Events.ApprovalRejected"/> for the matching <c>ContextType</c>/<c>ContextId</c>.
/// </summary>
public interface IApprovalCoordinator
{
    /// <summary>Looks up an active <see cref="Domain.Approvals.ApprovalFlowDefinition"/> for
    /// <paramref name="flowKey"/> — a company-specific override first, then a global one — and creates a
    /// new pending <see cref="Domain.Approvals.ApprovalInstance"/> from it. Throws
    /// <see cref="AssetManagement.Application.Common.Exceptions.ConflictException"/> if no active flow is
    /// configured for that key: unlike the fixed <c>PermissionCatalog</c>, flows reference real roles an
    /// administrator must create first.</summary>
    public Task<Guid> RequestApprovalAsync(
        Guid companyId, string flowKey, string contextType, Guid contextId, Guid requestedByUserId,
        string? comment, CancellationToken cancellationToken);
}
