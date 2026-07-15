using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using AlMuhasib.Sync.Dtos;
using AlMuhasib.Sync.Requests;
using AlMuhasib.Sync.Responses;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Services;

public sealed partial class SyncEngine : ISyncEngine
{
    private readonly CloudDbContext _db;

    public SyncEngine(CloudDbContext db)
    {
        _db = db;
    }

    public async Task<SyncPushResponse> PushAsync(int tenantId, SyncPushRequest request, CancellationToken ct = default)
    {
        var tenantType = await _db.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.ApplicationSystemType)
            .FirstOrDefaultAsync(ct);
        if (tenantType == (int)ApplicationSystemType.HotelManagement)
            return await PushHotelAsync(tenantId, request, ct);
        if (tenantType == (int)ApplicationSystemType.CarContracts)
            return await PushCarAsync(tenantId, request, ct);
        if (tenantType == (int)ApplicationSystemType.CarTrading)
            return await PushCarTradeAsync(tenantId, request, ct);

        var resolver = new SyncIdResolver(_db, tenantId);
        var response = new SyncPushResponse { ServerTime = DateTime.UtcNow };
        var accepted = 0;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var dto in request.Data.Categories)
                accepted += await UpsertCategoryAsync(tenantId, dto, response, ct);
            await _db.SaveChangesAsync(ct);

            foreach (var dto in request.Data.Products)
            {
                var categoryId = await resolver.ResolveCategoryAsync(dto.CategorySyncId, ct);
                if (categoryId is null) { AddConflict(response, "Product", dto.SyncId, "Category not found"); continue; }
                accepted += await UpsertProductAsync(tenantId, dto, categoryId.Value, response, ct);
            }
            await FlushAndCacheAsync(_db.Products, tenantId, request.Data.Products.Select(p => p.SyncId), resolver, ct);

            foreach (var dto in request.Data.PricingTypes)
                accepted += await UpsertPricingTypeAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.PricingTypes, tenantId, request.Data.PricingTypes.Select(p => p.SyncId), resolver, ct);

            foreach (var dto in request.Data.ProductPrices)
            {
                var productId = await resolver.ResolveProductAsync(dto.ProductSyncId, ct);
                var pricingTypeId = await resolver.ResolvePricingTypeAsync(dto.PricingTypeSyncId, ct);
                if (productId is null || pricingTypeId is null) { AddConflict(response, "ProductPrice", dto.SyncId, "FK not found"); continue; }
                accepted += await UpsertProductPriceAsync(tenantId, dto, productId.Value, pricingTypeId.Value, response, ct);
            }

            foreach (var dto in request.Data.BusinessSettings)
                accepted += await UpsertBusinessSettingsAsync(tenantId, dto, response, ct);

            foreach (var dto in request.Data.Warehouses)
                accepted += await UpsertWarehouseAsync(tenantId, dto, response, ct);

            foreach (var dto in request.Data.Customers)
                accepted += await UpsertCustomerAsync(tenantId, dto, response, ct);

            foreach (var dto in request.Data.Suppliers)
                accepted += await UpsertSupplierAsync(tenantId, dto, response, ct);

            foreach (var dto in request.Data.CashBoxes)
                accepted += await UpsertCashBoxAsync(tenantId, dto, response, ct);

            foreach (var dto in request.Data.BankAccounts)
                accepted += await UpsertBankAccountAsync(tenantId, dto, response, ct);

            foreach (var dto in request.Data.Investors)
                accepted += await UpsertInvestorAsync(tenantId, dto, response, ct);

            foreach (var dto in request.Data.ExpenseTypes)
                accepted += await UpsertExpenseTypeAsync(tenantId, dto, response, ct);

            foreach (var dto in request.Data.PrintBrandingSettings)
                accepted += await UpsertPrintBrandingAsync(tenantId, dto, response, ct);

            await _db.SaveChangesAsync(ct);
            await FlushAndCacheAsync(_db.Warehouses, tenantId, request.Data.Warehouses.Select(w => w.SyncId), resolver, ct);
            await FlushAndCacheAsync(_db.Customers, tenantId, request.Data.Customers.Select(c => c.SyncId), resolver, ct);
            await FlushAndCacheAsync(_db.Suppliers, tenantId, request.Data.Suppliers.Select(s => s.SyncId), resolver, ct);
            await FlushAndCacheAsync(_db.CashBoxes, tenantId, request.Data.CashBoxes.Select(c => c.SyncId), resolver, ct);
            await FlushAndCacheAsync(_db.BankAccounts, tenantId, request.Data.BankAccounts.Select(b => b.SyncId), resolver, ct);
            await FlushAndCacheAsync(_db.Investors, tenantId, request.Data.Investors.Select(i => i.SyncId), resolver, ct);
            await FlushAndCacheAsync(_db.ExpenseTypes, tenantId, request.Data.ExpenseTypes.Select(e => e.SyncId), resolver, ct);

            foreach (var dto in request.Data.WarehouseStocks)
            {
                var warehouseId = await resolver.ResolveWarehouseAsync(dto.WarehouseSyncId, ct);
                var productId = await resolver.ResolveProductAsync(dto.ProductSyncId, ct);
                if (warehouseId is null || productId is null) { AddConflict(response, "WarehouseStock", dto.SyncId, "FK not found"); continue; }
                accepted += await UpsertWarehouseStockAsync(tenantId, dto, warehouseId.Value, productId.Value, response, ct);
            }

            foreach (var dto in request.Data.Invoices)
            {
                var warehouseId = await resolver.ResolveWarehouseAsync(dto.WarehouseSyncId, ct);
                if (warehouseId is null) { AddConflict(response, "Invoice", dto.SyncId, "Warehouse not found"); continue; }
                var customerId = await resolver.ResolveCustomerAsync(dto.CustomerSyncId, ct);
                var supplierId = await resolver.ResolveSupplierAsync(dto.SupplierSyncId, ct);
                var cashBoxId = await resolver.ResolveCashBoxAsync(dto.CashBoxSyncId, ct);
                accepted += await UpsertInvoiceAsync(tenantId, dto, warehouseId.Value, customerId, supplierId, cashBoxId, response, ct);
            }
            await FlushAndCacheAsync(_db.Invoices, tenantId, request.Data.Invoices.Select(i => i.SyncId), resolver, ct);

            foreach (var dto in request.Data.InvoiceItems)
            {
                var invoiceId = await resolver.ResolveInvoiceAsync(dto.InvoiceSyncId, ct);
                if (invoiceId is null) { AddConflict(response, "InvoiceItem", dto.SyncId, "Invoice not found"); continue; }
                var productId = await resolver.ResolveProductAsync(dto.ProductSyncId, ct);
                var pricingTypeId = await resolver.ResolvePricingTypeAsync(dto.PricingTypeSyncId, ct);
                accepted += await UpsertInvoiceItemAsync(tenantId, dto, invoiceId.Value, productId, pricingTypeId, response, ct);
            }

            foreach (var dto in request.Data.InstallmentPlans)
            {
                var invoiceId = await resolver.ResolveInvoiceAsync(dto.InvoiceSyncId, ct);
                var customerId = await resolver.ResolveCustomerAsync(dto.CustomerSyncId, ct);
                if (invoiceId is null || customerId is null) { AddConflict(response, "InstallmentPlan", dto.SyncId, "FK not found"); continue; }
                accepted += await UpsertInstallmentPlanAsync(tenantId, dto, invoiceId.Value, customerId.Value, response, ct);
            }
            await FlushAndCacheAsync(_db.InstallmentPlans, tenantId, request.Data.InstallmentPlans.Select(p => p.SyncId), resolver, ct);

            foreach (var dto in request.Data.Installments)
            {
                var planId = await resolver.ResolveInstallmentPlanAsync(dto.InstallmentPlanSyncId, ct);
                if (planId is null) { AddConflict(response, "Installment", dto.SyncId, "Plan not found"); continue; }
                var cashBoxId = await resolver.ResolveCashBoxAsync(dto.CashBoxSyncId, ct);
                accepted += await UpsertInstallmentAsync(tenantId, dto, planId.Value, cashBoxId, response, ct);
            }

            foreach (var dto in request.Data.Vouchers)
            {
                var cashBoxId = await resolver.ResolveCashBoxAsync(dto.CashBoxSyncId, ct);
                if (cashBoxId is null) { AddConflict(response, "Voucher", dto.SyncId, "CashBox not found"); continue; }
                var customerId = await resolver.ResolveCustomerAsync(dto.CustomerSyncId, ct);
                var investorId = await resolver.ResolveInvestorAsync(dto.InvestorSyncId, ct);
                var bankId = await resolver.ResolveBankAccountAsync(dto.BankAccountSyncId, ct);
                accepted += await UpsertVoucherAsync(tenantId, dto, cashBoxId.Value, customerId, investorId, bankId, response, ct);
            }

            foreach (var dto in request.Data.Expenses)
            {
                var typeId = await resolver.ResolveExpenseTypeAsync(dto.ExpenseTypeSyncId, ct);
                var cashBoxId = await resolver.ResolveCashBoxAsync(dto.CashBoxSyncId, ct);
                if (typeId is null || cashBoxId is null) { AddConflict(response, "Expense", dto.SyncId, "FK not found"); continue; }
                accepted += await UpsertExpenseAsync(tenantId, dto, typeId.Value, cashBoxId.Value, response, ct);
            }

            foreach (var dto in request.Data.Transfers)
            {
                var fromId = await resolver.ResolveTransferAccountAsync(dto.FromType, dto.FromSyncId, ct);
                var toId = await resolver.ResolveTransferAccountAsync(dto.ToType, dto.ToSyncId, ct);
                if (fromId == 0 || toId == 0) { AddConflict(response, "Transfer", dto.SyncId, "Account not found"); continue; }
                accepted += await UpsertTransferAsync(tenantId, dto, fromId, toId, response, ct);
            }

            foreach (var dto in request.Data.InvestorTransactions)
            {
                var investorId = await resolver.ResolveInvestorAsync(dto.InvestorSyncId, ct);
                if (investorId is null) { AddConflict(response, "InvestorTransaction", dto.SyncId, "Investor not found"); continue; }
                accepted += await UpsertInvestorTransactionAsync(tenantId, dto, investorId.Value, response, ct);
            }

            foreach (var dto in request.Data.ProfitDistributions)
                accepted += await UpsertProfitDistributionAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.ProfitDistributions, tenantId, request.Data.ProfitDistributions.Select(d => d.SyncId), resolver, ct);

            foreach (var dto in request.Data.ProfitDistributionDetails)
            {
                var distId = await resolver.ResolveProfitDistributionAsync(dto.ProfitDistributionSyncId, ct);
                var investorId = await resolver.ResolveInvestorAsync(dto.InvestorSyncId, ct);
                if (distId is null || investorId is null) { AddConflict(response, "ProfitDistributionDetail", dto.SyncId, "FK not found"); continue; }
                accepted += await UpsertProfitDistributionDetailAsync(tenantId, dto, distId.Value, investorId.Value, response, ct);
            }

            foreach (var dto in request.Data.CapitalEntries)
                accepted += await UpsertCapitalEntryAsync(tenantId, dto, response, ct);

            foreach (var dto in request.Data.CustomerAttachments)
            {
                var customerId = await resolver.ResolveCustomerAsync(dto.CustomerSyncId, ct);
                if (customerId is null) { AddConflict(response, "CustomerAttachment", dto.SyncId, "Customer not found"); continue; }
                accepted += await UpsertCustomerAttachmentAsync(tenantId, dto, customerId.Value, response, ct);
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

    public async Task<SyncPullResponse> PullAsync(int tenantId, SyncPullRequest request, CancellationToken ct = default)
    {
        var tenantType = await _db.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.ApplicationSystemType)
            .FirstOrDefaultAsync(ct);
        if (tenantType == (int)ApplicationSystemType.HotelManagement)
            return await PullHotelAsync(tenantId, request, ct);
        if (tenantType == (int)ApplicationSystemType.CarContracts)
            return await PullCarAsync(tenantId, request, ct);
        if (tenantType == (int)ApplicationSystemType.CarTrading)
            return await PullCarTradeAsync(tenantId, request, ct);

        var since = request.Since ?? DateTime.MinValue;
        var bundle = new SyncDataBundle();

        bundle.Categories = await PullEntitiesAsync(_db.Categories, tenantId, since, MapCategory, ct);
        bundle.Products = await PullProductsAsync(tenantId, since, ct);
        bundle.PricingTypes = await PullEntitiesAsync(_db.PricingTypes, tenantId, since, MapPricingType, ct);
        bundle.ProductPrices = await PullProductPricesAsync(tenantId, since, ct);
        bundle.BusinessSettings = await PullEntitiesAsync(_db.BusinessSettings, tenantId, since, MapBusinessSettings, ct);
        bundle.Warehouses = await PullEntitiesAsync(_db.Warehouses, tenantId, since, MapWarehouse, ct);
        bundle.Customers = await PullEntitiesAsync(_db.Customers, tenantId, since, MapCustomer, ct);
        bundle.Suppliers = await PullEntitiesAsync(_db.Suppliers, tenantId, since, MapSupplier, ct);
        bundle.CashBoxes = await PullEntitiesAsync(_db.CashBoxes, tenantId, since, MapCashBox, ct);
        bundle.BankAccounts = await PullEntitiesAsync(_db.BankAccounts, tenantId, since, MapBankAccount, ct);
        bundle.Investors = await PullEntitiesAsync(_db.Investors, tenantId, since, MapInvestor, ct);
        bundle.ExpenseTypes = await PullEntitiesAsync(_db.ExpenseTypes, tenantId, since, MapExpenseType, ct);
        bundle.PrintBrandingSettings = await PullEntitiesAsync(_db.PrintBrandingSettings, tenantId, since, MapPrintBranding, ct);
        bundle.WarehouseStocks = await PullWarehouseStocksAsync(tenantId, since, ct);
        bundle.Invoices = await PullInvoicesAsync(tenantId, since, ct);
        bundle.InvoiceItems = await PullInvoiceItemsAsync(tenantId, since, ct);
        bundle.InstallmentPlans = await PullInstallmentPlansAsync(tenantId, since, ct);
        bundle.Installments = await PullInstallmentsAsync(tenantId, since, ct);
        bundle.Vouchers = await PullVouchersAsync(tenantId, since, ct);
        bundle.Expenses = await PullExpensesAsync(tenantId, since, ct);
        bundle.Transfers = await PullTransfersAsync(tenantId, since, ct);
        bundle.InvestorTransactions = await PullInvestorTransactionsAsync(tenantId, since, ct);
        bundle.ProfitDistributions = await PullEntitiesAsync(_db.ProfitDistributions, tenantId, since, MapProfitDistribution, ct);
        bundle.ProfitDistributionDetails = await PullProfitDistributionDetailsAsync(tenantId, since, ct);
        bundle.CapitalEntries = await PullEntitiesAsync(_db.CapitalEntries, tenantId, since, MapCapitalEntry, ct);
        bundle.CustomerAttachments = await PullCustomerAttachmentsAsync(tenantId, since, ct);

        var serverTime = DateTime.UtcNow;
        return new SyncPullResponse
        {
            Data = bundle,
            Cursor = serverTime.Ticks.ToString(),
            ServerTime = serverTime,
            HasMore = false
        };
    }

    public async Task<SyncStatusResponse> GetStatusAsync(int tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        return new SyncStatusResponse
        {
            LastSyncAt = tenant?.LastSyncAt,
            IsLicensed = tenant?.IsActive == true && tenant.IsMobileEnabled,
            LicenseMessage = tenant?.IsMobileEnabled == true ? null : "المزامنة غير مفعّلة"
        };
    }

    #region Upsert helpers

    private static void AddConflict(SyncPushResponse response, string entityType, Guid syncId, string reason, CloudBaseEntity? serverEntity = null) =>
        response.Conflicts.Add(new SyncConflict
        {
            EntityType = entityType,
            SyncId = syncId,
            Reason = reason,
            ServerVersion = serverEntity is null ? null : new CategorySyncDto
            {
                SyncId = serverEntity.SyncId,
                UpdatedAt = serverEntity.UpdatedAt,
                RowVersion = serverEntity.RowVersion
            }
        });

    private static bool ShouldReject<T>(T? existing, SyncDtoBase incoming) where T : CloudBaseEntity
    {
        if (existing is null) return false;
        if (incoming.UpdatedAt.HasValue && existing.UpdatedAt.HasValue &&
            incoming.UpdatedAt.Value < existing.UpdatedAt.Value)
            return true;
        return false;
    }

    private static void ApplyAudit(CloudBaseEntity entity, SyncDtoBase dto)
    {
        entity.SyncId = dto.SyncId;
        entity.CreatedAt = dto.CreatedAt;
        entity.CreatedBy = dto.CreatedBy;
        entity.UpdatedAt = dto.UpdatedAt ?? dto.CreatedAt;
        entity.UpdatedBy = dto.UpdatedBy;
        entity.IsDeleted = dto.IsDeleted;
        entity.DeletedAt = dto.DeletedAt;
        entity.DeletedBy = dto.DeletedBy;
    }

    private static string GetEntityTypeName(CloudBaseEntity entity) =>
        entity.GetType().Name.Replace("Cloud", "", StringComparison.Ordinal);

    private bool TryApplyAudit(CloudBaseEntity entity, SyncDtoBase dto, string entityType, SyncPushResponse response)
    {
        if (dto.SyncId == Guid.Empty)
        {
            AddConflict(response, entityType, dto.SyncId, "SyncId فارغ — حدّث التطبيق المحلي وأعد المزامنة");
            return false;
        }

        ApplyAudit(entity, dto);
        return true;
    }

    private async Task<T?> FindBySyncIdAsync<T>(DbSet<T> set, int tenantId, Guid syncId, CancellationToken ct)
        where T : CloudBaseEntity =>
        await set.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.TenantId == tenantId && e.SyncId == syncId, ct);

    private async Task FlushAndCacheAsync<T>(
        DbSet<T> set,
        int tenantId,
        IEnumerable<Guid> syncIds,
        SyncIdResolver resolver,
        CancellationToken ct) where T : CloudBaseEntity
    {
        await _db.SaveChangesAsync(ct);
        foreach (var syncId in syncIds)
        {
            var entity = await FindBySyncIdAsync(set, tenantId, syncId, ct);
            if (entity is not null)
                resolver.Cache<T>(syncId, entity.Id);
        }
    }

    private async Task<int> UpsertCategoryAsync(int tenantId, CategorySyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.Categories, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "Category", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null)
        {
            existing = new CloudCategory { TenantId = tenantId };
            _db.Categories.Add(existing);
        }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name;
        return 1;
    }

    private async Task<int> UpsertProductAsync(int tenantId, ProductSyncDto dto, int categoryId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.Products, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "Product", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudProduct { TenantId = tenantId }; _db.Products.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.Barcode = dto.Barcode;
        existing.CategoryId = categoryId;
        return 1;
    }

    private async Task<int> UpsertPricingTypeAsync(int tenantId, PricingTypeSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.PricingTypes, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "PricingType", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudPricingType { TenantId = tenantId }; _db.PricingTypes.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name;
        existing.IsDefault = dto.IsDefault;
        existing.IsActive = dto.IsActive;
        return 1;
    }

    private async Task<int> UpsertProductPriceAsync(int tenantId, ProductPriceSyncDto dto, int productId, int pricingTypeId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.ProductPrices, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "ProductPrice", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudProductPrice { TenantId = tenantId }; _db.ProductPrices.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.ProductId = productId;
        existing.PricingTypeId = pricingTypeId;
        existing.SalePrice = dto.SalePrice;
        existing.PurchasePrice = dto.PurchasePrice;
        return 1;
    }

    private async Task<int> UpsertBusinessSettingsAsync(int tenantId, BusinessSettingsSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.BusinessSettings, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "BusinessSettings", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudBusinessSettings { TenantId = tenantId }; _db.BusinessSettings.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.ProductPricingEnabled = dto.ProductPricingEnabled;
        existing.UpdateProductPriceOnPurchase = dto.UpdateProductPriceOnPurchase;
        return 1;
    }

    private async Task<int> UpsertWarehouseAsync(int tenantId, WarehouseSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.Warehouses, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "Warehouse", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudWarehouse { TenantId = tenantId }; _db.Warehouses.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name;
        existing.Location = dto.Location;
        return 1;
    }

    private async Task<int> UpsertCustomerAsync(int tenantId, CustomerSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.Customers, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "Customer", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudCustomer { TenantId = tenantId }; _db.Customers.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name;
        existing.Phone = dto.Phone;
        existing.Address = dto.Address;
        existing.FileNumber = dto.FileNumber;
        existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertSupplierAsync(int tenantId, SupplierSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.Suppliers, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "Supplier", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudSupplier { TenantId = tenantId }; _db.Suppliers.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name;
        existing.Phone = dto.Phone;
        existing.Address = dto.Address;
        existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertCashBoxAsync(int tenantId, CashBoxSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.CashBoxes, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "CashBox", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudCashBox { TenantId = tenantId }; _db.CashBoxes.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name;
        existing.Balance = dto.Balance;
        return 1;
    }

    private async Task<int> UpsertBankAccountAsync(int tenantId, BankAccountSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.BankAccounts, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "BankAccount", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudBankAccount { TenantId = tenantId }; _db.BankAccounts.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name;
        existing.AccountNumber = dto.AccountNumber;
        existing.Balance = dto.Balance;
        return 1;
    }

    private async Task<int> UpsertInvestorAsync(int tenantId, InvestorSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.Investors, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "Investor", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudInvestor { TenantId = tenantId }; _db.Investors.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name;
        existing.Phone = dto.Phone;
        existing.TotalDeposit = dto.TotalDeposit;
        existing.OpeningBalance = dto.OpeningBalance;
        existing.ProfitPercentage = dto.ProfitPercentage;
        return 1;
    }

    private async Task<int> UpsertExpenseTypeAsync(int tenantId, ExpenseTypeSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.ExpenseTypes, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "ExpenseType", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudExpenseType { TenantId = tenantId }; _db.ExpenseTypes.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name;
        return 1;
    }

    private async Task<int> UpsertPrintBrandingAsync(int tenantId, PrintBrandingSettingsSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.PrintBrandingSettings, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "PrintBrandingSettings", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudPrintBrandingSettings { TenantId = tenantId }; _db.PrintBrandingSettings.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.CompanyName = dto.CompanyName;
        existing.Address = dto.Address;
        existing.PhonePrimary = dto.PhonePrimary;
        existing.PhoneSecondary = dto.PhoneSecondary;
        existing.Email = dto.Email;
        existing.Details = dto.Details;
        existing.ShowHeaderText = dto.ShowHeaderText;
        existing.ShowHeaderImage = dto.ShowHeaderImage;
        existing.HeaderImageData = dto.HeaderImageData;
        existing.HeaderImageContentType = dto.HeaderImageContentType;
        existing.ShowFooterText = dto.ShowFooterText;
        existing.FooterText = dto.FooterText;
        existing.ShowFooterImage = dto.ShowFooterImage;
        existing.FooterImageData = dto.FooterImageData;
        existing.FooterImageContentType = dto.FooterImageContentType;
        return 1;
    }

    private async Task<int> UpsertWarehouseStockAsync(int tenantId, WarehouseStockSyncDto dto, int warehouseId, int productId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.WarehouseStocks, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "WarehouseStock", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudWarehouseStock { TenantId = tenantId }; _db.WarehouseStocks.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.WarehouseId = warehouseId;
        existing.ProductId = productId;
        existing.Quantity = dto.Quantity;
        existing.OpeningQuantity = dto.OpeningQuantity;
        existing.UnitCost = dto.UnitCost;
        return 1;
    }

    private async Task<int> UpsertInvoiceAsync(int tenantId, InvoiceSyncDto dto, int warehouseId, int? customerId, int? supplierId, int? cashBoxId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.Invoices, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "Invoice", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudInvoice { TenantId = tenantId }; _db.Invoices.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.InvoiceNumber = dto.InvoiceNumber;
        existing.InvoiceType = dto.InvoiceType;
        existing.CustomerId = customerId;
        existing.SupplierId = supplierId;
        existing.WarehouseId = warehouseId;
        existing.PaymentMethod = dto.PaymentMethod;
        existing.TotalAmount = dto.TotalAmount;
        existing.DiscountAmount = dto.DiscountAmount;
        existing.NetAmount = dto.NetAmount;
        existing.CompanyFeePercentage = dto.CompanyFeePercentage;
        existing.CompanyFeeAmount = dto.CompanyFeeAmount;
        existing.RoundingAmount = dto.RoundingAmount;
        existing.RoundingType = dto.RoundingType;
        existing.CashBoxId = cashBoxId;
        existing.Date = dto.Date;
        existing.CreditDueDate = dto.CreditDueDate;
        existing.Notes = dto.Notes;
        existing.PaidAmount = dto.PaidAmount;
        existing.RemainingAmount = dto.RemainingAmount;
        existing.IsCreditPaid = dto.IsCreditPaid;
        return 1;
    }

    private async Task<int> UpsertInvoiceItemAsync(int tenantId, InvoiceItemSyncDto dto, int invoiceId, int? productId, int? pricingTypeId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.InvoiceItems, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "InvoiceItem", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudInvoiceItem { TenantId = tenantId }; _db.InvoiceItems.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.InvoiceId = invoiceId;
        existing.ProductId = productId;
        existing.PricingTypeId = pricingTypeId;
        existing.ItemName = dto.ItemName;
        existing.Quantity = dto.Quantity;
        existing.UnitPrice = dto.UnitPrice;
        existing.TotalPrice = dto.TotalPrice;
        return 1;
    }

    private async Task<int> UpsertInstallmentPlanAsync(int tenantId, InstallmentPlanSyncDto dto, int invoiceId, int customerId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.InstallmentPlans, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "InstallmentPlan", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudInstallmentPlan { TenantId = tenantId }; _db.InstallmentPlans.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.InvoiceId = invoiceId;
        existing.CustomerId = customerId;
        existing.FileNumber = dto.FileNumber;
        existing.TotalAmount = dto.TotalAmount;
        existing.NumberOfInstallments = dto.NumberOfInstallments;
        existing.InstallmentAmount = dto.InstallmentAmount;
        existing.StartDate = dto.StartDate;
        existing.InstallmentType = dto.InstallmentType;
        existing.CompanyFeePercentage = dto.CompanyFeePercentage;
        existing.CompanyFeeAmount = dto.CompanyFeeAmount;
        return 1;
    }

    private async Task<int> UpsertInstallmentAsync(int tenantId, InstallmentSyncDto dto, int planId, int? cashBoxId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.Installments, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "Installment", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudInstallment { TenantId = tenantId }; _db.Installments.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.InstallmentPlanId = planId;
        existing.DueDate = dto.DueDate;
        existing.Amount = dto.Amount;
        existing.PaidAmount = dto.PaidAmount;
        existing.RemainingAmount = dto.RemainingAmount;
        existing.Status = dto.Status;
        existing.PaymentDate = dto.PaymentDate;
        existing.CashBoxId = cashBoxId;
        return 1;
    }

    private async Task<int> UpsertVoucherAsync(int tenantId, VoucherSyncDto dto, int cashBoxId, int? customerId, int? investorId, int? bankId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.Vouchers, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "Voucher", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudVoucher { TenantId = tenantId }; _db.Vouchers.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.VoucherNumber = dto.VoucherNumber;
        existing.VoucherType = dto.VoucherType;
        existing.Amount = dto.Amount;
        existing.BankFees = dto.BankFees;
        existing.CustomerId = customerId;
        existing.InvestorId = investorId;
        existing.CashBoxId = cashBoxId;
        existing.BankAccountId = bankId;
        existing.Date = dto.Date;
        existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertExpenseAsync(int tenantId, ExpenseSyncDto dto, int typeId, int cashBoxId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.Expenses, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "Expense", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudExpense { TenantId = tenantId }; _db.Expenses.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.ExpenseTypeId = typeId;
        existing.Amount = dto.Amount;
        existing.Date = dto.Date;
        existing.CashBoxId = cashBoxId;
        existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertTransferAsync(int tenantId, TransferSyncDto dto, int fromId, int toId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.Transfers, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "Transfer", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudTransfer { TenantId = tenantId }; _db.Transfers.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.FromType = dto.FromType;
        existing.FromId = fromId;
        existing.ToType = dto.ToType;
        existing.ToId = toId;
        existing.Amount = dto.Amount;
        existing.Date = dto.Date;
        existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertInvestorTransactionAsync(int tenantId, InvestorTransactionSyncDto dto, int investorId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.InvestorTransactions, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "InvestorTransaction", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudInvestorTransaction { TenantId = tenantId }; _db.InvestorTransactions.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.InvestorId = investorId;
        existing.Type = dto.Type;
        existing.Amount = dto.Amount;
        existing.Date = dto.Date;
        existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertProfitDistributionAsync(int tenantId, ProfitDistributionSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.ProfitDistributions, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "ProfitDistribution", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudProfitDistribution { TenantId = tenantId }; _db.ProfitDistributions.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.Date = dto.Date;
        existing.TotalProfit = dto.TotalProfit;
        existing.DistributedAmount = dto.DistributedAmount;
        return 1;
    }

    private async Task<int> UpsertProfitDistributionDetailAsync(int tenantId, ProfitDistributionDetailSyncDto dto, int distId, int investorId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.ProfitDistributionDetails, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "ProfitDistributionDetail", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudProfitDistributionDetail { TenantId = tenantId }; _db.ProfitDistributionDetails.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.ProfitDistributionId = distId;
        existing.InvestorId = investorId;
        existing.ProfitPercentage = dto.ProfitPercentage;
        existing.Amount = dto.Amount;
        return 1;
    }

    private async Task<int> UpsertCapitalEntryAsync(int tenantId, CapitalEntrySyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.CapitalEntries, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "CapitalEntry", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudCapitalEntry { TenantId = tenantId }; _db.CapitalEntries.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.Amount = dto.Amount;
        existing.Date = dto.Date;
        existing.Type = dto.Type;
        existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertCustomerAttachmentAsync(int tenantId, CustomerAttachmentSyncDto dto, int customerId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.CustomerAttachments, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "CustomerAttachment", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudCustomerAttachment { TenantId = tenantId }; _db.CustomerAttachments.Add(existing); }
        if (!TryApplyAudit(existing, dto, entityType: GetEntityTypeName(existing), response)) return 0;
        existing.CustomerId = customerId;
        existing.FileName = dto.FileName;
        existing.FilePath = dto.FilePath;
        existing.Description = dto.Description;
        existing.FileData = dto.FileData;
        return 1;
    }

    #endregion

    #region Pull helpers

    private static async Task<List<TDto>> PullEntitiesAsync<TEntity, TDto>(
        DbSet<TEntity> set, int tenantId, DateTime since, Func<TEntity, Dictionary<int, Guid>, TDto> map, CancellationToken ct)
        where TEntity : CloudBaseEntity
    {
        var entities = await set.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since)
            .ToListAsync(ct);
        return entities.Select(e => map(e, new Dictionary<int, Guid>())).ToList();
    }

    private async Task<List<ProductSyncDto>> PullProductsAsync(int tenantId, DateTime since, CancellationToken ct)
    {
        var categories = await _db.Categories.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToListAsync(ct);
        var catMap = categories.ToDictionary(c => c.Id, c => c.SyncId);
        var products = await _db.Products.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        return products.Select(p => new ProductSyncDto
        {
            SyncId = p.SyncId, CreatedAt = p.CreatedAt, CreatedBy = p.CreatedBy, UpdatedAt = p.UpdatedAt, UpdatedBy = p.UpdatedBy,
            IsDeleted = p.IsDeleted, DeletedAt = p.DeletedAt, DeletedBy = p.DeletedBy, RowVersion = p.RowVersion,
            Name = p.Name, Description = p.Description, Barcode = p.Barcode,
            CategorySyncId = catMap.GetValueOrDefault(p.CategoryId)
        }).ToList();
    }

    private async Task<List<ProductPriceSyncDto>> PullProductPricesAsync(int tenantId, DateTime since, CancellationToken ct)
    {
        var products = await _db.Products.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var types = await _db.PricingTypes.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var items = await _db.ProductPrices.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        return items.Select(p => new ProductPriceSyncDto
        {
            SyncId = p.SyncId, CreatedAt = p.CreatedAt, CreatedBy = p.CreatedBy, UpdatedAt = p.UpdatedAt, UpdatedBy = p.UpdatedBy,
            IsDeleted = p.IsDeleted, DeletedAt = p.DeletedAt, DeletedBy = p.DeletedBy, RowVersion = p.RowVersion,
            ProductSyncId = products.GetValueOrDefault(p.ProductId),
            PricingTypeSyncId = types.GetValueOrDefault(p.PricingTypeId),
            SalePrice = p.SalePrice,
            PurchasePrice = p.PurchasePrice
        }).ToList();
    }

    private async Task<List<WarehouseStockSyncDto>> PullWarehouseStocksAsync(int tenantId, DateTime since, CancellationToken ct)
    {
        var wh = await _db.Warehouses.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var pr = await _db.Products.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var items = await _db.WarehouseStocks.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        return items.Select(s => new WarehouseStockSyncDto
        {
            SyncId = s.SyncId, CreatedAt = s.CreatedAt, CreatedBy = s.CreatedBy, UpdatedAt = s.UpdatedAt, UpdatedBy = s.UpdatedBy,
            IsDeleted = s.IsDeleted, DeletedAt = s.DeletedAt, DeletedBy = s.DeletedBy, RowVersion = s.RowVersion,
            WarehouseSyncId = wh.GetValueOrDefault(s.WarehouseId), ProductSyncId = pr.GetValueOrDefault(s.ProductId),
            Quantity = s.Quantity, OpeningQuantity = s.OpeningQuantity, UnitCost = s.UnitCost
        }).ToList();
    }

    private async Task<List<InvoiceSyncDto>> PullInvoicesAsync(int tenantId, DateTime since, CancellationToken ct)
    {
        var customers = await _db.Customers.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var suppliers = await _db.Suppliers.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var warehouses = await _db.Warehouses.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var cashBoxes = await _db.CashBoxes.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var invoices = await _db.Invoices.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        return invoices.Select(i => new InvoiceSyncDto
        {
            SyncId = i.SyncId, CreatedAt = i.CreatedAt, CreatedBy = i.CreatedBy, UpdatedAt = i.UpdatedAt, UpdatedBy = i.UpdatedBy,
            IsDeleted = i.IsDeleted, DeletedAt = i.DeletedAt, DeletedBy = i.DeletedBy, RowVersion = i.RowVersion,
            InvoiceNumber = i.InvoiceNumber, InvoiceType = i.InvoiceType,
            CustomerSyncId = i.CustomerId.HasValue ? customers.GetValueOrDefault(i.CustomerId.Value) : null,
            SupplierSyncId = i.SupplierId.HasValue ? suppliers.GetValueOrDefault(i.SupplierId.Value) : null,
            WarehouseSyncId = warehouses.GetValueOrDefault(i.WarehouseId),
            PaymentMethod = i.PaymentMethod, TotalAmount = i.TotalAmount, DiscountAmount = i.DiscountAmount, NetAmount = i.NetAmount,
            CompanyFeePercentage = i.CompanyFeePercentage, CompanyFeeAmount = i.CompanyFeeAmount,
            RoundingAmount = i.RoundingAmount, RoundingType = i.RoundingType,
            CashBoxSyncId = i.CashBoxId.HasValue ? cashBoxes.GetValueOrDefault(i.CashBoxId.Value) : null,
            Date = i.Date, CreditDueDate = i.CreditDueDate, Notes = i.Notes,
            PaidAmount = i.PaidAmount, RemainingAmount = i.RemainingAmount, IsCreditPaid = i.IsCreditPaid
        }).ToList();
    }

    private async Task<List<InvoiceItemSyncDto>> PullInvoiceItemsAsync(int tenantId, DateTime since, CancellationToken ct)
    {
        var invoices = await _db.Invoices.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var products = await _db.Products.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var pricingTypes = await _db.PricingTypes.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var items = await _db.InvoiceItems.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        return items.Select(i => new InvoiceItemSyncDto
        {
            SyncId = i.SyncId, CreatedAt = i.CreatedAt, CreatedBy = i.CreatedBy, UpdatedAt = i.UpdatedAt, UpdatedBy = i.UpdatedBy,
            IsDeleted = i.IsDeleted, DeletedAt = i.DeletedAt, DeletedBy = i.DeletedBy, RowVersion = i.RowVersion,
            InvoiceSyncId = invoices.GetValueOrDefault(i.InvoiceId),
            ProductSyncId = i.ProductId.HasValue ? products.GetValueOrDefault(i.ProductId.Value) : null,
            PricingTypeSyncId = i.PricingTypeId.HasValue ? pricingTypes.GetValueOrDefault(i.PricingTypeId.Value) : null,
            ItemName = i.ItemName, Quantity = i.Quantity, UnitPrice = i.UnitPrice, TotalPrice = i.TotalPrice
        }).ToList();
    }

    private async Task<List<InstallmentPlanSyncDto>> PullInstallmentPlansAsync(int tenantId, DateTime since, CancellationToken ct)
    {
        var invoices = await _db.Invoices.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var customers = await _db.Customers.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var plans = await _db.InstallmentPlans.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        return plans.Select(p => new InstallmentPlanSyncDto
        {
            SyncId = p.SyncId, CreatedAt = p.CreatedAt, CreatedBy = p.CreatedBy, UpdatedAt = p.UpdatedAt, UpdatedBy = p.UpdatedBy,
            IsDeleted = p.IsDeleted, DeletedAt = p.DeletedAt, DeletedBy = p.DeletedBy, RowVersion = p.RowVersion,
            InvoiceSyncId = invoices.GetValueOrDefault(p.InvoiceId), CustomerSyncId = customers.GetValueOrDefault(p.CustomerId),
            FileNumber = p.FileNumber, TotalAmount = p.TotalAmount, NumberOfInstallments = p.NumberOfInstallments,
            InstallmentAmount = p.InstallmentAmount, StartDate = p.StartDate, InstallmentType = p.InstallmentType,
            CompanyFeePercentage = p.CompanyFeePercentage, CompanyFeeAmount = p.CompanyFeeAmount
        }).ToList();
    }

    private async Task<List<InstallmentSyncDto>> PullInstallmentsAsync(int tenantId, DateTime since, CancellationToken ct)
    {
        var plans = await _db.InstallmentPlans.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var cashBoxes = await _db.CashBoxes.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var items = await _db.Installments.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        return items.Select(i => new InstallmentSyncDto
        {
            SyncId = i.SyncId, CreatedAt = i.CreatedAt, CreatedBy = i.CreatedBy, UpdatedAt = i.UpdatedAt, UpdatedBy = i.UpdatedBy,
            IsDeleted = i.IsDeleted, DeletedAt = i.DeletedAt, DeletedBy = i.DeletedBy, RowVersion = i.RowVersion,
            InstallmentPlanSyncId = plans.GetValueOrDefault(i.InstallmentPlanId),
            DueDate = i.DueDate, Amount = i.Amount, PaidAmount = i.PaidAmount, RemainingAmount = i.RemainingAmount,
            Status = i.Status, PaymentDate = i.PaymentDate,
            CashBoxSyncId = i.CashBoxId.HasValue ? cashBoxes.GetValueOrDefault(i.CashBoxId.Value) : null
        }).ToList();
    }

    private async Task<List<VoucherSyncDto>> PullVouchersAsync(int tenantId, DateTime since, CancellationToken ct)
    {
        var customers = await _db.Customers.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var investors = await _db.Investors.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var cashBoxes = await _db.CashBoxes.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var banks = await _db.BankAccounts.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var vouchers = await _db.Vouchers.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        return vouchers.Select(v => new VoucherSyncDto
        {
            SyncId = v.SyncId, CreatedAt = v.CreatedAt, CreatedBy = v.CreatedBy, UpdatedAt = v.UpdatedAt, UpdatedBy = v.UpdatedBy,
            IsDeleted = v.IsDeleted, DeletedAt = v.DeletedAt, DeletedBy = v.DeletedBy, RowVersion = v.RowVersion,
            VoucherNumber = v.VoucherNumber, VoucherType = v.VoucherType, Amount = v.Amount, BankFees = v.BankFees,
            CustomerSyncId = v.CustomerId.HasValue ? customers.GetValueOrDefault(v.CustomerId.Value) : null,
            InvestorSyncId = v.InvestorId.HasValue ? investors.GetValueOrDefault(v.InvestorId.Value) : null,
            CashBoxSyncId = cashBoxes.GetValueOrDefault(v.CashBoxId),
            BankAccountSyncId = v.BankAccountId.HasValue ? banks.GetValueOrDefault(v.BankAccountId.Value) : null,
            Date = v.Date, Notes = v.Notes
        }).ToList();
    }

    private async Task<List<ExpenseSyncDto>> PullExpensesAsync(int tenantId, DateTime since, CancellationToken ct)
    {
        var types = await _db.ExpenseTypes.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var cashBoxes = await _db.CashBoxes.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var expenses = await _db.Expenses.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        return expenses.Select(e => new ExpenseSyncDto
        {
            SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
            IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
            ExpenseTypeSyncId = types.GetValueOrDefault(e.ExpenseTypeId),
            CashBoxSyncId = cashBoxes.GetValueOrDefault(e.CashBoxId),
            Amount = e.Amount, Date = e.Date, Notes = e.Notes
        }).ToList();
    }

    private async Task<List<TransferSyncDto>> PullTransfersAsync(int tenantId, DateTime since, CancellationToken ct)
    {
        var cashBoxes = await _db.CashBoxes.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var banks = await _db.BankAccounts.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        Guid ResolveId(TransferAccountType type, int id) => type switch
        {
            TransferAccountType.CashBox => cashBoxes.GetValueOrDefault(id),
            TransferAccountType.Bank => banks.GetValueOrDefault(id),
            _ => Guid.Empty
        };
        var transfers = await _db.Transfers.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        return transfers.Select(t => new TransferSyncDto
        {
            SyncId = t.SyncId, CreatedAt = t.CreatedAt, CreatedBy = t.CreatedBy, UpdatedAt = t.UpdatedAt, UpdatedBy = t.UpdatedBy,
            IsDeleted = t.IsDeleted, DeletedAt = t.DeletedAt, DeletedBy = t.DeletedBy, RowVersion = t.RowVersion,
            FromType = t.FromType, FromSyncId = ResolveId(t.FromType, t.FromId),
            ToType = t.ToType, ToSyncId = ResolveId(t.ToType, t.ToId),
            Amount = t.Amount, Date = t.Date, Notes = t.Notes
        }).ToList();
    }

    private async Task<List<InvestorTransactionSyncDto>> PullInvestorTransactionsAsync(int tenantId, DateTime since, CancellationToken ct)
    {
        var investors = await _db.Investors.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var txs = await _db.InvestorTransactions.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        return txs.Select(t => new InvestorTransactionSyncDto
        {
            SyncId = t.SyncId, CreatedAt = t.CreatedAt, CreatedBy = t.CreatedBy, UpdatedAt = t.UpdatedAt, UpdatedBy = t.UpdatedBy,
            IsDeleted = t.IsDeleted, DeletedAt = t.DeletedAt, DeletedBy = t.DeletedBy, RowVersion = t.RowVersion,
            InvestorSyncId = investors.GetValueOrDefault(t.InvestorId),
            Type = t.Type, Amount = t.Amount, Date = t.Date, Notes = t.Notes
        }).ToList();
    }

    private async Task<List<ProfitDistributionDetailSyncDto>> PullProfitDistributionDetailsAsync(int tenantId, DateTime since, CancellationToken ct)
    {
        var dists = await _db.ProfitDistributions.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var investors = await _db.Investors.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var details = await _db.ProfitDistributionDetails.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        return details.Select(d => new ProfitDistributionDetailSyncDto
        {
            SyncId = d.SyncId, CreatedAt = d.CreatedAt, CreatedBy = d.CreatedBy, UpdatedAt = d.UpdatedAt, UpdatedBy = d.UpdatedBy,
            IsDeleted = d.IsDeleted, DeletedAt = d.DeletedAt, DeletedBy = d.DeletedBy, RowVersion = d.RowVersion,
            ProfitDistributionSyncId = dists.GetValueOrDefault(d.ProfitDistributionId),
            InvestorSyncId = investors.GetValueOrDefault(d.InvestorId),
            ProfitPercentage = d.ProfitPercentage, Amount = d.Amount
        }).ToList();
    }

    private async Task<List<CustomerAttachmentSyncDto>> PullCustomerAttachmentsAsync(int tenantId, DateTime since, CancellationToken ct)
    {
        var customers = await _db.Customers.IgnoreQueryFilters().Where(e => e.TenantId == tenantId).ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var attachments = await _db.CustomerAttachments.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        return attachments.Select(a => new CustomerAttachmentSyncDto
        {
            SyncId = a.SyncId, CreatedAt = a.CreatedAt, CreatedBy = a.CreatedBy, UpdatedAt = a.UpdatedAt, UpdatedBy = a.UpdatedBy,
            IsDeleted = a.IsDeleted, DeletedAt = a.DeletedAt, DeletedBy = a.DeletedBy, RowVersion = a.RowVersion,
            CustomerSyncId = customers.GetValueOrDefault(a.CustomerId),
            FileName = a.FileName, FilePath = a.FilePath, Description = a.Description, FileData = a.FileData
        }).ToList();
    }

    private static CategorySyncDto MapCategory(CloudCategory e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion, Name = e.Name
    };

    private static PricingTypeSyncDto MapPricingType(CloudPricingType e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, IsDefault = e.IsDefault, IsActive = e.IsActive
    };

    private static BusinessSettingsSyncDto MapBusinessSettings(CloudBusinessSettings e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        ProductPricingEnabled = e.ProductPricingEnabled,
        UpdateProductPriceOnPurchase = e.UpdateProductPriceOnPurchase
    };

    private static WarehouseSyncDto MapWarehouse(CloudWarehouse e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, Location = e.Location
    };

    private static CustomerSyncDto MapCustomer(CloudCustomer e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, Phone = e.Phone, Address = e.Address, FileNumber = e.FileNumber, Notes = e.Notes
    };

    private static SupplierSyncDto MapSupplier(CloudSupplier e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, Phone = e.Phone, Address = e.Address, Notes = e.Notes
    };

    private static CashBoxSyncDto MapCashBox(CloudCashBox e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, Balance = e.Balance
    };

    private static BankAccountSyncDto MapBankAccount(CloudBankAccount e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, AccountNumber = e.AccountNumber, Balance = e.Balance
    };

    private static InvestorSyncDto MapInvestor(CloudInvestor e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, Phone = e.Phone, TotalDeposit = e.TotalDeposit, OpeningBalance = e.OpeningBalance, ProfitPercentage = e.ProfitPercentage
    };

    private static ExpenseTypeSyncDto MapExpenseType(CloudExpenseType e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion, Name = e.Name
    };

    private static PrintBrandingSettingsSyncDto MapPrintBranding(CloudPrintBrandingSettings e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        CompanyName = e.CompanyName, Address = e.Address, PhonePrimary = e.PhonePrimary, PhoneSecondary = e.PhoneSecondary,
        Email = e.Email, Details = e.Details, ShowHeaderText = e.ShowHeaderText, ShowHeaderImage = e.ShowHeaderImage,
        HeaderImageData = e.HeaderImageData, HeaderImageContentType = e.HeaderImageContentType,
        ShowFooterText = e.ShowFooterText, FooterText = e.FooterText, ShowFooterImage = e.ShowFooterImage,
        FooterImageData = e.FooterImageData, FooterImageContentType = e.FooterImageContentType
    };

    private static ProfitDistributionSyncDto MapProfitDistribution(CloudProfitDistribution e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Date = e.Date, TotalProfit = e.TotalProfit, DistributedAmount = e.DistributedAmount
    };

    private static CapitalEntrySyncDto MapCapitalEntry(CloudCapitalEntry e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Amount = e.Amount, Date = e.Date, Type = e.Type, Notes = e.Notes
    };

    #endregion
}
