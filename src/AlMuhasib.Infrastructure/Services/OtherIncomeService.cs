using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class OtherIncomeService : IOtherIncomeService
{
    public const string ScreenName = "OtherIncome";

    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBranchContext _branchContext;
    private readonly IAccountingPeriodLockService _periodLockService;

    public OtherIncomeService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICurrentUserService currentUserService,
        IBranchContext branchContext,
        IAccountingPeriodLockService periodLockService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _branchContext = branchContext;
        _periodLockService = periodLockService;
    }

    public async Task<IEnumerable<OtherIncomeType>> GetAllTypesAsync()
    {
        EnsureCanView();
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OtherIncomeTypes.OrderBy(t => t.Name).ToListAsync();
    }

    public async Task EnsureDefaultTypesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (await context.OtherIncomeTypes.AnyAsync())
            return;

        var defaults = new[]
        {
            "إيرادات الإيجارات",
            "إيرادات العمولات",
            "إيرادات الخدمات",
            "إيرادات متنوعة",
            "إيرادات أخرى"
        };

        var username = _currentUserService.Username ?? "system";
        foreach (var name in defaults)
        {
            await context.OtherIncomeTypes.AddAsync(new OtherIncomeType
            {
                Name = name,
                IsActive = true,
                CreatedBy = username
            });
        }

        await context.SaveChangesAsync();
    }

    public async Task<OtherIncomeType> AddTypeAsync(string name)
    {
        EnsureCanAdd();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new InvalidOperationException("اسم نوع الإيراد مطلوب");

        if (await context.OtherIncomeTypes.AnyAsync(t => t.Name == trimmed))
            throw new InvalidOperationException("اسم نوع الإيراد موجود مسبقاً");

        var softDeleted = await context.OtherIncomeTypes
            .IgnoreQueryFilters()
            .Where(t => t.IsDeleted && t.Name == trimmed && t.BranchId == _branchContext.RequireWriteBranchId())
            .OrderByDescending(t => t.DeletedAt)
            .FirstOrDefaultAsync();

        if (softDeleted is not null)
        {
            softDeleted.RestoreFromSoftDelete(_currentUserService.Username);
            softDeleted.Name = trimmed;
            softDeleted.IsActive = true;
            await context.SaveChangesAsync();
            return softDeleted;
        }

        var type = new OtherIncomeType
        {
            Name = trimmed,
            IsActive = true,
            CreatedBy = _currentUserService.Username
        };
        await context.OtherIncomeTypes.AddAsync(type);
        await context.SaveChangesAsync();
        return type;
    }

    public async Task UpdateTypeAsync(int id, string name)
    {
        EnsureCanEdit();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var type = await context.OtherIncomeTypes.FindAsync(id)
            ?? throw new InvalidOperationException("نوع الإيراد غير موجود");
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new InvalidOperationException("اسم نوع الإيراد مطلوب");

        if (await context.OtherIncomeTypes.AnyAsync(t => t.Id != id && t.Name == trimmed))
            throw new InvalidOperationException("اسم نوع الإيراد موجود مسبقاً");

        type.Name = trimmed;
        await context.SaveChangesAsync();
    }

    public async Task SetTypeActiveAsync(int id, bool isActive)
    {
        EnsureCanEdit();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var type = await context.OtherIncomeTypes.FindAsync(id)
            ?? throw new InvalidOperationException("نوع الإيراد غير موجود");
        type.IsActive = isActive;
        await context.SaveChangesAsync();
    }

    public async Task DeleteTypeAsync(int id)
    {
        EnsureCanDelete();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var type = await context.OtherIncomeTypes.FindAsync(id)
            ?? throw new InvalidOperationException("نوع الإيراد غير موجود");
        if (await context.OtherIncomes.AnyAsync(i => i.OtherIncomeTypeId == id))
            throw new InvalidOperationException("لا يمكن حذف نوع إيراد مرتبط بحركات. يمكن تعطيله.");
        type.MarkSoftDeleted(_currentUserService.Username);
        await context.SaveChangesAsync();
    }

    public async Task<OtherIncome> AddAsync(
        int typeId,
        decimal amount,
        DateTime date,
        int cashBoxId,
        string? notes,
        AccountingCurrency currency,
        decimal fxRate)
    {
        EnsureCanAdd();
        date = date.Date;
        await _periodLockService.EnsureDateAllowedAsync(date);

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var type = await context.OtherIncomeTypes.FindAsync(typeId)
                ?? throw new InvalidOperationException("نوع الإيراد غير موجود");
            if (!type.IsActive)
                throw new InvalidOperationException("نوع الإيراد موقوف");

            var cashBox = await RequireCashBoxAsync(context, cashBoxId, currency);
            (currency, fxRate) = await MultiCurrencyFeatureGate.ApplyForWriteAsync(
                context, currency, fxRate, "إيراد آخر");
            AccountingCurrencyRules.EnsureSameCurrency(currency, cashBox.Currency, "الإيراد", "القاصة");

            if (amount <= 0)
                throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر");

            var branchId = _branchContext.RequireWriteBranchId();
            var income = new OtherIncome
            {
                IncomeNumber = await NextNumberAsync(context, branchId),
                OtherIncomeTypeId = typeId,
                Amount = amount,
                Date = date,
                CashBoxId = cashBoxId,
                Notes = notes,
                Currency = currency,
                FxRate = fxRate
            };

            cashBox.Balance += amount;
            await context.OtherIncomes.AddAsync(income);
            await SaveWithNumberRetryAsync(context, income, branchId);
            await WriteAuditAsync(context, AuditAction.Add, income, $"إيراد آخر {income.IncomeNumber}: {amount:N0} إلى قاصة {cashBox.Name}");
            await transaction.CommitAsync();
            return income;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateAsync(
        int id,
        int typeId,
        decimal amount,
        DateTime date,
        int cashBoxId,
        string? notes,
        AccountingCurrency currency,
        decimal fxRate)
    {
        EnsureCanEdit();
        date = date.Date;
        if (amount <= 0)
            throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var income = await context.OtherIncomes.FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new InvalidOperationException("الإيراد غير موجود");

            await _periodLockService.EnsureDateAllowedAsync(income.Date);
            await _periodLockService.EnsureDateAllowedAsync(date);

            var type = await context.OtherIncomeTypes.FindAsync(typeId)
                ?? throw new InvalidOperationException("نوع الإيراد غير موجود");
            if (!type.IsActive && type.Id != income.OtherIncomeTypeId)
                throw new InvalidOperationException("نوع الإيراد موقوف");

            var oldBox = await context.CashBoxes.FindAsync(income.CashBoxId)
                ?? throw new InvalidOperationException("القاصة الأصلية غير موجودة");
            EnsureCanReverse(oldBox, income.Amount);
            oldBox.Balance -= income.Amount;

            var newBox = await RequireCashBoxAsync(context, cashBoxId, currency);
            (currency, fxRate) = await MultiCurrencyFeatureGate.ApplyForWriteAsync(
                context, currency, fxRate, "إيراد آخر");
            AccountingCurrencyRules.EnsureSameCurrency(currency, newBox.Currency, "الإيراد", "القاصة");
            newBox.Balance += amount;

            income.OtherIncomeTypeId = typeId;
            income.Amount = amount;
            income.Date = date;
            income.CashBoxId = cashBoxId;
            income.Notes = notes;
            income.Currency = currency;
            income.FxRate = fxRate;

            await context.SaveChangesAsync();
            await WriteAuditAsync(context, AuditAction.Edit, income, $"تعديل إيراد آخر {income.IncomeNumber}: {amount:N0}");
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        EnsureCanDelete();
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var income = await context.OtherIncomes
                .Include(i => i.OtherIncomeType)
                .FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new InvalidOperationException("الإيراد غير موجود");

            if (income.IsDeleted)
                return;

            await _periodLockService.EnsureDateAllowedAsync(income.Date);

            var cashBox = await context.CashBoxes.FindAsync(income.CashBoxId)
                ?? throw new InvalidOperationException("القاصة غير موجودة");
            EnsureCanReverse(cashBox, income.Amount);
            cashBox.Balance -= income.Amount;
            income.MarkSoftDeleted(_currentUserService.Username ?? "system");
            await context.SaveChangesAsync();
            await WriteAuditAsync(context, AuditAction.Delete, income, $"حذف إيراد آخر {income.IncomeNumber}: {income.Amount:N0}");
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<(IEnumerable<OtherIncome> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? typeId = null, int? cashBoxId = null,
        AccountingCurrency? currency = null, DateTime? fromDate = null, DateTime? toDate = null,
        string? searchTerm = null)
    {
        EnsureCanView();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.OtherIncomes
            .Include(i => i.OtherIncomeType)
            .Include(i => i.CashBox)
            .AsQueryable();

        if (typeId.HasValue) query = query.Where(i => i.OtherIncomeTypeId == typeId.Value);
        if (cashBoxId.HasValue) query = query.Where(i => i.CashBoxId == cashBoxId.Value);
        if (currency.HasValue) query = query.Where(i => i.Currency == currency.Value);
        if (fromDate.HasValue) query = query.Where(i => i.Date >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(i => i.Date < toDate.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(i =>
                i.IncomeNumber.Contains(term)
                || (i.Notes != null && i.Notes.Contains(term))
                || i.OtherIncomeType.Name.Contains(term));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, totalCount);
    }

    private static async Task<CashBox> RequireCashBoxAsync(AppDbContext context, int cashBoxId, AccountingCurrency currency)
    {
        var cashBox = await context.CashBoxes.FindAsync(cashBoxId)
            ?? throw new InvalidOperationException("القاصة غير موجودة");
        AccountingCurrencyRules.EnsureSameCurrency(currency, cashBox.Currency, "الإيراد", "القاصة");
        return cashBox;
    }

    private static void EnsureCanReverse(CashBox cashBox, decimal amount)
    {
        if (cashBox.Balance < amount)
            throw new InvalidOperationException(
                $"رصيد القاصة «{cashBox.Name}» غير كافٍ لعكس الإيراد. الرصيد الحالي: {cashBox.Balance:N0}");
    }

    private static async Task<string> NextNumberAsync(AppDbContext context, int branchId)
    {
        var last = await context.OtherIncomes
            .IgnoreQueryFilters()
            .Where(i => i.BranchId == branchId && i.IncomeNumber.StartsWith("OI-"))
            .OrderByDescending(i => i.Id)
            .Select(i => i.IncomeNumber)
            .FirstOrDefaultAsync();

        var next = 1;
        if (last is not null && int.TryParse(last.AsSpan(3), out var parsed))
            next = parsed + 1;
        return $"OI-{next:D6}";
    }

    private static async Task SaveWithNumberRetryAsync(AppDbContext context, OtherIncome income, int branchId)
    {
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            income.IncomeNumber = await NextNumberAsync(context, branchId);
            await context.SaveChangesAsync();
        }
    }

    private async Task WriteAuditAsync(AppDbContext context, AuditAction action, OtherIncome income, string text)
    {
        if (!_currentUserService.UserId.HasValue)
            return;

        await context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = _currentUserService.UserId.Value,
            Action = action,
            EntityName = "OtherIncome",
            EntityId = income.Id,
            NewValues = action == AuditAction.Delete ? null : text,
            OldValues = action == AuditAction.Delete ? text : null,
            Timestamp = DateTime.UtcNow,
            CreatedBy = _currentUserService.Username ?? "system",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner.Message.Contains("IX_OtherIncomes_BranchId_IncomeNumber", StringComparison.OrdinalIgnoreCase)
                || inner.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void EnsureCanView() => Ensure(_currentUserService.IsAdmin || _currentUserService.CanView(ScreenName));
    private void EnsureCanAdd() => Ensure(_currentUserService.IsAdmin || (_currentUserService.CanAdd(ScreenName) && !_currentUserService.IsViewOnly(ScreenName)));
    private void EnsureCanEdit() => Ensure(_currentUserService.IsAdmin || (_currentUserService.CanEdit(ScreenName) && !_currentUserService.IsViewOnly(ScreenName)));
    private void EnsureCanDelete() => Ensure(_currentUserService.IsAdmin || (_currentUserService.CanDelete(ScreenName) && !_currentUserService.IsViewOnly(ScreenName)));

    private static void Ensure(bool allowed)
    {
        if (!allowed)
            throw new InvalidOperationException("ليست لديك صلاحية هذه العملية على الإيرادات الأخرى");
    }
}
