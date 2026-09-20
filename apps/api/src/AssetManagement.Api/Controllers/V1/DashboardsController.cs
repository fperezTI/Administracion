using Asp.Versioning;
using AssetManagement.Application.Dashboards;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/dashboards")]
public sealed class DashboardsController(ISender mediator) : ControllerBase
{
    [HttpGet("executive")]
    [ProducesResponseType(typeof(ExecutiveDashboardResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExecutiveDashboardResult>> GetExecutive([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetExecutiveDashboardQuery(companyId), cancellationToken);
        return Ok(result);
    }
}
