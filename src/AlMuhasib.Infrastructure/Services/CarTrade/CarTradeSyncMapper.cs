using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Entities.CarTrade;
using AlMuhasib.Infrastructure.Data.CarTrade;
using AlMuhasib.Sync.Dtos;
using AlMuhasib.Sync.Requests;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.CarTrade;

internal static class CarTradeSyncMapper
{
    public static async Task<SyncDataBundle> BuildPushBundleAsync(
        CarTradeDbContext db,
        DateTime? since,
        CancellationToken ct)
    {
        var cutoff = since ?? DateTime.MinValue;
        bool ShouldSync(BaseEntity e) =>
            (e.UpdatedAt ?? e.CreatedAt) >= cutoff
            || (e.IsDeleted && (e.DeletedAt ?? e.UpdatedAt ?? e.CreatedAt) >= cutoff);

        var transactions = await db.CarTradeTransactions.IgnoreQueryFilters()
            .Include(t => t.Payments)
            .ToListAsync(ct);

        var transactionMap = transactions.ToDictionary(t => t.Id, t => t.SyncId);
        var changedTransactions = transactions.Where(ShouldSync).ToList();
        var changedPayments = transactions
            .SelectMany(t => t.Payments)
            .Where(ShouldSync)
            .ToList();

        return new SyncDataBundle
        {
            CarTradeTransactions = changedTransactions.Select(MapTransaction).ToList(),
            CarTradePayments = changedPayments
                .Where(p => transactionMap.ContainsKey(p.TransactionId))
                .Select(p => MapPayment(p, transactionMap))
                .ToList()
        };
    }

    public static async Task ApplyPullBundleAsync(CarTradeDbContext db, SyncDataBundle data, CancellationToken ct)
    {
        db.IsApplyingSyncPull = true;
        try
        {
            var transactionMap = await ApplyTransactionsAsync(db, data.CarTradeTransactions, ct);
            await ApplyPaymentsAsync(db, data.CarTradePayments, transactionMap, ct);
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            db.IsApplyingSyncPull = false;
        }
    }

    private static void CopyBase(BaseEntity e, SyncDtoBase d)
    {
        d.SyncId = e.SyncId;
        d.CreatedAt = e.CreatedAt;
        d.CreatedBy = e.CreatedBy;
        d.UpdatedAt = e.UpdatedAt;
        d.UpdatedBy = e.UpdatedBy;
        d.IsDeleted = e.IsDeleted;
        d.DeletedAt = e.DeletedAt;
        d.DeletedBy = e.DeletedBy;
        d.RowVersion = e.RowVersion;
    }

    private static void ApplyBase(BaseEntity e, SyncDtoBase d)
    {
        e.SyncId = d.SyncId;
        e.CreatedAt = d.CreatedAt;
        e.CreatedBy = d.CreatedBy;
        e.UpdatedAt = d.UpdatedAt;
        e.UpdatedBy = d.UpdatedBy;
        e.IsDeleted = d.IsDeleted;
        e.DeletedAt = d.DeletedAt;
        e.DeletedBy = d.DeletedBy;
    }

    private static CarTradeTransactionSyncDto MapTransaction(CarTradeTransaction t)
    {
        var dto = new CarTradeTransactionSyncDto
        {
            TransactionNumber = t.TransactionNumber,
            TransactionDate = t.TransactionDate,
            TradeType = t.TradeType,
            CarName = t.CarName,
            CarColor = t.CarColor,
            PlateNumber = t.PlateNumber,
            ChassisNumber = t.ChassisNumber,
            CarType = t.CarType,
            SellerName = t.SellerName,
            SellerPhone = t.SellerPhone,
            BuyerName = t.BuyerName,
            BuyerPhone = t.BuyerPhone,
            PurchasePrice = t.PurchasePrice,
            SalePrice = t.SalePrice,
            TotalAmount = t.TotalAmount,
            PaymentMode = t.PaymentMode,
            AmountPaid = t.AmountPaid,
            RemainingAmount = t.RemainingAmount,
            Status = t.Status,
            Notes = t.Notes
        };
        CopyBase(t, dto);
        return dto;
    }

    private static CarTradePaymentSyncDto MapPayment(
        CarTradePayment p,
        Dictionary<int, Guid> transactionMap) => new()
    {
        TransactionSyncId = transactionMap[p.TransactionId],
        PaymentDate = p.PaymentDate,
        Amount = p.Amount,
        Notes = p.Notes,
        RemainingBefore = p.RemainingBefore,
        RemainingAfter = p.RemainingAfter,
        SyncId = p.SyncId,
        CreatedAt = p.CreatedAt,
        CreatedBy = p.CreatedBy,
        UpdatedAt = p.UpdatedAt,
        UpdatedBy = p.UpdatedBy,
        IsDeleted = p.IsDeleted,
        DeletedAt = p.DeletedAt,
        DeletedBy = p.DeletedBy,
        RowVersion = p.RowVersion
    };

    private static async Task<Dictionary<Guid, int>> ApplyTransactionsAsync(
        CarTradeDbContext db,
        List<CarTradeTransactionSyncDto> dtos,
        CancellationToken ct)
    {
        var map = new Dictionary<Guid, int>();
        foreach (var dto in dtos)
        {
            var existing = await db.CarTradeTransactions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new CarTradeTransaction();
                db.CarTradeTransactions.Add(existing);
            }

            existing.TransactionNumber = dto.TransactionNumber;
            existing.TransactionDate = dto.TransactionDate;
            existing.TradeType = dto.TradeType;
            existing.CarName = dto.CarName;
            existing.CarColor = dto.CarColor;
            existing.PlateNumber = dto.PlateNumber;
            existing.ChassisNumber = dto.ChassisNumber;
            existing.CarType = dto.CarType;
            existing.SellerName = dto.SellerName;
            existing.SellerPhone = dto.SellerPhone;
            existing.BuyerName = dto.BuyerName;
            existing.BuyerPhone = dto.BuyerPhone;
            existing.PurchasePrice = dto.PurchasePrice;
            existing.SalePrice = dto.SalePrice;
            existing.TotalAmount = dto.TotalAmount;
            existing.PaymentMode = dto.PaymentMode;
            existing.AmountPaid = dto.AmountPaid;
            existing.RemainingAmount = dto.RemainingAmount;
            existing.Status = dto.Status;
            existing.Notes = dto.Notes;
            ApplyBase(existing, dto);
            await db.SaveChangesAsync(ct);
            map[dto.SyncId] = existing.Id;
        }

        return map;
    }

    private static async Task ApplyPaymentsAsync(
        CarTradeDbContext db,
        List<CarTradePaymentSyncDto> dtos,
        Dictionary<Guid, int> transactionMap,
        CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            if (!transactionMap.TryGetValue(dto.TransactionSyncId, out var transactionId))
                continue;

            var existing = await db.CarTradePayments.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new CarTradePayment { TransactionId = transactionId };
                db.CarTradePayments.Add(existing);
            }

            existing.TransactionId = transactionId;
            existing.PaymentDate = dto.PaymentDate;
            existing.Amount = dto.Amount;
            existing.Notes = dto.Notes;
            existing.RemainingBefore = dto.RemainingBefore;
            existing.RemainingAfter = dto.RemainingAfter;
            ApplyBase(existing, dto);
        }
    }
}
