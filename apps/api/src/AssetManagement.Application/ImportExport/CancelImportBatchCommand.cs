using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.ImportExport;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.ImportExport;

public sealed record CancelImportBatchCommand(Guid ImportBatchId) : IRequest, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Imports.Create;
}

public sealed class CancelImportBatchCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<CancelImportBatchCommand>
{
    public async Task Handle(CancelImportBatchCommand request, CancellationToken cancellationToken)
    {
        var batch = await db.ImportBatches.FirstOrDefaultAsync(b => b.Id == request.ImportBatchId, cancellationToken)
            ?? throw new NotFoundException(nameof(ImportBatch), request.ImportBatchId);

        if (!currentCompany.AccessibleCompanyIds.Contains(batch.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este lote.");
        }

        batch.Cancel(clock.UtcNow, currentUser.UserId!.Value);
        await db.SaveChangesAsync(cancellationToken);
    }
}
