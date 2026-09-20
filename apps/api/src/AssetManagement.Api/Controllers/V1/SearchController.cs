using Asp.Versioning;
using AssetManagement.Application.Search;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/search")]
public sealed class SearchController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SearchResultItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SearchResultItem>>> Search(
        [FromQuery] string term, [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GlobalSearchQuery(term ?? string.Empty, companyId), cancellationToken);
        return Ok(result);
    }
}
