using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Services.Gold;

/// <summary>Cloud-side write helpers for vouchers, collection, purchase, exchange, and sale returns.</summary>
public sealed class CloudGoldOpsHelper
{
    private readonly CloudDbContext _db;

    public CloudGoldOpsHelper(CloudDbContext db) => _db = db;

    public async Task<CloudGoldVoucher> CreateVoucherAsync(
        int tenantId,
        CloudGoldCreateVoucherRequest request,
        string createdBy,
        CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("مبلغ السند يجب أن يكون أكبر من صفر");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var cashBox = await ResolveCashBoxAsync(tenantId, request.CashBoxId, request.Currency, createdBy, ct);
            var voucher = new CloudGoldVoucher
            {
                TenantId = tenantId,
                SyncId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                VoucherNumber = string.IsNullOrWhiteSpace(request.VoucherNumber)
                    ? await NextVoucherNumberAsync(tenantId, request.VoucherType, ct)
                    : request.VoucherNumber.Trim(),
                VoucherDate = (request.VoucherDate == default ? DateTime.Today : request.VoucherDate).Date,
                VoucherType = request.VoucherType,
                Currency = cashBox.Currency,
                Amount = Round(request.Amount),
                CashBoxId = cashBox.Id,
                CustomerId = request.CustomerId,
                SupplierId = request.SupplierId,
                IsOpeningBalance = request.IsOpeningBalance,
                AffectsCashBox = request.AffectsCashBox,
                Notes = request.Notes ?? string.Empty
            };

            if (voucher.AffectsCashBox)
            {
                var delta = voucher.VoucherType == GoldVoucherType.Receipt ? voucher.Amount : -voucher.Amount;
                cashBox.Balance = Round(cashBox.Balance + delta);
                cashBox.UpdatedAt = DateTime.UtcNow;
                cashBox.UpdatedBy = createdBy;
            }

            if (!voucher.IsOpeningBalance && voucher.CustomerId.HasValue)
            {
                var customer = await _db.GoldCustomers
                    .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == voucher.CustomerId.Value, ct)
                    ?? throw new InvalidOperationException("الزبون غير موجود");
                var creditDelta = voucher.VoucherType == GoldVoucherType.Receipt ? -voucher.Amount : voucher.Amount;
                AdjustCustomerCredit(customer, voucher.Currency, creditDelta);
                customer.UpdatedAt = DateTime.UtcNow;
                customer.UpdatedBy = createdBy;
            }

