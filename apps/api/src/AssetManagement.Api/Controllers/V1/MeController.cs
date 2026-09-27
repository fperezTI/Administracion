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

    /// <summary>Sets the caller's own theme preference. A null <c>themeCode</c> restores "usar tema de
    /// la empresa" — see <see cref="SetMyThemePreferenceCommand"/>.</summary>
    [HttpPut("preferences/theme")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetThemePreference(
        [FromBody] SetThemePreferenceRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new SetMyThemePreferenceCommand(request.ThemeCode), cancellationToken);
        return NoContent();
    }
}

/// <summary>Minimal request DTO — kept distinct from the command so the controller never binds a
/// client-supplied user id onto it (mass-assignment guard, same reasoning as every other command here).</summary>
public sealed record SetThemePreferenceRequest(string? ThemeCode);
