using AssetManagement.Application.Common;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Application.ImportExport;
using MediatR;

namespace AssetManagement.Application.Reports;

public sealed record ExportMaintenanceKpisQuery(Guid? CompanyId, ExportFileFormat Format)
    : IRequest<ExportFileResult>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Reports.Export;
}

public sealed class ExportMaintenanceKpisQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany, IClock clock)
    : IRequestHandler<ExportMaintenanceKpisQuery, ExportFileResult>
{
    private static readonly string[] Headers = ["Categoría", "MTTR (horas)", "MTBF (días)", "Órdenes cerradas"];

    public async Task<ExportFileResult> Handle(ExportMaintenanceKpisQuery request, CancellationToken cancellationToken)
    {
        var result = await GetMaintenanceKpisQueryHandler.FetchAsync(db, currentCompany, request.CompanyId, cancellationToken);

        var exportRows = new List<string[]>
        {
            new[] { "Todas las categorías", Format(result.MttrHours), Format(result.MtbfDays), result.ClosedOrdersCount.ToString() },
        };
        exportRows.AddRange(result.ByCategory.Select(c =>
            new[] { c.AssetCategoryName, Format(c.MttrHours), Format(c.MtbfDays), c.ClosedOrdersCount.ToString() }));

        var timestamp = clock.UtcNow.ToString("yyyyMMdd-HHmmss");
        return request.Format switch
        {
            ExportFileFormat.Xlsx => new ExportFileResult(
                TabularFileBuilder.BuildXlsx("MTTR-MTBF", Headers, exportRows),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"mttr-mtbf-{timestamp}.xlsx"),
            ExportFileFormat.Pdf => new ExportFileResult(
                TabularFileBuilder.BuildPdf("MTTR / MTBF de mantenimiento", Headers, exportRows),
                "application/pdf",
                $"mttr-mtbf-{timestamp}.pdf"),
            _ => throw new ConflictException("Formato de exportación no soportado."),
        };
    }

    private static string Format(double? value) => value?.ToString("0.##") ?? "—";
}
