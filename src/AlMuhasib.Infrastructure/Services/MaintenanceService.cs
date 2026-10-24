using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class MaintenanceService : IMaintenanceService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICashBankService _cashBankService;

    public MaintenanceService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICurrentUserService currentUserService,
        ICashBankService cashBankService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _cashBankService = cashBankService;
    }

    public async Task<MaintenanceRecord?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MaintenanceRecords
            .Include(m => m.Customer)
            .Include(m => m.Product)
            .Include(m => m.CashBox)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IReadOnlyList<MaintenanceRecord>> GetRecentAsync(int take = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MaintenanceRecords
            .Include(m => m.Customer)
            .Include(m => m.Product)
            .OrderByDescending(m => m.Date)
            .ThenByDescending(m => m.Id)
            .Take(take)
            .ToListAsync();
    }

    public async Task<string> GetNextRecordNumberAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await NextNumberAsync(context);
    }

    public async Task<MaintenanceRecord> SaveAsync(MaintenanceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (record.CustomerId <= 0)
            throw new InvalidOperationException("اختر الزبون");
        if (string.IsNullOrWhiteSpace(record.ProductName))
            throw new InvalidOperationException("أدخل اسم المنتج");
        if (record.CostAmount < 0)
            throw new InvalidOperationException("مبلغ الصيانة غير صالح");
        if (record.Status != MaintenanceStatus.Delivered)
            record.WarrantyEndDate = null;
        else if (!record.WarrantyEndDate.HasValue)
            throw new InvalidOperationException("أدخل تاريخ انتهاء الضمان بعد التسليم");

        var username = _currentUserService.Username ?? "system";
        await using var context = await _contextFactory.CreateDbContextAsync();

        MaintenanceRecord entity;
        int? previousVoucherId = null;
        decimal previousAmount = 0;
        int? previousCashBoxId = null;

        if (record.Id > 0)
        {
            entity = await context.MaintenanceRecords.FirstOrDefaultAsync(m => m.Id == record.Id)
                ?? throw new InvalidOperationException("سجل الصيانة غير موجود");
            previousVoucherId = entity.VoucherId;
            previousAmount = entity.CostAmount;
            previousCashBoxId = entity.CashBoxId;

            entity.Date = record.Date.Date;
            entity.CustomerId = record.CustomerId;
            entity.ProductId = record.ProductId;
            entity.ProductName = record.ProductName.Trim();
            entity.SerialNumber = (record.SerialNumber ?? string.Empty).Trim();
            entity.CostAmount = record.CostAmount;
            entity.Notes = string.IsNullOrWhiteSpace(record.Notes) ? null : record.Notes.Trim();
            entity.Status = record.Status;
            entity.WarrantyEndDate = record.WarrantyEndDate?.Date;
            entity.CashBoxId = record.CashBoxId;
            entity.Currency = record.Currency;
            entity.FxRate = record.FxRate <= 0 ? 1m : record.FxRate;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = username;
        }
        else
        {
            entity = new MaintenanceRecord
            {
                RecordNumber = string.IsNullOrWhiteSpace(record.RecordNumber)
                    ? await NextNumberAsync(context)
                    : record.RecordNumber.Trim(),
                Date = record.Date.Date,
                CustomerId = record.CustomerId,
                ProductId = record.ProductId,
                ProductName = record.ProductName.Trim(),
                SerialNumber = (record.SerialNumber ?? string.Empty).Trim(),
                CostAmount = record.CostAmount,
                Notes = string.IsNullOrWhiteSpace(record.Notes) ? null : record.Notes.Trim(),
                Status = record.Status,
                WarrantyEndDate = record.WarrantyEndDate?.Date,
                CashBoxId = record.CashBoxId,
                Currency = record.Currency,
                FxRate = record.FxRate <= 0 ? 1m : record.FxRate,
                CreatedBy = username,
                CreatedAt = DateTime.UtcNow
            };
            await context.MaintenanceRecords.AddAsync(entity);
        }

        await context.SaveChangesAsync();

        // أعد سند القبض عند تغيّر المبلغ/القاصة
        if (previousVoucherId is int oldVoucherId)
        {
            try
            {
                await _cashBankService.DeleteVoucherAsync(oldVoucherId);
            }
            catch
            {
                // إن تعذر الحذف نترك السند القديم وننشئ واحداً جديداً فقط عند الحاجة
            }

            entity.VoucherId = null;
            await context.SaveChangesAsync();
        }

        if (entity.CostAmount > 0 && entity.CashBoxId is int cashBoxId)
        {
            var voucher = new Voucher
            {
                VoucherType = VoucherType.Receipt,
                Amount = entity.CostAmount,
                CashBoxId = cashBoxId,
                CustomerId = entity.CustomerId,
                Currency = entity.Currency,
                FxRate = entity.FxRate,
                Date = entity.Date,
                Notes = $"صيانة {entity.RecordNumber} — {entity.ProductName}"
            };
            var savedVoucher = await _cashBankService.CreateVoucherAsync(voucher);
            entity.VoucherId = savedVoucher.Id;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = username;
            await context.SaveChangesAsync();
        }
        else if (previousAmount > 0 && previousCashBoxId.HasValue && entity.CostAmount <= 0)
        {
            // تمت إزالة المبلغ — لا سند جديد
        }

        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.MaintenanceRecords.FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new InvalidOperationException("سجل الصيانة غير موجود");

        if (entity.VoucherId is int voucherId)
        {
            try
            {
                await _cashBankService.DeleteVoucherAsync(voucherId);
            }
            catch
            {
                // تابع حذف السجل حتى لو فشل عكس السند
            }
        }

        entity.MarkSoftDeleted(_currentUserService.Username ?? "system");
        entity.VoucherId = null;
        await context.SaveChangesAsync();
    }

    private static async Task<string> NextNumberAsync(AppDbContext context)
    {
        const string prefix = "MNT";
        var numbers = await context.MaintenanceRecords
            .IgnoreQueryFilters()
            .Where(m => m.RecordNumber.StartsWith(prefix + "-"))
            .Select(m => m.RecordNumber)
            .ToListAsync();

        var max = 0;
        foreach (var n in numbers)
        {
            var parts = n.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && int.TryParse(parts[^1], out var seq) && seq > max)
                max = seq;
        }

        return $"{prefix}-{(max + 1):D5}";
    }
}
