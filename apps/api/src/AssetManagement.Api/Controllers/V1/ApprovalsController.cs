using Asp.Versioning;
using AssetManagement.Application.Approvals;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/approvals")]
public sealed class ApprovalsController(ISender mediator) : ControllerBase
{
    /// <summary>Self-service: pending approvals the caller is currently eligible to decide — no permission
    /// required, see GetMyPendingApprovalsQuery.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<MyPendingApprovalSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MyPendingApprovalSummary>>> GetMine(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMyPendingApprovalsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{approvalInstanceId:guid}")]
    [ProducesResponseType(typeof(ApprovalInstanceDetail), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApprovalInstanceDetail>> GetById(Guid approvalInstanceId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetApprovalInstanceByIdQuery(approvalInstanceId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{approvalInstanceId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Approve(Guid approvalInstanceId, DecideApprovalRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new ApproveApprovalStepCommand(approvalInstanceId, request.Comment, request.SignatureMechanism, request.TypedFullName, request.SignatureImageDataUrl),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{approvalInstanceId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(Guid approvalInstanceId, DecideApprovalRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new RejectApprovalStepCommand(approvalInstanceId, request.Comment ?? string.Empty, request.SignatureMechanism, request.TypedFullName, request.SignatureImageDataUrl),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{approvalInstanceId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid approvalInstanceId, CancellationToken cancellationToken)
    {
        await mediator.Send(new CancelApprovalInstanceCommand(approvalInstanceId), cancellationToken);
        return NoContent();
    }
}

public sealed record DecideApprovalRequest(string? Comment, string SignatureMechanism, string? TypedFullName, string? SignatureImageDataUrl);
