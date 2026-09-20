using Asp.Versioning;
using AssetManagement.Application.ImportExport;
using AssetManagement.Application.Reports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/reports")]
public sealed class ReportsController(ISender mediator) : ControllerBase
{
    [HttpGet("inventory-summary")]
    [ProducesResponseType(typeof(InventorySummaryResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<InventorySummaryResult>> GetInventorySummary([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetInventorySummaryQuery(companyId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("maintenance-kpis")]
    [ProducesResponseType(typeof(MaintenanceKpisResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<MaintenanceKpisResult>> GetMaintenanceKpis([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMaintenanceKpisQuery(companyId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("maintenance-kpis/export")]
    public async Task<IActionResult> ExportMaintenanceKpis(
        [FromQuery] Guid? companyId, [FromQuery] ExportFileFormat format, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ExportMaintenanceKpisQuery(companyId, format), cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet("expiring-warranties")]
    [ProducesResponseType(typeof(IReadOnlyList<ExpiringWarrantyRow>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ExpiringWarrantyRow>>> GetExpiringWarranties(
        [FromQuery] Guid? companyId, [FromQuery] int withinDays = 30, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetExpiringWarrantiesQuery(companyId, withinDays), cancellationToken);
        return Ok(result);
    }

    [HttpGet("expiring-warranties/export")]
    public async Task<IActionResult> ExportExpiringWarranties(
        [FromQuery] Guid? companyId, [FromQuery] int withinDays, [FromQuery] ExportFileFormat format, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ExportExpiringWarrantiesQuery(companyId, withinDays, format), cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet("low-stock-consumables")]
    [ProducesResponseType(typeof(IReadOnlyList<LowStockConsumableRow>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LowStockConsumableRow>>> GetLowStockConsumables(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetLowStockConsumablesQuery(companyId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("low-stock-consumables/export")]
    public async Task<IActionResult> ExportLowStockConsumables(
        [FromQuery] Guid? companyId, [FromQuery] ExportFileFormat format, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ExportLowStockConsumablesQuery(companyId, format), cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }
}
