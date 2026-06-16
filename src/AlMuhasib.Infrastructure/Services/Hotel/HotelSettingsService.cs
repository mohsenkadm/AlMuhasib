using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

public sealed class HotelSettingsService : IHotelSettingsService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public HotelSettingsService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.HotelSettings.AnyAsync(s => s.IsConfigured, cancellationToken);
    }

    public async Task<HotelSettings?> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.HotelSettings.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<HotelSettings> SaveSettingsAsync(HotelSettings settings, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        if (settings.Id > 0)
        {
            var existing = await context.HotelSettings.FirstOrDefaultAsync(s => s.Id == settings.Id, cancellationToken)
                ?? throw new InvalidOperationException("إعدادات الفندق غير موجودة");

            existing.HotelName = settings.HotelName;
            existing.Address = settings.Address;
            existing.Phone = settings.Phone;
            existing.Email = settings.Email;
            existing.CheckInTime = settings.CheckInTime;
            existing.CheckOutTime = settings.CheckOutTime;
            existing.CancellationPolicy = settings.CancellationPolicy;
            existing.Currency = settings.Currency;
            existing.IsConfigured = settings.IsConfigured;

            await context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        await context.HotelSettings.AddAsync(settings, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task MarkConfiguredAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var settings = await context.HotelSettings.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new HotelSettings { IsConfigured = true };
            await context.HotelSettings.AddAsync(settings, cancellationToken);
        }
        else
        {
            settings.IsConfigured = true;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
