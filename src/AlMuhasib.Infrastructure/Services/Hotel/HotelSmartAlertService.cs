using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

public sealed class HotelSmartAlertService : IHotelSmartAlertService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public HotelSmartAlertService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<SmartAlert>> GetAlertsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var overdueCheckouts = await context.Reservations
            .AsNoTracking()
            .Include(r => r.Guest)
            .Where(r => r.Status == ReservationStatus.CheckedIn && r.CheckOutDate.Date < today)
            .CountAsync(cancellationToken);

        var pendingArrivals = await context.Reservations
            .AsNoTracking()
            .Where(r => r.Status == ReservationStatus.Confirmed && r.CheckInDate.Date == today)
            .CountAsync(cancellationToken);

        var dirtyRooms = await context.Rooms
            .AsNoTracking()
            .CountAsync(r => r.Status == RoomStatus.Dirty, cancellationToken);

        var alerts = new List<SmartAlert>();

        if (overdueCheckouts > 0)
        {
            alerts.Add(new SmartAlert
            {
                Title = "تأخر في المغادرة",
                Message = $"يوجد {overdueCheckouts} حجز/حجوزات تجاوزت موعد المغادرة",
                Severity = SmartAlertSeverity.Critical,
                Count = overdueCheckouts,
                Action = SmartAlertAction.OpenHotelCheckInOut
            });
        }

        if (pendingArrivals > 0)
        {
            alerts.Add(new SmartAlert
            {
                Title = "وصول اليوم",
                Message = $"يوجد {pendingArrivals} وصول/وصولات متوقعة اليوم",
                Severity = SmartAlertSeverity.Info,
                Count = pendingArrivals,
                Action = SmartAlertAction.OpenHotelCheckInOut
            });
        }

        if (dirtyRooms > 0)
        {
            alerts.Add(new SmartAlert
            {
                Title = "غرف تحتاج تنظيف",
                Message = $"يوجد {dirtyRooms} غرفة/غرف بحالة «تحتاج تنظيف»",
                Severity = SmartAlertSeverity.Warning,
                Count = dirtyRooms,
                Action = SmartAlertAction.OpenHotelHousekeeping
            });
        }

        return alerts;
    }
}
