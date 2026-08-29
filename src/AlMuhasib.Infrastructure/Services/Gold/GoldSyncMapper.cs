using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using AlMuhasib.Sync.Dtos;
using AlMuhasib.Sync.Requests;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

internal static class GoldSyncMapper
{
    public static async Task<SyncDataBundle> BuildPushBundleAsync(
        GoldDbContext db,
        DateTime? since,
        CancellationToken ct)
    {
        var cutoff = since ?? DateTime.MinValue;
        bool ShouldSync(BaseEntity e) =>
            (e.UpdatedAt ?? e.CreatedAt) >= cutoff
            || (e.IsDeleted && (e.DeletedAt ?? e.UpdatedAt ?? e.CreatedAt) >= cutoff);

        var settings = await db.GoldSettings.IgnoreQueryFilters().ToListAsync(ct);
        var fxRates = await db.GoldFxRates.IgnoreQueryFilters().ToListAsync(ct);
        var karats = await db.GoldKarats.IgnoreQueryFilters().ToListAsync(ct);
        var prices = await db.GoldMithqalPrices.IgnoreQueryFilters().ToListAsync(ct);
        var items = await db.GoldItems.IgnoreQueryFilters().ToListAsync(ct);
        var stocks = await db.GoldStockBalances.IgnoreQueryFilters().ToListAsync(ct);
        var customers = await db.GoldCustomers.IgnoreQueryFilters().ToListAsync(ct);
        var suppliers = await db.GoldSuppliers.IgnoreQueryFilters().ToListAsync(ct);
        var warehouses = await db.GoldWarehouses.IgnoreQueryFilters().ToListAsync(ct);
        var expenseTypes = await db.GoldExpenseTypes.IgnoreQueryFilters().ToListAsync(ct);
        var expenses = await db.GoldExpenses.IgnoreQueryFilters().ToListAsync(ct);
        var transfers = await db.GoldWarehouseTransfers.IgnoreQueryFilters().ToListAsync(ct);
        var cashBoxes = await db.GoldCashBoxes.IgnoreQueryFilters().ToListAsync(ct);
        var invoices = await db.GoldInvoices.IgnoreQueryFilters()
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .ToListAsync(ct);
        var vouchers = await db.GoldVouchers.IgnoreQueryFilters().ToListAsync(ct);
        var notifications = await db.GoldNotifications.IgnoreQueryFilters().ToListAsync(ct);

        var customerMap = customers.ToDictionary(c => c.Id, c => c.SyncId);
        var supplierMap = suppliers.ToDictionary(s => s.Id, s => s.SyncId);
        var warehouseMap = warehouses.ToDictionary(w => w.Id, w => w.SyncId);
        var expenseTypeMap = expenseTypes.ToDictionary(t => t.Id, t => t.SyncId);
        var cashBoxMap = cashBoxes.ToDictionary(c => c.Id, c => c.SyncId);
        var itemMap = items.ToDictionary(i => i.Id, i => i.SyncId);
        var invoiceMap = invoices.ToDictionary(i => i.Id, i => i.SyncId);

        var changedInvoices = invoices.Where(ShouldSync).ToList();
        var changedLines = invoices.SelectMany(i => i.Lines).Where(ShouldSync).ToList();
        var changedPayments = invoices.SelectMany(i => i.Payments).Where(ShouldSync).ToList();

        return new SyncDataBundle
        {
            GoldSettings = settings.Where(ShouldSync).Select(MapSettings).ToList(),
            GoldFxRates = fxRates.Where(ShouldSync).Select(MapFxRate).ToList(),
            GoldKarats = karats.Where(ShouldSync).Select(MapKarat).ToList(),
            GoldMithqalPrices = prices.Where(ShouldSync).Select(MapMithqalPrice).ToList(),
            GoldItems = items.Where(ShouldSync).Select(MapItem).ToList(),
            GoldWarehouses = warehouses.Where(ShouldSync).Select(MapWarehouse).ToList(),
            GoldStockBalances = stocks.Where(ShouldSync).Select(s => MapStock(s, warehouseMap)).ToList(),
            GoldCustomers = customers.Where(ShouldSync).Select(MapCustomer).ToList(),
            GoldSuppliers = suppliers.Where(ShouldSync).Select(MapSupplier).ToList(),
            GoldExpenseTypes = expenseTypes.Where(ShouldSync).Select(MapExpenseType).ToList(),
            GoldExpenses = expenses.Where(ShouldSync)
                .Select(e => MapExpense(e, expenseTypeMap, cashBoxMap, warehouseMap))
                .ToList(),
            GoldWarehouseTransfers = transfers.Where(ShouldSync)
                .Select(t => MapTransfer(t, warehouseMap))
                .ToList(),
            GoldCashBoxes = cashBoxes.Where(ShouldSync).Select(MapCashBox).ToList(),
            GoldInvoices = changedInvoices.Select(i => MapInvoice(i, customerMap, supplierMap, warehouseMap, cashBoxMap, invoiceMap)).ToList(),
            GoldInvoiceLines = changedLines
                .Where(l => invoiceMap.ContainsKey(l.InvoiceId))
                .Select(l => MapInvoiceLine(l, invoiceMap, itemMap))
                .ToList(),
            GoldPayments = changedPayments
                .Where(p => invoiceMap.ContainsKey(p.InvoiceId))
                .Select(p => MapPayment(p, invoiceMap, cashBoxMap))
                .ToList(),
            GoldVouchers = vouchers.Where(ShouldSync)
                .Select(v => MapVoucher(v, cashBoxMap, customerMap, supplierMap))
                .ToList(),
            GoldNotifications = notifications.Where(ShouldSync).Select(MapNotification).ToList()
        };
    }

