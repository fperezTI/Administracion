using Asp.Versioning;
using AssetManagement.Application.Identity.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/permissions")]
public sealed class PermissionsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<PermissionModuleGroup>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PermissionModuleGroup>>> GetPermissions(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPermissionsQuery(), cancellationToken);
        return Ok(result);
    }
}
