using System.Text;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.ImportExport;

/// <summary>Header-only CSV, columns for the fixed <c>Asset</c> fields plus one <c>CustomField:{Code}</c>
/// column per field of the chosen category (sorted the same way the category's own screen orders them) —
/// so the user never has to guess a field's code by hand.</summary>
public sealed record GetAssetImportTemplateQuery(Guid AssetCategoryId) : IRequest<ImportTemplateResult>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Imports.Read;
}

public sealed record ImportTemplateResult(byte[] Content, string FileName);

public sealed class GetAssetImportTemplateQueryHandler(IApplicationDbContext db) : IRequestHandler<GetAssetImportTemplateQuery, ImportTemplateResult>
{
    public async Task<ImportTemplateResult> Handle(GetAssetImportTemplateQuery request, CancellationToken cancellationToken)
    {
        var category = await db.AssetCategories.AsNoTracking().Include(c => c.CustomFields)
            .FirstOrDefaultAsync(c => c.Id == request.AssetCategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(AssetCategory), request.AssetCategoryId);

        var columns = AssetImportColumns.FixedColumns
            .Concat(category.CustomFields.OrderBy(f => f.SortOrder).Select(f => $"{AssetImportColumns.CustomFieldPrefix}{f.Code}"));

        var csv = string.Join(',', columns.Select(EscapeCsvField)) + "\r\n";
        var content = Encoding.UTF8.GetBytes(csv);

        return new ImportTemplateResult(content, $"plantilla-importacion-{category.Code}.csv");
    }

    private static string EscapeCsvField(string field) =>
        field.Contains(',') || field.Contains('"') ? $"\"{field.Replace("\"", "\"\"")}\"" : field;
}
