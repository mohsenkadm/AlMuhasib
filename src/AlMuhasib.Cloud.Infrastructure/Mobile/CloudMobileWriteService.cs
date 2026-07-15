using AlMuhasib.Cloud.Application.Abstractions;
using AlMuhasib.Cloud.Application.Models;
using AlMuhasib.Cloud.Application.Models.Mobile;
using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core;
using AlMuhasib.Core.Enums;
using AlMuhasib.Sync;
using AlMuhasib.Sync.Dtos;
using AlMuhasib.Sync.Requests;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Mobile;

public sealed class CloudMobileWriteService : ICloudMobileWriteService
{
    private readonly CloudDbContext _db;
    private readonly ISyncEngine _syncEngine;

    public CloudMobileWriteService(CloudDbContext db, ISyncEngine syncEngine)
    {
        _db = db;
        _syncEngine = syncEngine;
    }

    public Task<MobileWriteResponse> UpsertCustomerAsync(int tenantId, CreateCustomerRequest request, string username, CancellationToken ct = default)
    {
        var syncId = request.SyncId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto = new CustomerSyncDto
        {
            SyncId = syncId,
            Name = request.Name.Trim(),
            Phone = request.Phone,
            Address = request.Address,
            Notes = request.Notes,
            CreatedAt = now,
            CreatedBy = username
        };
        return PushSingleAsync(tenantId, bundle => bundle.Customers.Add(dto), syncId, ct);
    }

    public Task<MobileWriteResponse> UpsertSupplierAsync(int tenantId, CreateSupplierRequest request, string username, CancellationToken ct = default)
    {
        var syncId = request.SyncId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto = new SupplierSyncDto
        {
            SyncId = syncId,
            Name = request.Name.Trim(),
            Phone = request.Phone,
            Address = request.Address,
            Notes = request.Notes,
            CreatedAt = now,
            CreatedBy = username
        };
        return PushSingleAsync(tenantId, bundle => bundle.Suppliers.Add(dto), syncId, ct);
    }

    public Task<MobileWriteResponse> UpsertProductAsync(int tenantId, CreateProductRequest request, string username, CancellationToken ct = default)
    {
        var syncId = request.SyncId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto = new ProductSyncDto
        {
            SyncId = syncId,
            Name = request.Name.Trim(),
            Barcode = request.Barcode,
            Description = request.Description,
            CategorySyncId = request.CategorySyncId,
            CreatedAt = now,
            CreatedBy = username
        };
        return PushSingleAsync(tenantId, bundle => bundle.Products.Add(dto), syncId, ct);
    }

    public Task<MobileWriteResponse> UpsertInvestorAsync(int tenantId, CreateInvestorRequest request, string username, CancellationToken ct = default)
    {
        var syncId = request.SyncId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto = new InvestorSyncDto
        {
            SyncId = syncId,
            Name = request.Name.Trim(),
            Phone = request.Phone,
            ProfitPercentage = request.ProfitPercentage,
            OpeningBalance = request.OpeningBalance,
            TotalDeposit = request.OpeningBalance,
            CreatedAt = now,
            CreatedBy = username
        };
        return PushSingleAsync(tenantId, bundle => bundle.Investors.Add(dto), syncId, ct);
    }

    public Task<MobileWriteResponse> UpsertPricingTypeAsync(int tenantId, UpsertPricingTypeRequest request, string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("اسم نوع التسعير مطلوب");

        var syncId = request.SyncId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto = new PricingTypeSyncDto
        {
            SyncId = syncId,
            Name = request.Name.Trim(),
            IsDefault = request.IsDefault,
            IsActive = request.IsActive,
            CreatedAt = now,
            CreatedBy = username
        };
        return PushSingleAsync(tenantId, bundle => bundle.PricingTypes.Add(dto), syncId, ct);
    }

