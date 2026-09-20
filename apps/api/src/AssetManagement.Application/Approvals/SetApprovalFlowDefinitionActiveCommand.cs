using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Approvals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Approvals;

public sealed record SetApprovalFlowDefinitionActiveCommand(Guid FlowDefinitionId, bool IsActive)
    : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Approvals.Configure;
}

public sealed class SetApprovalFlowDefinitionActiveCommandHandler(
    IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<SetApprovalFlowDefinitionActiveCommand>
{
    public async Task Handle(SetApprovalFlowDefinitionActiveCommand request, CancellationToken cancellationToken)
    {
        var flow = await db.ApprovalFlowDefinitions.FirstOrDefaultAsync(f => f.Id == request.FlowDefinitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApprovalFlowDefinition), request.FlowDefinitionId);

        var now = clock.UtcNow;
        if (request.IsActive)
        {
            flow.Activate(now, currentUser.UserId);
        }
        else
        {
            flow.Deactivate(now, currentUser.UserId);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
