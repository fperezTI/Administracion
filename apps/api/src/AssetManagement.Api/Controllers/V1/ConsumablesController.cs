using Asp.Versioning;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.SparePartsAndConsumables;
using AssetManagement.Domain.SparePartsAndConsumables;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/consumables")]
public sealed class ConsumablesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ConsumableSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ConsumableSummary>>> GetConsumables(
        [FromQuery] Guid companyId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetConsumablesQuery(companyId, pageNumber, pageSize, sortBy, sortDescending), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{consumableId:guid}/movements")]
    [ProducesResponseType(typeof(IReadOnlyList<ConsumableStockMovementSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConsumableStockMovementSummary>>> GetMovements(
        Guid consumableId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetConsumableStockMovementsQuery(consumableId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(CreateConsumableCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetConsumables), new { companyId = command.CompanyId, version = "1.0" }, id);
    }

    [HttpPut("{consumableId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid consumableId, UpdateConsumableRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new UpdateConsumableCommand(consumableId, request.Name, request.Sku, request.UnitOfMeasure, request.MinimumStock),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{consumableId:guid}/movements")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> RegisterMovement(
        Guid consumableId, RegisterConsumableStockMovementRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(
            new RegisterConsumableStockMovementCommand(
                consumableId, request.WarehouseOrgUnitId, request.Direction, request.Reason, request.Quantity,
                request.ReferenceMaintenanceOrderId, request.Notes),
            cancellationToken);
        return CreatedAtAction(nameof(GetMovements), new { consumableId, version = "1.0" }, id);
    }
}

public sealed record UpdateConsumableRequest(string Name, string? Sku, string UnitOfMeasure, decimal? MinimumStock);

public sealed record RegisterConsumableStockMovementRequest(
    Guid WarehouseOrgUnitId, ConsumableStockDirection Direction, ConsumableStockMovementReason Reason, decimal Quantity,
    Guid? ReferenceMaintenanceOrderId, string? Notes);
