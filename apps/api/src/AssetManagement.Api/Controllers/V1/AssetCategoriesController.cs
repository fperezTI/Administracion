using Asp.Versioning;
using AssetManagement.Application.Assets.Categories;
using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Assets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/asset-categories")]
public sealed class AssetCategoriesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssetCategorySummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssetCategorySummary>>> GetCategories(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] bool? isActive = null,
        [FromQuery] string? sortBy = null, [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetAssetCategoriesQuery(pageNumber, pageSize, isActive, sortBy, sortDescending), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{assetCategoryId:guid}")]
    [ProducesResponseType(typeof(AssetCategoryDetail), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssetCategoryDetail>> GetCategoryById(Guid assetCategoryId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAssetCategoryByIdQuery(assetCategoryId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(CreateAssetCategoryCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetCategoryById), new { assetCategoryId = id, version = "1.0" }, id);
    }

    [HttpPost("{assetCategoryId:guid}/custom-fields")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> AddCustomField(
        Guid assetCategoryId, AddCustomFieldDefinitionRequest request, CancellationToken cancellationToken)
    {
        var command = new AddCustomFieldDefinitionCommand(
            assetCategoryId, request.Name, request.Code, request.DataType, request.IsRequired, request.Options);
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetCategoryById), new { assetCategoryId, version = "1.0" }, id);
    }

    [HttpPatch("{assetCategoryId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetActive(Guid assetCategoryId, [FromBody] bool isActive, CancellationToken cancellationToken)
    {
        await mediator.Send(new SetAssetCategoryActiveCommand(assetCategoryId, isActive), cancellationToken);
        return NoContent();
    }
}

public sealed record AddCustomFieldDefinitionRequest(
    string Name, string Code, CustomFieldDataType DataType, bool IsRequired, string? Options);
