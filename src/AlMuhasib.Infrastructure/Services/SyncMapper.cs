using AlMuhasib.Core.Entities;
using AlMuhasib.Infrastructure.Data;
using AlMuhasib.Sync.Dtos;
using AlMuhasib.Sync.Requests;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

internal static class SyncMapper
{
    public static async Task<SyncDataBundle> BuildPushBundleAsync(AppDbContext db, DateTime? since, CancellationToken ct)
    {
        var cutoff = since ?? DateTime.MinValue;
        bool ShouldSync(BaseEntity e) =>
            (e.UpdatedAt ?? e.CreatedAt) >= cutoff
            || (e.IsDeleted && (e.DeletedAt ?? e.UpdatedAt ?? e.CreatedAt) >= cutoff);

        var categories = await db.Categories.IgnoreQueryFilters().ToListAsync(ct);
        var products = await db.Products.IgnoreQueryFilters().ToListAsync(ct);
        var pricingTypes = await db.PricingTypes.IgnoreQueryFilters().ToListAsync(ct);
        var productPrices = await db.ProductPrices.IgnoreQueryFilters().ToListAsync(ct);
        var businessSettings = await db.BusinessSettings.IgnoreQueryFilters().ToListAsync(ct);
        var warehouses = await db.Warehouses.IgnoreQueryFilters().ToListAsync(ct);
        var customers = await db.Customers.IgnoreQueryFilters().ToListAsync(ct);
        var suppliers = await db.Suppliers.IgnoreQueryFilters().ToListAsync(ct);
        var cashBoxes = await db.CashBoxes.IgnoreQueryFilters().ToListAsync(ct);
        var bankAccounts = await db.BankAccounts.IgnoreQueryFilters().ToListAsync(ct);
        var investors = await db.Investors.IgnoreQueryFilters().ToListAsync(ct);
        var expenseTypes = await db.ExpenseTypes.IgnoreQueryFilters().ToListAsync(ct);
        var invoices = await db.Invoices.IgnoreQueryFilters().ToListAsync(ct);
        var invoiceItems = await db.InvoiceItems.IgnoreQueryFilters().ToListAsync(ct);
        var warehouseStocks = await db.WarehouseStocks.IgnoreQueryFilters().ToListAsync(ct);
        var warehouseTransfers = await db.WarehouseTransfers.IgnoreQueryFilters().ToListAsync(ct);
        var warehouseTransferItems = await db.WarehouseTransferItems.IgnoreQueryFilters().ToListAsync(ct);
        var installmentPlans = await db.InstallmentPlans.IgnoreQueryFilters().ToListAsync(ct);
        var installments = await db.Installments.IgnoreQueryFilters().ToListAsync(ct);
        var vouchers = await db.Vouchers.IgnoreQueryFilters().ToListAsync(ct);
        var expenses = await db.Expenses.IgnoreQueryFilters().ToListAsync(ct);
        var transfers = await db.Transfers.IgnoreQueryFilters().ToListAsync(ct);
        var investorTransactions = await db.InvestorTransactions.IgnoreQueryFilters().ToListAsync(ct);
        var profitDistributions = await db.ProfitDistributions.IgnoreQueryFilters().ToListAsync(ct);
        var profitDistributionDetails = await db.ProfitDistributionDetails.IgnoreQueryFilters().ToListAsync(ct);
        var capitalEntries = await db.CapitalEntries.IgnoreQueryFilters().ToListAsync(ct);
        var customerAttachments = await db.CustomerAttachments.IgnoreQueryFilters().ToListAsync(ct);
        var printBranding = await db.PrintBrandingSettings.IgnoreQueryFilters().ToListAsync(ct);

        var catMap = categories.ToDictionary(c => c.Id, c => c.SyncId);
        var changedStocks = warehouseStocks.Where(ShouldSync).ToList();
        var changedWarehouseTransferItems = warehouseTransferItems.Where(ShouldSync).ToList();
        var changedInvoices = invoices.Where(ShouldSync).ToList();
        var changedInvoiceItems = invoiceItems.Where(ShouldSync).ToList();
        var changedVouchers = vouchers.Where(ShouldSync).ToList();
        var changedExpenses = expenses.Where(ShouldSync).ToList();
        var changedTransfers = transfers.Where(ShouldSync).ToList();
        var changedInstallments = installments.Where(ShouldSync).ToList();
        var changedDistDetails = profitDistributionDetails.Where(ShouldSync).ToList();

        // Parent installment plans must travel with any changed installment, even if the plan itself
        // was not modified since LastPushedAt — otherwise the cloud reports "Plan not found".
        var referencedPlanIds = changedInstallments.Select(i => i.InstallmentPlanId).ToHashSet();
        var plansToPush = installmentPlans
            .Where(p => ShouldSync(p) || referencedPlanIds.Contains(p.Id))
            .ToList();

        var referencedTransferIds = changedWarehouseTransferItems.Select(i => i.WarehouseTransferId).ToHashSet();
        var transfersToPush = warehouseTransfers
            .Where(t => ShouldSync(t) || referencedTransferIds.Contains(t.Id))
            .ToList();

        var referencedProductIds = changedStocks.Select(s => s.ProductId)
            .Concat(changedInvoiceItems.Where(i => i.ProductId.HasValue).Select(i => i.ProductId!.Value))
            .Concat(changedWarehouseTransferItems.Select(i => i.ProductId))
            .Concat(productPrices.Where(ShouldSync).Select(p => p.ProductId))
            .ToHashSet();
        var referencedCategoryIds = products
            .Where(p => ShouldSync(p) || referencedProductIds.Contains(p.Id))
            .Select(p => p.CategoryId)
            .ToHashSet();
        var changedProductPrices = productPrices.Where(ShouldSync).ToList();
        var referencedPricingTypeIds = changedProductPrices.Select(p => p.PricingTypeId)
            .Concat(changedInvoiceItems.Where(i => i.PricingTypeId.HasValue).Select(i => i.PricingTypeId!.Value))
            .ToHashSet();
        var referencedWarehouseIds = changedStocks.Select(s => s.WarehouseId)
            .Concat(changedInvoices.Select(i => i.WarehouseId))
            .Concat(transfersToPush.Select(t => t.FromWarehouseId))
            .Concat(transfersToPush.Select(t => t.ToWarehouseId))
            .ToHashSet();
        var referencedCustomerIds = changedInvoices.Where(i => i.CustomerId.HasValue).Select(i => i.CustomerId!.Value)
            .Concat(plansToPush.Select(p => p.CustomerId))
            .Concat(changedVouchers.Where(v => v.CustomerId.HasValue).Select(v => v.CustomerId!.Value))
            .Concat(customerAttachments.Where(ShouldSync).Select(a => a.CustomerId))
            .ToHashSet();
        var referencedSupplierIds = changedInvoices.Where(i => i.SupplierId.HasValue).Select(i => i.SupplierId!.Value).ToHashSet();
        var referencedCashBoxIds = changedInvoices.Where(i => i.CashBoxId.HasValue).Select(i => i.CashBoxId!.Value)
            .Concat(changedVouchers.Select(v => v.CashBoxId))
            .Concat(changedExpenses.Select(e => e.CashBoxId))
            .Concat(changedInstallments.Where(i => i.CashBoxId.HasValue).Select(i => i.CashBoxId!.Value))
            .ToHashSet();
        var referencedBankIds = changedVouchers.Where(v => v.BankAccountId.HasValue).Select(v => v.BankAccountId!.Value)
            .Concat(changedTransfers.Where(t => t.FromType == Core.Enums.TransferAccountType.Bank).Select(t => t.FromId))
            .Concat(changedTransfers.Where(t => t.ToType == Core.Enums.TransferAccountType.Bank).Select(t => t.ToId))
            .ToHashSet();
        var referencedInvestorIds = changedVouchers.Where(v => v.InvestorId.HasValue).Select(v => v.InvestorId!.Value)
            .Concat(investorTransactions.Where(ShouldSync).Select(t => t.InvestorId))
            .Concat(changedDistDetails.Select(d => d.InvestorId))
            .ToHashSet();
        var referencedExpenseTypeIds = changedExpenses.Select(e => e.ExpenseTypeId).ToHashSet();
        var referencedInvoiceIds = changedInvoiceItems.Select(i => i.InvoiceId)
            .Concat(plansToPush.Select(p => p.InvoiceId))
            .ToHashSet();
        var referencedDistIds = changedDistDetails.Select(d => d.ProfitDistributionId).ToHashSet();

        var bundle = new SyncDataBundle
        {
            Categories = categories.Where(c => ShouldSync(c) || referencedCategoryIds.Contains(c.Id)).Select(MapCategory).ToList(),
            Products = products.Where(p => ShouldSync(p) || referencedProductIds.Contains(p.Id))
                .Select(p => MapProductSafe(p, catMap)).Where(p => p is not null).Cast<ProductSyncDto>().ToList(),
            PricingTypes = pricingTypes.Where(t => ShouldSync(t) || referencedPricingTypeIds.Contains(t.Id)).Select(MapPricingType).ToList(),
            BusinessSettings = businessSettings.Where(ShouldSync).Select(MapBusinessSettings).ToList(),
            Warehouses = warehouses.Where(w => ShouldSync(w) || referencedWarehouseIds.Contains(w.Id)).Select(MapWarehouse).ToList(),
            Customers = customers.Where(c => ShouldSync(c) || referencedCustomerIds.Contains(c.Id)).Select(MapCustomer).ToList(),
            Suppliers = suppliers.Where(s => ShouldSync(s) || referencedSupplierIds.Contains(s.Id)).Select(MapSupplier).ToList(),
            CashBoxes = cashBoxes.Where(c => ShouldSync(c) || referencedCashBoxIds.Contains(c.Id)).Select(MapCashBox).ToList(),
            BankAccounts = bankAccounts.Where(b => ShouldSync(b) || referencedBankIds.Contains(b.Id)).Select(MapBankAccount).ToList(),
            Investors = investors.Where(i => ShouldSync(i) || referencedInvestorIds.Contains(i.Id)).Select(MapInvestor).ToList(),
            ExpenseTypes = expenseTypes.Where(e => ShouldSync(e) || referencedExpenseTypeIds.Contains(e.Id)).Select(MapExpenseType).ToList(),
            PrintBrandingSettings = printBranding.Where(ShouldSync).Select(MapPrintBranding).ToList()
        };

        var whMap = warehouses.ToDictionary(w => w.Id, w => w.SyncId);
        var prMap = products.ToDictionary(p => p.Id, p => p.SyncId);
        var pricingTypeMap = pricingTypes.ToDictionary(t => t.Id, t => t.SyncId);
        bundle.ProductPrices = changedProductPrices
            .Where(p => prMap.ContainsKey(p.ProductId) && pricingTypeMap.ContainsKey(p.PricingTypeId))
            .Select(p => MapProductPrice(p, prMap, pricingTypeMap)).ToList();
        bundle.WarehouseStocks = changedStocks
            .Where(s => whMap.ContainsKey(s.WarehouseId) && prMap.ContainsKey(s.ProductId))
            .Select(s => MapWarehouseStock(s, whMap, prMap)).ToList();

        bundle.WarehouseTransfers = transfersToPush
            .Where(t => whMap.ContainsKey(t.FromWarehouseId) && whMap.ContainsKey(t.ToWarehouseId))
            .Select(t => MapWarehouseTransfer(t, whMap)).ToList();
        var wtMap = warehouseTransfers.ToDictionary(t => t.Id, t => t.SyncId);
        bundle.WarehouseTransferItems = changedWarehouseTransferItems
            .Where(i => wtMap.ContainsKey(i.WarehouseTransferId) && prMap.ContainsKey(i.ProductId))
            .Select(i => MapWarehouseTransferItem(i, wtMap, prMap)).ToList();

        var custMap = customers.ToDictionary(c => c.Id, c => c.SyncId);
        var supMap = suppliers.ToDictionary(s => s.Id, s => s.SyncId);
        var cbMap = cashBoxes.ToDictionary(c => c.Id, c => c.SyncId);
        var invMap = invoices.ToDictionary(i => i.Id, i => i.SyncId);

        bundle.Invoices = invoices
            .Where(i => ShouldSync(i) || referencedInvoiceIds.Contains(i.Id))
            .Where(i => whMap.ContainsKey(i.WarehouseId))
            .Select(i => MapInvoice(i, custMap, supMap, whMap, cbMap)).ToList();
        bundle.InvoiceItems = changedInvoiceItems
            .Where(i => invMap.ContainsKey(i.InvoiceId))
            .Select(i => MapInvoiceItem(i, invMap, prMap, pricingTypeMap)).ToList();
        bundle.InstallmentPlans = plansToPush
            .Where(p => invMap.ContainsKey(p.InvoiceId) && custMap.ContainsKey(p.CustomerId))
            .Select(p => MapInstallmentPlan(p, invMap, custMap)).ToList();

        var planMap = installmentPlans.ToDictionary(p => p.Id, p => p.SyncId);
        bundle.Installments = changedInstallments
            .Where(i => planMap.ContainsKey(i.InstallmentPlanId) && planMap[i.InstallmentPlanId] != Guid.Empty)
            .Select(i => MapInstallment(i, planMap, cbMap)).ToList();

        var investorMap = investors.ToDictionary(i => i.Id, i => i.SyncId);
        var bankMap = bankAccounts.ToDictionary(b => b.Id, b => b.SyncId);
        bundle.Vouchers = changedVouchers
            .Where(v => cbMap.ContainsKey(v.CashBoxId))
            .Select(v => MapVoucherSafe(v, custMap, investorMap, cbMap, bankMap))
            .Where(v => v is not null).Cast<VoucherSyncDto>().ToList();

        var etMap = expenseTypes.ToDictionary(e => e.Id, e => e.SyncId);
        bundle.Expenses = changedExpenses
            .Where(e => etMap.ContainsKey(e.ExpenseTypeId) && cbMap.ContainsKey(e.CashBoxId))
            .Select(e => MapExpense(e, etMap, cbMap)).ToList();
        bundle.Transfers = changedTransfers
            .Select(t => MapTransferSafe(t, cbMap, bankMap))
            .Where(t => t is not null).Cast<TransferSyncDto>().ToList();
        bundle.InvestorTransactions = investorTransactions.Where(ShouldSync)
            .Where(t => investorMap.ContainsKey(t.InvestorId))
            .Select(t => MapInvestorTransaction(t, investorMap)).ToList();

        var distMap = profitDistributions.ToDictionary(d => d.Id, d => d.SyncId);
        bundle.ProfitDistributions = profitDistributions
            .Where(d => ShouldSync(d) || referencedDistIds.Contains(d.Id))
            .Select(MapProfitDistribution).ToList();
        bundle.ProfitDistributionDetails = changedDistDetails
            .Where(d => distMap.ContainsKey(d.ProfitDistributionId) && investorMap.ContainsKey(d.InvestorId))
            .Select(d => MapProfitDistributionDetail(d, distMap, investorMap)).ToList();
        bundle.CapitalEntries = capitalEntries.Where(ShouldSync).Select(MapCapitalEntry).ToList();
        bundle.CustomerAttachments = customerAttachments.Where(ShouldSync)
            .Where(a => custMap.ContainsKey(a.CustomerId))
            .Select(a => MapCustomerAttachment(a, custMap)).ToList();

        return bundle;
    }

