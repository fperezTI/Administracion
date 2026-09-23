using Asp.Versioning;
using AssetManagement.Application.Approvals;
using AssetManagement.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/approval-flows")]
public sealed class ApprovalFlowDefinitionsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ApprovalFlowDefinitionSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ApprovalFlowDefinitionSummary>>> GetFlows(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetApprovalFlowDefinitionsQuery(pageNumber, pageSize, sortBy, sortDescending), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(CreateApprovalFlowDefinitionCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetFlows), new { version = "1.0" }, id);
    }

    [HttpPatch("{flowDefinitionId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetActive(Guid flowDefinitionId, [FromBody] bool isActive, CancellationToken cancellationToken)
    {
        await mediator.Send(new SetApprovalFlowDefinitionActiveCommand(flowDefinitionId, isActive), cancellationToken);
        return NoContent();
    }
}
