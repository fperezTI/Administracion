using Asp.Versioning;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.ImportExport;
using AssetManagement.Domain.ImportExport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/import-batches")]
public sealed class ImportBatchesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ImportBatchSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ImportBatchSummary>>> GetBatches(
        [FromQuery] Guid companyId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null, [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetImportBatchesQuery(companyId, pageNumber, pageSize, sortBy, sortDescending), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{importBatchId:guid}")]
    [ProducesResponseType(typeof(ImportBatchDetail), StatusCodes.Status200OK)]
    public async Task<ActionResult<ImportBatchDetail>> GetById(Guid importBatchId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetImportBatchByIdQuery(importBatchId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("template")]
    public async Task<IActionResult> GetTemplate([FromQuery] Guid assetCategoryId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAssetImportTemplateQuery(assetCategoryId), cancellationToken);
        return File(result.Content, "text/csv", result.FileName);
    }

    [HttpPost]
    [RequestSizeLimit(25 * 1024 * 1024)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Upload(
        [FromForm] Guid companyId, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var command = new UploadImportBatchCommand(companyId, file.FileName, file.Length, stream);
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { importBatchId = id, version = "1.0" }, id);
    }

    [HttpPost("{importBatchId:guid}/commit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Commit(Guid importBatchId, [FromBody] CommitImportBatchRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new CommitImportBatchCommand(importBatchId, request.Mode), cancellationToken);
        return NoContent();
    }

    [HttpPost("{importBatchId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid importBatchId, CancellationToken cancellationToken)
    {
        await mediator.Send(new CancelImportBatchCommand(importBatchId), cancellationToken);
        return NoContent();
    }
}

public sealed record CommitImportBatchRequest(ImportCommitMode Mode);
