using Asp.Versioning;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Inventory;
using AssetManagement.Domain.Assets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/assets")]
public sealed class AssetsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssetSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssetSummary>>> GetAssets(
        [FromQuery] Guid companyId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? assetCategoryId = null,
        [FromQuery] AssetStatus? status = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAssetsQuery(companyId, pageNumber, pageSize, assetCategoryId, status, search);
        var result = await mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{assetId:guid}")]
    [ProducesResponseType(typeof(AssetDetail), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssetDetail>> GetAssetById(Guid assetId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAssetByIdQuery(assetId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateAssetResult), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateAssetResult>> Create(CreateAssetCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAssetById), new { assetId = result.AssetId, version = "1.0" }, result);
    }

    [HttpPut("{assetId:guid}/general")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateGeneralInfo(
        Guid assetId, UpdateAssetGeneralInfoRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateAssetGeneralInfoCommand(
            assetId, request.Brand, request.Model, request.SerialNumber, request.Description,
            request.PatrimonialFolio, request.PhysicalCondition, request.CustomFieldValues);
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("{assetId:guid}/financial")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateFinancialInfo(
        Guid assetId, UpdateAssetFinancialInfoRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateAssetFinancialInfoCommand(
            assetId, request.AcquisitionDate, request.AcquisitionCost, request.Currency, request.Supplier,
            request.Invoice, request.PurchaseOrder);
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("{assetId:guid}/contractual")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateContractualInfo(
        Guid assetId, UpdateAssetContractualInfoRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateAssetContractualInfoCommand(
            assetId, request.WarrantyStartDate, request.WarrantyEndDate, request.SupportContract, request.SupportProvider);
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("{assetId:guid}/accessories/candidates")]
    [ProducesResponseType(typeof(IReadOnlyList<AccessoryCandidate>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccessoryCandidate>>> GetAccessoryCandidates(
        Guid assetId, [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEligibleAccessoryCandidatesQuery(companyId, assetId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{assetId:guid}/accessories/{accessoryAssetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LinkAccessory(Guid assetId, Guid accessoryAssetId, CancellationToken cancellationToken)
    {
        await mediator.Send(new LinkAssetAccessoryCommand(assetId, accessoryAssetId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{assetId:guid}/accessories/{accessoryAssetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnlinkAccessory(Guid assetId, Guid accessoryAssetId, CancellationToken cancellationToken)
    {
        await mediator.Send(new UnlinkAssetAccessoryCommand(accessoryAssetId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{assetId:guid}/tag/reprint")]
    [ProducesResponseType(typeof(ReprintAssetTagResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReprintAssetTagResult>> ReprintTag(Guid assetId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ReprintAssetTagCommand(assetId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{assetId:guid}/relocate")]
    [ProducesResponseType(typeof(RelocateAssetResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<RelocateAssetResult>> Relocate(
        Guid assetId, RelocateAssetRequest request, CancellationToken cancellationToken)
    {
        var command = new RelocateAssetCommand(assetId, request.NewOrgUnitId, request.Notes);
        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{assetId:guid}/decommission")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> RequestDecommission(
        Guid assetId, RequestAssetDecommissionRequest request, CancellationToken cancellationToken)
    {
        var approvalInstanceId = await mediator.Send(new RequestAssetDecommissionCommand(assetId, request.Justification), cancellationToken);
        return Ok(approvalInstanceId);
    }

    [HttpPost("{assetId:guid}/dispose")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> RequestDisposal(
        Guid assetId, RequestAssetDisposalRequest request, CancellationToken cancellationToken)
    {
        var approvalInstanceId = await mediator.Send(
            new RequestAssetDisposalCommand(assetId, request.TargetStatus, request.Justification), cancellationToken);
        return Ok(approvalInstanceId);
    }
}

public sealed record RequestAssetDecommissionRequest(string Justification);

public sealed record RequestAssetDisposalRequest(AssetStatus TargetStatus, string Justification);

public sealed record UpdateAssetGeneralInfoRequest(
    string Brand,
    string Model,
    string? SerialNumber,
    string? Description,
    string? PatrimonialFolio,
    PhysicalCondition PhysicalCondition,
    IReadOnlyDictionary<Guid, string>? CustomFieldValues);

public sealed record RelocateAssetRequest(Guid? NewOrgUnitId, string? Notes);

public sealed record UpdateAssetFinancialInfoRequest(
    DateOnly? AcquisitionDate,
    decimal? AcquisitionCost,
    string? Currency,
    string? Supplier,
    string? Invoice,
    string? PurchaseOrder);

public sealed record UpdateAssetContractualInfoRequest(
    DateOnly? WarrantyStartDate, DateOnly? WarrantyEndDate, string? SupportContract, string? SupportProvider);
