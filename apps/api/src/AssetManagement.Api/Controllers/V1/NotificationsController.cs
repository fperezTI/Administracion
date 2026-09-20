using Asp.Versioning;
using AssetManagement.Application.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/notifications")]
public sealed class NotificationsController(ISender mediator) : ControllerBase
{
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<MyNotificationSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MyNotificationSummary>>> GetMine(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMyNotificationsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken cancellationToken)
    {
        await mediator.Send(new MarkNotificationAsReadCommand(notificationId), cancellationToken);
        return NoContent();
    }
}
