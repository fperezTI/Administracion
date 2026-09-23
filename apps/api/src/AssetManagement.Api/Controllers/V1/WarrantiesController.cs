using Asp.Versioning;
using AssetManagement.Application.Common.Models;
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
    [ProducesResponseType(typeof(PagedResult<WarrantySummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<WarrantySummary>>> GetWarranties(
        [FromQuery] Guid companyId,
        [FromQuery] Guid? assetId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetWarrantiesQuery(companyId, assetId, pageNumber, pageSize, sortBy, sortDescending), cancellationToken);
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
