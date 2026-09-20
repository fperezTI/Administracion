using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.ImportExport;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.ImportExport;

/// <summary>Only flips the batch to <see cref="ImportBatchStatus.Processing"/> and re-queues it — the
/// actual row-by-row creation happens in the worker (<see cref="ImportBatchProcessor.CommitAsync"/>), so
/// this responds immediately even for a 10,000-row batch (ADR 0011).</summary>
public sealed record CommitImportBatchCommand(Guid ImportBatchId, ImportCommitMode Mode) : IRequest, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Imports.Create;
}

public sealed class CommitImportBatchCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IImportQueue importQueue, IClock clock)
    : IRequestHandler<CommitImportBatchCommand>
{
    public async Task Handle(CommitImportBatchCommand request, CancellationToken cancellationToken)
    {
        var batch = await db.ImportBatches.FirstOrDefaultAsync(b => b.Id == request.ImportBatchId, cancellationToken)
            ?? throw new NotFoundException(nameof(ImportBatch), request.ImportBatchId);

        if (!currentCompany.AccessibleCompanyIds.Contains(batch.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este lote.");
        }

        batch.RequestCommit(request.Mode, clock.UtcNow, currentUser.UserId!.Value);
        await db.SaveChangesAsync(cancellationToken);

        await importQueue.EnqueueAsync(batch.Id, cancellationToken);
    }
}