    public static async Task ApplyPullBundleAsync(AppDbContext db, SyncDataBundle data, CancellationToken ct)
    {
        db.IsApplyingSyncPull = true;
        try
        {
            await ApplyPullBundleCoreAsync(db, data, ct);
        }
        finally
        {
            db.IsApplyingSyncPull = false;
        }
    }

    private static async Task ApplyPullBundleCoreAsync(AppDbContext db, SyncDataBundle data, CancellationToken ct)
    {
        var catBySync = await UpsertCategoriesAsync(db, data.Categories, ct);
        await UpsertProductsAsync(db, data.Products, catBySync, ct);
        var pricingTypeBySync = await UpsertPricingTypesAsync(db, data.PricingTypes, ct);
        var prBySync = await db.Products.IgnoreQueryFilters().ToDictionaryAsync(p => p.SyncId, p => p.Id, ct);
        await UpsertProductPricesAsync(db, data.ProductPrices, prBySync, pricingTypeBySync, ct);
        await UpsertBusinessSettingsAsync(db, data.BusinessSettings, ct);
        var whBySync = await UpsertWarehousesAsync(db, data.Warehouses, ct);
        var custBySync = await UpsertCustomersAsync(db, data.Customers, ct);
        var supBySync = await UpsertSuppliersAsync(db, data.Suppliers, ct);
        var cbBySync = await UpsertCashBoxesAsync(db, data.CashBoxes, ct);
        var bankBySync = await UpsertBankAccountsAsync(db, data.BankAccounts, ct);
        var invBySync = await UpsertInvestorsAsync(db, data.Investors, ct);
        var etBySync = await UpsertExpenseTypesAsync(db, data.ExpenseTypes, ct);
        await UpsertPrintBrandingAsync(db, data.PrintBrandingSettings, ct);

        await UpsertWarehouseStocksAsync(db, data.WarehouseStocks, whBySync, prBySync, ct);

        var wtMap = await UpsertWarehouseTransfersAsync(db, data.WarehouseTransfers, whBySync, ct);
        await UpsertWarehouseTransferItemsAsync(db, data.WarehouseTransferItems, wtMap, prBySync, ct);

        var invMap = await UpsertInvoicesAsync(db, data.Invoices, custBySync, supBySync, whBySync, cbBySync, ct);
        await UpsertInvoiceItemsAsync(db, data.InvoiceItems, invMap, prBySync, pricingTypeBySync, ct);
        var planMap = await UpsertInstallmentPlansAsync(db, data.InstallmentPlans, invMap, custBySync, ct);
        await UpsertInstallmentsAsync(db, data.Installments, planMap, cbBySync, ct);
        await UpsertVouchersAsync(db, data.Vouchers, custBySync, invBySync, cbBySync, bankBySync, ct);
        await UpsertExpensesAsync(db, data.Expenses, etBySync, cbBySync, ct);
        await UpsertTransfersAsync(db, data.Transfers, cbBySync, bankBySync, ct);
        await UpsertInvestorTransactionsAsync(db, data.InvestorTransactions, invBySync, ct);
        var distMap = await UpsertProfitDistributionsAsync(db, data.ProfitDistributions, ct);
        await UpsertProfitDistributionDetailsAsync(db, data.ProfitDistributionDetails, distMap, invBySync, ct);
        await UpsertCapitalEntriesAsync(db, data.CapitalEntries, ct);
        await UpsertCustomerAttachmentsAsync(db, data.CustomerAttachments, custBySync, ct);
    }

