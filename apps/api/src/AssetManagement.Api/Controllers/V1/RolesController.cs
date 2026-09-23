using Asp.Versioning;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Identity.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/roles")]
public sealed class RolesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RoleSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RoleSummary>>> GetRoles(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] bool? isActive = null,
        [FromQuery] string? sortBy = null, [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetRolesQuery(pageNumber, pageSize, isActive, sortBy, sortDescending), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{roleId:guid}")]
    [ProducesResponseType(typeof(RoleDetail), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleDetail>> GetRoleById(Guid roleId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRoleByIdQuery(roleId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetRoleById), new { roleId = id, version = "1.0" }, id);
    }

    [HttpPost("{sourceRoleId:guid}/duplicate")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Duplicate(Guid sourceRoleId, [FromBody] string newName, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new DuplicateRoleCommand(sourceRoleId, newName), cancellationToken);
        return CreatedAtAction(nameof(GetRoleById), new { roleId = id, version = "1.0" }, id);
    }

    [HttpPut("{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid roleId, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateRoleCommand(roleId, request.Name, request.Description), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{roleId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetActive(Guid roleId, [FromBody] bool isActive, CancellationToken cancellationToken)
    {
        await mediator.Send(new SetRoleActiveCommand(roleId, isActive), cancellationToken);
        return NoContent();
    }

    [HttpPut("{roleId:guid}/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetPermissions(
        Guid roleId, [FromBody] IReadOnlyCollection<Guid> permissionIds, CancellationToken cancellationToken)
    {
        await mediator.Send(new SetRolePermissionsCommand(roleId, permissionIds), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateRoleRequest(string Name, string? Description);
