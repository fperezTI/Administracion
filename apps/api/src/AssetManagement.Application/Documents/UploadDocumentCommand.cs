using System.Text.Json.Serialization;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Documents;
using AssetManagement.Domain.Maintenance;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Documents;

/// <summary>
/// Uploads evidence for an allowlisted entity (pedido: "documentos/evidencias" — see
/// <see cref="DocumentEntityTypes"/> for why only <c>Asset</c>/<c>MaintenanceOrder</c> in V1). Constructed
/// directly by the controller from a multipart file upload, not JSON-bound — <see cref="Content"/> is
/// excluded from the audit trail's JSON snapshot (it isn't serializable, and the file itself isn't the
/// point of the audit row).
/// </summary>
public sealed record UploadDocumentCommand(
    string EntityType, Guid EntityId, string FileName, string ContentType, long SizeBytes,
    [property: JsonIgnore] Stream Content)
    : IRequest<Guid>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Documents.Create;
}

public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.EntityType).Must(t => DocumentEntityTypes.All.Contains(t))
            .WithMessage("Tipo de entidad no válido para documentos.");
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SizeBytes).GreaterThan(0).LessThanOrEqualTo(25 * 1024 * 1024).WithMessage("El archivo no puede superar 25 MB.");
    }
}

public sealed class UploadDocumentCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IFileStorage fileStorage, IClock clock)
    : IRequestHandler<UploadDocumentCommand, Guid>
{
    public async Task<Guid> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        var companyId = request.EntityType switch
        {
            DocumentEntityTypes.Asset => (await db.Assets.FirstOrDefaultAsync(a => a.Id == request.EntityId, cancellationToken)
                ?? throw new NotFoundException(nameof(Asset), request.EntityId)).CompanyId,
            DocumentEntityTypes.MaintenanceOrder => (await db.MaintenanceOrders.FirstOrDefaultAsync(o => o.Id == request.EntityId, cancellationToken)
                ?? throw new NotFoundException(nameof(MaintenanceOrder), request.EntityId)).CompanyId,
            _ => throw new ConflictException("Tipo de entidad no válido para documentos."),
        };

        if (!currentCompany.AccessibleCompanyIds.Contains(companyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de esta entidad.");
        }

        var blobPath = $"{companyId}/{request.EntityType}/{request.EntityId}/{Guid.NewGuid()}-{SanitizeFileName(request.FileName)}";
        await fileStorage.UploadAsync(blobPath, request.ContentType, request.Content, cancellationToken);

        var document = Document.Create(
            companyId, request.EntityType, request.EntityId, request.FileName, request.ContentType, request.SizeBytes,
            blobPath, currentUser.UserId!.Value, clock.UtcNow);

        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        return document.Id;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