    public static async Task ApplyPullBundleAsync(GoldDbContext db, SyncDataBundle data, CancellationToken ct)
    {
        db.IsApplyingSyncPull = true;
        try
        {
            await ApplySettingsAsync(db, data.GoldSettings, ct);
            await ApplyFxRatesAsync(db, data.GoldFxRates, ct);
            await ApplyKaratsAsync(db, data.GoldKarats, ct);
            await ApplyMithqalPricesAsync(db, data.GoldMithqalPrices, ct);
            var itemMap = await ApplyItemsAsync(db, data.GoldItems, ct);
            var warehouseMap = await ApplyWarehousesAsync(db, data.GoldWarehouses, ct);
            await ApplyStockBalancesAsync(db, data.GoldStockBalances, warehouseMap, ct);
            var customerMap = await ApplyCustomersAsync(db, data.GoldCustomers, ct);
            var supplierMap = await ApplySuppliersAsync(db, data.GoldSuppliers, ct);
            var expenseTypeMap = await ApplyExpenseTypesAsync(db, data.GoldExpenseTypes, ct);
            var cashBoxMap = await ApplyCashBoxesAsync(db, data.GoldCashBoxes, ct);
            await ApplyExpensesAsync(db, data.GoldExpenses, expenseTypeMap, cashBoxMap, warehouseMap, ct);
            await ApplyTransfersAsync(db, data.GoldWarehouseTransfers, warehouseMap, ct);
            var invoiceMap = await ApplyInvoicesAsync(db, data.GoldInvoices, customerMap, supplierMap, warehouseMap, cashBoxMap, ct);
            await ApplyInvoiceLinesAsync(db, data.GoldInvoiceLines, invoiceMap, itemMap, ct);
            await ApplyPaymentsAsync(db, data.GoldPayments, invoiceMap, cashBoxMap, ct);
            await ApplyVouchersAsync(db, data.GoldVouchers, cashBoxMap, customerMap, supplierMap, ct);
            await ApplyNotificationsAsync(db, data.GoldNotifications, ct);
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

    private static GoldSettingsSyncDto MapSettings(GoldSettings s)
    {
        var d = new GoldSettingsSyncDto
        {
            MithqalGrams = s.MithqalGrams,
            ScaleComPort = s.ScaleComPort,
            ScaleBaudRate = s.ScaleBaudRate,
            ScaleStabilityThresholdGrams = s.ScaleStabilityThresholdGrams,
            AllowManualWeightEdit = s.AllowManualWeightEdit,
            LowStockAlertGrams = s.LowStockAlertGrams,
            OverdueDaysThreshold = s.OverdueDaysThreshold,
            EnabledKaratsCsv = s.EnabledKaratsCsv,
            DefaultMakingChargeMode = s.DefaultMakingChargeMode,
            IsConfigured = s.IsConfigured
        };
        CopyBase(s, d);
        return d;
    }

    private static GoldFxRateSyncDto MapFxRate(GoldFxRate r)
    {
        var d = new GoldFxRateSyncDto { RateDate = r.RateDate, UsdToIqd = r.UsdToIqd, Notes = r.Notes };
        CopyBase(r, d);
        return d;
    }

    private static GoldKaratSyncDto MapKarat(GoldKarat k)
    {
        var d = new GoldKaratSyncDto
        {
            KaratValue = k.KaratValue,
            Name = k.Name,
            PurityFactor = k.PurityFactor,
            IsActive = k.IsActive,
            DisplayOrder = k.DisplayOrder
        };
        CopyBase(k, d);
        return d;
    }

    private static GoldMithqalPriceSyncDto MapMithqalPrice(GoldMithqalPrice p)
    {
        var d = new GoldMithqalPriceSyncDto
        {
            PriceDate = p.PriceDate,
            KaratValue = p.KaratValue,
            PricePerMithqal = p.PricePerMithqal,
            Currency = p.Currency,
            FxRateUsed = p.FxRateUsed,
            Notes = p.Notes
        };
        CopyBase(p, d);
        return d;
    }

    private static GoldItemSyncDto MapItem(GoldItem i)
    {
        var d = new GoldItemSyncDto
        {
            Name = i.Name,
            Barcode = i.Barcode,
            Category = i.Category,
            Notes = i.Notes,
            KaratValue = i.KaratValue,
            WeightGrams = i.WeightGrams,
            SuggestedMakingCharge = i.SuggestedMakingCharge,
            MakingChargeCurrency = i.MakingChargeCurrency,
            CostPerGram = i.CostPerGram,
            Status = i.Status,
            TrackAsPiece = i.TrackAsPiece
        };
        CopyBase(i, d);
        return d;
    }

    private static GoldStockBalanceSyncDto MapStock(GoldStockBalance s, Dictionary<int, Guid> warehouseMap)
    {
        var d = new GoldStockBalanceSyncDto
        {
            WarehouseId = s.WarehouseId,
            WarehouseSyncId = warehouseMap.GetValueOrDefault(s.WarehouseId),
            KaratValue = s.KaratValue,
            GramsOnHand = s.GramsOnHand,
            AverageCostPerGram = s.AverageCostPerGram
        };
        CopyBase(s, d);
        if (d.WarehouseSyncId == Guid.Empty) d.WarehouseSyncId = null;
        return d;
    }

    private static GoldWarehouseSyncDto MapWarehouse(GoldWarehouse w)
    {
        var d = new GoldWarehouseSyncDto
        {
            Name = w.Name,
            IsDefault = w.IsDefault,
            IsActive = w.IsActive,
            Notes = w.Notes
        };
        CopyBase(w, d);
        return d;
    }

    private static GoldSupplierSyncDto MapSupplier(GoldSupplier s)
    {
        var d = new GoldSupplierSyncDto
        {
            Name = s.Name,
            Phone = s.Phone,
            Address = s.Address,
            Notes = s.Notes,
            CreditBalanceIqd = s.CreditBalanceIqd,
            CreditBalanceUsd = s.CreditBalanceUsd,
            IsActive = s.IsActive
        };
        CopyBase(s, d);
        return d;
    }

    private static GoldExpenseTypeSyncDto MapExpenseType(GoldExpenseType t)
    {
        var d = new GoldExpenseTypeSyncDto { Name = t.Name, IsActive = t.IsActive };
        CopyBase(t, d);
        return d;
    }

    private static GoldExpenseSyncDto MapExpense(
        GoldExpense e,
        Dictionary<int, Guid> expenseTypeMap,
        Dictionary<int, Guid> cashBoxMap,
        Dictionary<int, Guid> warehouseMap)
    {
        var d = new GoldExpenseSyncDto
        {
            ExpenseDate = e.ExpenseDate,
            ExpenseTypeSyncId = expenseTypeMap.GetValueOrDefault(e.ExpenseTypeId),
            Amount = e.Amount,
            Currency = e.Currency,
            CashBoxSyncId = cashBoxMap.GetValueOrDefault(e.CashBoxId),
            Notes = e.Notes,
            WarehouseSyncId = e.WarehouseId.HasValue ? warehouseMap.GetValueOrDefault(e.WarehouseId.Value) : null
        };
        CopyBase(e, d);
        if (d.WarehouseSyncId == Guid.Empty) d.WarehouseSyncId = null;
        return d;
    }

    private static GoldWarehouseTransferSyncDto MapTransfer(
        GoldWarehouseTransfer t,
        Dictionary<int, Guid> warehouseMap)
    {
        var d = new GoldWarehouseTransferSyncDto
        {
            TransferDate = t.TransferDate,
            FromWarehouseSyncId = warehouseMap.GetValueOrDefault(t.FromWarehouseId),
            ToWarehouseSyncId = warehouseMap.GetValueOrDefault(t.ToWarehouseId),
            KaratValue = t.KaratValue,
            WeightGrams = t.WeightGrams,
            Notes = t.Notes
        };
        CopyBase(t, d);
        return d;
    }

    private static GoldCustomerSyncDto MapCustomer(GoldCustomer c)
    {
        var d = new GoldCustomerSyncDto
        {
            Name = c.Name,
            Phone = c.Phone,
            Address = c.Address,
            Notes = c.Notes,
            CreditBalanceIqd = c.CreditBalanceIqd,
            CreditBalanceUsd = c.CreditBalanceUsd,
            GoldCreditGrams = c.GoldCreditGrams,
            IsActive = c.IsActive
        };
        CopyBase(c, d);
        return d;
    }

    private static GoldCashBoxSyncDto MapCashBox(GoldCashBox c)
    {
        var d = new GoldCashBoxSyncDto
        {
            Name = c.Name,
            Currency = c.Currency,
            Balance = c.Balance,
            IsDefault = c.IsDefault,
            IsActive = c.IsActive
        };
        CopyBase(c, d);
        return d;
    }

    private static GoldInvoiceSyncDto MapInvoice(
        GoldInvoice i,
        Dictionary<int, Guid> customerMap,
        Dictionary<int, Guid> supplierMap,
        Dictionary<int, Guid> warehouseMap,
        Dictionary<int, Guid> cashBoxMap,
        Dictionary<int, Guid> invoiceMap)
    {
        var d = new GoldInvoiceSyncDto
        {
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            InvoiceType = i.InvoiceType,
            PaymentMethod = i.PaymentMethod,
            Status = i.Status,
            CustomerSyncId = i.CustomerId.HasValue ? customerMap.GetValueOrDefault(i.CustomerId.Value) : null,
            SupplierSyncId = i.SupplierId.HasValue ? supplierMap.GetValueOrDefault(i.SupplierId.Value) : null,
            WarehouseSyncId = i.WarehouseId.HasValue ? warehouseMap.GetValueOrDefault(i.WarehouseId.Value) : null,
            IsExchange = i.IsExchange,
            ExchangeCashDifference = i.ExchangeCashDifference,
            PricingCurrency = i.PricingCurrency,
            PaymentCurrency = i.PaymentCurrency,
            FxRate = i.FxRate,
            TotalGoldValue = i.TotalGoldValue,
            TotalMakingCharge = i.TotalMakingCharge,
            DiscountAmount = i.DiscountAmount,
            TotalAmount = i.TotalAmount,
            TotalAmountIqd = i.TotalAmountIqd,
            TotalAmountUsd = i.TotalAmountUsd,
            PaidAmount = i.PaidAmount,
            RemainingAmount = i.RemainingAmount,
            TotalWeightGrams = i.TotalWeightGrams,
            CashBoxSyncId = i.CashBoxId.HasValue ? cashBoxMap.GetValueOrDefault(i.CashBoxId.Value) : null,
            Notes = i.Notes,
            WeightFromScale = i.WeightFromScale,
            RelatedInvoiceSyncId = i.RelatedInvoiceId.HasValue
                ? invoiceMap.GetValueOrDefault(i.RelatedInvoiceId.Value)
                : null
        };
        CopyBase(i, d);
        if (d.CustomerSyncId == Guid.Empty) d.CustomerSyncId = null;
        if (d.SupplierSyncId == Guid.Empty) d.SupplierSyncId = null;
        if (d.WarehouseSyncId == Guid.Empty) d.WarehouseSyncId = null;
        if (d.CashBoxSyncId == Guid.Empty) d.CashBoxSyncId = null;
        if (d.RelatedInvoiceSyncId == Guid.Empty) d.RelatedInvoiceSyncId = null;
        return d;
    }

    private static GoldInvoiceLineSyncDto MapInvoiceLine(
        GoldInvoiceLine l,
        Dictionary<int, Guid> invoiceMap,
        Dictionary<int, Guid> itemMap)
    {
        var d = new GoldInvoiceLineSyncDto
        {
            InvoiceSyncId = invoiceMap[l.InvoiceId],
            ItemSyncId = l.ItemId.HasValue ? itemMap.GetValueOrDefault(l.ItemId.Value) : null,
            KaratValue = l.KaratValue,
            WeightGrams = l.WeightGrams,
            MithqalPrice = l.MithqalPrice,
            PricePerGram = l.PricePerGram,
            GoldValue = l.GoldValue,
            MakingCharge = l.MakingCharge,
            MakingChargeMode = l.MakingChargeMode,
            MakingChargeRate = l.MakingChargeRate,
            LineTotal = l.LineTotal,
            Description = l.Description,
            WeightFromScale = l.WeightFromScale,
            LineDirection = l.LineDirection
        };
        CopyBase(l, d);
        if (d.ItemSyncId == Guid.Empty) d.ItemSyncId = null;
        return d;
    }

    private static GoldPaymentSyncDto MapPayment(
        GoldPayment p,
        Dictionary<int, Guid> invoiceMap,
        Dictionary<int, Guid> cashBoxMap)
    {
        var d = new GoldPaymentSyncDto
        {
            InvoiceSyncId = invoiceMap[p.InvoiceId],
            PaymentDate = p.PaymentDate,
            Amount = p.Amount,
            Currency = p.Currency,
            FxRate = p.FxRate,
            CashBoxSyncId = p.CashBoxId.HasValue ? cashBoxMap.GetValueOrDefault(p.CashBoxId.Value) : null,
            Notes = p.Notes
        };
        CopyBase(p, d);
        if (d.CashBoxSyncId == Guid.Empty) d.CashBoxSyncId = null;
        return d;
    }

    private static GoldVoucherSyncDto MapVoucher(
        GoldVoucher v,
        Dictionary<int, Guid> cashBoxMap,
        Dictionary<int, Guid> customerMap,
        Dictionary<int, Guid> supplierMap)
    {
        var d = new GoldVoucherSyncDto
        {
            VoucherNumber = v.VoucherNumber,
            VoucherDate = v.VoucherDate,
            VoucherType = v.VoucherType,
            Currency = v.Currency,
            Amount = v.Amount,
            CashBoxSyncId = v.CashBoxId.HasValue ? cashBoxMap.GetValueOrDefault(v.CashBoxId.Value) : null,
            CustomerSyncId = v.CustomerId.HasValue ? customerMap.GetValueOrDefault(v.CustomerId.Value) : null,
            SupplierSyncId = v.SupplierId.HasValue ? supplierMap.GetValueOrDefault(v.SupplierId.Value) : null,
            IsOpeningBalance = v.IsOpeningBalance,
            AffectsCashBox = v.AffectsCashBox,
            Notes = v.Notes
        };
        CopyBase(v, d);
        if (d.CashBoxSyncId == Guid.Empty) d.CashBoxSyncId = null;
        if (d.CustomerSyncId == Guid.Empty) d.CustomerSyncId = null;
        if (d.SupplierSyncId == Guid.Empty) d.SupplierSyncId = null;
        return d;
    }

    private static GoldNotificationSyncDto MapNotification(GoldNotification n)
    {
        var d = new GoldNotificationSyncDto
        {
            Type = n.Type,
            Title = n.Title,
            Message = n.Message,
            IsRead = n.IsRead,
            ReadAt = n.ReadAt,
            RelatedEntity = n.RelatedEntity,
            RelatedId = n.RelatedId
        };
        CopyBase(n, d);
        return d;
    }

    private static async Task ApplySettingsAsync(GoldDbContext db, List<GoldSettingsSyncDto> dtos, CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            var existing = await db.GoldSettings.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = await db.GoldSettings.IgnoreQueryFilters().FirstOrDefaultAsync(ct);
                if (existing is null)
                {
                    existing = new GoldSettings();
                    db.GoldSettings.Add(existing);
                }
            }

            existing.MithqalGrams = dto.MithqalGrams;
            existing.ScaleComPort = dto.ScaleComPort;
            existing.ScaleBaudRate = dto.ScaleBaudRate;
            existing.ScaleStabilityThresholdGrams = dto.ScaleStabilityThresholdGrams;
            existing.AllowManualWeightEdit = dto.AllowManualWeightEdit;
            existing.LowStockAlertGrams = dto.LowStockAlertGrams;
            existing.OverdueDaysThreshold = dto.OverdueDaysThreshold;
            existing.EnabledKaratsCsv = dto.EnabledKaratsCsv;
            existing.DefaultMakingChargeMode = dto.DefaultMakingChargeMode;
            // Never clear local first-run completion via sync.
            if (dto.IsConfigured)
                existing.IsConfigured = true;
            ApplyBase(existing, dto);
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task ApplyFxRatesAsync(GoldDbContext db, List<GoldFxRateSyncDto> dtos, CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            var existing = await db.GoldFxRates.IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldFxRate();
                db.GoldFxRates.Add(existing);
            }

            existing.RateDate = dto.RateDate;
            existing.UsdToIqd = dto.UsdToIqd;
            existing.Notes = dto.Notes;
            ApplyBase(existing, dto);
        }
    }

