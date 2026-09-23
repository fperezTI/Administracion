using Asp.Versioning;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Templates;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/templates")]
public sealed class TemplatesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TemplateSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TemplateSummary>>> GetTemplates(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetTemplatesQuery(pageNumber, pageSize, sortBy, sortDescending), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{templateId:guid}")]
    [ProducesResponseType(typeof(TemplateDetail), StatusCodes.Status200OK)]
    public async Task<ActionResult<TemplateDetail>> GetById(Guid templateId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTemplateByIdQuery(templateId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(CreateTemplateCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { templateId = id, version = "1.0" }, id);
    }

    [HttpPost("{templateId:guid}/versions")]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    public async Task<ActionResult<int>> AddVersion(Guid templateId, AddTemplateVersionRequest request, CancellationToken cancellationToken)
    {
        var versionNumber = await mediator.Send(new AddTemplateVersionCommand(templateId, request.Content), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { templateId, version = "1.0" }, versionNumber);
    }

    [HttpPatch("{templateId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetActive(Guid templateId, [FromBody] bool isActive, CancellationToken cancellationToken)
    {
        await mediator.Send(new SetTemplateActiveCommand(templateId, isActive), cancellationToken);
        return NoContent();
    }
}

public sealed record AddTemplateVersionRequest(string Content);
