using Asp.Versioning;
using AssetManagement.Application.Organization.OrgUnits;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/org-units")]
public sealed class OrgUnitsController(ISender mediator) : ControllerBase
{
    [HttpGet("types")]
    [ProducesResponseType(typeof(IReadOnlyCollection<OrgUnitTypeSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<OrgUnitTypeSummary>>> GetTypes(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetOrgUnitTypesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<OrgUnitNode>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<OrgUnitNode>>> GetTree(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetOrgUnitTreeQuery(companyId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(CreateOrgUnitCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetTree), new { companyId = command.CompanyId, version = "1.0" }, id);
    }

    [HttpPatch("{orgUnitId:guid}/move")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Move(Guid orgUnitId, [FromBody] Guid? newParentOrgUnitId, CancellationToken cancellationToken)
    {
        await mediator.Send(new MoveOrgUnitCommand(orgUnitId, newParentOrgUnitId), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{orgUnitId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetActive(Guid orgUnitId, [FromBody] bool isActive, CancellationToken cancellationToken)
    {
        await mediator.Send(new SetOrgUnitActiveCommand(orgUnitId, isActive), cancellationToken);
        return NoContent();
    }
}
