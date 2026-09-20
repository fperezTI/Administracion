using Asp.Versioning;
using AssetManagement.Application.Maintenance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/maintenance-checklists")]
public sealed class MaintenanceChecklistsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MaintenanceChecklistDefinitionSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MaintenanceChecklistDefinitionSummary>>> GetChecklists(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMaintenanceChecklistDefinitionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{checklistDefinitionId:guid}")]
    [ProducesResponseType(typeof(MaintenanceChecklistDefinitionDetail), StatusCodes.Status200OK)]
    public async Task<ActionResult<MaintenanceChecklistDefinitionDetail>> GetById(Guid checklistDefinitionId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMaintenanceChecklistDefinitionByIdQuery(checklistDefinitionId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(CreateMaintenanceChecklistDefinitionCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { checklistDefinitionId = id, version = "1.0" }, id);
    }

    [HttpPost("{checklistDefinitionId:guid}/versions")]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    public async Task<ActionResult<int>> AddVersion(
        Guid checklistDefinitionId, AddMaintenanceChecklistVersionRequest request, CancellationToken cancellationToken)
    {
        var versionNumber = await mediator.Send(
            new AddMaintenanceChecklistVersionCommand(checklistDefinitionId, request.Items), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { checklistDefinitionId, version = "1.0" }, versionNumber);
    }

    [HttpPatch("{checklistDefinitionId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetActive(Guid checklistDefinitionId, [FromBody] bool isActive, CancellationToken cancellationToken)
    {
        await mediator.Send(new SetMaintenanceChecklistDefinitionActiveCommand(checklistDefinitionId, isActive), cancellationToken);
        return NoContent();
    }
}

public sealed record AddMaintenanceChecklistVersionRequest(IReadOnlyList<string> Items);
