using Asp.Versioning;
using AssetManagement.Application.ImportExport;
using AssetManagement.Domain.Assets;
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
}