    private static async Task ApplyKaratsAsync(GoldDbContext db, List<GoldKaratSyncDto> dtos, CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            var existing = await db.GoldKarats.IgnoreQueryFilters()
                .FirstOrDefaultAsync(k => k.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldKarat();
                db.GoldKarats.Add(existing);
            }

            existing.KaratValue = dto.KaratValue;
            existing.Name = dto.Name;
            existing.PurityFactor = dto.PurityFactor;
            existing.IsActive = dto.IsActive;
            existing.DisplayOrder = dto.DisplayOrder;
            ApplyBase(existing, dto);
        }
    }

    private static async Task ApplyMithqalPricesAsync(GoldDbContext db, List<GoldMithqalPriceSyncDto> dtos, CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            var existing = await db.GoldMithqalPrices.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldMithqalPrice();
                db.GoldMithqalPrices.Add(existing);
            }

            existing.PriceDate = dto.PriceDate;
            existing.KaratValue = dto.KaratValue;
            existing.PricePerMithqal = dto.PricePerMithqal;
            existing.Currency = dto.Currency;
            existing.FxRateUsed = dto.FxRateUsed;
            existing.Notes = dto.Notes;
            ApplyBase(existing, dto);
        }
    }

    private static async Task<Dictionary<Guid, int>> ApplyItemsAsync(
        GoldDbContext db, List<GoldItemSyncDto> dtos, CancellationToken ct)
    {
        var map = new Dictionary<Guid, int>();
        foreach (var dto in dtos)
        {
            var existing = await db.GoldItems.IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldItem();
                db.GoldItems.Add(existing);
            }

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
            ApplyBase(existing, dto);
            await db.SaveChangesAsync(ct);
            map[dto.SyncId] = existing.Id;
        }

        foreach (var item in await db.GoldItems.IgnoreQueryFilters().ToListAsync(ct))
            map.TryAdd(item.SyncId, item.Id);

        return map;
    }

    private static async Task ApplyStockBalancesAsync(
        GoldDbContext db,
        List<GoldStockBalanceSyncDto> dtos,
        Dictionary<Guid, int> warehouseMap,
        CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            var existing = await db.GoldStockBalances.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldStockBalance();
                db.GoldStockBalances.Add(existing);
            }

            if (dto.WarehouseSyncId.HasValue && warehouseMap.TryGetValue(dto.WarehouseSyncId.Value, out var whId))
                existing.WarehouseId = whId;
            else if (dto.WarehouseId > 0)
                existing.WarehouseId = dto.WarehouseId;

            existing.KaratValue = dto.KaratValue;
            existing.GramsOnHand = dto.GramsOnHand;
            existing.AverageCostPerGram = dto.AverageCostPerGram;
            ApplyBase(existing, dto);
        }
    }

    private static async Task<Dictionary<Guid, int>> ApplyWarehousesAsync(
        GoldDbContext db, List<GoldWarehouseSyncDto> dtos, CancellationToken ct)
    {
        var map = new Dictionary<Guid, int>();
        foreach (var dto in dtos)
        {
            var existing = await db.GoldWarehouses.IgnoreQueryFilters()
                .FirstOrDefaultAsync(w => w.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldWarehouse();
                db.GoldWarehouses.Add(existing);
            }

            existing.Name = dto.Name;
            existing.IsDefault = dto.IsDefault;
            existing.IsActive = dto.IsActive;
            existing.Notes = dto.Notes;
            ApplyBase(existing, dto);
            await db.SaveChangesAsync(ct);
            map[dto.SyncId] = existing.Id;
        }

        foreach (var w in await db.GoldWarehouses.IgnoreQueryFilters().ToListAsync(ct))
            map.TryAdd(w.SyncId, w.Id);

        return map;
    }

    private static async Task<Dictionary<Guid, int>> ApplySuppliersAsync(
        GoldDbContext db, List<GoldSupplierSyncDto> dtos, CancellationToken ct)
    {
        var map = new Dictionary<Guid, int>();
        foreach (var dto in dtos)
        {
            var existing = await db.GoldSuppliers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldSupplier();
                db.GoldSuppliers.Add(existing);
            }

            existing.Name = dto.Name;
            existing.Phone = dto.Phone;
            existing.Address = dto.Address;
            existing.Notes = dto.Notes;
            existing.CreditBalanceIqd = dto.CreditBalanceIqd;
            existing.CreditBalanceUsd = dto.CreditBalanceUsd;
            existing.IsActive = dto.IsActive;
            ApplyBase(existing, dto);
            await db.SaveChangesAsync(ct);
            map[dto.SyncId] = existing.Id;
        }

        foreach (var s in await db.GoldSuppliers.IgnoreQueryFilters().ToListAsync(ct))
            map.TryAdd(s.SyncId, s.Id);

        return map;
    }

    private static async Task<Dictionary<Guid, int>> ApplyExpenseTypesAsync(
        GoldDbContext db, List<GoldExpenseTypeSyncDto> dtos, CancellationToken ct)
    {
        var map = new Dictionary<Guid, int>();
        foreach (var dto in dtos)
        {
            var existing = await db.GoldExpenseTypes.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldExpenseType();
                db.GoldExpenseTypes.Add(existing);
            }

            existing.Name = dto.Name;
            existing.IsActive = dto.IsActive;
            ApplyBase(existing, dto);
            await db.SaveChangesAsync(ct);
            map[dto.SyncId] = existing.Id;
        }

        foreach (var t in await db.GoldExpenseTypes.IgnoreQueryFilters().ToListAsync(ct))
            map.TryAdd(t.SyncId, t.Id);

        return map;
    }

    private static async Task ApplyExpensesAsync(
        GoldDbContext db,
        List<GoldExpenseSyncDto> dtos,
        Dictionary<Guid, int> expenseTypeMap,
        Dictionary<Guid, int> cashBoxMap,
        Dictionary<Guid, int> warehouseMap,
        CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            if (!expenseTypeMap.TryGetValue(dto.ExpenseTypeSyncId, out var typeId))
                continue;
            if (!cashBoxMap.TryGetValue(dto.CashBoxSyncId, out var boxId))
                continue;

            var existing = await db.GoldExpenses.IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldExpense();
                db.GoldExpenses.Add(existing);
            }

            existing.ExpenseDate = dto.ExpenseDate;
            existing.ExpenseTypeId = typeId;
            existing.Amount = dto.Amount;
            existing.Currency = dto.Currency;
            existing.CashBoxId = boxId;
            existing.Notes = dto.Notes;
            existing.WarehouseId = dto.WarehouseSyncId.HasValue && warehouseMap.TryGetValue(dto.WarehouseSyncId.Value, out var whId)
                ? whId : null;
            ApplyBase(existing, dto);
        }
    }

    private static async Task ApplyTransfersAsync(
        GoldDbContext db,
        List<GoldWarehouseTransferSyncDto> dtos,
        Dictionary<Guid, int> warehouseMap,
        CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            if (!warehouseMap.TryGetValue(dto.FromWarehouseSyncId, out var fromId))
                continue;
            if (!warehouseMap.TryGetValue(dto.ToWarehouseSyncId, out var toId))
                continue;

            var existing = await db.GoldWarehouseTransfers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldWarehouseTransfer();
                db.GoldWarehouseTransfers.Add(existing);
            }

            existing.TransferDate = dto.TransferDate;
            existing.FromWarehouseId = fromId;
            existing.ToWarehouseId = toId;
            existing.KaratValue = dto.KaratValue;
            existing.WeightGrams = dto.WeightGrams;
            existing.Notes = dto.Notes;
            ApplyBase(existing, dto);
        }
    }

    private static async Task<Dictionary<Guid, int>> ApplyCustomersAsync(
        GoldDbContext db, List<GoldCustomerSyncDto> dtos, CancellationToken ct)
    {
        var map = new Dictionary<Guid, int>();
        foreach (var dto in dtos)
        {
            var existing = await db.GoldCustomers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldCustomer();
                db.GoldCustomers.Add(existing);
            }

            existing.Name = dto.Name;
            existing.Phone = dto.Phone;
            existing.Address = dto.Address;
            existing.Notes = dto.Notes;
            existing.CreditBalanceIqd = dto.CreditBalanceIqd;
            existing.CreditBalanceUsd = dto.CreditBalanceUsd;
            existing.GoldCreditGrams = dto.GoldCreditGrams;
            existing.IsActive = dto.IsActive;
            ApplyBase(existing, dto);
            await db.SaveChangesAsync(ct);
            map[dto.SyncId] = existing.Id;
        }

        foreach (var c in await db.GoldCustomers.IgnoreQueryFilters().ToListAsync(ct))
            map.TryAdd(c.SyncId, c.Id);

        return map;
    }

    private static async Task<Dictionary<Guid, int>> ApplyCashBoxesAsync(
        GoldDbContext db, List<GoldCashBoxSyncDto> dtos, CancellationToken ct)
    {
        var map = new Dictionary<Guid, int>();
        foreach (var dto in dtos)
        {
            var existing = await db.GoldCashBoxes.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldCashBox();
                db.GoldCashBoxes.Add(existing);
            }

            existing.Name = dto.Name;
            existing.Currency = dto.Currency;
            existing.Balance = dto.Balance;
            existing.IsDefault = dto.IsDefault;
            existing.IsActive = dto.IsActive;
            ApplyBase(existing, dto);
            await db.SaveChangesAsync(ct);
            map[dto.SyncId] = existing.Id;
        }

        foreach (var c in await db.GoldCashBoxes.IgnoreQueryFilters().ToListAsync(ct))
            map.TryAdd(c.SyncId, c.Id);

        return map;
    }

    private static async Task<Dictionary<Guid, int>> ApplyInvoicesAsync(
        GoldDbContext db,
        List<GoldInvoiceSyncDto> dtos,
        Dictionary<Guid, int> customerMap,
        Dictionary<Guid, int> supplierMap,
        Dictionary<Guid, int> warehouseMap,
        Dictionary<Guid, int> cashBoxMap,
        CancellationToken ct)
    {
        var map = new Dictionary<Guid, int>();
        foreach (var dto in dtos)
        {
            var existing = await db.GoldInvoices.IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldInvoice();
                db.GoldInvoices.Add(existing);
            }

            existing.InvoiceNumber = dto.InvoiceNumber;
            existing.InvoiceDate = dto.InvoiceDate;
            existing.InvoiceType = dto.InvoiceType;
            existing.PaymentMethod = dto.PaymentMethod;
            existing.Status = dto.Status;
            existing.CustomerId = dto.CustomerSyncId.HasValue && customerMap.TryGetValue(dto.CustomerSyncId.Value, out var cid)
                ? cid : null;
            existing.SupplierId = dto.SupplierSyncId.HasValue && supplierMap.TryGetValue(dto.SupplierSyncId.Value, out var sid)
                ? sid : null;
            existing.WarehouseId = dto.WarehouseSyncId.HasValue && warehouseMap.TryGetValue(dto.WarehouseSyncId.Value, out var whId)
                ? whId : null;
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
            existing.CashBoxId = dto.CashBoxSyncId.HasValue && cashBoxMap.TryGetValue(dto.CashBoxSyncId.Value, out var boxId)
                ? boxId : null;
            existing.Notes = dto.Notes;
            existing.WeightFromScale = dto.WeightFromScale;
            ApplyBase(existing, dto);
            await db.SaveChangesAsync(ct);
            map[dto.SyncId] = existing.Id;
        }

        foreach (var i in await db.GoldInvoices.IgnoreQueryFilters().ToListAsync(ct))
            map.TryAdd(i.SyncId, i.Id);

        // Second pass: resolve RelatedInvoiceId after all invoices are mapped.
        foreach (var dto in dtos)
        {
            if (!dto.RelatedInvoiceSyncId.HasValue || dto.RelatedInvoiceSyncId == Guid.Empty)
                continue;
            if (!map.TryGetValue(dto.SyncId, out var invoiceId))
                continue;
            if (!map.TryGetValue(dto.RelatedInvoiceSyncId.Value, out var relatedId))
                continue;

            var existing = await db.GoldInvoices.IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
            if (existing is not null)
                existing.RelatedInvoiceId = relatedId;
        }

        return map;
    }

    private static async Task ApplyInvoiceLinesAsync(
        GoldDbContext db,
        List<GoldInvoiceLineSyncDto> dtos,
        Dictionary<Guid, int> invoiceMap,
        Dictionary<Guid, int> itemMap,
        CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            if (!invoiceMap.TryGetValue(dto.InvoiceSyncId, out var invoiceId))
                continue;

            var existing = await db.GoldInvoiceLines.IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldInvoiceLine { InvoiceId = invoiceId };
                db.GoldInvoiceLines.Add(existing);
            }

            existing.InvoiceId = invoiceId;
            existing.ItemId = dto.ItemSyncId.HasValue && itemMap.TryGetValue(dto.ItemSyncId.Value, out var itemId)
                ? itemId : null;
            existing.KaratValue = dto.KaratValue;
            existing.WeightGrams = dto.WeightGrams;
            existing.MithqalPrice = dto.MithqalPrice;
            existing.PricePerGram = dto.PricePerGram;
            existing.GoldValue = dto.GoldValue;
            existing.MakingCharge = dto.MakingCharge;
            existing.MakingChargeMode = dto.MakingChargeMode;
            existing.MakingChargeRate = dto.MakingChargeRate;
            existing.LineTotal = dto.LineTotal;
            existing.Description = dto.Description;
            existing.WeightFromScale = dto.WeightFromScale;
            existing.LineDirection = dto.LineDirection;
            ApplyBase(existing, dto);
        }
    }

    private static async Task ApplyPaymentsAsync(
        GoldDbContext db,
        List<GoldPaymentSyncDto> dtos,
        Dictionary<Guid, int> invoiceMap,
        Dictionary<Guid, int> cashBoxMap,
        CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            if (!invoiceMap.TryGetValue(dto.InvoiceSyncId, out var invoiceId))
                continue;

            var existing = await db.GoldPayments.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldPayment { InvoiceId = invoiceId };
                db.GoldPayments.Add(existing);
            }

            existing.InvoiceId = invoiceId;
            existing.PaymentDate = dto.PaymentDate;
            existing.Amount = dto.Amount;
            existing.Currency = dto.Currency;
            existing.FxRate = dto.FxRate;
            existing.CashBoxId = dto.CashBoxSyncId.HasValue && cashBoxMap.TryGetValue(dto.CashBoxSyncId.Value, out var boxId)
                ? boxId : null;
            existing.Notes = dto.Notes;
            ApplyBase(existing, dto);
        }
    }

    private static async Task ApplyVouchersAsync(
        GoldDbContext db,
        List<GoldVoucherSyncDto> dtos,
        Dictionary<Guid, int> cashBoxMap,
        Dictionary<Guid, int> customerMap,
        Dictionary<Guid, int> supplierMap,
        CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            var existing = await db.GoldVouchers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(v => v.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldVoucher();
                db.GoldVouchers.Add(existing);
            }

            existing.VoucherNumber = dto.VoucherNumber;
            existing.VoucherDate = dto.VoucherDate;
            existing.VoucherType = dto.VoucherType;
            existing.Currency = dto.Currency;
            existing.Amount = dto.Amount;
            existing.CashBoxId = dto.CashBoxSyncId.HasValue && cashBoxMap.TryGetValue(dto.CashBoxSyncId.Value, out var boxId)
                ? boxId : null;
            existing.CustomerId = dto.CustomerSyncId.HasValue && customerMap.TryGetValue(dto.CustomerSyncId.Value, out var cid)
                ? cid : null;
            existing.SupplierId = dto.SupplierSyncId.HasValue && supplierMap.TryGetValue(dto.SupplierSyncId.Value, out var sid)
                ? sid : null;
            existing.IsOpeningBalance = dto.IsOpeningBalance;
            existing.AffectsCashBox = dto.AffectsCashBox;
            existing.Notes = dto.Notes;
            ApplyBase(existing, dto);
        }
    }

    private static async Task ApplyNotificationsAsync(GoldDbContext db, List<GoldNotificationSyncDto> dtos, CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            var existing = await db.GoldNotifications.IgnoreQueryFilters()
                .FirstOrDefaultAsync(n => n.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new GoldNotification();
                db.GoldNotifications.Add(existing);
            }

            existing.Type = dto.Type;
            existing.Title = dto.Title;
            existing.Message = dto.Message;
            existing.IsRead = dto.IsRead;
            existing.ReadAt = dto.ReadAt;
            existing.RelatedEntity = dto.RelatedEntity;
            existing.RelatedId = dto.RelatedId;
            ApplyBase(existing, dto);
        }
    }
}
