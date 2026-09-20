using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Documents;

/// <summary>No explicit company-access check here — <c>Document</c> carries the standard
/// <c>CompanyId</c> global query filter (AppDbContext), so a caller without access to the entity's
/// company simply sees an empty list, same as every other company-scoped query in this app.</summary>
public sealed record GetDocumentsQuery(string EntityType, Guid EntityId) : IRequest<IReadOnlyList<DocumentSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Documents.Read;
}

public sealed record DocumentSummary(
    Guid Id, string FileName, string ContentType, long SizeBytes, Guid UploadedByUserId, string UploadedByDisplayName,
    DateTimeOffset UploadedAtUtc);

public sealed class GetDocumentsQueryHandler(IApplicationDbContext db) : IRequestHandler<GetDocumentsQuery, IReadOnlyList<DocumentSummary>>
{
    public async Task<IReadOnlyList<DocumentSummary>> Handle(GetDocumentsQuery request, CancellationToken cancellationToken)
    {
        var projected =
            from d in db.Documents.AsNoTracking()
            where d.EntityType == request.EntityType && d.EntityId == request.EntityId
            join uploader in db.Users.AsNoTracking() on d.UploadedByUserId equals uploader.Id
            orderby d.UploadedAtUtc descending
            select new DocumentSummary(d.Id, d.FileName, d.ContentType, d.SizeBytes, d.UploadedByUserId, uploader.DisplayName, d.UploadedAtUtc);

        return await projected.ToListAsync(cancellationToken);
    }
}
