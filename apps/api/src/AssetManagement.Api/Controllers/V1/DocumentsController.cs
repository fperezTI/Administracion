using Asp.Versioning;
using AssetManagement.Application.Documents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/documents")]
public sealed class DocumentsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentSummary>>> GetDocuments(
        [FromQuery] string entityType, [FromQuery] Guid entityId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetDocumentsQuery(entityType, entityId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequestSizeLimit(25 * 1024 * 1024)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Upload(
        [FromForm] string entityType, [FromForm] Guid entityId, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var command = new UploadDocumentCommand(entityType, entityId, file.FileName, file.ContentType, file.Length, stream);
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetDocuments), new { entityType, entityId, version = "1.0" }, id);
    }

    [HttpGet("{documentId:guid}/content")]
    public async Task<IActionResult> GetContent(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetDocumentContentQuery(documentId), cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }
}
