using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Documents;

/// <summary>
/// Metadata for one uploaded file (pedido: "documentos/evidencias"), polymorphically associated to
/// another entity via <see cref="EntityType"/>/<see cref="EntityId"/> — same pattern
/// <see cref="Signature.SignatureRecord"/> already uses for its <c>ContextType</c>/<c>ContextId</c>.
/// <see cref="EntityType"/> is validated against an allowlist in Application
/// (<c>DocumentEntityTypes</c>), not here — the domain only knows it is a non-empty string. The actual
/// bytes live in Blob Storage at <see cref="BlobPath"/>; this aggregate only tracks metadata. Immutable
/// once created — no edit/delete, matching the seeded permission set (only <c>Documents.Read</c>/
/// <c>Documents.Create</c> exist).
/// </summary>
public sealed class Document : AuditableAggregateRoot<Guid>
{
    public Guid CompanyId { get; private set; }
    public string EntityType { get; private set; } = null!;
    public Guid EntityId { get; private set; }
    public string FileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    public string BlobPath { get; private set; } = null!;
    public Guid UploadedByUserId { get; private set; }
    public DateTimeOffset UploadedAtUtc { get; private set; }

    private Document()
    {
    }

    private Document(
        Guid id, Guid companyId, string entityType, Guid entityId, string fileName, string contentType,
        long sizeBytes, string blobPath, Guid uploadedByUserId, DateTimeOffset nowUtc)
        : base(id, nowUtc, uploadedByUserId)
    {
        CompanyId = companyId;
        EntityType = entityType;
        EntityId = entityId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        BlobPath = blobPath;
        UploadedByUserId = uploadedByUserId;
        UploadedAtUtc = nowUtc;
    }

    public static Document Create(
        Guid companyId, string entityType, Guid entityId, string fileName, string contentType, long sizeBytes,
        string blobPath, Guid uploadedByUserId, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new DomainException("El tipo de entidad del documento es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainException("El nombre del archivo es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(blobPath))
        {
            throw new DomainException("La ruta de almacenamiento del documento es obligatoria.");
        }

        if (sizeBytes <= 0)
        {
            throw new DomainException("El tamaño del documento debe ser mayor a cero.");
        }

        return new Document(
            Guid.NewGuid(), companyId, entityType.Trim(), entityId, fileName.Trim(), contentType.Trim(), sizeBytes,
            blobPath.Trim(), uploadedByUserId, nowUtc);
    }
}
