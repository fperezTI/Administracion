using AssetManagement.Application.Common;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Application.ImportExport;
using MediatR;

namespace AssetManagement.Application.Reports;

public sealed record ExportExpiringWarrantiesQuery(Guid? CompanyId, int WithinDays, ExportFileFormat Format)
    : IRequest<ExportFileResult>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Reports.Export;
}

public sealed class ExportExpiringWarrantiesQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany, IClock clock)
    : IRequestHandler<ExportExpiringWarrantiesQuery, ExportFileResult>
{
    private static readonly string[] Headers = ["Folio", "Proveedor", "Tipo", "Vence", "Días restantes"];

    public async Task<ExportFileResult> Handle(ExportExpiringWarrantiesQuery request, CancellationToken cancellationToken)
    {
        var rows = await GetExpiringWarrantiesQueryHandler.FetchAsync(
            db, currentCompany, clock, request.CompanyId, request.WithinDays, cancellationToken);

        var exportRows = rows.Select(r => new[]
        {
            r.AssetFolio, r.Provider, r.Type.ToString(), r.EndDate.ToString("yyyy-MM-dd"), r.DaysRemaining.ToString(),
        });

        var timestamp = clock.UtcNow.ToString("yyyyMMdd-HHmmss");
        return request.Format switch
        {
            ExportFileFormat.Xlsx => new ExportFileResult(
                TabularFileBuilder.BuildXlsx("Garantías por vencer", Headers, exportRows),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"garantias-por-vencer-{timestamp}.xlsx"),
            ExportFileFormat.Pdf => new ExportFileResult(
                TabularFileBuilder.BuildPdf("Garantías por vencer", Headers, exportRows),
                "application/pdf",
                $"garantias-por-vencer-{timestamp}.pdf"),
            _ => throw new ConflictException("Formato de exportación no soportado."),
        };
    }
}
