using Asp.Versioning;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Organization.Companies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/companies")]
public sealed class CompaniesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CompanySummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CompanySummary>>> GetCompanies(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetCompaniesQuery(pageNumber, pageSize, isActive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{companyId:guid}")]
    [ProducesResponseType(typeof(CompanySummary), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanySummary>> GetCompanyById(Guid companyId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCompanyByIdQuery(companyId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateCompany(CreateCompanyCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetCompanyById), new { companyId = id, version = "1.0" }, id);
    }

    [HttpPatch("{companyId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetActive(Guid companyId, [FromBody] bool isActive, CancellationToken cancellationToken)
    {
        await mediator.Send(new SetCompanyActiveCommand(companyId, isActive), cancellationToken);
        return NoContent();
    }
}
