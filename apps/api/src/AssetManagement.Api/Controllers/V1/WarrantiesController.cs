using Asp.Versioning;
using AssetManagement.Application.Maintenance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/warranties")]
public sealed class WarrantiesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WarrantySummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WarrantySummary>>> GetWarranties(
        [FromQuery] Guid companyId, [FromQuery] Guid? assetId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWarrantiesQuery(companyId, assetId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(CreateWarrantyCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetWarranties), new { assetId = command.AssetId, version = "1.0" }, id);
    }

    [HttpPut("{warrantyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid warrantyId, UpdateWarrantyRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new UpdateWarrantyCommand(warrantyId, request.Type, request.Provider, request.StartDate, request.EndDate, request.Terms),
            cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateWarrantyRequest(
    AssetManagement.Domain.Maintenance.WarrantyType Type, string Provider, DateOnly StartDate, DateOnly EndDate, string? Terms);
