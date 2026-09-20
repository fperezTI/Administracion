using Asp.Versioning;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserSummary>>> GetUsers(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetUsersQuery(pageNumber, pageSize, isActive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(UserDetail), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDetail>> GetUserById(Guid userId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserByIdQuery(userId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{userId:guid}/roles/{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignRole(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        await mediator.Send(new AssignRoleToUserCommand(userId, roleId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        await mediator.Send(new RemoveRoleFromUserCommand(userId, roleId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{userId:guid}/companies/{companyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GrantCompanyAccess(Guid userId, Guid companyId, CancellationToken cancellationToken)
    {
        await mediator.Send(new GrantUserCompanyAccessCommand(userId, companyId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{userId:guid}/companies/{companyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeCompanyAccess(Guid userId, Guid companyId, CancellationToken cancellationToken)
    {
        await mediator.Send(new RevokeUserCompanyAccessCommand(userId, companyId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{userId:guid}/anonymize")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Anonymize(Guid userId, CancellationToken cancellationToken)
    {
        await mediator.Send(new AnonymizeUserCommand(userId), cancellationToken);
        return NoContent();
    }
}
