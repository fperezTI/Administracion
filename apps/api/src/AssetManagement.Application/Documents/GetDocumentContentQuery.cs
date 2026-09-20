using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Documents;

/// <summary>Streams the file back through the API instead of issuing a time-limited SAS URL — simpler to
/// operate and re-validates permission/company access on every download, not just at upload time (see the
/// F8 plan decision 1).</summary>
public sealed record GetDocumentContentQuery(Guid DocumentId) : IRequest<DocumentContent>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Documents.Read;
}

public sealed record DocumentContent(Stream Content, string FileName, string ContentType);

public sealed class GetDocumentContentQueryHandler(IApplicationDbContext db, IFileStorage fileStorage)
    : IRequestHandler<GetDocumentContentQuery, DocumentContent>
{
    public async Task<DocumentContent> Handle(GetDocumentContentQuery request, CancellationToken cancellationToken)
    {
        var document = await db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Document), request.DocumentId);

        var stream = await fileStorage.OpenReadAsync(document.BlobPath, cancellationToken);

        return new DocumentContent(stream, document.FileName, document.ContentType);
    }
}
