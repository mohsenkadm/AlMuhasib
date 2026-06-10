using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Sync.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMuhasib.Api.Controllers;

[ApiController]
[Route("api/devices")]
[Authorize(Policy = "Tenant")]
public sealed class DevicesController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public DevicesController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceRequest request, CancellationToken ct)
    {
        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        await _notificationService.RegisterDeviceAsync(tenantId, request.PlayerId, request.DeviceName, request.Platform, ct);
        return NoContent();
    }
}
