using Asp.Versioning;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Maintenance;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Maintenance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/maintenance-orders")]
public sealed class MaintenanceOrdersController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MaintenanceOrderSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MaintenanceOrderSummary>>> GetOrders(
        [FromQuery] Guid companyId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? assetId = null,
        [FromQuery] MaintenanceOrderStatus? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetMaintenanceOrdersQuery(companyId, pageNumber, pageSize, assetId, status, sortBy, sortDescending),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{maintenanceOrderId:guid}")]
    [ProducesResponseType(typeof(MaintenanceOrderDetail), StatusCodes.Status200OK)]
    public async Task<ActionResult<MaintenanceOrderDetail>> GetById(Guid maintenanceOrderId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMaintenanceOrderByIdQuery(maintenanceOrderId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Open(OpenMaintenanceOrderCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { maintenanceOrderId = id, version = "1.0" }, id);
    }

    [HttpPost("{maintenanceOrderId:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Close(Guid maintenanceOrderId, CloseMaintenanceOrderRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new CloseMaintenanceOrderCommand(maintenanceOrderId, request.ResultStatus, request.ResultNotes, request.ChecklistItemResults),
            cancellationToken);
        return NoContent();
    }
}

public sealed record CloseMaintenanceOrderRequest(
    AssetStatus ResultStatus, string ResultNotes, IReadOnlyList<MaintenanceOrderChecklistItemResultInput>? ChecklistItemResults);
