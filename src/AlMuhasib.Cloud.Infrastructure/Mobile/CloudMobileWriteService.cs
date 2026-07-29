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

    public Task<MobileWriteResponse> UpsertCashBoxAsync(int tenantId, UpsertCashBoxRequest request, string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("اسم الصندوق مطلوب");

        var syncId = request.SyncId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto = new CashBoxSyncDto
        {
            SyncId = syncId,
            Name = request.Name.Trim(),
            Balance = request.OpeningBalance,
            CreatedAt = now,
            CreatedBy = username
        };
        return PushSingleAsync(tenantId, bundle => bundle.CashBoxes.Add(dto), syncId, ct);
    }

    public Task<MobileWriteResponse> UpsertBankAccountAsync(int tenantId, UpsertBankAccountRequest request, string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("اسم الحساب البنكي مطلوب");

        var syncId = request.SyncId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto = new BankAccountSyncDto
        {
            SyncId = syncId,
            Name = request.Name.Trim(),
            AccountNumber = request.AccountNumber,
            Balance = request.OpeningBalance,
            CreatedAt = now,
            CreatedBy = username
        };
        return PushSingleAsync(tenantId, bundle => bundle.BankAccounts.Add(dto), syncId, ct);
    }

    public Task<MobileWriteResponse> UpsertExpenseTypeAsync(int tenantId, UpsertExpenseTypeRequest request, string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("اسم نوع المصروف مطلوب");

        var syncId = request.SyncId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto = new ExpenseTypeSyncDto
        {
            SyncId = syncId,
            Name = request.Name.Trim(),
            CreatedAt = now,
            CreatedBy = username
        };
        return PushSingleAsync(tenantId, bundle => bundle.ExpenseTypes.Add(dto), syncId, ct);
    }

    public async Task<MobileWriteResponse> CreateVoucherAsync(int tenantId, CreateVoucherRequest request, string username, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("مبلغ السند يجب أن يكون أكبر من صفر");
        if (request.CashBoxSyncId == Guid.Empty)
            throw new ArgumentException("الصندوق مطلوب");

        switch (request.VoucherType)
        {
            case VoucherType.BankReceipt when !request.BankAccountSyncId.HasValue:
                throw new ArgumentException("يجب تحديد المصرف لسند القبض المصرفي");
            case VoucherType.InvestorDeposit or VoucherType.InvestorWithdrawal when !request.InvestorSyncId.HasValue:
                throw new ArgumentException("يجب تحديد المستثمر");
            case VoucherType.Receipt or VoucherType.DebtReceipt or VoucherType.Payment when !request.CustomerSyncId.HasValue:
                throw new ArgumentException("يجب تحديد العميل");
        }

        var syncId = request.SyncId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto = new VoucherSyncDto
        {
            SyncId = syncId,
            VoucherNumber = string.Empty,
            VoucherType = request.VoucherType,
            Amount = request.Amount,
            BankFees = request.BankFees,
            CustomerSyncId = request.CustomerSyncId,
            InvestorSyncId = request.InvestorSyncId,
            CashBoxSyncId = request.CashBoxSyncId,
            BankAccountSyncId = request.BankAccountSyncId,
            Date = request.Date == default ? DateTime.UtcNow : request.Date,
            Notes = request.Notes,
            CreatedAt = now,
            CreatedBy = username
        };

        var pushResponse = await _syncEngine.PushAsync(tenantId, new SyncPushRequest
        {
            Data = new SyncDataBundle { Vouchers = [dto] }
        }, ct);

        if (pushResponse.Conflicts.Count > 0)
        {
            return new MobileWriteResponse
            {
                SyncId = syncId,
                Message = "تعذر حفظ السند",
                Conflicts = pushResponse.Conflicts
            };
        }

        await ApplyVoucherBusinessLogicAsync(tenantId, syncId, username, ct);

        var saved = await _db.Vouchers.AsNoTracking()
            .FirstAsync(v => v.TenantId == tenantId && v.SyncId == syncId, ct);

        return new MobileWriteResponse
        {
            SyncId = syncId,
            InvoiceNumber = saved.VoucherNumber,
            Message = "تم حفظ السند بنجاح",
            Conflicts = pushResponse.Conflicts
        };
    }

    public async Task<MobileWriteResponse> CreateExpenseAsync(int tenantId, CreateExpenseRequest request, string username, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("مبلغ المصروف يجب أن يكون أكبر من صفر");
        if (request.ExpenseTypeSyncId == Guid.Empty)
            throw new ArgumentException("نوع المصروف مطلوب");
        if (request.CashBoxSyncId == Guid.Empty)
            throw new ArgumentException("الصندوق مطلوب");

        var syncId = request.SyncId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto = new ExpenseSyncDto
        {
            SyncId = syncId,
            ExpenseTypeSyncId = request.ExpenseTypeSyncId,
            Amount = request.Amount,
            Date = request.Date == default ? DateTime.UtcNow : request.Date,
            CashBoxSyncId = request.CashBoxSyncId,
            Notes = request.Notes,
            CreatedAt = now,
            CreatedBy = username
        };

        var pushResponse = await _syncEngine.PushAsync(tenantId, new SyncPushRequest
        {
            Data = new SyncDataBundle { Expenses = [dto] }
        }, ct);

        if (pushResponse.Conflicts.Count > 0)
        {
            return new MobileWriteResponse
            {
                SyncId = syncId,
                Message = "تعذر حفظ المصروف",
                Conflicts = pushResponse.Conflicts
            };
        }

        var expense = await _db.Expenses
            .FirstAsync(e => e.TenantId == tenantId && e.SyncId == syncId, ct);
        var cashBox = await _db.CashBoxes
            .FirstAsync(c => c.TenantId == tenantId && c.Id == expense.CashBoxId, ct);
        if (cashBox.Balance < request.Amount)
            throw new ArgumentException($"رصيد الصندوق ({cashBox.Balance:N0}) غير كافٍ");

        cashBox.Balance -= request.Amount;
        cashBox.UpdatedAt = DateTime.UtcNow;
        cashBox.UpdatedBy = username;
        await _db.SaveChangesAsync(ct);

        return new MobileWriteResponse
        {
            SyncId = syncId,
            Message = "تم حفظ المصروف بنجاح",
            Conflicts = pushResponse.Conflicts
        };
    }

    public async Task<MobileWriteResponse> CreateTransferAsync(int tenantId, CreateTransferRequest request, string username, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("مبلغ التحويل يجب أن يكون أكبر من صفر");
        if (request.FromSyncId == Guid.Empty || request.ToSyncId == Guid.Empty)
            throw new ArgumentException("حساب المصدر والهدف مطلوبان");
        if (request.FromType == request.ToType && request.FromSyncId == request.ToSyncId)
            throw new ArgumentException("لا يمكن التحويل لنفس الحساب");

        var syncId = request.SyncId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto = new TransferSyncDto
        {
            SyncId = syncId,
            FromType = request.FromType,
            FromSyncId = request.FromSyncId,
            ToType = request.ToType,
            ToSyncId = request.ToSyncId,
            Amount = request.Amount,
            Date = request.Date == default ? DateTime.UtcNow : request.Date,
            Notes = request.Notes,
            CreatedAt = now,
            CreatedBy = username
        };

        var pushResponse = await _syncEngine.PushAsync(tenantId, new SyncPushRequest
        {
            Data = new SyncDataBundle { Transfers = [dto] }
        }, ct);

        if (pushResponse.Conflicts.Count > 0)
        {
            return new MobileWriteResponse
            {
                SyncId = syncId,
                Message = "تعذر حفظ التحويل",
                Conflicts = pushResponse.Conflicts
            };
        }

        await ApplyTransferBalancesAsync(tenantId, request, username, ct);

        return new MobileWriteResponse
        {
            SyncId = syncId,
            Message = "تم حفظ التحويل بنجاح",
            Conflicts = pushResponse.Conflicts
        };
    }

    public Task<MobileWriteResponse> UpsertWarehouseAsync(int tenantId, UpsertWarehouseRequest request, string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("اسم المستودع مطلوب");

        var syncId = request.SyncId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto = new WarehouseSyncDto
        {
            SyncId = syncId,
            Name = request.Name.Trim(),
            Location = request.Location,
            CreatedAt = now,
            CreatedBy = username
        };
        return PushSingleAsync(tenantId, bundle => bundle.Warehouses.Add(dto), syncId, ct);
    }

    public async Task<MobileWriteResponse> CreateWarehouseTransferAsync(
        int tenantId, CreateWarehouseTransferRequest request, string username, CancellationToken ct = default)
    {
        if (request.FromWarehouseSyncId == Guid.Empty || request.ToWarehouseSyncId == Guid.Empty)
            throw new ArgumentException("المستودع المصدر والهدف مطلوبان");
        if (request.FromWarehouseSyncId == request.ToWarehouseSyncId)
            throw new ArgumentException("لا يمكن النقل لنفس المستودع");
        if (request.Items.Count == 0)
            throw new ArgumentException("يجب إضافة بند واحد على الأقل");
        if (request.Items.Any(i => i.Quantity <= 0 || i.ProductSyncId == Guid.Empty))
            throw new ArgumentException("بنود النقل غير صالحة");

        var syncId = request.SyncId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        var transferDto = new WarehouseTransferSyncDto
        {
            SyncId = syncId,
            TransferNumber = string.Empty,
            FromWarehouseSyncId = request.FromWarehouseSyncId,
            ToWarehouseSyncId = request.ToWarehouseSyncId,
            Date = request.Date == default ? DateTime.UtcNow : request.Date,
            Notes = request.Notes,
            CreatedAt = now,
            CreatedBy = username
        };

        var itemDtos = request.Items.Select(i => new WarehouseTransferItemSyncDto
        {
            SyncId = Guid.NewGuid(),
            WarehouseTransferSyncId = syncId,
            ProductSyncId = i.ProductSyncId,
            Quantity = i.Quantity,
            CreatedAt = now,
            CreatedBy = username
        }).ToList();

        var pushResponse = await _syncEngine.PushAsync(tenantId, new SyncPushRequest
        {
            Data = new SyncDataBundle
            {
                WarehouseTransfers = [transferDto],
                WarehouseTransferItems = itemDtos
            }
        }, ct);

        if (pushResponse.Conflicts.Count > 0)
        {
            return new MobileWriteResponse
            {
                SyncId = syncId,
                Message = "تعذر حفظ نقل المخزون",
                Conflicts = pushResponse.Conflicts
            };
        }

        await ApplyWarehouseTransferStockAsync(tenantId, syncId, username, ct);

        var saved = await _db.WarehouseTransfers.AsNoTracking()
            .FirstAsync(t => t.TenantId == tenantId && t.SyncId == syncId, ct);

        return new MobileWriteResponse
        {
            SyncId = syncId,
            InvoiceNumber = saved.TransferNumber,
            Message = "تم حفظ نقل المخزون بنجاح",
            Conflicts = pushResponse.Conflicts
        };
    }

    public async Task<MobileWriteResponse> AdjustStockAsync(
        int tenantId, CreateStockAdjustmentRequest request, string username, CancellationToken ct = default)
    {
        if (request.WarehouseSyncId == Guid.Empty)
            throw new ArgumentException("المستودع مطلوب");
        if (request.Items.Count == 0)
            throw new ArgumentException("يجب تحديد أصناف للتسوية");

        var warehouse = await _db.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.SyncId == request.WarehouseSyncId && !w.IsDeleted, ct)
            ?? throw new ArgumentException("المستودع غير موجود");

        var now = DateTime.UtcNow;
        var stockDtos = new List<WarehouseStockSyncDto>();

        foreach (var item in request.Items)
        {
            if (item.ProductSyncId == Guid.Empty || item.NewQuantity < 0)
                throw new ArgumentException("بيانات تسوية المخزون غير صالحة");

            var product = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.SyncId == item.ProductSyncId && !p.IsDeleted, ct)
                ?? throw new ArgumentException("المنتج غير موجود");

            var existing = await _db.WarehouseStocks.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s =>
                    s.TenantId == tenantId &&
                    s.WarehouseId == warehouse.Id &&
                    s.ProductId == product.Id &&
                    !s.IsDeleted, ct);

            stockDtos.Add(new WarehouseStockSyncDto
            {
                SyncId = existing?.SyncId ?? Guid.NewGuid(),
                WarehouseSyncId = request.WarehouseSyncId,
                ProductSyncId = item.ProductSyncId,
                Quantity = item.NewQuantity,
                OpeningQuantity = existing?.OpeningQuantity ?? 0,
                UnitCost = existing?.UnitCost ?? 0,
                MinQuantity = existing?.MinQuantity ?? 0,
                CreatedAt = existing?.CreatedAt ?? now,
                CreatedBy = existing?.CreatedBy ?? username,
                UpdatedAt = now,
                UpdatedBy = username,
                RowVersion = existing?.RowVersion
            });
        }

        var pushResponse = await _syncEngine.PushAsync(tenantId, new SyncPushRequest
        {
            Data = new SyncDataBundle { WarehouseStocks = stockDtos }
        }, ct);

        return new MobileWriteResponse
        {
            SyncId = stockDtos.FirstOrDefault()?.SyncId ?? Guid.Empty,
            Message = pushResponse.Conflicts.Count == 0 ? "تم تسوية المخزون بنجاح" : "تعذر تسوية المخزون",
            Conflicts = pushResponse.Conflicts
        };
    }

    public async Task<MobileWriteResponse> PayInstallmentAsync(
        int tenantId, Guid installmentSyncId, PayInstallmentRequest request, string username, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("مبلغ الدفعة يجب أن يكون أكبر من صفر");
        if (request.CashBoxSyncId == Guid.Empty)
            throw new ArgumentException("الصندوق مطلوب");

        var installment = await _db.Installments.AsNoTracking()
            .Include(i => i.InstallmentPlan)
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.SyncId == installmentSyncId && !i.IsDeleted, ct)
            ?? throw new ArgumentException("القسط غير موجود");

        if (installment.Status == InstallmentStatus.Paid)
            throw new ArgumentException("القسط مدفوع بالكامل");

        var cashBox = await _db.CashBoxes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.SyncId == request.CashBoxSyncId && !c.IsDeleted, ct)
            ?? throw new ArgumentException("الصندوق غير موجود");

        var payAmount = Math.Min(request.Amount, installment.RemainingAmount);
        var now = DateTime.UtcNow;
        var paymentDate = request.PaymentDate ?? DateTime.UtcNow;
        var newPaid = installment.PaidAmount + payAmount;
        var newRemaining = Math.Max(0, installment.Amount - newPaid);
        var newStatus = newRemaining <= 0 ? InstallmentStatus.Paid : InstallmentStatus.PartiallyPaid;

        var installmentDto = new InstallmentSyncDto
        {
            SyncId = installment.SyncId,
            InstallmentPlanSyncId = installment.InstallmentPlan.SyncId,
            DueDate = installment.DueDate,
            Amount = installment.Amount,
            PaidAmount = newPaid,
            RemainingAmount = newRemaining,
            Status = newStatus,
            PaymentDate = paymentDate,
            CashBoxSyncId = cashBox.SyncId,
            CreatedAt = installment.CreatedAt,
            CreatedBy = installment.CreatedBy,
            UpdatedAt = now,
            UpdatedBy = username,
            RowVersion = installment.RowVersion
        };

        var pushResponse = await _syncEngine.PushAsync(tenantId, new SyncPushRequest
        {
            Data = new SyncDataBundle { Installments = [installmentDto] }
        }, ct);

        if (pushResponse.Conflicts.Count > 0)
        {
            return new MobileWriteResponse
            {
                SyncId = installmentSyncId,
                Message = "تعذر تسجيل الدفعة",
                Conflicts = pushResponse.Conflicts
            };
        }

        var cashBoxEntity = await _db.CashBoxes
            .FirstAsync(c => c.TenantId == tenantId && c.SyncId == request.CashBoxSyncId, ct);
        cashBoxEntity.Balance += payAmount;
        cashBoxEntity.UpdatedAt = now;
        cashBoxEntity.UpdatedBy = username;
        await _db.SaveChangesAsync(ct);

        return new MobileWriteResponse
        {
            SyncId = installmentSyncId,
            Message = "تم تسجيل دفعة القسط بنجاح",
            Conflicts = pushResponse.Conflicts
        };
    }

    private async Task ApplyVoucherBusinessLogicAsync(int tenantId, Guid voucherSyncId, string username, CancellationToken ct)
    {
        var voucher = await _db.Vouchers
            .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.SyncId == voucherSyncId, ct)
            ?? throw new InvalidOperationException("Voucher not found after sync");

        if (string.IsNullOrWhiteSpace(voucher.VoucherNumber))
        {
            voucher.VoucherNumber = await GenerateVoucherNumberAsync(tenantId, voucher.VoucherType, ct);
            voucher.UpdatedAt = DateTime.UtcNow;
            voucher.UpdatedBy = username;
        }

        var cashBox = await _db.CashBoxes.FirstAsync(c => c.TenantId == tenantId && c.Id == voucher.CashBoxId, ct);

        switch (voucher.VoucherType)
        {
            case VoucherType.Receipt:
            case VoucherType.DebtReceipt:
                cashBox.Balance += voucher.Amount;
                break;
            case VoucherType.Payment:
                if (cashBox.Balance < voucher.Amount)
                    throw new ArgumentException($"رصيد الصندوق ({cashBox.Balance:N0}) غير كافٍ");
                cashBox.Balance -= voucher.Amount;
                break;
            case VoucherType.BankReceipt:
            {
                var bank = await _db.BankAccounts.FirstAsync(
                    b => b.TenantId == tenantId && b.Id == voucher.BankAccountId!.Value, ct);
                var net = voucher.Amount - voucher.BankFees;
                if (bank.Balance < voucher.Amount)
                    throw new ArgumentException($"رصيد المصرف ({bank.Balance:N0}) غير كافٍ");
                bank.Balance -= voucher.Amount;
                bank.UpdatedAt = DateTime.UtcNow;
                bank.UpdatedBy = username;
                cashBox.Balance += net;
                break;
            }
            case VoucherType.InvestorDeposit:
            {
                var investor = await _db.Investors.FirstAsync(
                    i => i.TenantId == tenantId && i.Id == voucher.InvestorId!.Value, ct);
                cashBox.Balance += voucher.Amount;
                investor.TotalDeposit += voucher.Amount;
                investor.UpdatedAt = DateTime.UtcNow;
                investor.UpdatedBy = username;
                break;
            }
            case VoucherType.InvestorWithdrawal:
            {
                var investor = await _db.Investors.FirstAsync(
                    i => i.TenantId == tenantId && i.Id == voucher.InvestorId!.Value, ct);
                if (cashBox.Balance < voucher.Amount)
                    throw new ArgumentException($"رصيد الصندوق ({cashBox.Balance:N0}) غير كافٍ");
                cashBox.Balance -= voucher.Amount;
                investor.TotalDeposit -= voucher.Amount;
                investor.UpdatedAt = DateTime.UtcNow;
                investor.UpdatedBy = username;
                break;
            }
        }

        cashBox.UpdatedAt = DateTime.UtcNow;
        cashBox.UpdatedBy = username;
        await _db.SaveChangesAsync(ct);
    }

    private async Task ApplyTransferBalancesAsync(
        int tenantId, CreateTransferRequest request, string username, CancellationToken ct)
    {
        async Task AdjustAsync(TransferAccountType type, Guid syncId, decimal delta)
        {
            if (type == TransferAccountType.CashBox)
            {
                var box = await _db.CashBoxes.FirstAsync(
                    c => c.TenantId == tenantId && c.SyncId == syncId && !c.IsDeleted, ct);
                if (delta < 0 && box.Balance < -delta)
                    throw new ArgumentException($"رصيد الصندوق ({box.Balance:N0}) غير كافٍ");
                box.Balance += delta;
                box.UpdatedAt = DateTime.UtcNow;
                box.UpdatedBy = username;
            }
            else
            {
                var bank = await _db.BankAccounts.FirstAsync(
                    b => b.TenantId == tenantId && b.SyncId == syncId && !b.IsDeleted, ct);
                if (delta < 0 && bank.Balance < -delta)
                    throw new ArgumentException($"رصيد المصرف ({bank.Balance:N0}) غير كافٍ");
                bank.Balance += delta;
                bank.UpdatedAt = DateTime.UtcNow;
                bank.UpdatedBy = username;
            }
        }

        await AdjustAsync(request.FromType, request.FromSyncId, -request.Amount);
        await AdjustAsync(request.ToType, request.ToSyncId, request.Amount);
        await _db.SaveChangesAsync(ct);
    }

    private async Task ApplyWarehouseTransferStockAsync(int tenantId, Guid transferSyncId, string username, CancellationToken ct)
    {
        var transfer = await _db.WarehouseTransfers
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.SyncId == transferSyncId, ct)
            ?? throw new InvalidOperationException("Warehouse transfer not found");

        if (string.IsNullOrWhiteSpace(transfer.TransferNumber))
        {
            transfer.TransferNumber = await GenerateWarehouseTransferNumberAsync(tenantId, ct);
            transfer.UpdatedAt = DateTime.UtcNow;
            transfer.UpdatedBy = username;
        }

        foreach (var item in transfer.Items.Where(i => !i.IsDeleted))
        {
            var fromStock = await _db.WarehouseStocks.FirstOrDefaultAsync(s =>
                s.TenantId == tenantId &&
                s.WarehouseId == transfer.FromWarehouseId &&
                s.ProductId == item.ProductId &&
                !s.IsDeleted, ct);

            if (fromStock is null || fromStock.Quantity < item.Quantity)
                throw new ArgumentException("الكمية غير كافية في المستودع المصدر");

            fromStock.Quantity -= item.Quantity;
            fromStock.UpdatedAt = DateTime.UtcNow;
            fromStock.UpdatedBy = username;

            var toStock = await _db.WarehouseStocks.FirstOrDefaultAsync(s =>
                s.TenantId == tenantId &&
                s.WarehouseId == transfer.ToWarehouseId &&
                s.ProductId == item.ProductId &&
                !s.IsDeleted, ct);

            if (toStock is null)
            {
                await _db.WarehouseStocks.AddAsync(new CloudWarehouseStock
                {
                    TenantId = tenantId,
                    SyncId = Guid.NewGuid(),
                    WarehouseId = transfer.ToWarehouseId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = username
                }, ct);
            }
            else
            {
                toStock.Quantity += item.Quantity;
                toStock.UpdatedAt = DateTime.UtcNow;
                toStock.UpdatedBy = username;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<string> GenerateVoucherNumberAsync(int tenantId, VoucherType type, CancellationToken ct)
    {
        var prefix = type switch
        {
            VoucherType.Receipt => "RCV",
            VoucherType.Payment => "PAY",
            VoucherType.BankReceipt => "BRV",
            VoucherType.InvestorDeposit => "IDP",
            VoucherType.InvestorWithdrawal => "IWD",
            VoucherType.DebtReceipt => "DRC",
            _ => "VCH"
        };

        var numbers = await _db.Vouchers.AsNoTracking()
            .Where(v => v.TenantId == tenantId && v.VoucherType == type && v.VoucherNumber.StartsWith(prefix + "-"))
            .Select(v => v.VoucherNumber)
            .ToListAsync(ct);

        var max = 0;
        foreach (var num in numbers)
        {
            var parts = num.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var seq) && seq > max)
                max = seq;
        }

        return $"{prefix}-{(max + 1):D4}";
    }

    private async Task<string> GenerateWarehouseTransferNumberAsync(int tenantId, CancellationToken ct)
    {
        const string prefix = "WTR";
        var year = DateTime.Now.Year;
        var numberPrefix = $"{prefix}-{year}-";
        var numbers = await _db.WarehouseTransfers.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.TransferNumber.StartsWith(numberPrefix))
            .Select(t => t.TransferNumber)
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