    private static bool ShouldRejectIncoming(BaseEntity? local, SyncDtoBase incoming)
    {
        if (local is null || local.Id == 0) return false;
        if (!incoming.UpdatedAt.HasValue || !local.UpdatedAt.HasValue) return false;
        return incoming.UpdatedAt.Value < local.UpdatedAt.Value;
    }

    private static void ApplyBase(BaseEntity entity, SyncDtoBase dto)
    {
        entity.SyncId = dto.SyncId;
        entity.CreatedAt = dto.CreatedAt;
        entity.CreatedBy = dto.CreatedBy;
        entity.UpdatedAt = dto.UpdatedAt;
        entity.UpdatedBy = dto.UpdatedBy;
        entity.IsDeleted = dto.IsDeleted;
        entity.DeletedAt = dto.DeletedAt;
        entity.DeletedBy = dto.DeletedBy;
    }

    private static SyncDtoBase MapBase(BaseEntity e) => new CategorySyncDto
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy,
        UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy, IsDeleted = e.IsDeleted,
        DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion
    };

    private static CategorySyncDto MapCategory(Category c) { var d = new CategorySyncDto(); CopyBase(c, d); d.Name = c.Name; return d; }
    private static ProductSyncDto MapProduct(Product p, Dictionary<int, Guid> cats) { var d = new ProductSyncDto(); CopyBase(p, d); d.Name = p.Name; d.Description = p.Description; d.Barcode = p.Barcode; d.CategorySyncId = cats[p.CategoryId]; return d; }
    private static ProductSyncDto? MapProductSafe(Product p, Dictionary<int, Guid> cats)
    {
        if (!cats.TryGetValue(p.CategoryId, out var catSyncId)) return null;
        var d = new ProductSyncDto(); CopyBase(p, d); d.Name = p.Name; d.Description = p.Description; d.Barcode = p.Barcode; d.CategorySyncId = catSyncId;
        return d;
    }
    private static PricingTypeSyncDto MapPricingType(PricingType t)
    {
        var d = new PricingTypeSyncDto();
        CopyBase(t, d);
        d.Name = t.Name;
        d.IsDefault = t.IsDefault;
        d.IsActive = t.IsActive;
        return d;
    }
    private static ProductPriceSyncDto MapProductPrice(ProductPrice p, Dictionary<int, Guid> products, Dictionary<int, Guid> types)
    {
        var d = new ProductPriceSyncDto();
        CopyBase(p, d);
        d.ProductSyncId = products[p.ProductId];
        d.PricingTypeSyncId = types[p.PricingTypeId];
        d.SalePrice = p.SalePrice;
        d.PurchasePrice = p.PurchasePrice;
        return d;
    }
    private static BusinessSettingsSyncDto MapBusinessSettings(BusinessSettings s)
    {
        var d = new BusinessSettingsSyncDto();
        CopyBase(s, d);
        d.ProductPricingEnabled = s.ProductPricingEnabled;
        d.UpdateProductPriceOnPurchase = s.UpdateProductPriceOnPurchase;
        return d;
    }
    private static WarehouseSyncDto MapWarehouse(Warehouse w) { var d = new WarehouseSyncDto(); CopyBase(w, d); d.Name = w.Name; d.Location = w.Location; return d; }
    private static CustomerSyncDto MapCustomer(Customer c) { var d = new CustomerSyncDto(); CopyBase(c, d); d.Name = c.Name; d.Phone = c.Phone; d.Address = c.Address; d.FileNumber = c.FileNumber; d.Notes = c.Notes; return d; }
    private static SupplierSyncDto MapSupplier(Supplier s) { var d = new SupplierSyncDto(); CopyBase(s, d); d.Name = s.Name; d.Phone = s.Phone; d.Address = s.Address; d.Notes = s.Notes; return d; }
    private static CashBoxSyncDto MapCashBox(CashBox c) { var d = new CashBoxSyncDto(); CopyBase(c, d); d.Name = c.Name; d.Balance = c.Balance; return d; }
    private static BankAccountSyncDto MapBankAccount(BankAccount b) { var d = new BankAccountSyncDto(); CopyBase(b, d); d.Name = b.Name; d.AccountNumber = b.AccountNumber; d.Balance = b.Balance; return d; }
    private static InvestorSyncDto MapInvestor(Investor i) { var d = new InvestorSyncDto(); CopyBase(i, d); d.Name = i.Name; d.Phone = i.Phone; d.TotalDeposit = i.TotalDeposit; d.OpeningBalance = i.OpeningBalance; d.ProfitPercentage = i.ProfitPercentage; return d; }
    private static ExpenseTypeSyncDto MapExpenseType(ExpenseType e) { var d = new ExpenseTypeSyncDto(); CopyBase(e, d); d.Name = e.Name; return d; }
    private static PrintBrandingSettingsSyncDto MapPrintBranding(PrintBrandingSettings p)
    {
        var d = new PrintBrandingSettingsSyncDto(); CopyBase(p, d);
        d.CompanyName = p.CompanyName; d.Address = p.Address; d.PhonePrimary = p.PhonePrimary; d.PhoneSecondary = p.PhoneSecondary;
        d.Email = p.Email; d.Details = p.Details; d.ShowHeaderText = p.ShowHeaderText; d.ShowHeaderImage = p.ShowHeaderImage;
        d.HeaderImageData = p.HeaderImageData; d.HeaderImageContentType = p.HeaderImageContentType;
        d.ShowFooterText = p.ShowFooterText; d.FooterText = p.FooterText; d.ShowFooterImage = p.ShowFooterImage;
        d.FooterImageData = p.FooterImageData; d.FooterImageContentType = p.FooterImageContentType;
        return d;
    }
    private static WarehouseStockSyncDto MapWarehouseStock(WarehouseStock s, Dictionary<int, Guid> wh, Dictionary<int, Guid> pr) { var d = new WarehouseStockSyncDto(); CopyBase(s, d); d.WarehouseSyncId = wh[s.WarehouseId]; d.ProductSyncId = pr[s.ProductId]; d.Quantity = s.Quantity; d.OpeningQuantity = s.OpeningQuantity; d.UnitCost = s.UnitCost; d.MinQuantity = s.MinQuantity; return d; }
    private static WarehouseTransferSyncDto MapWarehouseTransfer(WarehouseTransfer t, Dictionary<int, Guid> wh)
    {
        var d = new WarehouseTransferSyncDto();
        CopyBase(t, d);
        d.TransferNumber = t.TransferNumber;
        d.FromWarehouseSyncId = wh[t.FromWarehouseId];
        d.ToWarehouseSyncId = wh[t.ToWarehouseId];
        d.Date = t.Date;
        d.Notes = t.Notes;
        return d;
    }
    private static WarehouseTransferItemSyncDto MapWarehouseTransferItem(WarehouseTransferItem i, Dictionary<int, Guid> transfers, Dictionary<int, Guid> pr)
    {
        var d = new WarehouseTransferItemSyncDto();
        CopyBase(i, d);
        d.WarehouseTransferSyncId = transfers[i.WarehouseTransferId];
        d.ProductSyncId = pr[i.ProductId];
        d.Quantity = i.Quantity;
        return d;
    }
    private static InvoiceSyncDto MapInvoice(Invoice i, Dictionary<int, Guid> cust, Dictionary<int, Guid> sup, Dictionary<int, Guid> wh, Dictionary<int, Guid> cb) { var d = new InvoiceSyncDto(); CopyBase(i, d); d.InvoiceNumber = i.InvoiceNumber; d.InvoiceType = i.InvoiceType; d.CustomerSyncId = i.CustomerId.HasValue ? cust.GetValueOrDefault(i.CustomerId.Value) : null; d.SupplierSyncId = i.SupplierId.HasValue ? sup.GetValueOrDefault(i.SupplierId.Value) : null; d.WarehouseSyncId = wh[i.WarehouseId]; d.PaymentMethod = i.PaymentMethod; d.TotalAmount = i.TotalAmount; d.DiscountAmount = i.DiscountAmount; d.NetAmount = i.NetAmount; d.CompanyFeePercentage = i.CompanyFeePercentage; d.CompanyFeeAmount = i.CompanyFeeAmount; d.RoundingAmount = i.RoundingAmount; d.RoundingType = i.RoundingType; d.CashBoxSyncId = i.CashBoxId.HasValue ? cb.GetValueOrDefault(i.CashBoxId.Value) : null; d.Date = i.Date; d.CreditDueDate = i.CreditDueDate; d.Notes = i.Notes; d.PaidAmount = i.PaidAmount; d.RemainingAmount = i.RemainingAmount; d.IsCreditPaid = i.IsCreditPaid; return d; }
    private static InvoiceItemSyncDto MapInvoiceItem(InvoiceItem i, Dictionary<int, Guid> inv, Dictionary<int, Guid> pr, Dictionary<int, Guid> pricingTypes)
    {
        var d = new InvoiceItemSyncDto();
        CopyBase(i, d);
        d.InvoiceSyncId = inv[i.InvoiceId];
        d.ProductSyncId = i.ProductId.HasValue ? pr.GetValueOrDefault(i.ProductId.Value) : null;
        d.PricingTypeSyncId = i.PricingTypeId.HasValue ? pricingTypes.GetValueOrDefault(i.PricingTypeId.Value) : null;
        d.ItemName = i.ItemName;
        d.Quantity = i.Quantity;
        d.UnitPrice = i.UnitPrice;
        d.TotalPrice = i.TotalPrice;
        return d;
    }
    private static InstallmentPlanSyncDto MapInstallmentPlan(InstallmentPlan p, Dictionary<int, Guid> inv, Dictionary<int, Guid> cust) { var d = new InstallmentPlanSyncDto(); CopyBase(p, d); d.InvoiceSyncId = inv[p.InvoiceId]; d.CustomerSyncId = cust[p.CustomerId]; d.FileNumber = p.FileNumber; d.TotalAmount = p.TotalAmount; d.NumberOfInstallments = p.NumberOfInstallments; d.InstallmentAmount = p.InstallmentAmount; d.StartDate = p.StartDate; d.InstallmentType = p.InstallmentType; d.CompanyFeePercentage = p.CompanyFeePercentage; d.CompanyFeeAmount = p.CompanyFeeAmount; return d; }
    private static InstallmentSyncDto MapInstallment(Installment i, Dictionary<int, Guid> plans, Dictionary<int, Guid> cb) { var d = new InstallmentSyncDto(); CopyBase(i, d); d.InstallmentPlanSyncId = plans[i.InstallmentPlanId]; d.DueDate = i.DueDate; d.Amount = i.Amount; d.PaidAmount = i.PaidAmount; d.RemainingAmount = i.RemainingAmount; d.Status = i.Status; d.PaymentDate = i.PaymentDate; d.CashBoxSyncId = i.CashBoxId.HasValue ? cb.GetValueOrDefault(i.CashBoxId.Value) : null; return d; }
    private static VoucherSyncDto MapVoucher(Voucher v, Dictionary<int, Guid> cust, Dictionary<int, Guid> inv, Dictionary<int, Guid> cb, Dictionary<int, Guid> bank) { var d = new VoucherSyncDto(); CopyBase(v, d); d.VoucherNumber = v.VoucherNumber; d.VoucherType = v.VoucherType; d.Amount = v.Amount; d.BankFees = v.BankFees; d.CustomerSyncId = v.CustomerId.HasValue ? cust.GetValueOrDefault(v.CustomerId.Value) : null; d.InvestorSyncId = v.InvestorId.HasValue ? inv.GetValueOrDefault(v.InvestorId.Value) : null; d.CashBoxSyncId = cb[v.CashBoxId]; d.BankAccountSyncId = v.BankAccountId.HasValue ? bank.GetValueOrDefault(v.BankAccountId.Value) : null; d.Date = v.Date; d.Notes = v.Notes; return d; }
    private static VoucherSyncDto? MapVoucherSafe(Voucher v, Dictionary<int, Guid> cust, Dictionary<int, Guid> inv, Dictionary<int, Guid> cb, Dictionary<int, Guid> bank)
    {
        if (!cb.TryGetValue(v.CashBoxId, out var cashBoxSyncId)) return null;
        var d = new VoucherSyncDto(); CopyBase(v, d); d.VoucherNumber = v.VoucherNumber; d.VoucherType = v.VoucherType; d.Amount = v.Amount; d.BankFees = v.BankFees;
        d.CustomerSyncId = v.CustomerId.HasValue ? cust.GetValueOrDefault(v.CustomerId.Value) : null;
        d.InvestorSyncId = v.InvestorId.HasValue ? inv.GetValueOrDefault(v.InvestorId.Value) : null;
        d.CashBoxSyncId = cashBoxSyncId;
        d.BankAccountSyncId = v.BankAccountId.HasValue ? bank.GetValueOrDefault(v.BankAccountId.Value) : null;
        d.Date = v.Date; d.Notes = v.Notes;
        return d;
    }
    private static ExpenseSyncDto MapExpense(Expense e, Dictionary<int, Guid> et, Dictionary<int, Guid> cb) { var d = new ExpenseSyncDto(); CopyBase(e, d); d.ExpenseTypeSyncId = et[e.ExpenseTypeId]; d.CashBoxSyncId = cb[e.CashBoxId]; d.Amount = e.Amount; d.Date = e.Date; d.Notes = e.Notes; return d; }
    private static TransferSyncDto MapTransfer(Transfer t, Dictionary<int, Guid> cb, Dictionary<int, Guid> bank) { var d = new TransferSyncDto(); CopyBase(t, d); d.FromType = t.FromType; d.ToType = t.ToType; d.FromSyncId = t.FromType == Core.Enums.TransferAccountType.CashBox ? cb[t.FromId] : bank[t.FromId]; d.ToSyncId = t.ToType == Core.Enums.TransferAccountType.CashBox ? cb[t.ToId] : bank[t.ToId]; d.Amount = t.Amount; d.Date = t.Date; d.Notes = t.Notes; return d; }
    private static TransferSyncDto? MapTransferSafe(Transfer t, Dictionary<int, Guid> cb, Dictionary<int, Guid> bank)
    {
        var fromMap = t.FromType == Core.Enums.TransferAccountType.CashBox ? cb : bank;
        var toMap = t.ToType == Core.Enums.TransferAccountType.CashBox ? cb : bank;
        if (!fromMap.TryGetValue(t.FromId, out var fromSyncId) || !toMap.TryGetValue(t.ToId, out var toSyncId)) return null;
        var d = new TransferSyncDto(); CopyBase(t, d); d.FromType = t.FromType; d.ToType = t.ToType;
        d.FromSyncId = fromSyncId; d.ToSyncId = toSyncId; d.Amount = t.Amount; d.Date = t.Date; d.Notes = t.Notes;
        return d;
    }
    private static InvestorTransactionSyncDto MapInvestorTransaction(InvestorTransaction t, Dictionary<int, Guid> inv) { var d = new InvestorTransactionSyncDto(); CopyBase(t, d); d.InvestorSyncId = inv[t.InvestorId]; d.Type = t.Type; d.Amount = t.Amount; d.Date = t.Date; d.Notes = t.Notes; return d; }
    private static ProfitDistributionSyncDto MapProfitDistribution(ProfitDistribution p) { var d = new ProfitDistributionSyncDto(); CopyBase(p, d); d.Date = p.Date; d.TotalProfit = p.TotalProfit; d.DistributedAmount = p.DistributedAmount; return d; }
    private static ProfitDistributionDetailSyncDto MapProfitDistributionDetail(ProfitDistributionDetail dtl, Dictionary<int, Guid> dist, Dictionary<int, Guid> inv) { var d = new ProfitDistributionDetailSyncDto(); CopyBase(dtl, d); d.ProfitDistributionSyncId = dist[dtl.ProfitDistributionId]; d.InvestorSyncId = inv[dtl.InvestorId]; d.ProfitPercentage = dtl.ProfitPercentage; d.Amount = dtl.Amount; return d; }
    private static CapitalEntrySyncDto MapCapitalEntry(CapitalEntry c) { var d = new CapitalEntrySyncDto(); CopyBase(c, d); d.Amount = c.Amount; d.Date = c.Date; d.Type = c.Type; d.Notes = c.Notes; return d; }
    private static CustomerAttachmentSyncDto MapCustomerAttachment(CustomerAttachment a, Dictionary<int, Guid> cust)
    {
        var d = new CustomerAttachmentSyncDto();
        CopyBase(a, d);
        d.CustomerSyncId = cust[a.CustomerId];
        d.FileName = a.FileName;
        d.FilePath = a.FilePath;
        d.Description = a.Description;
        if (!string.IsNullOrWhiteSpace(a.FilePath) && File.Exists(a.FilePath))
            d.FileData = File.ReadAllBytes(a.FilePath);
        return d;
    }

    private static void CopyBase(BaseEntity src, SyncDtoBase dst)
    {
        dst.SyncId = src.SyncId; dst.CreatedAt = src.CreatedAt; dst.CreatedBy = src.CreatedBy;
        dst.UpdatedAt = src.UpdatedAt; dst.UpdatedBy = src.UpdatedBy; dst.IsDeleted = src.IsDeleted;
        dst.DeletedAt = src.DeletedAt; dst.DeletedBy = src.DeletedBy; dst.RowVersion = src.RowVersion;
    }

    private static async Task<T?> FindBySyncIdAsync<T>(DbSet<T> set, Guid syncId, CancellationToken ct) where T : BaseEntity =>
        await set.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.SyncId == syncId, ct);

    private static async Task<Dictionary<Guid, int>> UpsertCategoriesAsync(AppDbContext db, List<CategorySyncDto> items, CancellationToken ct)
    {
        var map = new Dictionary<Guid, int>();
        foreach (var dto in items)
        {
            var entity = await FindBySyncIdAsync(db.Categories, dto.SyncId, ct) ?? new Category();
            if (entity.Id == 0) db.Categories.Add(entity);
            if (ShouldRejectIncoming(entity, dto)) continue;
            ApplyBase(entity, dto); entity.Name = dto.Name;
            map[dto.SyncId] = entity.Id == 0 ? 0 : entity.Id;
        }
        await db.SaveChangesAsync(ct);
        foreach (var dto in items) map[dto.SyncId] = (await FindBySyncIdAsync(db.Categories, dto.SyncId, ct))!.Id;
        return map;
    }

    private static async Task UpsertProductsAsync(AppDbContext db, List<ProductSyncDto> items, Dictionary<Guid, int> cats, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!cats.TryGetValue(dto.CategorySyncId, out var catId)) continue;
            var entity = await FindBySyncIdAsync(db.Products, dto.SyncId, ct) ?? new Product();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.Products.Add(entity);
            ApplyBase(entity, dto); entity.Name = dto.Name; entity.Description = dto.Description; entity.Barcode = dto.Barcode; entity.CategoryId = catId;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task<Dictionary<Guid, int>> UpsertPricingTypesAsync(AppDbContext db, List<PricingTypeSyncDto> items, CancellationToken ct)
    {
        var map = new Dictionary<Guid, int>();
        foreach (var dto in items)
        {
            var entity = await FindBySyncIdAsync(db.PricingTypes, dto.SyncId, ct) ?? new PricingType();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.PricingTypes.Add(entity);
            ApplyBase(entity, dto);
            entity.Name = dto.Name;
            entity.IsDefault = dto.IsDefault;
            entity.IsActive = dto.IsActive;
        }
        await db.SaveChangesAsync(ct);
        foreach (var dto in items)
        {
            var e = await FindBySyncIdAsync(db.PricingTypes, dto.SyncId, ct);
            if (e is not null) map[dto.SyncId] = e.Id;
        }
        return map;
    }

    private static async Task UpsertProductPricesAsync(
        AppDbContext db,
        List<ProductPriceSyncDto> items,
        Dictionary<Guid, int> products,
        Dictionary<Guid, int> pricingTypes,
        CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!products.TryGetValue(dto.ProductSyncId, out var productId)
                || !pricingTypes.TryGetValue(dto.PricingTypeSyncId, out var pricingTypeId))
                continue;
            var entity = await FindBySyncIdAsync(db.ProductPrices, dto.SyncId, ct) ?? new ProductPrice();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.ProductPrices.Add(entity);
            ApplyBase(entity, dto);
            entity.ProductId = productId;
            entity.PricingTypeId = pricingTypeId;
            entity.SalePrice = dto.SalePrice;
            entity.PurchasePrice = dto.PurchasePrice;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task UpsertBusinessSettingsAsync(AppDbContext db, List<BusinessSettingsSyncDto> items, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            var entity = await FindBySyncIdAsync(db.BusinessSettings, dto.SyncId, ct)
                ?? await db.BusinessSettings.IgnoreQueryFilters().OrderBy(s => s.Id).FirstOrDefaultAsync(s => !s.IsDeleted, ct)
                ?? new BusinessSettings();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.BusinessSettings.Add(entity);
            ApplyBase(entity, dto);
            entity.ProductPricingEnabled = dto.ProductPricingEnabled;
            entity.UpdateProductPriceOnPurchase = dto.UpdateProductPriceOnPurchase;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task<Dictionary<Guid, int>> UpsertWarehousesAsync(AppDbContext db, List<WarehouseSyncDto> items, CancellationToken ct) =>
        await UpsertSimpleAsync(db, db.Warehouses, items, (e, d) => { e.Name = d.Name; e.Location = d.Location; }, ct);

    private static async Task<Dictionary<Guid, int>> UpsertCustomersAsync(AppDbContext db, List<CustomerSyncDto> items, CancellationToken ct) =>
        await UpsertSimpleAsync(db, db.Customers, items, (e, d) => { e.Name = d.Name; e.Phone = d.Phone; e.Address = d.Address; e.FileNumber = d.FileNumber; e.Notes = d.Notes; }, ct);

    private static async Task<Dictionary<Guid, int>> UpsertSuppliersAsync(AppDbContext db, List<SupplierSyncDto> items, CancellationToken ct) =>
        await UpsertSimpleAsync(db, db.Suppliers, items, (e, d) => { e.Name = d.Name; e.Phone = d.Phone; e.Address = d.Address; e.Notes = d.Notes; }, ct);

    private static async Task<Dictionary<Guid, int>> UpsertCashBoxesAsync(AppDbContext db, List<CashBoxSyncDto> items, CancellationToken ct) =>
        await UpsertSimpleAsync(db, db.CashBoxes, items, (e, d) => { e.Name = d.Name; e.Balance = d.Balance; }, ct);

    private static async Task<Dictionary<Guid, int>> UpsertBankAccountsAsync(AppDbContext db, List<BankAccountSyncDto> items, CancellationToken ct) =>
        await UpsertSimpleAsync(db, db.BankAccounts, items, (e, d) => { e.Name = d.Name; e.AccountNumber = d.AccountNumber; e.Balance = d.Balance; }, ct);

    private static async Task<Dictionary<Guid, int>> UpsertInvestorsAsync(AppDbContext db, List<InvestorSyncDto> items, CancellationToken ct) =>
        await UpsertSimpleAsync(db, db.Investors, items, (e, d) => { e.Name = d.Name; e.Phone = d.Phone; e.TotalDeposit = d.TotalDeposit; e.OpeningBalance = d.OpeningBalance; e.ProfitPercentage = d.ProfitPercentage; }, ct);

    private static async Task<Dictionary<Guid, int>> UpsertExpenseTypesAsync(AppDbContext db, List<ExpenseTypeSyncDto> items, CancellationToken ct) =>
        await UpsertSimpleAsync(db, db.ExpenseTypes, items, (e, d) => e.Name = d.Name, ct);

    private static async Task UpsertPrintBrandingAsync(AppDbContext db, List<PrintBrandingSettingsSyncDto> items, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            var entity = await db.PrintBrandingSettings.FirstOrDefaultAsync(p => p.SyncId == dto.SyncId, ct)
                ?? await db.PrintBrandingSettings.FindAsync([PrintBrandingSettings.SingletonId], ct)
                ?? new PrintBrandingSettings { Id = PrintBrandingSettings.SingletonId };
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.PrintBrandingSettings.Add(entity);
            ApplyBase(entity, dto);
            entity.CompanyName = dto.CompanyName; entity.Address = dto.Address; entity.PhonePrimary = dto.PhonePrimary;
            entity.PhoneSecondary = dto.PhoneSecondary; entity.Email = dto.Email; entity.Details = dto.Details;
            entity.ShowHeaderText = dto.ShowHeaderText; entity.ShowHeaderImage = dto.ShowHeaderImage;
            entity.HeaderImageData = dto.HeaderImageData; entity.HeaderImageContentType = dto.HeaderImageContentType;
            entity.ShowFooterText = dto.ShowFooterText; entity.FooterText = dto.FooterText;
            entity.ShowFooterImage = dto.ShowFooterImage; entity.FooterImageData = dto.FooterImageData;
            entity.FooterImageContentType = dto.FooterImageContentType;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task UpsertWarehouseStocksAsync(AppDbContext db, List<WarehouseStockSyncDto> items, Dictionary<Guid, int> wh, Dictionary<Guid, int> pr, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!wh.TryGetValue(dto.WarehouseSyncId, out var wId) || !pr.TryGetValue(dto.ProductSyncId, out var pId)) continue;
            var entity = await FindBySyncIdAsync(db.WarehouseStocks, dto.SyncId, ct) ?? new WarehouseStock();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.WarehouseStocks.Add(entity);
            ApplyBase(entity, dto); entity.WarehouseId = wId; entity.ProductId = pId;
            entity.Quantity = dto.Quantity; entity.OpeningQuantity = dto.OpeningQuantity; entity.UnitCost = dto.UnitCost; entity.MinQuantity = dto.MinQuantity;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task<Dictionary<Guid, int>> UpsertWarehouseTransfersAsync(AppDbContext db, List<WarehouseTransferSyncDto> items, Dictionary<Guid, int> wh, CancellationToken ct)
    {
        var map = new Dictionary<Guid, int>();
        foreach (var dto in items)
        {
            if (!wh.TryGetValue(dto.FromWarehouseSyncId, out var fromId) || !wh.TryGetValue(dto.ToWarehouseSyncId, out var toId)) continue;
            var entity = await FindBySyncIdAsync(db.WarehouseTransfers, dto.SyncId, ct) ?? new WarehouseTransfer();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.WarehouseTransfers.Add(entity);
            ApplyBase(entity, dto);
            entity.TransferNumber = dto.TransferNumber;
            entity.FromWarehouseId = fromId;
            entity.ToWarehouseId = toId;
            entity.Date = dto.Date;
            entity.Notes = dto.Notes;
        }
        await db.SaveChangesAsync(ct);
        foreach (var dto in items)
        {
            var e = await FindBySyncIdAsync(db.WarehouseTransfers, dto.SyncId, ct);
            if (e is not null) map[dto.SyncId] = e.Id;
        }
        return map;
    }

    private static async Task UpsertWarehouseTransferItemsAsync(AppDbContext db, List<WarehouseTransferItemSyncDto> items, Dictionary<Guid, int> transfers, Dictionary<Guid, int> pr, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!transfers.TryGetValue(dto.WarehouseTransferSyncId, out var tId) || !pr.TryGetValue(dto.ProductSyncId, out var pId)) continue;
            var entity = await FindBySyncIdAsync(db.WarehouseTransferItems, dto.SyncId, ct) ?? new WarehouseTransferItem();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.WarehouseTransferItems.Add(entity);
            ApplyBase(entity, dto);
            entity.WarehouseTransferId = tId;
            entity.ProductId = pId;
            entity.Quantity = dto.Quantity;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task<Dictionary<Guid, int>> UpsertInvoicesAsync(AppDbContext db, List<InvoiceSyncDto> items, Dictionary<Guid, int> cust, Dictionary<Guid, int> sup, Dictionary<Guid, int> wh, Dictionary<Guid, int> cb, CancellationToken ct)
    {
        var map = new Dictionary<Guid, int>();
        foreach (var dto in items)
        {
            if (!wh.TryGetValue(dto.WarehouseSyncId, out var wId)) continue;
            var entity = await FindBySyncIdAsync(db.Invoices, dto.SyncId, ct) ?? new Invoice();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.Invoices.Add(entity);
            ApplyBase(entity, dto);
            entity.InvoiceNumber = dto.InvoiceNumber; entity.InvoiceType = dto.InvoiceType;
            entity.CustomerId = dto.CustomerSyncId.HasValue && cust.TryGetValue(dto.CustomerSyncId.Value, out var cId) ? cId : null;
            entity.SupplierId = dto.SupplierSyncId.HasValue && sup.TryGetValue(dto.SupplierSyncId.Value, out var sId) ? sId : null;
            entity.WarehouseId = wId;
            entity.PaymentMethod = dto.PaymentMethod; entity.TotalAmount = dto.TotalAmount; entity.DiscountAmount = dto.DiscountAmount;
            entity.NetAmount = dto.NetAmount; entity.CompanyFeePercentage = dto.CompanyFeePercentage; entity.CompanyFeeAmount = dto.CompanyFeeAmount;
            entity.RoundingAmount = dto.RoundingAmount; entity.RoundingType = dto.RoundingType;
            entity.CashBoxId = dto.CashBoxSyncId.HasValue && cb.TryGetValue(dto.CashBoxSyncId.Value, out var cbId) ? cbId : null;
            entity.Date = dto.Date; entity.CreditDueDate = dto.CreditDueDate; entity.Notes = dto.Notes;
            entity.PaidAmount = dto.PaidAmount; entity.RemainingAmount = dto.RemainingAmount; entity.IsCreditPaid = dto.IsCreditPaid;
        }
        await db.SaveChangesAsync(ct);
        foreach (var dto in items)
        {
            var e = await FindBySyncIdAsync(db.Invoices, dto.SyncId, ct);
            if (e is not null) map[dto.SyncId] = e.Id;
        }
        return map;
    }

    private static async Task UpsertInvoiceItemsAsync(
        AppDbContext db,
        List<InvoiceItemSyncDto> items,
        Dictionary<Guid, int> inv,
        Dictionary<Guid, int> pr,
        Dictionary<Guid, int> pricingTypes,
        CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!inv.TryGetValue(dto.InvoiceSyncId, out var iId)) continue;
            var entity = await FindBySyncIdAsync(db.InvoiceItems, dto.SyncId, ct) ?? new InvoiceItem();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.InvoiceItems.Add(entity);
            ApplyBase(entity, dto); entity.InvoiceId = iId;
            entity.ProductId = dto.ProductSyncId.HasValue && pr.TryGetValue(dto.ProductSyncId.Value, out var pId) ? pId : null;
            entity.PricingTypeId = dto.PricingTypeSyncId.HasValue && pricingTypes.TryGetValue(dto.PricingTypeSyncId.Value, out var ptId) ? ptId : null;
            entity.ItemName = dto.ItemName; entity.Quantity = dto.Quantity; entity.UnitPrice = dto.UnitPrice; entity.TotalPrice = dto.TotalPrice;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task<Dictionary<Guid, int>> UpsertInstallmentPlansAsync(AppDbContext db, List<InstallmentPlanSyncDto> items, Dictionary<Guid, int> inv, Dictionary<Guid, int> cust, CancellationToken ct)
    {
        var map = new Dictionary<Guid, int>();
        foreach (var dto in items)
        {
            if (!inv.TryGetValue(dto.InvoiceSyncId, out var iId) || !cust.TryGetValue(dto.CustomerSyncId, out var cId)) continue;
            var entity = await FindBySyncIdAsync(db.InstallmentPlans, dto.SyncId, ct) ?? new InstallmentPlan();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.InstallmentPlans.Add(entity);
            ApplyBase(entity, dto); entity.InvoiceId = iId; entity.CustomerId = cId;
            entity.FileNumber = dto.FileNumber; entity.TotalAmount = dto.TotalAmount; entity.NumberOfInstallments = dto.NumberOfInstallments;
            entity.InstallmentAmount = dto.InstallmentAmount; entity.StartDate = dto.StartDate; entity.InstallmentType = dto.InstallmentType;
            entity.CompanyFeePercentage = dto.CompanyFeePercentage; entity.CompanyFeeAmount = dto.CompanyFeeAmount;
        }
        await db.SaveChangesAsync(ct);
        foreach (var dto in items) { var e = await FindBySyncIdAsync(db.InstallmentPlans, dto.SyncId, ct); if (e is not null) map[dto.SyncId] = e.Id; }
        return map;
    }

    private static async Task UpsertInstallmentsAsync(AppDbContext db, List<InstallmentSyncDto> items, Dictionary<Guid, int> plans, Dictionary<Guid, int> cb, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!plans.TryGetValue(dto.InstallmentPlanSyncId, out var pId)) continue;
            var entity = await FindBySyncIdAsync(db.Installments, dto.SyncId, ct) ?? new Installment();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.Installments.Add(entity);
            ApplyBase(entity, dto); entity.InstallmentPlanId = pId;
            entity.CashBoxId = dto.CashBoxSyncId.HasValue && cb.TryGetValue(dto.CashBoxSyncId.Value, out var cbId) ? cbId : null;
            entity.DueDate = dto.DueDate; entity.Amount = dto.Amount; entity.PaidAmount = dto.PaidAmount;
            entity.RemainingAmount = dto.RemainingAmount; entity.Status = dto.Status; entity.PaymentDate = dto.PaymentDate;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task UpsertVouchersAsync(AppDbContext db, List<VoucherSyncDto> items, Dictionary<Guid, int> cust, Dictionary<Guid, int> inv, Dictionary<Guid, int> cb, Dictionary<Guid, int> bank, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!cb.TryGetValue(dto.CashBoxSyncId, out var cbId)) continue;
            var entity = await FindBySyncIdAsync(db.Vouchers, dto.SyncId, ct) ?? new Voucher();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.Vouchers.Add(entity);
            ApplyBase(entity, dto);
            entity.VoucherNumber = dto.VoucherNumber; entity.VoucherType = dto.VoucherType; entity.Amount = dto.Amount; entity.BankFees = dto.BankFees;
            entity.CustomerId = dto.CustomerSyncId.HasValue && cust.TryGetValue(dto.CustomerSyncId.Value, out var cId) ? cId : null;
            entity.InvestorId = dto.InvestorSyncId.HasValue && inv.TryGetValue(dto.InvestorSyncId.Value, out var iId) ? iId : null;
            entity.CashBoxId = cbId;
            entity.BankAccountId = dto.BankAccountSyncId.HasValue && bank.TryGetValue(dto.BankAccountSyncId.Value, out var bId) ? bId : null;
            entity.Date = dto.Date; entity.Notes = dto.Notes;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task UpsertExpensesAsync(AppDbContext db, List<ExpenseSyncDto> items, Dictionary<Guid, int> et, Dictionary<Guid, int> cb, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!et.TryGetValue(dto.ExpenseTypeSyncId, out var tId) || !cb.TryGetValue(dto.CashBoxSyncId, out var cbId)) continue;
            var entity = await FindBySyncIdAsync(db.Expenses, dto.SyncId, ct) ?? new Expense();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.Expenses.Add(entity);
            ApplyBase(entity, dto); entity.ExpenseTypeId = tId; entity.CashBoxId = cbId;
            entity.Amount = dto.Amount; entity.Date = dto.Date; entity.Notes = dto.Notes;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task UpsertTransfersAsync(AppDbContext db, List<TransferSyncDto> items, Dictionary<Guid, int> cb, Dictionary<Guid, int> bank, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            int Resolve(Core.Enums.TransferAccountType type, Guid syncId) => type == Core.Enums.TransferAccountType.CashBox
                ? cb.GetValueOrDefault(syncId) : bank.GetValueOrDefault(syncId);
            var fromId = Resolve(dto.FromType, dto.FromSyncId);
            var toId = Resolve(dto.ToType, dto.ToSyncId);
            if (fromId == 0 || toId == 0) continue;
            var entity = await FindBySyncIdAsync(db.Transfers, dto.SyncId, ct) ?? new Transfer();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.Transfers.Add(entity);
            ApplyBase(entity, dto); entity.FromType = dto.FromType; entity.FromId = fromId; entity.ToType = dto.ToType; entity.ToId = toId;
            entity.Amount = dto.Amount; entity.Date = dto.Date; entity.Notes = dto.Notes;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task UpsertInvestorTransactionsAsync(AppDbContext db, List<InvestorTransactionSyncDto> items, Dictionary<Guid, int> inv, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!inv.TryGetValue(dto.InvestorSyncId, out var iId)) continue;
            var entity = await FindBySyncIdAsync(db.InvestorTransactions, dto.SyncId, ct) ?? new InvestorTransaction();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.InvestorTransactions.Add(entity);
            ApplyBase(entity, dto); entity.InvestorId = iId; entity.Type = dto.Type; entity.Amount = dto.Amount; entity.Date = dto.Date; entity.Notes = dto.Notes;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task<Dictionary<Guid, int>> UpsertProfitDistributionsAsync(AppDbContext db, List<ProfitDistributionSyncDto> items, CancellationToken ct) =>
        await UpsertSimpleAsync(db, db.ProfitDistributions, items, (e, d) => { e.Date = d.Date; e.TotalProfit = d.TotalProfit; e.DistributedAmount = d.DistributedAmount; }, ct);

    private static async Task UpsertProfitDistributionDetailsAsync(AppDbContext db, List<ProfitDistributionDetailSyncDto> items, Dictionary<Guid, int> dist, Dictionary<Guid, int> inv, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!dist.TryGetValue(dto.ProfitDistributionSyncId, out var dId) || !inv.TryGetValue(dto.InvestorSyncId, out var iId)) continue;
            var entity = await FindBySyncIdAsync(db.ProfitDistributionDetails, dto.SyncId, ct) ?? new ProfitDistributionDetail();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.ProfitDistributionDetails.Add(entity);
            ApplyBase(entity, dto); entity.ProfitDistributionId = dId; entity.InvestorId = iId;
            entity.ProfitPercentage = dto.ProfitPercentage; entity.Amount = dto.Amount;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task UpsertCapitalEntriesAsync(AppDbContext db, List<CapitalEntrySyncDto> items, CancellationToken ct) =>
        await UpsertSimpleAsync(db, db.CapitalEntries, items, (e, d) => { e.Amount = d.Amount; e.Date = d.Date; e.Type = d.Type; e.Notes = d.Notes; }, ct);

    private static async Task UpsertCustomerAttachmentsAsync(AppDbContext db, List<CustomerAttachmentSyncDto> items, Dictionary<Guid, int> cust, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!cust.TryGetValue(dto.CustomerSyncId, out var cId)) continue;
            var entity = await FindBySyncIdAsync(db.CustomerAttachments, dto.SyncId, ct) ?? new CustomerAttachment();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) db.CustomerAttachments.Add(entity);
            ApplyBase(entity, dto);
            entity.CustomerId = cId;
            entity.FileName = dto.FileName;
            entity.FilePath = dto.FilePath;
            entity.Description = dto.Description;
            if (dto.FileData is { Length: > 0 })
            {
                var directory = Path.GetDirectoryName(dto.FilePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                    await File.WriteAllBytesAsync(dto.FilePath, dto.FileData, ct);
                }
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task<Dictionary<Guid, int>> UpsertSimpleAsync<TEntity, TDto>(
        AppDbContext db, DbSet<TEntity> set, List<TDto> items, Action<TEntity, TDto> apply, CancellationToken ct)
        where TEntity : BaseEntity, new()
        where TDto : SyncDtoBase
    {
        var map = new Dictionary<Guid, int>();
        foreach (var dto in items)
        {
            var entity = await set.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.SyncId == dto.SyncId, ct) ?? new TEntity();
            if (ShouldRejectIncoming(entity, dto)) continue;
            if (entity.Id == 0) set.Add(entity);
            ApplyBase(entity, dto);
            apply(entity, dto);
        }
        await db.SaveChangesAsync(ct);
        foreach (var dto in items)
        {
            var e = await set.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.SyncId == dto.SyncId, ct);
            if (e is not null) map[dto.SyncId] = e.Id;
        }
        return map;
    }
}
