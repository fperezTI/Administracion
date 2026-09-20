using Asp.Versioning;
using AssetManagement.Application.SparePartsAndConsumables;
using AssetManagement.Domain.SparePartsAndConsumables;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/spare-parts")]
public sealed class SparePartsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SparePartSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SparePartSummary>>> GetSpareParts(
        [FromQuery] Guid companyId, [FromQuery] SparePartStatus? status, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSparePartsQuery(companyId, status), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{sparePartId:guid}")]
    [ProducesResponseType(typeof(SparePartDetail), StatusCodes.Status200OK)]
    public async Task<ActionResult<SparePartDetail>> GetById(Guid sparePartId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSparePartByIdQuery(sparePartId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(CreateSparePartCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { sparePartId = id, version = "1.0" }, id);
    }

    [HttpPost("{sparePartId:guid}/install")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Install(Guid sparePartId, InstallSparePartRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new InstallSparePartCommand(sparePartId, request.AssetId, request.MaintenanceOrderId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{sparePartId:guid}/uninstall")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Uninstall(Guid sparePartId, UninstallSparePartRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new UninstallSparePartCommand(sparePartId, request.WarehouseOrgUnitId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{sparePartId:guid}/dispose")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Dispose(Guid sparePartId, CancellationToken cancellationToken)
    {
        await mediator.Send(new DisposeSparePartCommand(sparePartId), cancellationToken);
        return NoContent();
    }
}

public sealed record InstallSparePartRequest(Guid AssetId, Guid? MaintenanceOrderId);

public sealed record UninstallSparePartRequest(Guid WarehouseOrgUnitId);