            if (!voucher.IsOpeningBalance && voucher.SupplierId.HasValue)
            {
                var supplier = await _db.GoldSuppliers
                    .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == voucher.SupplierId.Value, ct)
                    ?? throw new InvalidOperationException("المورد غير موجود");
                var creditDelta = voucher.VoucherType == GoldVoucherType.Payment ? -voucher.Amount : voucher.Amount;
                AdjustSupplierCredit(supplier, voucher.Currency, creditDelta);
                supplier.UpdatedAt = DateTime.UtcNow;
                supplier.UpdatedBy = createdBy;
            }

            _db.GoldVouchers.Add(voucher);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return voucher;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<CloudGoldPayment> CollectAsync(
        int tenantId,
        CloudGoldCollectionRequest request,
        string createdBy,
        CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("مبلغ التحصيل يجب أن يكون أكبر من صفر");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var invoice = await _db.GoldInvoices
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Id == request.InvoiceId, ct)
                ?? throw new InvalidOperationException("الفاتورة غير موجودة");

            if (invoice.RemainingAmount <= 0)
                throw new InvalidOperationException("لا يوجد متبقي على هذه الفاتورة");

            var amount = Round(Math.Min(request.Amount, invoice.RemainingAmount));
            var cashBox = await ResolveCashBoxAsync(tenantId, request.CashBoxId, request.Currency, createdBy, ct);

            cashBox.Balance = Round(cashBox.Balance + amount);
            cashBox.UpdatedAt = DateTime.UtcNow;
            cashBox.UpdatedBy = createdBy;

            var payment = new CloudGoldPayment
            {
                TenantId = tenantId,
                SyncId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                InvoiceId = invoice.Id,
                PaymentDate = (request.PaymentDate == default ? DateTime.Today : request.PaymentDate).Date,
                Amount = amount,
                Currency = cashBox.Currency,
                FxRate = invoice.FxRate,
                CashBoxId = cashBox.Id,
                Notes = request.Notes ?? "تحصيل"
            };

            var paidInPricing = ConvertAmount(amount, cashBox.Currency, invoice.PricingCurrency, invoice.FxRate);
            invoice.PaidAmount = Round(invoice.PaidAmount + paidInPricing);
            invoice.RemainingAmount = Round(Math.Max(0, invoice.TotalAmount - invoice.PaidAmount));
            invoice.Status = invoice.RemainingAmount <= 0.0001m
                ? GoldInvoiceStatus.Completed
                : GoldInvoiceStatus.PartiallyPaid;
            invoice.UpdatedAt = DateTime.UtcNow;
            invoice.UpdatedBy = createdBy;

            if (invoice.Customer is not null)
            {
                AdjustCustomerCredit(invoice.Customer, cashBox.Currency, -amount);
                invoice.Customer.UpdatedAt = DateTime.UtcNow;
                invoice.Customer.UpdatedBy = createdBy;
            }

            _db.GoldPayments.Add(payment);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return payment;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<CloudGoldInvoice> CreatePurchaseAsync(
        int tenantId, CloudGoldCreateSaleRequest request, string createdBy, CancellationToken ct = default)
    {
        return await CreateDirectionalInvoiceAsync(
            tenantId, request, createdBy, GoldInvoiceType.Purchase, GoldInvoiceLineDirection.In, stockSign: +1, null, ct);
    }

    public async Task<CloudGoldInvoice> CreateSaleReturnAsync(
        int tenantId, CloudGoldCreateSaleRequest request, int? relatedInvoiceId, string createdBy, CancellationToken ct = default)
    {
        return await CreateDirectionalInvoiceAsync(
            tenantId, request, createdBy, GoldInvoiceType.SaleReturn, GoldInvoiceLineDirection.In, stockSign: +1, relatedInvoiceId, ct);
    }

    public async Task<CloudGoldInvoice> CreateExchangeAsync(
        int tenantId,
        CloudGoldCreateExchangeRequest request,
        string createdBy,
        CancellationToken ct = default)
    {
        if ((request.InLines?.Count ?? 0) == 0 && (request.OutLines?.Count ?? 0) == 0)
            throw new InvalidOperationException("أضف بنداً وارداً أو صادراً على الأقل");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var settings = await _db.GoldSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);
            var mithqalGrams = settings?.MithqalGrams > 0 ? settings.MithqalGrams : 5m;
            var fx = request.FxRate > 0 ? request.FxRate : await LatestFxAsync(tenantId, ct);
            var warehouseId = await ResolveWarehouseIdAsync(tenantId, request.WarehouseId, createdBy, ct);

            CloudGoldCustomer? customer = null;
            if (request.CustomerId.HasValue)
            {
                customer = await _db.GoldCustomers
                    .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == request.CustomerId.Value, ct)
                    ?? throw new InvalidOperationException("الزبون غير موجود");
            }

            var invoice = new CloudGoldInvoice
            {
                TenantId = tenantId,
                SyncId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                InvoiceNumber = await NextInvoiceNumberAsync(tenantId, GoldInvoiceType.Exchange, "GX", ct),
                InvoiceDate = (request.InvoiceDate == default ? DateTime.Today : request.InvoiceDate).Date,
                InvoiceType = GoldInvoiceType.Exchange,
                IsExchange = true,
                PaymentMethod = request.PaymentMethod,
                CustomerId = request.CustomerId,
                WarehouseId = warehouseId,
                PricingCurrency = request.PricingCurrency,
                PaymentCurrency = request.PaymentCurrency,
                FxRate = fx,
                Notes = request.Notes ?? string.Empty,
                WeightFromScale = request.WeightFromScale
            };

            decimal inTotal = 0, outTotal = 0, totalWeight = 0;

            foreach (var line in request.InLines ?? [])
            {
                var built = BuildLine(tenantId, createdBy, line, mithqalGrams, GoldInvoiceLineDirection.In);
                await AdjustStockAsync(tenantId, warehouseId, line.KaratValue, line.WeightGrams, createdBy, ct);
                invoice.Lines.Add(built);
                inTotal += built.LineTotal;
                totalWeight += built.WeightGrams;
            }

            foreach (var line in request.OutLines ?? [])
            {
                var built = BuildLine(tenantId, createdBy, line, mithqalGrams, GoldInvoiceLineDirection.Out);
                await AdjustStockAsync(tenantId, warehouseId, line.KaratValue, -line.WeightGrams, createdBy, ct);
                invoice.Lines.Add(built);
                outTotal += built.LineTotal;
                totalWeight += built.WeightGrams;
            }

            invoice.TotalGoldValue = Round(outTotal + inTotal);
            invoice.TotalMakingCharge = 0;
            invoice.TotalWeightGrams = Round(totalWeight);
            invoice.ExchangeCashDifference = request.ExchangeCashDifference != 0
                ? Round(request.ExchangeCashDifference)
                : Round(outTotal - inTotal);
            invoice.TotalAmount = Round(Math.Abs(invoice.ExchangeCashDifference));
            ApplyDualTotals(invoice);

            var cashDiffInPayment = ConvertAmount(
                Math.Abs(invoice.ExchangeCashDifference),
                request.PricingCurrency,
                request.PaymentCurrency,
                fx);

            if (request.PaymentMethod == GoldPaymentMethod.Cash && cashDiffInPayment > 0)
            {
                var cashBox = await ResolveCashBoxAsync(tenantId, request.CashBoxId, request.PaymentCurrency, createdBy, ct);
                invoice.CashBoxId = cashBox.Id;
                // Positive difference: customer pays shop (cash in); negative: shop pays customer (cash out)
                var signed = invoice.ExchangeCashDifference >= 0 ? cashDiffInPayment : -cashDiffInPayment;
                cashBox.Balance = Round(cashBox.Balance + signed);
                cashBox.UpdatedAt = DateTime.UtcNow;
                cashBox.UpdatedBy = createdBy;
                invoice.PaidAmount = Round(cashDiffInPayment);
                invoice.RemainingAmount = 0;
                invoice.Status = GoldInvoiceStatus.Completed;
                invoice.Payments.Add(new CloudGoldPayment
                {
                    TenantId = tenantId,
                    SyncId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy,
                    PaymentDate = invoice.InvoiceDate,
                    Amount = Round(cashDiffInPayment),
                    Currency = request.PaymentCurrency,
                    FxRate = fx,
                    CashBoxId = cashBox.Id,
                    Notes = "فرق مبادلة"
                });
            }
            else
            {
                invoice.PaidAmount = 0;
                invoice.RemainingAmount = Round(cashDiffInPayment);
                invoice.Status = cashDiffInPayment > 0 ? GoldInvoiceStatus.Open : GoldInvoiceStatus.Completed;
                if (invoice.RemainingAmount > 0)
                {
                    if (customer is null)
                        throw new InvalidOperationException("فرق المبادلة الآجل يتطلب زبون");
                    AdjustCustomerCredit(customer, request.PaymentCurrency, invoice.ExchangeCashDifference >= 0
                        ? invoice.RemainingAmount
                        : -invoice.RemainingAmount);
                }
            }

            _db.GoldInvoices.Add(invoice);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return await LoadInvoiceAsync(invoice.Id, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<CloudGoldExpense> CreateExpenseAsync(
        int tenantId, CloudGoldCreateExpenseRequest request, string createdBy, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("مبلغ المصروف يجب أن يكون أكبر من صفر");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var expenseType = await _db.GoldExpenseTypes
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == request.ExpenseTypeId, ct)
                ?? throw new InvalidOperationException("نوع المصروف غير موجود");

            var cashBox = await ResolveCashBoxAsync(tenantId, request.CashBoxId, request.Currency, createdBy, ct);
            cashBox.Balance = Round(cashBox.Balance - request.Amount);
            cashBox.UpdatedAt = DateTime.UtcNow;
            cashBox.UpdatedBy = createdBy;

            var expense = new CloudGoldExpense
            {
                TenantId = tenantId,
                SyncId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                ExpenseDate = (request.ExpenseDate == default ? DateTime.Today : request.ExpenseDate).Date,
                ExpenseTypeId = expenseType.Id,
                Amount = Round(request.Amount),
                Currency = cashBox.Currency,
                CashBoxId = cashBox.Id,
                Notes = request.Notes ?? string.Empty,
                WarehouseId = request.WarehouseId
            };
            _db.GoldExpenses.Add(expense);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return expense;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<CloudGoldInvoice> CreateDirectionalInvoiceAsync(
        int tenantId,
        CloudGoldCreateSaleRequest request,
        string createdBy,
        GoldInvoiceType type,
        GoldInvoiceLineDirection direction,
        int stockSign,
        int? relatedInvoiceId,
        CancellationToken ct)
    {
        if (request.Lines is null || request.Lines.Count == 0)
            throw new InvalidOperationException("At least one invoice line is required.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var settings = await _db.GoldSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);
            var mithqalGrams = settings?.MithqalGrams > 0 ? settings.MithqalGrams : 5m;
            var fx = request.FxRate > 0 ? request.FxRate : await LatestFxAsync(tenantId, ct);
            var warehouseId = await ResolveWarehouseIdAsync(tenantId, request.WarehouseId, createdBy, ct);

            CloudGoldCustomer? customer = null;
            if (request.CustomerId.HasValue)
            {
                customer = await _db.GoldCustomers
                    .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == request.CustomerId.Value, ct)
                    ?? throw new InvalidOperationException("Customer not found.");
            }

            CloudGoldSupplier? supplier = null;
            if (request.SupplierId.HasValue)
            {
                supplier = await _db.GoldSuppliers
                    .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == request.SupplierId.Value, ct)
                    ?? throw new InvalidOperationException("Supplier not found.");
            }

            if (type == GoldInvoiceType.Purchase && supplier is null)
                throw new InvalidOperationException("Purchase requires a supplier.");

            var prefix = type switch
            {
                GoldInvoiceType.Purchase => "GP",
                GoldInvoiceType.SaleReturn => "GR",
                _ => "G"
            };

            var invoice = new CloudGoldInvoice
            {
                TenantId = tenantId,
                SyncId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                InvoiceNumber = await NextInvoiceNumberAsync(tenantId, type, prefix, ct),
                InvoiceDate = (request.InvoiceDate == default ? DateTime.Today : request.InvoiceDate).Date,
                InvoiceType = type,
                PaymentMethod = request.PaymentMethod,
                CustomerId = request.CustomerId,
                SupplierId = request.SupplierId,
                WarehouseId = warehouseId,
                PricingCurrency = request.PricingCurrency,
                PaymentCurrency = request.PaymentCurrency,
                FxRate = fx,
                DiscountAmount = Math.Max(0, request.DiscountAmount),
                Notes = request.Notes ?? string.Empty,
                WeightFromScale = request.WeightFromScale,
                CashBoxId = request.CashBoxId,
                RelatedInvoiceId = relatedInvoiceId
            };

            decimal totalGold = 0, totalMaking = 0, totalWeight = 0;
            foreach (var lineReq in request.Lines)
            {
                var built = BuildLine(tenantId, createdBy, lineReq, mithqalGrams, direction);
                await AdjustStockAsync(tenantId, warehouseId, lineReq.KaratValue, stockSign * lineReq.WeightGrams, createdBy, ct);
                invoice.Lines.Add(built);
                totalGold += built.GoldValue;
                totalMaking += built.MakingCharge;
                totalWeight += built.WeightGrams;
            }

            invoice.TotalGoldValue = Round(totalGold);
            invoice.TotalMakingCharge = Round(totalMaking);
            invoice.TotalWeightGrams = Round(totalWeight);
            invoice.TotalAmount = Round(invoice.TotalGoldValue + invoice.TotalMakingCharge - invoice.DiscountAmount);
            if (invoice.TotalAmount < 0) invoice.TotalAmount = 0;
            ApplyDualTotals(invoice);

            var paidInPricing = ConvertAmount(Math.Max(0, request.PaidAmount), request.PaymentCurrency, request.PricingCurrency, fx);
            if (request.PaymentMethod == GoldPaymentMethod.Cash && paidInPricing <= 0)
                paidInPricing = invoice.TotalAmount;
            if (paidInPricing > invoice.TotalAmount) paidInPricing = invoice.TotalAmount;

            invoice.PaidAmount = Round(paidInPricing);
            invoice.RemainingAmount = Round(invoice.TotalAmount - invoice.PaidAmount);
            invoice.Status = ResolveStatus(invoice.TotalAmount, invoice.PaidAmount, request.PaymentMethod);

            if (invoice.PaidAmount > 0)
            {
                var paidInPayment = ConvertAmount(invoice.PaidAmount, request.PricingCurrency, request.PaymentCurrency, fx);
                var cashBox = await ResolveCashBoxAsync(tenantId, request.CashBoxId, request.PaymentCurrency, createdBy, ct);
                invoice.CashBoxId = cashBox.Id;
                // Purchase/return cash out; sale would be in — here purchase & return decrease cash
                cashBox.Balance = Round(cashBox.Balance - paidInPayment);
                cashBox.UpdatedAt = DateTime.UtcNow;
                cashBox.UpdatedBy = createdBy;
                invoice.Payments.Add(new CloudGoldPayment
                {
                    TenantId = tenantId,
                    SyncId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy,
                    PaymentDate = invoice.InvoiceDate,
                    Amount = Round(paidInPayment),
                    Currency = request.PaymentCurrency,
                    FxRate = fx,
                    CashBoxId = cashBox.Id,
                    Notes = type == GoldInvoiceType.Purchase ? "دفع شراء" : "استرداد مرتجع"
                });
            }

            if (invoice.RemainingAmount > 0)
            {
                var credit = ConvertAmount(invoice.RemainingAmount, request.PricingCurrency, request.PaymentCurrency, fx);
                if (type == GoldInvoiceType.Purchase && supplier is not null)
                    AdjustSupplierCredit(supplier, request.PaymentCurrency, credit);
                else if (type == GoldInvoiceType.SaleReturn && customer is not null)
                    AdjustCustomerCredit(customer, request.PaymentCurrency, -credit);
            }

            _db.GoldInvoices.Add(invoice);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return await LoadInvoiceAsync(invoice.Id, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private CloudGoldInvoiceLine BuildLine(
        int tenantId, string createdBy, CloudGoldCreateSaleLineRequest lineReq, decimal mithqalGrams, GoldInvoiceLineDirection direction)
    {
        if (lineReq.WeightGrams <= 0) throw new InvalidOperationException("Line weight must be greater than zero.");
        if (lineReq.MithqalPrice <= 0) throw new InvalidOperationException("Mithqal price must be greater than zero.");
        var pricePerGram = Round(lineReq.MithqalPrice / mithqalGrams, 6);
        var goldValue = Round(lineReq.WeightGrams * pricePerGram);
        var making = Math.Max(0, lineReq.MakingCharge);
        return new CloudGoldInvoiceLine
        {
            TenantId = tenantId,
            SyncId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            ItemId = lineReq.ItemId,
            KaratValue = lineReq.KaratValue,
            WeightGrams = lineReq.WeightGrams,
            MithqalPrice = lineReq.MithqalPrice,
            PricePerGram = pricePerGram,
            GoldValue = goldValue,
            MakingCharge = making,
            LineTotal = Round(goldValue + making),
            Description = string.IsNullOrWhiteSpace(lineReq.Description) ? $"عيار {lineReq.KaratValue}" : lineReq.Description,
            WeightFromScale = lineReq.WeightFromScale,
            LineDirection = direction
        };
    }

    private async Task<CloudGoldInvoice> LoadInvoiceAsync(int id, CancellationToken ct) =>
        await _db.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstAsync(i => i.Id == id, ct);

    private async Task<string> NextVoucherNumberAsync(int tenantId, GoldVoucherType type, CancellationToken ct)
    {
        var prefix = type == GoldVoucherType.Receipt ? "GRV-" : "GPV-";
        var last = await _db.GoldVouchers
            .Where(v => v.TenantId == tenantId && v.VoucherType == type && v.VoucherNumber.StartsWith(prefix))
            .OrderByDescending(v => v.Id)
            .Select(v => v.VoucherNumber)
            .FirstOrDefaultAsync(ct);
        var next = 1;
        if (last is not null)
        {
            var parts = last.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var n)) next = n + 1;
        }
        return $"{prefix}{next:D4}";
    }

    private async Task<string> NextInvoiceNumberAsync(int tenantId, GoldInvoiceType type, string prefix, CancellationToken ct)
    {
        var fullPrefix = $"{prefix}-";
        var last = await _db.GoldInvoices
            .Where(i => i.TenantId == tenantId && i.InvoiceType == type && i.InvoiceNumber.StartsWith(fullPrefix))
            .OrderByDescending(i => i.Id)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(ct);
        var next = 1;
        if (last is not null)
        {
            var parts = last.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var n)) next = n + 1;
        }
        return $"{fullPrefix}{next:D4}";
    }

    private async Task<decimal> LatestFxAsync(int tenantId, CancellationToken ct) =>
        (await _db.GoldFxRates.Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.RateDate).ThenByDescending(r => r.Id)
            .Select(r => (decimal?)r.UsdToIqd).FirstOrDefaultAsync(ct)) ?? 1m;

    private async Task<int> ResolveWarehouseIdAsync(int tenantId, int? warehouseId, string username, CancellationToken ct)
    {
        if (warehouseId.HasValue)
        {
            var exists = await _db.GoldWarehouses.AnyAsync(w => w.TenantId == tenantId && w.Id == warehouseId.Value && w.IsActive, ct);
            if (!exists) throw new InvalidOperationException("Warehouse not found.");
            return warehouseId.Value;
        }
        var def = await _db.GoldWarehouses.Where(w => w.TenantId == tenantId && w.IsActive)
            .OrderByDescending(w => w.IsDefault).ThenBy(w => w.Id).FirstOrDefaultAsync(ct);
        if (def is not null) return def.Id;
        var created = new CloudGoldWarehouse
        {
            TenantId = tenantId, SyncId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, CreatedBy = username,
            Name = "المخزن الرئيسي", IsDefault = true, IsActive = true
        };
        _db.GoldWarehouses.Add(created);
        await _db.SaveChangesAsync(ct);
        return created.Id;
    }

    private async Task AdjustStockAsync(int tenantId, int warehouseId, int karatValue, decimal gramsDelta, string username, CancellationToken ct)
    {
        var balance = await _db.GoldStockBalances
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.WarehouseId == warehouseId && s.KaratValue == karatValue, ct);
        if (balance is null)
        {
            balance = new CloudGoldStockBalance
            {
                TenantId = tenantId, SyncId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, CreatedBy = username,
                WarehouseId = warehouseId, KaratValue = karatValue, GramsOnHand = 0, AverageCostPerGram = 0
            };
            _db.GoldStockBalances.Add(balance);
        }
        balance.GramsOnHand = Round(balance.GramsOnHand + gramsDelta, 4);
        if (balance.GramsOnHand < 0) balance.GramsOnHand = 0;
        balance.UpdatedAt = DateTime.UtcNow;
        balance.UpdatedBy = username;
    }

    private async Task<CloudGoldCashBox> ResolveCashBoxAsync(int tenantId, int? cashBoxId, GoldCurrency currency, string username, CancellationToken ct)
    {
        if (cashBoxId.HasValue)
            return await _db.GoldCashBoxes.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == cashBoxId.Value, ct)
                ?? throw new InvalidOperationException("Cash box not found.");
        var box = await _db.GoldCashBoxes
            .Where(c => c.TenantId == tenantId && c.IsActive && c.Currency == currency)
            .OrderByDescending(c => c.IsDefault).ThenBy(c => c.Id).FirstOrDefaultAsync(ct);
        if (box is not null) return box;
        box = new CloudGoldCashBox
        {
            TenantId = tenantId, SyncId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, CreatedBy = username,
            Name = currency == GoldCurrency.USD ? "قاصة دولار" : "قاصة دينار",
            Currency = currency, IsDefault = true, IsActive = true, Balance = 0
        };
        _db.GoldCashBoxes.Add(box);
        await _db.SaveChangesAsync(ct);
        return box;
    }

    private static void AdjustCustomerCredit(CloudGoldCustomer c, GoldCurrency currency, decimal delta)
    {
        if (currency == GoldCurrency.USD) c.CreditBalanceUsd = Round(c.CreditBalanceUsd + delta);
        else c.CreditBalanceIqd = Round(c.CreditBalanceIqd + delta);
    }

    private static void AdjustSupplierCredit(CloudGoldSupplier s, GoldCurrency currency, decimal delta)
    {
        if (currency == GoldCurrency.USD) s.CreditBalanceUsd = Round(s.CreditBalanceUsd + delta);
        else s.CreditBalanceIqd = Round(s.CreditBalanceIqd + delta);
    }

    private static void ApplyDualTotals(CloudGoldInvoice invoice)
    {
        var fx = invoice.FxRate <= 0 ? 1m : invoice.FxRate;
        if (invoice.PricingCurrency == GoldCurrency.USD)
        {
            invoice.TotalAmountUsd = invoice.TotalAmount;
            invoice.TotalAmountIqd = Round(invoice.TotalAmount * fx);
        }
        else
        {
            invoice.TotalAmountIqd = invoice.TotalAmount;
            invoice.TotalAmountUsd = Round(invoice.TotalAmount / fx);
        }
    }

    private static decimal ConvertAmount(decimal amount, GoldCurrency from, GoldCurrency to, decimal fxRate)
    {
        if (from == to) return amount;
        var fx = fxRate <= 0 ? 1m : fxRate;
        return from == GoldCurrency.USD ? amount * fx : amount / fx;
    }

    private static GoldInvoiceStatus ResolveStatus(decimal totalAmount, decimal paidAmount, GoldPaymentMethod method)
    {
        if (paidAmount <= 0)
            return method == GoldPaymentMethod.Credit ? GoldInvoiceStatus.Open : GoldInvoiceStatus.Completed;
        if (paidAmount + 0.0001m >= totalAmount) return GoldInvoiceStatus.Completed;
        return GoldInvoiceStatus.PartiallyPaid;
    }

    private static decimal Round(decimal value, int decimals = 4) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}

