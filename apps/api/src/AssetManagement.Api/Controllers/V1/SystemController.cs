using Asp.Versioning;
using AssetManagement.Application.SystemInfo;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system")]
public sealed class SystemController(ISender mediator) : ControllerBase
{
    /// <summary>Anonymous diagnostic endpoint proving the API -> Application -> Infrastructure pipeline is wired.</summary>
    [HttpGet("info")]
    [ProducesResponseType(typeof(SystemInfoResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemInfoResponse>> GetInfo(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSystemInfoQuery(), cancellationToken);
        return Ok(result);
    }
}
