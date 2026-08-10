namespace AlMuhasib.Core.Interfaces.Services;

/// <summary>قفل الفترة المحاسبية — يمنع إنشاء/تعديل مستندات بتاريخ ضمن الفترة المقفلة.</summary>
public interface IAccountingPeriodLockService
{
    Task EnsureDateAllowedAsync(DateTime documentDate, CancellationToken cancellationToken = default);
    Task<(bool IsLocked, DateTime? LockedThroughDate)> GetLockInfoAsync(CancellationToken cancellationToken = default);
}
