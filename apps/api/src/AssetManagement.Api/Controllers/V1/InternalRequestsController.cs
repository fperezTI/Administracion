using Asp.Versioning;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Requests;
using AssetManagement.Domain.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/requests")]
public sealed class InternalRequestsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InternalRequestSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InternalRequestSummary>>> GetRequests(
        [FromQuery] Guid companyId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] InternalRequestStatus? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetInternalRequestsQuery(companyId, pageNumber, pageSize, status, sortBy, sortDescending),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<MyInternalRequestSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MyInternalRequestSummary>>> GetMine(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMyInternalRequestsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{internalRequestId:guid}")]
    [ProducesResponseType(typeof(InternalRequestDetail), StatusCodes.Status200OK)]
    public async Task<ActionResult<InternalRequestDetail>> GetById(Guid internalRequestId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetInternalRequestByIdQuery(internalRequestId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(CreateInternalRequestCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { internalRequestId = id, version = "1.0" }, id);
    }

    [HttpPost("{internalRequestId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid internalRequestId, CancellationToken cancellationToken)
    {
        await mediator.Send(new CancelInternalRequestCommand(internalRequestId), cancellationToken);
        return NoContent();
    }
}
