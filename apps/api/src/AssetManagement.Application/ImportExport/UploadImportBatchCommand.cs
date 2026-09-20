using System.Text.Json.Serialization;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.ImportExport;
using FluentValidation;
using MediatR;

namespace AssetManagement.Application.ImportExport;

/// <summary>Only uploads the file and queues the batch (ADR 0003/0011) — nothing is validated or written
/// here, the worker does both. <see cref="Content"/> is excluded from the audit trail's JSON snapshot,
/// same reasoning as <c>UploadDocumentCommand</c> (F8).</summary>
public sealed record UploadImportBatchCommand(
    Guid CompanyId, string FileName, long SizeBytes, [property: JsonIgnore] Stream Content)
    : IRequest<Guid>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Imports.Create;
}

public sealed class UploadImportBatchCommandValidator : AbstractValidator<UploadImportBatchCommand>
{
    public UploadImportBatchCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260).Must(f => f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            .WithMessage("El archivo de importación debe ser un CSV (.csv).");
        RuleFor(x => x.SizeBytes).GreaterThan(0).LessThanOrEqualTo(25 * 1024 * 1024).WithMessage("El archivo no puede superar 25 MB.");
    }
}

public sealed class UploadImportBatchCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IFileStorage fileStorage, IImportQueue importQueue, IClock clock)
    : IRequestHandler<UploadImportBatchCommand, Guid>
{
    public async Task<Guid> Handle(UploadImportBatchCommand request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var now = clock.UtcNow;
        var blobPath = $"imports/{request.CompanyId}/{Guid.NewGuid()}.csv";
        await fileStorage.UploadAsync(blobPath, "text/csv", request.Content, cancellationToken);

        var batch = ImportBatch.Create(request.CompanyId, request.FileName, blobPath, now, currentUser.UserId!.Value);
        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync(cancellationToken);

        await importQueue.EnqueueAsync(batch.Id, cancellationToken);

        return batch.Id;
    }
}
