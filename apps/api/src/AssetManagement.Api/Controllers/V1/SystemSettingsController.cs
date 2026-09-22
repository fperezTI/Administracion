using Asp.Versioning;
using AssetManagement.Application.SystemConfiguration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/system/settings")]
public sealed class SystemSettingsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(SystemSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSystemSettingsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateSettings(UpdateSystemSettingsCommand command, CancellationToken cancellationToken)
    {
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
