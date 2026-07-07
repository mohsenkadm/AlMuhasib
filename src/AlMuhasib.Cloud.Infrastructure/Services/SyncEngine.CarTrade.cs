using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Sync.Dtos;
using AlMuhasib.Sync.Requests;
using AlMuhasib.Sync.Responses;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Services;

public sealed partial class SyncEngine
{
    private async Task<SyncPushResponse> PushCarTradeAsync(int tenantId, SyncPushRequest request, CancellationToken ct)
    {
        var resolver = new SyncIdResolver(_db, tenantId);
        var response = new SyncPushResponse { ServerTime = DateTime.UtcNow };
        var accepted = 0;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var dto in request.Data.CarTradeTransactions)
                accepted += await UpsertCarTradeTransactionAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.CarTradeTransactions, tenantId, request.Data.CarTradeTransactions.Select(t => t.SyncId), resolver, ct);

            foreach (var dto in request.Data.CarTradePayments)
            {
                var transactionId = await resolver.ResolveCarTradeTransactionAsync(dto.TransactionSyncId, ct);
                if (transactionId is null)
                {
                    AddConflict(response, "CarTradePayment", dto.SyncId, "Transaction not found");
                    continue;
                }
                accepted += await UpsertCarTradePaymentAsync(tenantId, dto, transactionId.Value, response, ct);
            }

            var tenant = await _db.Tenants.FindAsync([tenantId], ct);
            if (tenant is not null)
                tenant.LastSyncAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            response.AcceptedCount = accepted;
            response.RejectedCount = response.Conflicts.Count;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        return response;
    }

    private async Task<SyncPullResponse> PullCarTradeAsync(int tenantId, SyncPullRequest request, CancellationToken ct)
    {
        var since = request.Since ?? DateTime.MinValue;
        var bundle = new SyncDataBundle
        {
            CarTradeTransactions = await PullEntitiesAsync(_db.CarTradeTransactions, tenantId, since, MapCarTradeTransaction, ct)
        };

        var transactionMap = await _db.CarTradeTransactions.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);

        var payments = await _db.CarTradePayments.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.CarTradePayments = payments.Select(p => new CarTradePaymentSyncDto
        {
            SyncId = p.SyncId, CreatedAt = p.CreatedAt, CreatedBy = p.CreatedBy, UpdatedAt = p.UpdatedAt, UpdatedBy = p.UpdatedBy,
            IsDeleted = p.IsDeleted, DeletedAt = p.DeletedAt, DeletedBy = p.DeletedBy, RowVersion = p.RowVersion,
            TransactionSyncId = transactionMap.GetValueOrDefault(p.TransactionId),
            PaymentDate = p.PaymentDate, Amount = p.Amount, Notes = p.Notes,
            RemainingBefore = p.RemainingBefore, RemainingAfter = p.RemainingAfter
        }).ToList();

        var serverTime = DateTime.UtcNow;
        return new SyncPullResponse
        {
            Data = bundle,
            Cursor = serverTime.Ticks.ToString(),
            ServerTime = serverTime,
            HasMore = false
        };
    }

    private static CarTradeTransactionSyncDto MapCarTradeTransaction(CloudCarTradeTransaction e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        TransactionNumber = e.TransactionNumber, TransactionDate = e.TransactionDate, TradeType = e.TradeType,
        CarName = e.CarName, CarColor = e.CarColor, PlateNumber = e.PlateNumber, ChassisNumber = e.ChassisNumber,
        CarType = e.CarType, SellerName = e.SellerName, SellerPhone = e.SellerPhone,
        BuyerName = e.BuyerName, BuyerPhone = e.BuyerPhone,
        PurchasePrice = e.PurchasePrice, SalePrice = e.SalePrice, TotalAmount = e.TotalAmount,
        PaymentMode = e.PaymentMode, AmountPaid = e.AmountPaid, RemainingAmount = e.RemainingAmount,
        Status = e.Status, Notes = e.Notes
    };

    private async Task<int> UpsertCarTradeTransactionAsync(int tenantId, CarTradeTransactionSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.CarTradeTransactions, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "CarTradeTransaction", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudCarTradeTransaction { TenantId = tenantId }; _db.CarTradeTransactions.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
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
        return 1;
    }

    private async Task<int> UpsertCarTradePaymentAsync(int tenantId, CarTradePaymentSyncDto dto, int transactionId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.CarTradePayments, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "CarTradePayment", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudCarTradePayment { TenantId = tenantId }; _db.CarTradePayments.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.TransactionId = transactionId;
        existing.PaymentDate = dto.PaymentDate;
        existing.Amount = dto.Amount;
        existing.Notes = dto.Notes;
        existing.RemainingBefore = dto.RemainingBefore;
        existing.RemainingAfter = dto.RemainingAfter;
        return 1;
    }
}
