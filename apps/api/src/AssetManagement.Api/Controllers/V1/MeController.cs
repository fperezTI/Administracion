using Asp.Versioning;
using AssetManagement.Application.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/me")]
public sealed class MeController(ISender mediator) : ControllerBase
{
    /// <summary>The caller's own profile, permissions and accessible companies. No permission code is
    /// required — this endpoint can never return another user's data.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMeQuery(), cancellationToken);
        return Ok(result);
    }
}
