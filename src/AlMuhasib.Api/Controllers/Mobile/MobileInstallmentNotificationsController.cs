using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.Mobile;

[ApiController]
[Route("api/mobile/installments")]
[Authorize(Policy = "Tenant")]
public sealed class MobileInstallmentNotificationsController : ControllerBase
{
    private readonly CloudDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly INotificationService _notifications;

    public MobileInstallmentNotificationsController(
        CloudDbContext db,
        ITenantContext tenantContext,
        INotificationService notifications)
    {
        _db = db;
        _tenantContext = tenantContext;
        _notifications = notifications;
    }

    /// Sends a OneSignal push summarizing overdue installments for the current tenant.
    [HttpPost("notify-overdue")]
    public async Task<IActionResult> NotifyOverdue(CancellationToken ct)
    {
        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        _tenantContext.SetTenant(tenantId);

        var today = DateTime.UtcNow.Date;
        var overdue = await _db.Installments.AsNoTracking()
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Where(i =>
                i.Status != InstallmentStatus.Paid &&
                i.RemainingAmount > 0 &&
                i.DueDate.Date < today)
            .ToListAsync(ct);

        if (overdue.Count == 0)
            return Ok(new { sent = false, count = 0, message = "لا توجد أقساط متأخرة" });

        var total = overdue.Sum(i => i.RemainingAmount);
        var title = "أقساط متأخرة";
        var message = $"لديك {overdue.Count} قسطاً متأخراً بإجمالي {total:N0}";
        await _notifications.SendToTenantAsync(tenantId, title, message, ct);

        return Ok(new { sent = true, count = overdue.Count, totalRemaining = total, message });
    }
}