public sealed class CloudGoldCreateVoucherRequest
{
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    public GoldVoucherType VoucherType { get; set; } = GoldVoucherType.Receipt;
    public GoldCurrency Currency { get; set; } = GoldCurrency.IQD;
    public decimal Amount { get; set; }
    public int? CashBoxId { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public bool IsOpeningBalance { get; set; }
    public bool AffectsCashBox { get; set; } = true;
    public string? VoucherNumber { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class CloudGoldCollectionRequest
{
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public GoldCurrency Currency { get; set; } = GoldCurrency.IQD;
    public int? CashBoxId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public string Notes { get; set; } = string.Empty;
}

public sealed class CloudGoldCreateExchangeRequest
{
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public GoldPaymentMethod PaymentMethod { get; set; } = GoldPaymentMethod.Cash;
    public int? CustomerId { get; set; }
    public int? WarehouseId { get; set; }
    public GoldCurrency PricingCurrency { get; set; } = GoldCurrency.IQD;
    public GoldCurrency PaymentCurrency { get; set; } = GoldCurrency.IQD;
    public decimal FxRate { get; set; }
    public decimal ExchangeCashDifference { get; set; }
    public decimal PaidAmount { get; set; }
    public int? CashBoxId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool WeightFromScale { get; set; }
    public List<CloudGoldCreateSaleLineRequest> InLines { get; set; } = [];
    public List<CloudGoldCreateSaleLineRequest> OutLines { get; set; } = [];
}

public sealed class CloudGoldCreateExpenseRequest
{
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public int ExpenseTypeId { get; set; }
    public decimal Amount { get; set; }
    public GoldCurrency Currency { get; set; } = GoldCurrency.IQD;
    public int? CashBoxId { get; set; }
    public int? WarehouseId { get; set; }
    public string Notes { get; set; } = string.Empty;
}
