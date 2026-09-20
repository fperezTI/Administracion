using AssetManagement.Application.Common;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Application.ImportExport;
using MediatR;

namespace AssetManagement.Application.Reports;

public sealed record ExportLowStockConsumablesQuery(Guid? CompanyId, ExportFileFormat Format)
    : IRequest<ExportFileResult>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Reports.Export;
}

public sealed class ExportLowStockConsumablesQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany, IClock clock)
    : IRequestHandler<ExportLowStockConsumablesQuery, ExportFileResult>
{
    private static readonly string[] Headers = ["Nombre", "SKU", "Unidad", "Existencia actual", "Existencia mínima", "Faltante"];

    public async Task<ExportFileResult> Handle(ExportLowStockConsumablesQuery request, CancellationToken cancellationToken)
    {
        var rows = await GetLowStockConsumablesQueryHandler.FetchAsync(db, currentCompany, request.CompanyId, cancellationToken);

        var exportRows = rows.Select(r => new[]
        {
            r.Name, r.Sku ?? "", r.UnitOfMeasure, r.CurrentStock.ToString("0.##"), r.MinimumStock.ToString("0.##"), r.Shortfall.ToString("0.##"),
        });

        var timestamp = clock.UtcNow.ToString("yyyyMMdd-HHmmss");
        return request.Format switch
        {
            ExportFileFormat.Xlsx => new ExportFileResult(
                TabularFileBuilder.BuildXlsx("Existencias bajas", Headers, exportRows),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"existencias-bajas-{timestamp}.xlsx"),
            ExportFileFormat.Pdf => new ExportFileResult(
                TabularFileBuilder.BuildPdf("Existencias bajas", Headers, exportRows),
                "application/pdf",
                $"existencias-bajas-{timestamp}.pdf"),
            _ => throw new ConflictException("Formato de exportación no soportado."),
        };
    }
}
