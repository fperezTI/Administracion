using Asp.Versioning;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Inventory;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/assignments")]
public sealed class AssignmentsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssignmentSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssignmentSummary>>> GetAssignments(
        [FromQuery] Guid companyId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] AssignmentStatus? status = null,
        [FromQuery] Guid? assetId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetAssignmentsQuery(companyId, pageNumber, pageSize, status, assetId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Self-service: every assignment made to the caller — no permission required, see
    /// GetMyAssignmentsQuery.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<MyAssignmentSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MyAssignmentSummary>>> GetMine(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMyAssignmentsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{assignmentId:guid}")]
    [ProducesResponseType(typeof(AssignmentDetail), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentDetail>> GetById(Guid assignmentId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAssignmentByIdQuery(assignmentId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Self-service: only the named recipient may call this successfully (enforced in the
    /// handler) — the report behind the "confirm what was assigned to me" email link.</summary>
    [HttpGet("mine/{assignmentId:guid}")]
    [ProducesResponseType(typeof(AssignmentDetail), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentDetail>> GetMineById(Guid assignmentId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMyAssignmentByIdQuery(assignmentId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateAssignmentResult), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateAssignmentResult>> Create(CreateAssignmentCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { assignmentId = result.AssignmentId, version = "1.0" }, result);
    }

    /// <summary>Self-service: only the named recipient may call this successfully (enforced in the handler).</summary>
    [HttpPost("{assignmentId:guid}/sign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Sign(Guid assignmentId, SignAssignmentRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new SignAssignmentCommand(assignmentId, request.TypedFullName), cancellationToken);
        return NoContent();
    }

    [HttpPost("{assignmentId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid assignmentId, CancellationToken cancellationToken)
    {
        await mediator.Send(new CancelPendingAssignmentCommand(assignmentId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{assignmentId:guid}/return")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Return(Guid assignmentId, ReturnAssignmentRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new ReturnAssignmentCommand(assignmentId, request.TypedFullName, request.Notes), cancellationToken);
        return NoContent();
    }

    [HttpPost("reassign")]
    [ProducesResponseType(typeof(CreateAssignmentResult), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateAssignmentResult>> Reassign(ReassignAssetCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { assignmentId = result.AssignmentId, version = "1.0" }, result);
    }
}

public sealed record SignAssignmentRequest(string TypedFullName);

public sealed record ReturnAssignmentRequest(string TypedFullName, string? Notes);
