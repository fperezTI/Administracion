using Asp.Versioning;
using AssetManagement.Application.Audit;
using AssetManagement.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/audit-entries")]
public sealed class AuditController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditEntrySummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditEntrySummary>>> GetEntries(
        [FromQuery] Guid? companyId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? commandName = null,
        [FromQuery] DateTimeOffset? fromUtc = null,
        [FromQuery] DateTimeOffset? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetAuditEntriesQuery(companyId, pageNumber, pageSize, userId, commandName, fromUtc, toUtc), cancellationToken);
        return Ok(result);
    }
}
