using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Sync.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMuhasib.Api.Controllers;

[ApiController]
[Route("api/admin/notifications")]
[Authorize(Policy = "Developer")]
public sealed class AdminNotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public AdminNotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequest request, CancellationToken ct)
    {
        if (request.TenantId.HasValue)
            await _notificationService.SendToTenantAsync(request.TenantId.Value, request.Title, request.Message, ct);
        else
            await _notificationService.SendToAllAsync(request.Title, request.Message, ct);

        return NoContent();
    }
}
