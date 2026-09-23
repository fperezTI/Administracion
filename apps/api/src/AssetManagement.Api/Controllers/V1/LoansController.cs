using Asp.Versioning;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Inventory;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/loans")]
public sealed class LoansController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LoanSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LoanSummary>>> GetLoans(
        [FromQuery] Guid companyId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] LoanStatus? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetLoansQuery(companyId, pageNumber, pageSize, status, sortBy, sortDescending), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{loanId:guid}")]
    [ProducesResponseType(typeof(LoanDetail), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoanDetail>> GetById(Guid loanId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetLoanByIdQuery(loanId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateLoanResult), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateLoanResult>> Create(CreateLoanCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { loanId = result.LoanId, version = "1.0" }, result);
    }

    [HttpPost("{loanId:guid}/return")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Return(Guid loanId, ReturnLoanRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new ReturnLoanCommand(loanId, request.Notes), cancellationToken);
        return NoContent();
    }
}

public sealed record ReturnLoanRequest(string? Notes);
