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

            foreach (var dto in request.Data.GoldWarehouses)
                accepted += await UpsertGoldWarehouseAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.GoldWarehouses, tenantId, request.Data.GoldWarehouses.Select(w => w.SyncId), resolver, ct);

            foreach (var dto in request.Data.GoldItems)
                accepted += await UpsertGoldItemAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.GoldItems, tenantId, request.Data.GoldItems.Select(i => i.SyncId), resolver, ct);

            foreach (var dto in request.Data.GoldStockBalances)
            {
                int warehouseId = 0;
                if (dto.WarehouseSyncId.HasValue && dto.WarehouseSyncId != Guid.Empty)
                {
                    var resolved = await resolver.ResolveGoldWarehouseAsync(dto.WarehouseSyncId, ct);
                    if (resolved is null)
                    {
                        AddConflict(response, "GoldStockBalance", dto.SyncId, "Warehouse not found");
                        continue;
                    }
                    warehouseId = resolved.Value;
                }
                else if (dto.WarehouseId > 0)
                {
                    // Desktop may send local id; prefer sync id. Fall back to default warehouse.
                    warehouseId = await ResolveDefaultGoldWarehouseIdAsync(tenantId, ct) ?? 0;
                }
                else
                {
                    warehouseId = await ResolveDefaultGoldWarehouseIdAsync(tenantId, ct) ?? 0;
                }

                if (warehouseId <= 0)
                {
                    AddConflict(response, "GoldStockBalance", dto.SyncId, "Warehouse not found");
                    continue;
                }

                accepted += await UpsertGoldStockBalanceAsync(tenantId, dto, warehouseId, response, ct);
            }

            foreach (var dto in request.Data.GoldCustomers)
                accepted += await UpsertGoldCustomerAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.GoldCustomers, tenantId, request.Data.GoldCustomers.Select(c => c.SyncId), resolver, ct);

            foreach (var dto in request.Data.GoldSuppliers)
                accepted += await UpsertGoldSupplierAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.GoldSuppliers, tenantId, request.Data.GoldSuppliers.Select(s => s.SyncId), resolver, ct);

            foreach (var dto in request.Data.GoldExpenseTypes)
                accepted += await UpsertGoldExpenseTypeAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.GoldExpenseTypes, tenantId, request.Data.GoldExpenseTypes.Select(t => t.SyncId), resolver, ct);

            foreach (var dto in request.Data.GoldCashBoxes)
                accepted += await UpsertGoldCashBoxAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.GoldCashBoxes, tenantId, request.Data.GoldCashBoxes.Select(c => c.SyncId), resolver, ct);

            foreach (var dto in request.Data.GoldExpenses)
            {
                var expenseTypeId = await resolver.ResolveGoldExpenseTypeAsync(dto.ExpenseTypeSyncId, ct);
                if (expenseTypeId is null)
                {
                    AddConflict(response, "GoldExpense", dto.SyncId, "ExpenseType not found");
                    continue;
                }

                var cashBoxId = await resolver.ResolveGoldCashBoxAsync(dto.CashBoxSyncId, ct);
                if (cashBoxId is null)
                {
                    AddConflict(response, "GoldExpense", dto.SyncId, "CashBox not found");
                    continue;
                }

                int? warehouseId = null;
                if (dto.WarehouseSyncId.HasValue && dto.WarehouseSyncId != Guid.Empty)
                {
                    warehouseId = await resolver.ResolveGoldWarehouseAsync(dto.WarehouseSyncId, ct);
                    if (warehouseId is null)
                    {
                        AddConflict(response, "GoldExpense", dto.SyncId, "Warehouse not found");
                        continue;
                    }
                }

                accepted += await UpsertGoldExpenseAsync(tenantId, dto, expenseTypeId.Value, cashBoxId.Value, warehouseId, response, ct);
            }

            foreach (var dto in request.Data.GoldWarehouseTransfers)
            {
                var fromId = await resolver.ResolveGoldWarehouseAsync(dto.FromWarehouseSyncId, ct);
                var toId = await resolver.ResolveGoldWarehouseAsync(dto.ToWarehouseSyncId, ct);
                if (fromId is null || toId is null)
                {
                    AddConflict(response, "GoldWarehouseTransfer", dto.SyncId, "Warehouse not found");
                    continue;
                }

                accepted += await UpsertGoldWarehouseTransferAsync(tenantId, dto, fromId.Value, toId.Value, response, ct);
            }

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

                int? supplierId = null;
                if (dto.SupplierSyncId.HasValue && dto.SupplierSyncId != Guid.Empty)
                {
                    supplierId = await resolver.ResolveGoldSupplierAsync(dto.SupplierSyncId, ct);
                    if (supplierId is null)
                    {
                        AddConflict(response, "GoldInvoice", dto.SyncId, "Supplier not found");
                        continue;
                    }
                }

                int? warehouseId = null;
                if (dto.WarehouseSyncId.HasValue && dto.WarehouseSyncId != Guid.Empty)
                {
                    warehouseId = await resolver.ResolveGoldWarehouseAsync(dto.WarehouseSyncId, ct);
                    if (warehouseId is null)
                    {
                        AddConflict(response, "GoldInvoice", dto.SyncId, "Warehouse not found");
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

                accepted += await UpsertGoldInvoiceAsync(tenantId, dto, customerId, supplierId, warehouseId, cashBoxId, response, ct);
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
            GoldWarehouses = await PullEntitiesAsync(_db.GoldWarehouses, tenantId, since, MapGoldWarehouse, ct),
            GoldItems = await PullEntitiesAsync(_db.GoldItems, tenantId, since, MapGoldItem, ct),
            GoldCustomers = await PullEntitiesAsync(_db.GoldCustomers, tenantId, since, MapGoldCustomer, ct),
            GoldSuppliers = await PullEntitiesAsync(_db.GoldSuppliers, tenantId, since, MapGoldSupplier, ct),
            GoldExpenseTypes = await PullEntitiesAsync(_db.GoldExpenseTypes, tenantId, since, MapGoldExpenseType, ct),
            GoldCashBoxes = await PullEntitiesAsync(_db.GoldCashBoxes, tenantId, since, MapGoldCashBox, ct),
            GoldNotifications = await PullEntitiesAsync(_db.GoldNotifications, tenantId, since, MapGoldNotification, ct)
        };

        var warehouseMap = await _db.GoldWarehouses.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var customerMap = await _db.GoldCustomers.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var supplierMap = await _db.GoldSuppliers.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var cashBoxMap = await _db.GoldCashBoxes.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var expenseTypeMap = await _db.GoldExpenseTypes.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var itemMap = await _db.GoldItems.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var invoiceMap = await _db.GoldInvoices.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);

        var stocks = await _db.GoldStockBalances.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.GoldStockBalances = stocks.Select(s => new GoldStockBalanceSyncDto
        {
            SyncId = s.SyncId, CreatedAt = s.CreatedAt, CreatedBy = s.CreatedBy, UpdatedAt = s.UpdatedAt, UpdatedBy = s.UpdatedBy,
            IsDeleted = s.IsDeleted, DeletedAt = s.DeletedAt, DeletedBy = s.DeletedBy, RowVersion = s.RowVersion,
            WarehouseId = s.WarehouseId,
            WarehouseSyncId = warehouseMap.GetValueOrDefault(s.WarehouseId),
            KaratValue = s.KaratValue, GramsOnHand = s.GramsOnHand, AverageCostPerGram = s.AverageCostPerGram
        }).ToList();

        var expenses = await _db.GoldExpenses.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.GoldExpenses = expenses.Select(e => new GoldExpenseSyncDto
        {
            SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
            IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
            ExpenseDate = e.ExpenseDate,
            ExpenseTypeSyncId = expenseTypeMap.GetValueOrDefault(e.ExpenseTypeId),
            Amount = e.Amount, Currency = e.Currency,
            CashBoxSyncId = cashBoxMap.GetValueOrDefault(e.CashBoxId),
            Notes = e.Notes,
            WarehouseSyncId = e.WarehouseId.HasValue ? warehouseMap.GetValueOrDefault(e.WarehouseId.Value) : null
        }).ToList();

        var transfers = await _db.GoldWarehouseTransfers.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.GoldWarehouseTransfers = transfers.Select(t => new GoldWarehouseTransferSyncDto
        {
            SyncId = t.SyncId, CreatedAt = t.CreatedAt, CreatedBy = t.CreatedBy, UpdatedAt = t.UpdatedAt, UpdatedBy = t.UpdatedBy,
            IsDeleted = t.IsDeleted, DeletedAt = t.DeletedAt, DeletedBy = t.DeletedBy, RowVersion = t.RowVersion,
            TransferDate = t.TransferDate,
            FromWarehouseSyncId = warehouseMap.GetValueOrDefault(t.FromWarehouseId),
            ToWarehouseSyncId = warehouseMap.GetValueOrDefault(t.ToWarehouseId),
            KaratValue = t.KaratValue, WeightGrams = t.WeightGrams, Notes = t.Notes
        }).ToList();

        var invoices = await _db.GoldInvoices.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.GoldInvoices = invoices.Select(i => new GoldInvoiceSyncDto
        {
            SyncId = i.SyncId, CreatedAt = i.CreatedAt, CreatedBy = i.CreatedBy, UpdatedAt = i.UpdatedAt, UpdatedBy = i.UpdatedBy,
            IsDeleted = i.IsDeleted, DeletedAt = i.DeletedAt, DeletedBy = i.DeletedBy, RowVersion = i.RowVersion,
            InvoiceNumber = i.InvoiceNumber, InvoiceDate = i.InvoiceDate, InvoiceType = i.InvoiceType,
            PaymentMethod = i.PaymentMethod, Status = i.Status,
            CustomerSyncId = i.CustomerId.HasValue ? customerMap.GetValueOrDefault(i.CustomerId.Value) : null,
            SupplierSyncId = i.SupplierId.HasValue ? supplierMap.GetValueOrDefault(i.SupplierId.Value) : null,
            WarehouseSyncId = i.WarehouseId.HasValue ? warehouseMap.GetValueOrDefault(i.WarehouseId.Value) : null,
            IsExchange = i.IsExchange, ExchangeCashDifference = i.ExchangeCashDifference,
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
            LineTotal = l.LineTotal, Description = l.Description, WeightFromScale = l.WeightFromScale,
            LineDirection = l.LineDirection
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

    private async Task<int?> ResolveDefaultGoldWarehouseIdAsync(int tenantId, CancellationToken ct)
    {
        var wh = await _db.GoldWarehouses.IgnoreQueryFilters()
            .Where(w => w.TenantId == tenantId && !w.IsDeleted && w.IsActive)
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Id)
            .FirstOrDefaultAsync(ct);
        return wh?.Id;
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

    private static GoldWarehouseSyncDto MapGoldWarehouse(CloudGoldWarehouse e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, IsDefault = e.IsDefault, IsActive = e.IsActive, Notes = e.Notes
    };

    private static GoldItemSyncDto MapGoldItem(CloudGoldItem e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, Barcode = e.Barcode, Category = e.Category, Notes = e.Notes, KaratValue = e.KaratValue,
        WeightGrams = e.WeightGrams, SuggestedMakingCharge = e.SuggestedMakingCharge,
        MakingChargeCurrency = e.MakingChargeCurrency, CostPerGram = e.CostPerGram, Status = e.Status, TrackAsPiece = e.TrackAsPiece
    };

    private static GoldCustomerSyncDto MapGoldCustomer(CloudGoldCustomer e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, Phone = e.Phone, Address = e.Address, Notes = e.Notes,
        CreditBalanceIqd = e.CreditBalanceIqd, CreditBalanceUsd = e.CreditBalanceUsd, IsActive = e.IsActive
    };

    private static GoldSupplierSyncDto MapGoldSupplier(CloudGoldSupplier e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, Phone = e.Phone, Address = e.Address, Notes = e.Notes,
        CreditBalanceIqd = e.CreditBalanceIqd, CreditBalanceUsd = e.CreditBalanceUsd, IsActive = e.IsActive
    };

    private static GoldExpenseTypeSyncDto MapGoldExpenseType(CloudGoldExpenseType e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, IsActive = e.IsActive
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

    private async Task<int> UpsertGoldWarehouseAsync(int tenantId, GoldWarehouseSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldWarehouses, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldWarehouse", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldWarehouse { TenantId = tenantId }; _db.GoldWarehouses.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name;
        existing.IsDefault = dto.IsDefault;
        existing.IsActive = dto.IsActive;
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

    private async Task<int> UpsertGoldStockBalanceAsync(
        int tenantId, GoldStockBalanceSyncDto dto, int warehouseId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldStockBalances, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldStockBalance", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldStockBalance { TenantId = tenantId }; _db.GoldStockBalances.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.WarehouseId = warehouseId;
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

    private async Task<int> UpsertGoldSupplierAsync(int tenantId, GoldSupplierSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldSuppliers, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldSupplier", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldSupplier { TenantId = tenantId }; _db.GoldSuppliers.Add(existing); }
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

    private async Task<int> UpsertGoldExpenseTypeAsync(int tenantId, GoldExpenseTypeSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldExpenseTypes, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldExpenseType", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldExpenseType { TenantId = tenantId }; _db.GoldExpenseTypes.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name;
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

    private async Task<int> UpsertGoldExpenseAsync(
        int tenantId, GoldExpenseSyncDto dto, int expenseTypeId, int cashBoxId, int? warehouseId,
        SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldExpenses, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldExpense", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldExpense { TenantId = tenantId }; _db.GoldExpenses.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.ExpenseDate = dto.ExpenseDate;
        existing.ExpenseTypeId = expenseTypeId;
        existing.Amount = dto.Amount;
        existing.Currency = dto.Currency;
        existing.CashBoxId = cashBoxId;
        existing.Notes = dto.Notes;
        existing.WarehouseId = warehouseId;
        return 1;
    }

    private async Task<int> UpsertGoldWarehouseTransferAsync(
        int tenantId, GoldWarehouseTransferSyncDto dto, int fromWarehouseId, int toWarehouseId,
        SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.GoldWarehouseTransfers, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "GoldWarehouseTransfer", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudGoldWarehouseTransfer { TenantId = tenantId }; _db.GoldWarehouseTransfers.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.TransferDate = dto.TransferDate;
        existing.FromWarehouseId = fromWarehouseId;
        existing.ToWarehouseId = toWarehouseId;
        existing.KaratValue = dto.KaratValue;
        existing.WeightGrams = dto.WeightGrams;
        existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertGoldInvoiceAsync(
        int tenantId, GoldInvoiceSyncDto dto, int? customerId, int? supplierId, int? warehouseId, int? cashBoxId,
        SyncPushResponse response, CancellationToken ct)
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
        existing.SupplierId = supplierId;
        existing.WarehouseId = warehouseId;
        existing.IsExchange = dto.IsExchange;
        existing.ExchangeCashDifference = dto.ExchangeCashDifference;
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
        existing.LineDirection = dto.LineDirection;
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
