using Asp.Versioning;
using AssetManagement.Application.ImportExport;
using AssetManagement.Application.Inventory;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/exports")]
public sealed class ExportsController(ISender mediator) : ControllerBase
{
    [HttpGet("assets")]
    public async Task<IActionResult> ExportAssets(
        [FromQuery] Guid companyId, [FromQuery] ExportFileFormat format, [FromQuery] Guid? assetCategoryId = null,
        [FromQuery] AssetStatus? status = null, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ExportAssetsQuery(companyId, format, assetCategoryId, status, search), cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet("assignments")]
    public async Task<IActionResult> ExportAssignments(
        [FromQuery] Guid companyId,
        [FromQuery] ExportFileFormat format,
        [FromQuery] AssignmentStatus? status = null,
        [FromQuery] Guid? assetId = null,
        [FromQuery] string? assignedToSearch = null,
        [FromQuery] string? search = null,
        [FromQuery] DateOnly? assignedFrom = null,
        [FromQuery] DateOnly? assignedTo = null,
        [FromQuery] Guid? orgUnitId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new ExportAssignmentsQuery(
                companyId, format, status, assetId, assignedToSearch, search, assignedFrom, assignedTo, orgUnitId),
            cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }
}
