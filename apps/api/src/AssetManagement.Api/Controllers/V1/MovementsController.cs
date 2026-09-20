using Asp.Versioning;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Inventory;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/movements")]
public sealed class MovementsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MovementSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MovementSummary>>> GetMovements(
        [FromQuery] Guid companyId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? assetId = null,
        [FromQuery] MovementType? type = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetMovementsQuery(companyId, pageNumber, pageSize, assetId, type), cancellationToken);
        return Ok(result);
    }
}
