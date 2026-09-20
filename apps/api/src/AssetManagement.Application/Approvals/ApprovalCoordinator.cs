using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Domain.Approvals;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Approvals;

public sealed class ApprovalCoordinator(IApplicationDbContext db, IClock clock) : IApprovalCoordinator
{
    public async Task<Guid> RequestApprovalAsync(
        Guid companyId, string flowKey, string contextType, Guid contextId, Guid requestedByUserId,
        string? comment, CancellationToken cancellationToken)
    {
        var flow = await db.ApprovalFlowDefinitions
            .Where(f => f.Key == flowKey && f.IsActive && f.CompanyId == companyId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await db.ApprovalFlowDefinitions
                .Where(f => f.Key == flowKey && f.IsActive && f.CompanyId == null)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ConflictException(
                $"No hay un flujo de aprobación configurado para '{flowKey}'. Pide a un administrador que configure uno en Aprobaciones.");

        var now = clock.UtcNow;
        var instance = ApprovalInstance.Create(flow, companyId, contextType, contextId, requestedByUserId, comment, now, requestedByUserId);
        db.ApprovalInstances.Add(instance);

        return instance.Id;
    }
}
