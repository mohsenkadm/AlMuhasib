using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldPurchaseService : IGoldPurchaseService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;

    public GoldPurchaseService(IDbContextFactory<GoldDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<(IReadOnlyList<GoldInvoiceListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        GoldInvoiceStatus? status = null,
        int? customerId = null,
        CancellationToken cancellationToken = default)
    {
        return await GoldInvoiceQueryHelper.GetPagedAsync(
            _contextFactory,
            GoldInvoiceType.Purchase,
            page,
            pageSize,
            search,
            dateFrom,
            dateTo,
            status,
            customerId,
            cancellationToken);
    }

    public async Task<GoldInvoice?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GoldInvoices
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id && i.InvoiceType == GoldInvoiceType.Purchase, cancellationToken);
    }

    public async Task<GoldInvoice> CreatePurchaseAsync(GoldPurchaseRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Lines is null || request.Lines.Count == 0)
            throw new InvalidOperationException("يجب إضافة بند واحد على الأقل لفاتورة الشراء");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var settings = await GoldSettingsService.EnsureSettingsAsync(context, cancellationToken);
            var mithqalGrams = settings.MithqalGrams <= 0 ? 5m : settings.MithqalGrams;
            var fx = request.FxRate > 0
                ? request.FxRate
                : (await context.GoldFxRates
                    .OrderByDescending(r => r.RateDate)
                    .ThenByDescending(r => r.Id)
                    .Select(r => (decimal?)r.UsdToIqd)
                    .FirstOrDefaultAsync(cancellationToken)) ?? 1m;

            GoldCustomer? customer = null;
            if (request.CustomerId.HasValue)
            {
                customer = await context.GoldCustomers.FirstOrDefaultAsync(c => c.Id == request.CustomerId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("الزبون/المورد غير موجود");
            }

            GoldSupplier? supplier = null;
            if (request.SupplierId.HasValue)
            {
                supplier = await context.GoldSuppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("المورد غير موجود");
            }

            var warehouseId = await GoldWarehouseService.ResolveWarehouseIdAsync(
                context,
                request.WarehouseId,
                cancellationToken);

            var invoice = new GoldInvoice
            {
                InvoiceNumber = await GoldInvoiceQueryHelper.GetNextInvoiceNumberAsync(context, GoldInvoiceType.Purchase, cancellationToken),
                InvoiceDate = request.InvoiceDate.Date,
                InvoiceType = GoldInvoiceType.Purchase,
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
                CashBoxId = request.CashBoxId
            };

            var purityByKarat = await context.GoldKarats.AsNoTracking()
                .ToDictionaryAsync(k => k.KaratValue, k => k.PurityFactor, cancellationToken);

            decimal totalGold = 0, totalMaking = 0, totalWeight = 0;

            foreach (var lineReq in request.Lines)
            {
                purityByKarat.TryGetValue(lineReq.KaratValue, out var purity);
                if (purity <= 0) purity = 1m;

                if (lineReq.CreateAsPiece)
                {
                    var pieceName = string.IsNullOrWhiteSpace(lineReq.Description)
                        ? $"قطعة عيار {lineReq.KaratValue}"
                        : lineReq.Description.Trim();
                    var barcode = string.IsNullOrWhiteSpace(lineReq.PieceBarcode)
                        ? string.Empty
                        : lineReq.PieceBarcode.Trim();

                    if (!string.IsNullOrEmpty(barcode))
                    {
                        var barcodeTaken = await context.GoldItems.AnyAsync(i => i.Barcode == barcode, cancellationToken);
                        if (barcodeTaken)
                            throw new InvalidOperationException($"الباركود «{barcode}» مستخدم مسبقاً");
                    }

                    var costPerGram = lineReq.WeightGrams > 0
                        ? GoldLinePricingHelper.Calculate(
                            lineReq.WeightGrams,
                            lineReq.MithqalPrice,
                            mithqalGrams,
                            purity,
                            lineReq.MakingChargeMode,
                            lineReq.MakingCharge,
                            lineReq.MakingChargeRate).PricePerGram
                        : 0m;

                    var item = new GoldItem
                    {
                        Name = pieceName,
                        Barcode = barcode,
                        KaratValue = lineReq.KaratValue,
                        WeightGrams = lineReq.WeightGrams,
                        SuggestedMakingCharge = lineReq.MakingCharge,
                        MakingChargeCurrency = request.PricingCurrency,
                        CostPerGram = costPerGram,
                        Status = GoldItemStatus.InStock,
                        TrackAsPiece = true,
                        Category = "شراء",
                        Notes = $"من فاتورة شراء — {invoice.InvoiceNumber}"
                    };
                    await context.GoldItems.AddAsync(item, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                    lineReq.ItemId = item.Id;
                    if (string.IsNullOrWhiteSpace(lineReq.Description))
                        lineReq.Description = pieceName;
                }

                var line = GoldLinePricingHelper.BuildInvoiceLine(
                    lineReq,
                    mithqalGrams,
                    purity,
                    GoldInvoiceLineDirection.In,
                    lineReq.CreateAsPiece ? "شراء قطعة" : "شراء كسر");

                // Scrap or piece purchase increases karat stock once (piece catalog is separate tracking).
                await GoldInventoryService.AdjustStockInternalAsync(
                    context,
                    line.KaratValue,
                    line.WeightGrams,
                    line.PricePerGram,
                    warehouseId,
                    cancellationToken);

                invoice.Lines.Add(line);

                totalGold += line.GoldValue;
                totalMaking += line.MakingCharge;
                totalWeight += line.WeightGrams;
            }

            invoice.TotalGoldValue = GoldCurrencyHelper.Round(totalGold);
            invoice.TotalMakingCharge = GoldCurrencyHelper.Round(totalMaking);
            invoice.TotalWeightGrams = GoldCurrencyHelper.Round(totalWeight);
            invoice.TotalAmount = GoldCurrencyHelper.Round(invoice.TotalGoldValue + invoice.TotalMakingCharge - invoice.DiscountAmount);
            if (invoice.TotalAmount < 0)
                invoice.TotalAmount = 0;

            GoldCurrencyHelper.ApplyDualTotals(invoice);

            var paidInPricing = GoldCurrencyHelper.ConvertAmount(
                Math.Max(0, request.PaidAmount),
                request.PaymentCurrency,
                request.PricingCurrency,
                fx);

            if (request.PaymentMethod == GoldPaymentMethod.Cash && paidInPricing <= 0)
                paidInPricing = invoice.TotalAmount;

            if (paidInPricing > invoice.TotalAmount)
                paidInPricing = invoice.TotalAmount;

            invoice.PaidAmount = GoldCurrencyHelper.Round(paidInPricing);
            invoice.RemainingAmount = GoldCurrencyHelper.Round(invoice.TotalAmount - invoice.PaidAmount);
            invoice.Status = GoldCurrencyHelper.ResolveStatus(invoice.TotalAmount, invoice.PaidAmount, request.PaymentMethod);

            await GoldCashService.EnsureDefaultCashBoxesAsync(context, cancellationToken);

            if (invoice.PaidAmount > 0)
            {
                var paidInPaymentCurrency = GoldCurrencyHelper.ConvertAmount(
                    invoice.PaidAmount,
                    request.PricingCurrency,
                    request.PaymentCurrency,
                    fx);

                var cashBox = await GoldCashService.ResolveCashBoxAsync(
                    context,
                    request.CashBoxId,
                    request.PaymentCurrency,
                    cancellationToken);
                invoice.CashBoxId = cashBox.Id;

                // Purchase decreases cash.
                GoldCashService.AdjustCashBoxBalance(cashBox, -paidInPaymentCurrency);

                invoice.Payments.Add(new GoldPayment
                {
                    PaymentDate = request.InvoiceDate.Date,
                    Amount = GoldCurrencyHelper.Round(paidInPaymentCurrency),
                    Currency = request.PaymentCurrency,
                    FxRate = fx,
                    CashBoxId = cashBox.Id,
                    Notes = "دفع عند الشراء"
                });
            }

            if (invoice.RemainingAmount > 0)
            {
                if (supplier is null && customer is null)
                    throw new InvalidOperationException("لا يمكن ترك مبلغ متبقٍ بدون زبون/مورد");

                var creditInPaymentCurrency = GoldCurrencyHelper.ConvertAmount(
                    invoice.RemainingAmount,
                    request.PricingCurrency,
                    request.PaymentCurrency,
                    fx);

                if (supplier is not null)
                    GoldSupplierService.AdjustCredit(supplier, request.PaymentCurrency, creditInPaymentCurrency);
                else if (customer is not null)
                {
                    GoldCustomerService.AdjustCredit(customer, request.PaymentCurrency, creditInPaymentCurrency);
                    // Scrap purchased on credit from customer settles gold-credit grams they owed.
                    GoldCustomerService.AdjustGoldCreditGrams(customer, -invoice.TotalWeightGrams);
                }
            }

            await context.GoldInvoices.AddAsync(invoice, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return await context.GoldInvoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Supplier)
                .Include(i => i.Lines)
                .Include(i => i.Payments)
                .FirstAsync(i => i.Id == invoice.Id, cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<GoldInvoice> RecordPaymentAsync(GoldPaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("مبلغ الدفعة يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var invoice = await context.GoldInvoices
                .Include(i => i.Customer)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && i.InvoiceType == GoldInvoiceType.Purchase, cancellationToken)
                ?? throw new InvalidOperationException("فاتورة الشراء غير موجودة");

            if (invoice.Status == GoldInvoiceStatus.Cancelled)
                throw new InvalidOperationException("لا يمكن تسجيل دفعة على فاتورة ملغاة");
            if (invoice.RemainingAmount <= 0)
                throw new InvalidOperationException("الفاتورة مسددة بالكامل");

            var fx = request.FxRate > 0 ? request.FxRate : (invoice.FxRate > 0 ? invoice.FxRate : 1m);
            var paidInPricing = GoldCurrencyHelper.ConvertAmount(
                request.Amount,
                request.Currency,
                invoice.PricingCurrency,
                fx);

            if (paidInPricing > invoice.RemainingAmount)
                paidInPricing = invoice.RemainingAmount;

            var paidInPaymentCurrency = GoldCurrencyHelper.ConvertAmount(
                paidInPricing,
                invoice.PricingCurrency,
                request.Currency,
                fx);

            await GoldCashService.EnsureDefaultCashBoxesAsync(context, cancellationToken);
            var cashBox = await GoldCashService.ResolveCashBoxAsync(
                context,
                request.CashBoxId,
                request.Currency,
                cancellationToken);
            GoldCashService.AdjustCashBoxBalance(cashBox, -paidInPaymentCurrency);

            if (invoice.Customer is not null)
                GoldCustomerService.AdjustCredit(invoice.Customer, request.Currency, -paidInPaymentCurrency);

            if (invoice.SupplierId.HasValue)
            {
                var supplier = await context.GoldSuppliers.FirstOrDefaultAsync(s => s.Id == invoice.SupplierId.Value, cancellationToken);
                if (supplier is not null)
                    GoldSupplierService.AdjustCredit(supplier, request.Currency, -paidInPaymentCurrency);
            }

            invoice.PaidAmount = GoldCurrencyHelper.Round(invoice.PaidAmount + paidInPricing);
            invoice.RemainingAmount = GoldCurrencyHelper.Round(invoice.TotalAmount - invoice.PaidAmount);
            if (invoice.RemainingAmount < 0)
                invoice.RemainingAmount = 0;
            invoice.Status = GoldCurrencyHelper.ResolveStatus(invoice.TotalAmount, invoice.PaidAmount, invoice.PaymentMethod);
            invoice.CashBoxId ??= cashBox.Id;

            invoice.Payments.Add(new GoldPayment
            {
                PaymentDate = request.PaymentDate.Date,
                Amount = GoldCurrencyHelper.Round(request.Amount),
                Currency = request.Currency,
                FxRate = fx,
                CashBoxId = cashBox.Id,
                Notes = request.Notes ?? string.Empty
            });

            await context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return await context.GoldInvoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Supplier)
                .Include(i => i.Lines)
                .Include(i => i.Payments)
                .FirstAsync(i => i.Id == invoice.Id, cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task CancelAsync(int id, string cancelledBy, string? reason = null, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var invoice = await context.GoldInvoices
                .Include(i => i.Customer)
                .Include(i => i.Supplier)
                .Include(i => i.Lines)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == id && i.InvoiceType == GoldInvoiceType.Purchase, cancellationToken)
                ?? throw new InvalidOperationException("فاتورة الشراء غير موجودة");

            if (invoice.Status == GoldInvoiceStatus.Cancelled)
                return;

            var warehouseId = invoice.WarehouseId
                ?? (await GoldWarehouseService.EnsureDefaultInternalAsync(context, cancellationToken)).Id;

            foreach (var line in invoice.Lines)
            {
                try
                {
                    await GoldInventoryService.AdjustStockInternalAsync(
                        context,
                        line.KaratValue,
                        -line.WeightGrams,
                        null,
                        warehouseId,
                        cancellationToken);
                }
                catch
                {
                    // best effort
                }
            }

            foreach (var payment in invoice.Payments)
            {
                try
                {
                    if (!payment.CashBoxId.HasValue)
                        continue;

                    var cashBox = await context.GoldCashBoxes.FirstOrDefaultAsync(c => c.Id == payment.CashBoxId.Value, cancellationToken);
                    if (cashBox is not null)
                        GoldCashService.AdjustCashBoxBalance(cashBox, payment.Amount); // restore cash
                }
                catch
                {
                    // best effort
                }
            }

            if (invoice.Customer is not null && invoice.RemainingAmount > 0)
            {
                try
                {
                    var credit = GoldCurrencyHelper.ConvertAmount(
                        invoice.RemainingAmount,
                        invoice.PricingCurrency,
                        invoice.PaymentCurrency,
                        invoice.FxRate > 0 ? invoice.FxRate : 1m);
                    GoldCustomerService.AdjustCredit(invoice.Customer, invoice.PaymentCurrency, -credit);
                }
                catch
                {
                    // best effort
                }
            }

            if (invoice.SupplierId.HasValue && invoice.RemainingAmount > 0)
            {
                try
                {
                    var supplier = await context.GoldSuppliers.FirstOrDefaultAsync(s => s.Id == invoice.SupplierId.Value, cancellationToken);
                    if (supplier is not null)
                    {
                        var credit = GoldCurrencyHelper.ConvertAmount(
                            invoice.RemainingAmount,
                            invoice.PricingCurrency,
                            invoice.PaymentCurrency,
                            invoice.FxRate > 0 ? invoice.FxRate : 1m);
                        GoldSupplierService.AdjustCredit(supplier, invoice.PaymentCurrency, -credit);
                    }
                }
                catch
                {
                    // best effort
                }
            }

            invoice.Status = GoldInvoiceStatus.Cancelled;
            invoice.Notes = string.IsNullOrWhiteSpace(reason)
                ? $"{invoice.Notes}\n[ملغاة بواسطة {cancelledBy}]".Trim()
                : $"{invoice.Notes}\n[ملغاة بواسطة {cancelledBy}: {reason}]".Trim();
            invoice.UpdatedBy = cancelledBy;
            invoice.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<string> GetNextInvoiceNumberAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await GoldInvoiceQueryHelper.GetNextInvoiceNumberAsync(context, GoldInvoiceType.Purchase, cancellationToken);
    }
}