    public async Task<MobileWriteResponse> DeletePricingTypeAsync(int tenantId, Guid syncId, string username, CancellationToken ct = default)
    {
        var existing = await _db.PricingTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.SyncId == syncId && !t.IsDeleted, ct);
        if (existing is null)
            return new MobileWriteResponse { SyncId = syncId, Message = "نوع التسعير غير موجود" };

        if (existing.IsDefault)
            throw new ArgumentException("لا يمكن حذف نوع التسعير الافتراضي");

        var inUse = await _db.ProductPrices.IgnoreQueryFilters()
            .AnyAsync(p => p.TenantId == tenantId && p.PricingTypeId == existing.Id && !p.IsDeleted, ct);
        if (inUse)
            throw new ArgumentException("نوع التسعير مستخدم في أسعار منتجات");

        var now = DateTime.UtcNow;
        var dto = new PricingTypeSyncDto
        {
            SyncId = existing.SyncId,
            Name = existing.Name,
            IsDefault = existing.IsDefault,
            IsActive = existing.IsActive,
            CreatedAt = existing.CreatedAt,
            CreatedBy = existing.CreatedBy,
            UpdatedAt = now,
            UpdatedBy = username,
            IsDeleted = true,
            DeletedAt = now,
            DeletedBy = username,
            RowVersion = existing.RowVersion
        };
        return await PushSingleAsync(tenantId, bundle => bundle.PricingTypes.Add(dto), syncId, ct);
    }

    public Task<MobileWriteResponse> UpsertProductPriceAsync(int tenantId, UpsertProductPriceRequest request, string username, CancellationToken ct = default)
    {
        if (request.SalePrice < 0 || request.PurchasePrice < 0)
            throw new ArgumentException("الأسعار لا يمكن أن تكون سالبة");

        var syncId = request.SyncId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto = new ProductPriceSyncDto
        {
            SyncId = syncId,
            ProductSyncId = request.ProductSyncId,
            PricingTypeSyncId = request.PricingTypeSyncId,
            SalePrice = request.SalePrice,
            PurchasePrice = request.PurchasePrice,
            CreatedAt = now,
            CreatedBy = username
        };
        return PushSingleAsync(tenantId, bundle => bundle.ProductPrices.Add(dto), syncId, ct);
    }

    public async Task<MobileWriteResponse> DeleteProductPriceAsync(int tenantId, Guid syncId, string username, CancellationToken ct = default)
    {
        var existing = await _db.ProductPrices.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.SyncId == syncId && !p.IsDeleted, ct);
        if (existing is null)
            return new MobileWriteResponse { SyncId = syncId, Message = "سعر المنتج غير موجود" };

        var product = await _db.Products.IgnoreQueryFilters()
            .FirstAsync(p => p.Id == existing.ProductId, ct);
        var pricingType = await _db.PricingTypes.IgnoreQueryFilters()
            .FirstAsync(t => t.Id == existing.PricingTypeId, ct);

        var now = DateTime.UtcNow;
        var dto = new ProductPriceSyncDto
        {
            SyncId = existing.SyncId,
            ProductSyncId = product.SyncId,
            PricingTypeSyncId = pricingType.SyncId,
            SalePrice = existing.SalePrice,
            PurchasePrice = existing.PurchasePrice,
            CreatedAt = existing.CreatedAt,
            CreatedBy = existing.CreatedBy,
            UpdatedAt = now,
            UpdatedBy = username,
            IsDeleted = true,
            DeletedAt = now,
            DeletedBy = username,
            RowVersion = existing.RowVersion
        };
        return await PushSingleAsync(tenantId, bundle => bundle.ProductPrices.Add(dto), syncId, ct);
    }

    public async Task<BusinessSettingsDto> UpdateBusinessSettingsAsync(
        int tenantId, UpdateBusinessSettingsRequest request, string username, CancellationToken ct = default)
    {
        var existing = await _db.BusinessSettings.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && !s.IsDeleted, ct);

        var syncId = existing?.SyncId ?? ProductPricingSyncIds.BusinessSettings;
        var now = DateTime.UtcNow;
        var dto = new BusinessSettingsSyncDto
        {
            SyncId = syncId,
            ProductPricingEnabled = request.ProductPricingEnabled,
            UpdateProductPriceOnPurchase = request.UpdateProductPriceOnPurchase,
            CreatedAt = existing?.CreatedAt ?? now,
            CreatedBy = existing?.CreatedBy ?? username,
            UpdatedAt = now,
            UpdatedBy = username,
            RowVersion = existing?.RowVersion
        };

        var response = await _syncEngine.PushAsync(tenantId, new SyncPushRequest
        {
            Data = new SyncDataBundle { BusinessSettings = [dto] }
        }, ct);
        if (response.Conflicts.Count > 0)
            throw new InvalidOperationException(response.Conflicts[0].Reason);

        if (request.ProductPricingEnabled)
            await EnsureDefaultPricingTypeAsync(tenantId, username, ct);

        var saved = await _db.BusinessSettings.AsNoTracking()
            .FirstAsync(s => s.TenantId == tenantId && s.SyncId == syncId, ct);
        return new BusinessSettingsDto
        {
            SyncId = saved.SyncId,
            ProductPricingEnabled = saved.ProductPricingEnabled,
            UpdateProductPriceOnPurchase = saved.UpdateProductPriceOnPurchase
        };
    }

    public async Task<MobileWriteResponse> CreateInvoiceAsync(int tenantId, CreateInvoiceRequest request, string username, CancellationToken ct = default)
    {
        ValidateInvoiceRequest(request);

        var invoiceSyncId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var items = request.Items.Select(i =>
        {
            var lineTotal = i.Quantity * i.UnitPrice - i.DiscountAmount;
            return new CreateInvoiceItemRequest
            {
                ProductSyncId = i.ProductSyncId,
                PricingTypeSyncId = i.PricingTypeSyncId,
                ItemName = string.IsNullOrWhiteSpace(i.ItemName) ? "بند" : i.ItemName.Trim(),
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountAmount = i.DiscountAmount
            };
        }).ToList();

        decimal subtotal = items.Sum(i => i.Quantity * i.UnitPrice - i.DiscountAmount);
        decimal netBeforeRounding = subtotal - request.DiscountAmount;
        decimal rounding = CalculateRounding(netBeforeRounding, request.InvoiceType);
        decimal netAmount = netBeforeRounding + rounding;

        var isCredit = request.PaymentMethod == PaymentMethod.Credit;
        var isInstallment = request.PaymentMethod == PaymentMethod.Installment
                            || request.InvoiceType == InvoiceType.Installment;

        var invoiceDto = new InvoiceSyncDto
        {
            SyncId = invoiceSyncId,
            InvoiceNumber = string.Empty,
            InvoiceType = request.InvoiceType,
            CustomerSyncId = request.CustomerSyncId,
            SupplierSyncId = request.SupplierSyncId,
            WarehouseSyncId = request.WarehouseSyncId,
            PaymentMethod = request.PaymentMethod,
            TotalAmount = subtotal,
            DiscountAmount = request.DiscountAmount,
            NetAmount = netAmount,
            RoundingAmount = rounding,
            RoundingType = request.InvoiceType == InvoiceType.Purchase || request.InvoiceType == InvoiceType.PurchaseReturn
                ? RoundingType.RoundUp
                : RoundingType.RoundDown,
            CashBoxSyncId = request.CashBoxSyncId,
            Date = request.Date,
            CreditDueDate = request.CreditDueDate,
            Notes = request.Notes,
            PaidAmount = isCredit ? 0 : netAmount,
            RemainingAmount = isCredit ? netAmount : 0,
            IsCreditPaid = !isCredit,
            CreatedAt = now,
            CreatedBy = username
        };

        var itemDtos = new List<InvoiceItemSyncDto>();
        foreach (var item in items)
        {
            itemDtos.Add(new InvoiceItemSyncDto
            {
                SyncId = Guid.NewGuid(),
                InvoiceSyncId = invoiceSyncId,
                ProductSyncId = item.ProductSyncId,
                PricingTypeSyncId = item.PricingTypeSyncId,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.Quantity * item.UnitPrice - item.DiscountAmount,
                CreatedAt = now,
                CreatedBy = username
            });
        }

        InstallmentPlanSyncDto? planDto = null;
        var installmentDtos = new List<InstallmentSyncDto>();
        if (isInstallment && request.InstallmentPlan is not null && request.CustomerSyncId.HasValue)
        {
            var plan = request.InstallmentPlan;
            var planSyncId = Guid.NewGuid();
            var (feePct, feeAmt) = CompanyFeeHelper.ResolveForInstallment(netAmount, plan.InstallmentType);
            invoiceDto.CompanyFeePercentage = feePct;
            invoiceDto.CompanyFeeAmount = feeAmt;

            var installmentAmount = Math.Floor(netAmount / plan.NumberOfInstallments);
            planDto = new InstallmentPlanSyncDto
            {
                SyncId = planSyncId,
                InvoiceSyncId = invoiceSyncId,
                CustomerSyncId = request.CustomerSyncId.Value,
                FileNumber = plan.FileNumber,
                TotalAmount = netAmount,
                NumberOfInstallments = plan.NumberOfInstallments,
                InstallmentAmount = installmentAmount,
                StartDate = plan.StartDate,
                InstallmentType = plan.InstallmentType,
                CompanyFeePercentage = feePct,
                CompanyFeeAmount = feeAmt,
                CreatedAt = now,
                CreatedBy = username
            };

            for (var i = 0; i < plan.NumberOfInstallments; i++)
            {
                var amount = i < plan.NumberOfInstallments - 1
                    ? installmentAmount
                    : netAmount - installmentAmount * (plan.NumberOfInstallments - 1);
                installmentDtos.Add(new InstallmentSyncDto
                {
                    SyncId = Guid.NewGuid(),
                    InstallmentPlanSyncId = planSyncId,
                    DueDate = plan.StartDate.AddMonths(i),
                    Amount = amount,
                    PaidAmount = 0,
                    RemainingAmount = amount,
                    Status = InstallmentStatus.Pending,
                    CreatedAt = now,
                    CreatedBy = username
                });
            }
        }

        var bundle = new SyncDataBundle();
        bundle.Invoices.Add(invoiceDto);
        bundle.InvoiceItems.AddRange(itemDtos);
        if (planDto is not null)
        {
            bundle.InstallmentPlans.Add(planDto);
            bundle.Installments.AddRange(installmentDtos);
        }

        var pushResponse = await _syncEngine.PushAsync(tenantId, new SyncPushRequest { Data = bundle }, ct);
        if (pushResponse.Conflicts.Count > 0)
        {
            return new MobileWriteResponse
            {
                SyncId = invoiceSyncId,
                Message = "تعذر حفظ الفاتورة",
                Conflicts = pushResponse.Conflicts
            };
        }

        await ApplyInvoiceBusinessLogicAsync(tenantId, invoiceSyncId, username, ct);

        var saved = await _db.Invoices.AsNoTracking()
            .FirstAsync(i => i.TenantId == tenantId && i.SyncId == invoiceSyncId, ct);

        return new MobileWriteResponse
        {
            SyncId = invoiceSyncId,
            InvoiceNumber = saved.InvoiceNumber,
            Message = "تم حفظ الفاتورة بنجاح",
            Conflicts = pushResponse.Conflicts
        };
    }

    private async Task ApplyInvoiceBusinessLogicAsync(int tenantId, Guid invoiceSyncId, string username, CancellationToken ct)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.SyncId == invoiceSyncId, ct)
            ?? throw new InvalidOperationException("Invoice not found after sync");

        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            invoice.InvoiceNumber = await CloudInvoiceNumberHelper.GenerateNextAsync(_db, tenantId, invoice.InvoiceType, ct);
            invoice.UpdatedBy = username;
            invoice.UpdatedAt = DateTime.UtcNow;
        }

        var affectsStock = invoice.InvoiceType is InvoiceType.Purchase or InvoiceType.Sale
            or InvoiceType.Installment or InvoiceType.PurchaseReturn;

        if (affectsStock)
        {
            foreach (var item in invoice.Items.Where(i => !i.IsDeleted && i.ProductId.HasValue))
            {
                var stock = await _db.WarehouseStocks
                    .FirstOrDefaultAsync(s =>
                        s.TenantId == tenantId &&
                        s.WarehouseId == invoice.WarehouseId &&
                        s.ProductId == item.ProductId!.Value, ct);

                if (invoice.InvoiceType == InvoiceType.Purchase)
                {
                    if (stock is not null)
                    {
                        stock.Quantity += item.Quantity;
                        stock.UpdatedBy = username;
                        stock.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        await _db.WarehouseStocks.AddAsync(new CloudWarehouseStock
                        {
                            TenantId = tenantId,
                            SyncId = Guid.NewGuid(),
                            WarehouseId = invoice.WarehouseId,
                            ProductId = item.ProductId!.Value,
                            Quantity = item.Quantity,
                            CreatedBy = username,
                            CreatedAt = DateTime.UtcNow
                        }, ct);
                    }
                }
                else
                {
                    if (stock is not null)
                    {
                        stock.Quantity -= item.Quantity;
                        stock.UpdatedBy = username;
                        stock.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
        }

        if (invoice.PaymentMethod == PaymentMethod.Cash && invoice.CashBoxId.HasValue)
        {
            var cashBox = await _db.CashBoxes.FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.Id == invoice.CashBoxId.Value, ct);
            if (cashBox is not null)
            {
                if (invoice.InvoiceType == InvoiceType.Purchase)
                    cashBox.Balance -= invoice.NetAmount;
                else if (invoice.InvoiceType == InvoiceType.PurchaseReturn)
                    cashBox.Balance += invoice.NetAmount;
                else
                    cashBox.Balance += invoice.NetAmount;

                cashBox.UpdatedBy = username;
                cashBox.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (invoice.InvoiceType == InvoiceType.Purchase)
            await ApplyPurchasePriceUpdatesAsync(tenantId, invoice, username, ct);

        await _db.SaveChangesAsync(ct);
    }

    private async Task ApplyPurchasePriceUpdatesAsync(int tenantId, CloudInvoice invoice, string username, CancellationToken ct)
    {
        var settings = await _db.BusinessSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && !s.IsDeleted, ct);
        if (settings is null || !settings.UpdateProductPriceOnPurchase)
            return;

        foreach (var item in invoice.Items.Where(i => !i.IsDeleted && i.ProductId.HasValue && i.PricingTypeId.HasValue))
        {
            var price = await _db.ProductPrices.FirstOrDefaultAsync(p =>
                p.TenantId == tenantId &&
                p.ProductId == item.ProductId!.Value &&
                p.PricingTypeId == item.PricingTypeId!.Value &&
                !p.IsDeleted, ct);

            if (price is null)
            {
                await _db.ProductPrices.AddAsync(new CloudProductPrice
                {
                    TenantId = tenantId,
                    SyncId = Guid.NewGuid(),
                    ProductId = item.ProductId!.Value,
                    PricingTypeId = item.PricingTypeId!.Value,
                    SalePrice = 0,
                    PurchasePrice = item.UnitPrice,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = username
                }, ct);
            }
            else
            {
                price.PurchasePrice = item.UnitPrice;
                price.UpdatedAt = DateTime.UtcNow;
                price.UpdatedBy = username;
            }
        }
    }

    private async Task EnsureDefaultPricingTypeAsync(int tenantId, string username, CancellationToken ct)
    {
        var hasDefault = await _db.PricingTypes.IgnoreQueryFilters()
            .AnyAsync(t => t.TenantId == tenantId && !t.IsDeleted && t.IsDefault, ct);
        if (hasDefault)
            return;

        var existing = await _db.PricingTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && !t.IsDeleted &&
                (t.SyncId == ProductPricingSyncIds.DefaultPricingType || t.Name == "سعر مفرد"), ct);
        if (existing is not null)
        {
            existing.IsDefault = true;
            existing.IsActive = true;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = username;
            await _db.SaveChangesAsync(ct);
            return;
        }

        _db.PricingTypes.Add(new CloudPricingType
        {
            TenantId = tenantId,
            SyncId = ProductPricingSyncIds.DefaultPricingType,
            Name = "سعر مفرد",
            IsDefault = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task<MobileWriteResponse> PushSingleAsync(
        int tenantId,
        Action<SyncDataBundle> configure,
        Guid syncId,
        CancellationToken ct)
    {
        var bundle = new SyncDataBundle();
        configure(bundle);
        var response = await _syncEngine.PushAsync(tenantId, new SyncPushRequest { Data = bundle }, ct);
        return new MobileWriteResponse
        {
            SyncId = syncId,
            Message = response.Conflicts.Count == 0 ? "تم الحفظ بنجاح" : "تعذر الحفظ",
            Conflicts = response.Conflicts
        };
    }

    private static void ValidateInvoiceRequest(CreateInvoiceRequest request)
    {
        if (request.Items.Count == 0)
            throw new ArgumentException("يجب إضافة بند واحد على الأقل");

        switch (request.InvoiceType)
        {
            case InvoiceType.Sale or InvoiceType.Installment:
                if (!request.CustomerSyncId.HasValue)
                    throw new ArgumentException("العميل مطلوب");
                break;
            case InvoiceType.Purchase or InvoiceType.PurchaseReturn:
                if (!request.SupplierSyncId.HasValue)
                    throw new ArgumentException("المورد مطلوب");
                break;
        }

        if (request.PaymentMethod == PaymentMethod.Cash && !request.CashBoxSyncId.HasValue)
            throw new ArgumentException("الصندوق مطلوب للفواتير النقدية");

        if ((request.PaymentMethod == PaymentMethod.Installment || request.InvoiceType == InvoiceType.Installment)
            && request.InstallmentPlan is null)
            throw new ArgumentException("خطة الأقساط مطلوبة");
    }

    private static decimal CalculateRounding(decimal netAmount, InvoiceType invoiceType)
    {
        const decimal roundingStep = 250m;
        var remainder = netAmount % roundingStep;
        if (remainder == 0) return 0m;
        return invoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn
            ? roundingStep - remainder
            : -remainder;
    }
}

internal static class CloudInvoiceNumberHelper
{
    public static async Task<string> GenerateNextAsync(
        CloudDbContext db, int tenantId, InvoiceType type, CancellationToken ct)
    {
        var prefix = type switch
        {
            InvoiceType.Purchase => "PUR",
            InvoiceType.Sale => "SAL",
            InvoiceType.Installment => "INS",
            InvoiceType.PurchaseReturn => "PRT",
            _ => "INV"
        };
        var year = DateTime.Now.Year;
        var numberPrefix = $"{prefix}-{year}-";

        var numbers = await db.Invoices.AsNoTracking()
            .Where(i => i.TenantId == tenantId && i.InvoiceType == type && i.InvoiceNumber.StartsWith(numberPrefix))
            .Select(i => i.InvoiceNumber)
            .ToListAsync(ct);

        var max = 0;
        foreach (var num in numbers)
        {
            if (num.Length > numberPrefix.Length &&
                int.TryParse(num[numberPrefix.Length..], out var seq) && seq > max)
                max = seq;
        }

        return $"{prefix}-{year}-{(max + 1):D5}";
    }
}
