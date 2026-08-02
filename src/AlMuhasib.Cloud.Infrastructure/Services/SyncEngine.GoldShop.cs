using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Sync.Dtos;
using AlMuhasib.Sync.Requests;
using AlMuhasib.Sync.Responses;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Services;

public sealed partial class SyncEngine
{
    private async Task<SyncPushResponse> PushGoldShopAsync(int tenantId, SyncPushRequest request, CancellationToken ct)
    {
        var resolver = new SyncIdResolver(_db, tenantId);
        var response = new SyncPushResponse { ServerTime = DateTime.UtcNow };
        var accepted = 0;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var dto in request.Data.GoldSettings)
                accepted += await UpsertGoldSettingsAsync(tenantId, dto, response, ct);

            foreach (var dto in request.Data.GoldFxRates)
                accepted += await UpsertGoldFxRateAsync(tenantId, dto, response, ct);

            foreach (var dto in request.Data.GoldKarats)
                accepted += await UpsertGoldKaratAsync(tenantId, dto, response, ct);

            foreach (var dto in request.Data.GoldMithqalPrices)
                accepted += await UpsertGoldMithqalPriceAsync(tenantId, dto, response, ct);

            foreach (var dto in request.Data.GoldItems)
                accepted += await UpsertGoldItemAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.GoldItems, tenantId, request.Data.GoldItems.Select(i => i.SyncId), resolver, ct);

            foreach (var dto in request.Data.GoldStockBalances)
                accepted += await UpsertGoldStockBalanceAsync(tenantId, dto, response, ct);

            foreach (var dto in request.Data.GoldCustomers)
                accepted += await UpsertGoldCustomerAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.GoldCustomers, tenantId, request.Data.GoldCustomers.Select(c => c.SyncId), resolver, ct);

            foreach (var dto in request.Data.GoldCashBoxes)
                accepted += await UpsertGoldCashBoxAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.GoldCashBoxes, tenantId, request.Data.GoldCashBoxes.Select(c => c.SyncId), resolver, ct);

            foreach (var dto in request.Data.GoldInvoices)
            {
                int? customerId = null;
                if (dto.CustomerSyncId.HasValue && dto.CustomerSyncId != Guid.Empty)
                {
                    customerId = await resolver.ResolveGoldCustomerAsync(dto.CustomerSyncId, ct);
                    if (customerId is null)
                    {
                        AddConflict(response, "GoldInvoice", dto.SyncId, "Customer not found");
                        continue;
                    }
                }

                int? cashBoxId = null;
                if (dto.CashBoxSyncId.HasValue && dto.CashBoxSyncId != Guid.Empty)
                {
                    cashBoxId = await resolver.ResolveGoldCashBoxAsync(dto.CashBoxSyncId, ct);
                    if (cashBoxId is null)
                    {
                        AddConflict(response, "GoldInvoice", dto.SyncId, "CashBox not found");
                        continue;
                    }
                }

                accepted += await UpsertGoldInvoiceAsync(tenantId, dto, customerId, cashBoxId, response, ct);
            }
            await FlushAndCacheAsync(_db.GoldInvoices, tenantId, request.Data.GoldInvoices.Select(i => i.SyncId), resolver, ct);

            foreach (var dto in request.Data.GoldInvoiceLines)
            {
                var invoiceId = await resolver.ResolveGoldInvoiceAsync(dto.InvoiceSyncId, ct);
                if (invoiceId is null)
                {
                    AddConflict(response, "GoldInvoiceLine", dto.SyncId, "Invoice not found");
                    continue;
                }

                int? itemId = null;
                if (dto.ItemSyncId.HasValue && dto.ItemSyncId != Guid.Empty)
                    itemId = await resolver.ResolveGoldItemAsync(dto.ItemSyncId, ct);

                accepted += await UpsertGoldInvoiceLineAsync(tenantId, dto, invoiceId.Value, itemId, response, ct);
            }

            foreach (var dto in request.Data.GoldPayments)
            {
                var invoiceId = await resolver.ResolveGoldInvoiceAsync(dto.InvoiceSyncId, ct);
                if (invoiceId is null)
                {
                    AddConflict(response, "GoldPayment", dto.SyncId, "Invoice not found");
                    continue;
                }

                int? cashBoxId = null;
                if (dto.CashBoxSyncId.HasValue && dto.CashBoxSyncId != Guid.Empty)
                    cashBoxId = await resolver.ResolveGoldCashBoxAsync(dto.CashBoxSyncId, ct);

                accepted += await UpsertGoldPaymentAsync(tenantId, dto, invoiceId.Value, cashBoxId, response, ct);
            }

            foreach (var dto in request.Data.GoldVouchers)
            {
                int? cashBoxId = null;
                if (dto.CashBoxSyncId.HasValue && dto.CashBoxSyncId != Guid.Empty)
                {
                    cashBoxId = await resolver.ResolveGoldCashBoxAsync(dto.CashBoxSyncId, ct);
                    if (cashBoxId is null)
                    {
                        AddConflict(response, "GoldVoucher", dto.SyncId, "CashBox not found");
                        continue;
                    }
                }

                int? customerId = null;
                if (dto.CustomerSyncId.HasValue && dto.CustomerSyncId != Guid.Empty)
                {
                    customerId = await resolver.ResolveGoldCustomerAsync(dto.CustomerSyncId, ct);
                    if (customerId is null)
                    {
                        AddConflict(response, "GoldVoucher", dto.SyncId, "Customer not found");
                        continue;
                    }
                }

                accepted += await UpsertGoldVoucherAsync(tenantId, dto, cashBoxId, customerId, response, ct);
            }

            foreach (var dto in request.Data.GoldNotifications)
                accepted += await UpsertGoldNotificationAsync(tenantId, dto, response, ct);

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

    private async Task<SyncPullResponse> PullGoldShopAsync(int tenantId, SyncPullRequest request, CancellationToken ct)
    {
        var since = request.Since ?? DateTime.MinValue;
        var bundle = new SyncDataBundle
        {
            GoldSettings = await PullEntitiesAsync(_db.GoldSettings, tenantId, since, MapGoldSettings, ct),
            GoldFxRates = await PullEntitiesAsync(_db.GoldFxRates, tenantId, since, MapGoldFxRate, ct),
            GoldKarats = await PullEntitiesAsync(_db.GoldKarats, tenantId, since, MapGoldKarat, ct),
            GoldMithqalPrices = await PullEntitiesAsync(_db.GoldMithqalPrices, tenantId, since, MapGoldMithqalPrice, ct),
            GoldItems = await PullEntitiesAsync(_db.GoldItems, tenantId, since, MapGoldItem, ct),
            GoldStockBalances = await PullEntitiesAsync(_db.GoldStockBalances, tenantId, since, MapGoldStockBalance, ct),
            GoldCustomers = await PullEntitiesAsync(_db.GoldCustomers, tenantId, since, MapGoldCustomer, ct),
            GoldCashBoxes = await PullEntitiesAsync(_db.GoldCashBoxes, tenantId, since, MapGoldCashBox, ct),
            GoldNotifications = await PullEntitiesAsync(_db.GoldNotifications, tenantId, since, MapGoldNotification, ct)
        };

        var customerMap = await _db.GoldCustomers.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var cashBoxMap = await _db.GoldCashBoxes.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var itemMap = await _db.GoldItems.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var invoiceMap = await _db.GoldInvoices.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);

        var invoices = await _db.GoldInvoices.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.GoldInvoices = invoices.Select(i => new GoldInvoiceSyncDto
        {
            SyncId = i.SyncId, CreatedAt = i.CreatedAt, CreatedBy = i.CreatedBy, UpdatedAt = i.UpdatedAt, UpdatedBy = i.UpdatedBy,
            IsDeleted = i.IsDeleted, DeletedAt = i.DeletedAt, DeletedBy = i.DeletedBy, RowVersion = i.RowVersion,
            InvoiceNumber = i.InvoiceNumber, InvoiceDate = i.InvoiceDate, InvoiceType = i.InvoiceType,
            PaymentMethod = i.PaymentMethod, Status = i.Status,
            CustomerSyncId = i.CustomerId.HasValue ? customerMap.GetValueOrDefault(i.CustomerId.Value) : null,
            PricingCurrency = i.PricingCurrency, PaymentCurrency = i.PaymentCurrency, FxRate = i.FxRate,
            TotalGoldValue = i.TotalGoldValue, TotalMakingCharge = i.TotalMakingCharge, DiscountAmount = i.DiscountAmount,
            TotalAmount = i.TotalAmount, TotalAmountIqd = i.TotalAmountIqd, TotalAmountUsd = i.TotalAmountUsd,
            PaidAmount = i.PaidAmount, RemainingAmount = i.RemainingAmount, TotalWeightGrams = i.TotalWeightGrams,
            CashBoxSyncId = i.CashBoxId.HasValue ? cashBoxMap.GetValueOrDefault(i.CashBoxId.Value) : null,
            Notes = i.Notes, WeightFromScale = i.WeightFromScale
        }).ToList();

        var lines = await _db.GoldInvoiceLines.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.GoldInvoiceLines = lines.Select(l => new GoldInvoiceLineSyncDto
        {
            SyncId = l.SyncId, CreatedAt = l.CreatedAt, CreatedBy = l.CreatedBy, UpdatedAt = l.UpdatedAt, UpdatedBy = l.UpdatedBy,
            IsDeleted = l.IsDeleted, DeletedAt = l.DeletedAt, DeletedBy = l.DeletedBy, RowVersion = l.RowVersion,
            InvoiceSyncId = invoiceMap.GetValueOrDefault(l.InvoiceId),
            ItemSyncId = l.ItemId.HasValue ? itemMap.GetValueOrDefault(l.ItemId.Value) : null,
            KaratValue = l.KaratValue, WeightGrams = l.WeightGrams, MithqalPrice = l.MithqalPrice,
            PricePerGram = l.PricePerGram, GoldValue = l.GoldValue, MakingCharge = l.MakingCharge,
            LineTotal = l.LineTotal, Description = l.Description, WeightFromScale = l.WeightFromScale
        }).ToList();

        var payments = await _db.GoldPayments.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.GoldPayments = payments.Select(p => new GoldPaymentSyncDto
        {
            SyncId = p.SyncId, CreatedAt = p.CreatedAt, CreatedBy = p.CreatedBy, UpdatedAt = p.UpdatedAt, UpdatedBy = p.UpdatedBy,
            IsDeleted = p.IsDeleted, DeletedAt = p.DeletedAt, DeletedBy = p.DeletedBy, RowVersion = p.RowVersion,
            InvoiceSyncId = invoiceMap.GetValueOrDefault(p.InvoiceId),
            PaymentDate = p.PaymentDate, Amount = p.Amount, Currency = p.Currency, FxRate = p.FxRate,
            CashBoxSyncId = p.CashBoxId.HasValue ? cashBoxMap.GetValueOrDefault(p.CashBoxId.Value) : null,
            Notes = p.Notes
        }).ToList();

        var vouchers = await _db.GoldVouchers.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.GoldVouchers = vouchers.Select(v => new GoldVoucherSyncDto
        {
            SyncId = v.SyncId, CreatedAt = v.CreatedAt, CreatedBy = v.CreatedBy, UpdatedAt = v.UpdatedAt, UpdatedBy = v.UpdatedBy,
            IsDeleted = v.IsDeleted, DeletedAt = v.DeletedAt, DeletedBy = v.DeletedBy, RowVersion = v.RowVersion,
            VoucherNumber = v.VoucherNumber, VoucherDate = v.VoucherDate, VoucherType = v.VoucherType,
            Currency = v.Currency, Amount = v.Amount,
            CashBoxSyncId = v.CashBoxId.HasValue ? cashBoxMap.GetValueOrDefault(v.CashBoxId.Value) : null,
            CustomerSyncId = v.CustomerId.HasValue ? customerMap.GetValueOrDefault(v.CustomerId.Value) : null,
            Notes = v.Notes
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

    private static GoldSettingsSyncDto MapGoldSettings(CloudGoldSettings e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        MithqalGrams = e.MithqalGrams, ScaleComPort = e.ScaleComPort, ScaleBaudRate = e.ScaleBaudRate,
        ScaleStabilityThresholdGrams = e.ScaleStabilityThresholdGrams, AllowManualWeightEdit = e.AllowManualWeightEdit,
        LowStockAlertGrams = e.LowStockAlertGrams, OverdueDaysThreshold = e.OverdueDaysThreshold,
        EnabledKaratsCsv = e.EnabledKaratsCsv
    };

    private static GoldFxRateSyncDto MapGoldFxRate(CloudGoldFxRate e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        RateDate = e.RateDate, UsdToIqd = e.UsdToIqd, Notes = e.Notes
    };

    private static GoldKaratSyncDto MapGoldKarat(CloudGoldKarat e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        KaratValue = e.KaratValue, Name = e.Name, PurityFactor = e.PurityFactor, IsActive = e.IsActive, DisplayOrder = e.DisplayOrder
    };

    private static GoldMithqalPriceSyncDto MapGoldMithqalPrice(CloudGoldMithqalPrice e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        PriceDate = e.PriceDate, KaratValue = e.KaratValue, PricePerMithqal = e.PricePerMithqal,
        Currency = e.Currency, FxRateUsed = e.FxRateUsed, Notes = e.Notes
    };

    private static GoldItemSyncDto MapGoldItem(CloudGoldItem e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, Barcode = e.Barcode, Category = e.Category, Notes = e.Notes, KaratValue = e.KaratValue,
        WeightGrams = e.WeightGrams, SuggestedMakingCharge = e.SuggestedMakingCharge,
        MakingChargeCurrency = e.MakingChargeCurrency, CostPerGram = e.CostPerGram, Status = e.Status, TrackAsPiece = e.TrackAsPiece
    };

    private static GoldStockBalanceSyncDto MapGoldStockBalance(CloudGoldStockBalance e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        KaratValue = e.KaratValue, GramsOnHand = e.GramsOnHand, AverageCostPerGram = e.AverageCostPerGram
    };

    private static GoldCustomerSyncDto MapGoldCustomer(CloudGoldCustomer e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, Phone = e.Phone, Address = e.Address, Notes = e.Notes,
        CreditBalanceIqd = e.CreditBalanceIqd, CreditBalanceUsd = e.CreditBalanceUsd, IsActive = e.IsActive
    };

    private static GoldCashBoxSyncDto MapGoldCashBox(CloudGoldCashBox e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, Currency = e.Currency, Balance = e.Balance, IsDefault = e.IsDefault, IsActive = e.IsActive
    };

    private static GoldNotificationSyncDto MapGoldNotification(CloudGoldNotification e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Type = e.Type, Title = e.Title, Message = e.Message, IsRead = e.IsRead, ReadAt = e.ReadAt,
        RelatedEntity = e.RelatedEntity, RelatedId = e.RelatedId
    };

    private async Task<int> UpsertGoldSettingsAsync(int tenantId, GoldSettingsSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldSettings, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldSettings", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldSettings { TenantId = tenantId }; _db.GoldSettings.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.MithqalGrams = dto.MithqalGrams;
        existing.ScaleComPort = dto.ScaleComPort;
        existing.ScaleBaudRate = dto.ScaleBaudRate;
        existing.ScaleStabilityThresholdGrams = dto.ScaleStabilityThresholdGrams;
        existing.AllowManualWeightEdit = dto.AllowManualWeightEdit;
        existing.LowStockAlertGrams = dto.LowStockAlertGrams;
        existing.OverdueDaysThreshold = dto.OverdueDaysThreshold;
        existing.EnabledKaratsCsv = dto.EnabledKaratsCsv;
        return 1;
    }

    private async Task<int> UpsertGoldFxRateAsync(int tenantId, GoldFxRateSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldFxRates, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldFxRate", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldFxRate { TenantId = tenantId }; _db.GoldFxRates.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.RateDate = dto.RateDate;
        existing.UsdToIqd = dto.UsdToIqd;
        existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertGoldKaratAsync(int tenantId, GoldKaratSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldKarats, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldKarat", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldKarat { TenantId = tenantId }; _db.GoldKarats.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.KaratValue = dto.KaratValue;
        existing.Name = dto.Name;
        existing.PurityFactor = dto.PurityFactor;
        existing.IsActive = dto.IsActive;
        existing.DisplayOrder = dto.DisplayOrder;
        return 1;
    }

    private async Task<int> UpsertGoldMithqalPriceAsync(int tenantId, GoldMithqalPriceSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldMithqalPrices, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldMithqalPrice", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldMithqalPrice { TenantId = tenantId }; _db.GoldMithqalPrices.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.PriceDate = dto.PriceDate;
        existing.KaratValue = dto.KaratValue;
        existing.PricePerMithqal = dto.PricePerMithqal;
        existing.Currency = dto.Currency;
        existing.FxRateUsed = dto.FxRateUsed;
        existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertGoldItemAsync(int tenantId, GoldItemSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldItems, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldItem", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldItem { TenantId = tenantId }; _db.GoldItems.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name;
        existing.Barcode = dto.Barcode;
        existing.Category = dto.Category;
        existing.Notes = dto.Notes;
        existing.KaratValue = dto.KaratValue;
        existing.WeightGrams = dto.WeightGrams;
        existing.SuggestedMakingCharge = dto.SuggestedMakingCharge;
        existing.MakingChargeCurrency = dto.MakingChargeCurrency;
        existing.CostPerGram = dto.CostPerGram;
        existing.Status = dto.Status;
        existing.TrackAsPiece = dto.TrackAsPiece;
        return 1;
    }

    private async Task<int> UpsertGoldStockBalanceAsync(int tenantId, GoldStockBalanceSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldStockBalances, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldStockBalance", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldStockBalance { TenantId = tenantId }; _db.GoldStockBalances.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.KaratValue = dto.KaratValue;
        existing.GramsOnHand = dto.GramsOnHand;
        existing.AverageCostPerGram = dto.AverageCostPerGram;
        return 1;
    }

    private async Task<int> UpsertGoldCustomerAsync(int tenantId, GoldCustomerSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldCustomers, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldCustomer", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldCustomer { TenantId = tenantId }; _db.GoldCustomers.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name;
        existing.Phone = dto.Phone;
        existing.Address = dto.Address;
        existing.Notes = dto.Notes;
        existing.CreditBalanceIqd = dto.CreditBalanceIqd;
        existing.CreditBalanceUsd = dto.CreditBalanceUsd;
        existing.IsActive = dto.IsActive;
        return 1;
    }

    private async Task<int> UpsertGoldCashBoxAsync(int tenantId, GoldCashBoxSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldCashBoxes, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldCashBox", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldCashBox { TenantId = tenantId }; _db.GoldCashBoxes.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name;
        existing.Currency = dto.Currency;
        existing.Balance = dto.Balance;
        existing.IsDefault = dto.IsDefault;
        existing.IsActive = dto.IsActive;
        return 1;
    }

    private async Task<int> UpsertGoldInvoiceAsync(
        int tenantId, GoldInvoiceSyncDto dto, int? customerId, int? cashBoxId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldInvoices, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldInvoice", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldInvoice { TenantId = tenantId }; _db.GoldInvoices.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.InvoiceNumber = dto.InvoiceNumber;
        existing.InvoiceDate = dto.InvoiceDate;
        existing.InvoiceType = dto.InvoiceType;
        existing.PaymentMethod = dto.PaymentMethod;
        existing.Status = dto.Status;
        existing.CustomerId = customerId;
        existing.PricingCurrency = dto.PricingCurrency;
        existing.PaymentCurrency = dto.PaymentCurrency;
        existing.FxRate = dto.FxRate;
        existing.TotalGoldValue = dto.TotalGoldValue;
        existing.TotalMakingCharge = dto.TotalMakingCharge;
        existing.DiscountAmount = dto.DiscountAmount;
        existing.TotalAmount = dto.TotalAmount;
        existing.TotalAmountIqd = dto.TotalAmountIqd;
        existing.TotalAmountUsd = dto.TotalAmountUsd;
        existing.PaidAmount = dto.PaidAmount;
        existing.RemainingAmount = dto.RemainingAmount;
        existing.TotalWeightGrams = dto.TotalWeightGrams;
        existing.CashBoxId = cashBoxId;
        existing.Notes = dto.Notes;
        existing.WeightFromScale = dto.WeightFromScale;
        return 1;
    }

    private async Task<int> UpsertGoldInvoiceLineAsync(
        int tenantId, GoldInvoiceLineSyncDto dto, int invoiceId, int? itemId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldInvoiceLines, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldInvoiceLine", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldInvoiceLine { TenantId = tenantId }; _db.GoldInvoiceLines.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.InvoiceId = invoiceId;
        existing.ItemId = itemId;
        existing.KaratValue = dto.KaratValue;
        existing.WeightGrams = dto.WeightGrams;
        existing.MithqalPrice = dto.MithqalPrice;
        existing.PricePerGram = dto.PricePerGram;
        existing.GoldValue = dto.GoldValue;
        existing.MakingCharge = dto.MakingCharge;
        existing.LineTotal = dto.LineTotal;
        existing.Description = dto.Description;
        existing.WeightFromScale = dto.WeightFromScale;
        return 1;
    }

    private async Task<int> UpsertGoldPaymentAsync(
        int tenantId, GoldPaymentSyncDto dto, int invoiceId, int? cashBoxId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldPayments, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldPayment", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldPayment { TenantId = tenantId }; _db.GoldPayments.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.InvoiceId = invoiceId;
        existing.PaymentDate = dto.PaymentDate;
        existing.Amount = dto.Amount;
        existing.Currency = dto.Currency;
        existing.FxRate = dto.FxRate;
        existing.CashBoxId = cashBoxId;
        existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertGoldVoucherAsync(
        int tenantId, GoldVoucherSyncDto dto, int? cashBoxId, int? customerId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldVouchers, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldVoucher", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldVoucher { TenantId = tenantId }; _db.GoldVouchers.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.VoucherNumber = dto.VoucherNumber;
        existing.VoucherDate = dto.VoucherDate;
        existing.VoucherType = dto.VoucherType;
        existing.Currency = dto.Currency;
        existing.Amount = dto.Amount;
        existing.CashBoxId = cashBoxId;
        existing.CustomerId = customerId;
        existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertGoldNotificationAsync(int tenantId, GoldNotificationSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldNotifications, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldNotification", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldNotification { TenantId = tenantId }; _db.GoldNotifications.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.Type = dto.Type;
        existing.Title = dto.Title;
        existing.Message = dto.Message;
        existing.IsRead = dto.IsRead;
        existing.ReadAt = dto.ReadAt;
        existing.RelatedEntity = dto.RelatedEntity;
        existing.RelatedId = dto.RelatedId;
        return 1;
    }
}
