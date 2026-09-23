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
[Route("api/v{version:apiVersion}/transfers")]
public sealed class TransfersController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TransferSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TransferSummary>>> GetTransfers(
        [FromQuery] Guid companyId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] TransferStatus? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetTransfersQuery(companyId, pageNumber, pageSize, status, sortBy, sortDescending), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{transferId:guid}")]
    [ProducesResponseType(typeof(TransferDetail), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransferDetail>> GetById(Guid transferId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTransferByIdQuery(transferId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(RequestCrossCompanyTransferCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { transferId = id, version = "1.0" }, id);
    }

    [HttpPost("{transferId:guid}/receive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Receive(Guid transferId, ReceiveTransferRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new ReceiveCrossCompanyTransferCommand(transferId, request.SignatureMechanism, request.TypedFullName, request.SignatureImageDataUrl),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{transferId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid transferId, CancellationToken cancellationToken)
    {
        await mediator.Send(new CancelTransferCommand(transferId), cancellationToken);
        return NoContent();
    }
}

public sealed record ReceiveTransferRequest(string SignatureMechanism, string? TypedFullName, string? SignatureImageDataUrl);
