using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldSaleService : IGoldSaleService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;

    public GoldSaleService(IDbContextFactory<GoldDbContext> contextFactory)
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
            GoldInvoiceType.Sale,
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
            .Include(i => i.Warehouse)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id && i.InvoiceType != GoldInvoiceType.Purchase, cancellationToken);
    }

    public async Task<GoldInvoice> CreateSaleAsync(GoldSaleRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Lines is null || request.Lines.Count == 0)
            throw new InvalidOperationException("يجب إضافة بند واحد على الأقل للفاتورة");

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
                    ?? throw new InvalidOperationException("الزبون غير موجود");
            }
            else if (request.PaymentMethod == GoldPaymentMethod.Credit)
            {
                throw new InvalidOperationException("البيع الآجل يتطلب اختيار زبون");
            }

            var warehouseId = await GoldWarehouseService.ResolveWarehouseIdAsync(
                context,
                request.WarehouseId,
                cancellationToken);

            var invoice = new GoldInvoice
            {
                InvoiceNumber = await GoldInvoiceQueryHelper.GetNextInvoiceNumberAsync(context, GoldInvoiceType.Sale, cancellationToken),
                InvoiceDate = request.InvoiceDate.Date,
                InvoiceType = GoldInvoiceType.Sale,
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

                if (lineReq.MakingChargeMode == GoldMakingChargeMode.Fixed
                    && lineReq.MakingChargeRate == 0
                    && lineReq.MakingCharge == 0
                    && settings.DefaultMakingChargeMode != GoldMakingChargeMode.Fixed)
                {
                    lineReq.MakingChargeMode = settings.DefaultMakingChargeMode;
                }

                var line = GoldLinePricingHelper.BuildInvoiceLine(
                    lineReq,
                    mithqalGrams,
                    purity,
                    GoldInvoiceLineDirection.Out,
                    "بيع");

                if (lineReq.ItemId.HasValue)
                {
                    var item = await context.GoldItems.FirstOrDefaultAsync(i => i.Id == lineReq.ItemId.Value, cancellationToken)
                        ?? throw new InvalidOperationException($"القطعة رقم {lineReq.ItemId} غير موجودة");

                    if (item.Status != GoldItemStatus.InStock)
                        throw new InvalidOperationException($"القطعة «{item.Name}» غير متاحة للبيع");

                    if (item.TrackAsPiece)
                        item.Status = GoldItemStatus.Sold;
                }

                await GoldInventoryService.AdjustStockInternalAsync(
                    context,
                    lineReq.KaratValue,
                    -lineReq.WeightGrams,
                    null,
                    warehouseId,
                    cancellationToken);

                invoice.Lines.Add(line);

                totalGold += line.GoldValue;
                totalMaking += line.MakingCharge;
                totalWeight += lineReq.WeightGrams;
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
            // طريقة الدفع تُحفظ كما اختارها المستخدم — المدفوع صفر مع نقدي ≠ آجل
            invoice.PaymentMethod = request.PaymentMethod;
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
                GoldCashService.AdjustCashBoxBalance(cashBox, paidInPaymentCurrency);

                invoice.Payments.Add(new GoldPayment
                {
                    PaymentDate = request.InvoiceDate.Date,
                    Amount = GoldCurrencyHelper.Round(paidInPaymentCurrency),
                    Currency = request.PaymentCurrency,
                    FxRate = fx,
                    CashBoxId = cashBox.Id,
                    Notes = "دفعة عند البيع"
                });
            }

            if (invoice.RemainingAmount > 0)
            {
                if (customer is null)
                    throw new InvalidOperationException("لا يمكن ترك مبلغ متبقٍ بدون زبون");

                var creditInPaymentCurrency = GoldCurrencyHelper.ConvertAmount(
                    invoice.RemainingAmount,
                    request.PricingCurrency,
                    request.PaymentCurrency,
                    fx);
                GoldCustomerService.AdjustCredit(customer, request.PaymentCurrency, creditInPaymentCurrency);
            }

            // Credit sale (or partial unpaid): track grams sold on credit. Cash collection does not reduce grams.
            if (customer is not null
                && invoice.TotalWeightGrams > 0
                && (request.PaymentMethod == GoldPaymentMethod.Credit || invoice.RemainingAmount > 0))
            {
                GoldCustomerService.AdjustGoldCreditGrams(customer, invoice.TotalWeightGrams);
            }

            await context.GoldInvoices.AddAsync(invoice, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return (await context.GoldInvoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Lines)
                .Include(i => i.Payments)
                .FirstAsync(i => i.Id == invoice.Id, cancellationToken));
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<GoldInvoice> CreateSaleReturnAsync(
        GoldSaleReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Lines is null || request.Lines.Count == 0)
            throw new InvalidOperationException("يجب إضافة بند واحد على الأقل لمرتجع البيع");

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
                    ?? throw new InvalidOperationException("الزبون غير موجود");
            }
            else if (request.PaymentMethod == GoldPaymentMethod.Credit)
            {
                throw new InvalidOperationException("مرتجع الآجل يتطلب اختيار زبون");
            }

            if (request.RelatedInvoiceId.HasValue)
            {
                var original = await context.GoldInvoices.AsNoTracking()
                    .FirstOrDefaultAsync(
                        i => i.Id == request.RelatedInvoiceId.Value && i.InvoiceType == GoldInvoiceType.Sale,
                        cancellationToken)
                    ?? throw new InvalidOperationException("فاتورة البيع الأصلية غير موجودة");

                if (original.Status == GoldInvoiceStatus.Cancelled)
                    throw new InvalidOperationException("لا يمكن إرجاع فاتورة ملغاة");

                request.CustomerId ??= original.CustomerId;
                if (customer is null && request.CustomerId.HasValue)
                {
                    customer = await context.GoldCustomers.FirstOrDefaultAsync(c => c.Id == request.CustomerId.Value, cancellationToken);
                }
            }

            var warehouseId = await GoldWarehouseService.ResolveWarehouseIdAsync(
                context,
                request.WarehouseId,
                cancellationToken);

            var purityByKarat = await context.GoldKarats.AsNoTracking()
                .ToDictionaryAsync(k => k.KaratValue, k => k.PurityFactor, cancellationToken);

            var invoice = new GoldInvoice
            {
                InvoiceNumber = await GoldInvoiceQueryHelper.GetNextInvoiceNumberAsync(
                    context, GoldInvoiceType.SaleReturn, cancellationToken),
                InvoiceDate = request.InvoiceDate.Date,
                InvoiceType = GoldInvoiceType.SaleReturn,
                PaymentMethod = request.PaymentMethod,
                CustomerId = request.CustomerId ?? customer?.Id,
                WarehouseId = warehouseId,
                RelatedInvoiceId = request.RelatedInvoiceId,
                PricingCurrency = request.PricingCurrency,
                PaymentCurrency = request.PaymentCurrency,
                FxRate = fx,
                DiscountAmount = Math.Max(0, request.DiscountAmount),
                Notes = request.Notes ?? string.Empty,
                WeightFromScale = request.WeightFromScale,
                CashBoxId = request.CashBoxId
            };

            decimal totalGold = 0, totalMaking = 0, totalWeight = 0;

            foreach (var lineReq in request.Lines)
            {
                purityByKarat.TryGetValue(lineReq.KaratValue, out var purity);
                if (purity <= 0) purity = 1m;

                var line = GoldLinePricingHelper.BuildInvoiceLine(
                    lineReq,
                    mithqalGrams,
                    purity,
                    GoldInvoiceLineDirection.In,
                    "مرتجع بيع");

                // Return increases stock
                await GoldInventoryService.AdjustStockInternalAsync(
                    context,
                    line.KaratValue,
                    line.WeightGrams,
                    line.PricePerGram > 0 ? line.PricePerGram : null,
                    warehouseId,
                    cancellationToken);

                if (lineReq.ItemId.HasValue)
                {
                    var item = await context.GoldItems.FirstOrDefaultAsync(i => i.Id == lineReq.ItemId.Value, cancellationToken);
                    if (item is not null && item.Status == GoldItemStatus.Sold)
                        item.Status = GoldItemStatus.InStock;
                }

                invoice.Lines.Add(line);
                totalGold += line.GoldValue;
                totalMaking += line.MakingCharge;
                totalWeight += line.WeightGrams;
            }

            invoice.TotalGoldValue = GoldCurrencyHelper.Round(totalGold);
            invoice.TotalMakingCharge = GoldCurrencyHelper.Round(totalMaking);
            invoice.TotalWeightGrams = GoldCurrencyHelper.Round(totalWeight);
            invoice.TotalAmount = GoldCurrencyHelper.Round(
                invoice.TotalGoldValue + invoice.TotalMakingCharge - invoice.DiscountAmount);
            if (invoice.TotalAmount < 0)
                invoice.TotalAmount = 0;

            GoldCurrencyHelper.ApplyDualTotals(invoice);

            var refundInPricing = GoldCurrencyHelper.ConvertAmount(
                Math.Max(0, request.PaidAmount),
                request.PaymentCurrency,
                request.PricingCurrency,
                fx);

            if (request.PaymentMethod == GoldPaymentMethod.Cash && refundInPricing <= 0)
                refundInPricing = invoice.TotalAmount;

            if (refundInPricing > invoice.TotalAmount)
                refundInPricing = invoice.TotalAmount;

            invoice.PaidAmount = GoldCurrencyHelper.Round(refundInPricing);
            invoice.RemainingAmount = GoldCurrencyHelper.Round(invoice.TotalAmount - invoice.PaidAmount);
            invoice.Status = GoldCurrencyHelper.ResolveStatus(
                invoice.TotalAmount, invoice.PaidAmount, request.PaymentMethod);

            await GoldCashService.EnsureDefaultCashBoxesAsync(context, cancellationToken);

            // Cash refund: money leaves the till
            if (invoice.PaidAmount > 0 && request.PaymentMethod != GoldPaymentMethod.Credit)
            {
                var refundInPaymentCurrency = GoldCurrencyHelper.ConvertAmount(
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
                GoldCashService.AdjustCashBoxBalance(cashBox, -refundInPaymentCurrency);

                invoice.Payments.Add(new GoldPayment
                {
                    PaymentDate = request.InvoiceDate.Date,
                    Amount = GoldCurrencyHelper.Round(refundInPaymentCurrency),
                    Currency = request.PaymentCurrency,
                    FxRate = fx,
                    CashBoxId = cashBox.Id,
                    Notes = "استرداد نقدي لمرتجع بيع"
                });
            }

            // Credit return: reduce customer money credit for unpaid portion of return value
            if (invoice.RemainingAmount > 0 || request.PaymentMethod == GoldPaymentMethod.Credit)
            {
                if (customer is null)
                    throw new InvalidOperationException("مرتجع الآجل يتطلب زبون");

                var creditReduce = request.PaymentMethod == GoldPaymentMethod.Credit
                    ? GoldCurrencyHelper.ConvertAmount(
                        invoice.TotalAmount,
                        request.PricingCurrency,
                        request.PaymentCurrency,
                        fx)
                    : GoldCurrencyHelper.ConvertAmount(
                        invoice.RemainingAmount,
                        request.PricingCurrency,
                        request.PaymentCurrency,
                        fx);

                if (creditReduce > 0)
                    GoldCustomerService.AdjustCredit(customer, request.PaymentCurrency, -creditReduce);

                if (request.PaymentMethod == GoldPaymentMethod.Credit)
                {
                    invoice.PaidAmount = 0;
                    invoice.RemainingAmount = 0;
                    invoice.Status = GoldInvoiceStatus.Completed;
                }
            }

            // Sale return always reduces tracked credit grams (return of gold).
            if (customer is not null && invoice.TotalWeightGrams > 0)
                GoldCustomerService.AdjustGoldCreditGrams(customer, -invoice.TotalWeightGrams);

            await context.GoldInvoices.AddAsync(invoice, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return await context.GoldInvoices
                .AsNoTracking()
                .Include(i => i.Customer)
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
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && i.InvoiceType == GoldInvoiceType.Sale, cancellationToken)
                ?? throw new InvalidOperationException("فاتورة البيع غير موجودة");

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
            GoldCashService.AdjustCashBoxBalance(cashBox, paidInPaymentCurrency);

            if (invoice.Customer is not null)
                GoldCustomerService.AdjustCredit(invoice.Customer, request.Currency, -paidInPaymentCurrency);

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

            return (await context.GoldInvoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Lines)
                .Include(i => i.Payments)
                .FirstAsync(i => i.Id == invoice.Id, cancellationToken));
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
                .Include(i => i.Lines)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == id && i.InvoiceType == GoldInvoiceType.Sale, cancellationToken)
                ?? throw new InvalidOperationException("فاتورة البيع غير موجودة");

            if (invoice.Status == GoldInvoiceStatus.Cancelled)
                return;

            // Best-effort stock restoration
            var warehouseId = invoice.WarehouseId
                ?? (await GoldWarehouseService.EnsureDefaultInternalAsync(context, cancellationToken)).Id;
            foreach (var line in invoice.Lines)
            {
                try
                {
                    var delta = line.LineDirection == GoldInvoiceLineDirection.In
                        ? -line.WeightGrams
                        : line.WeightGrams;
                    await GoldInventoryService.AdjustStockInternalAsync(
                        context,
                        line.KaratValue,
                        delta,
                        line.PricePerGram > 0 ? line.PricePerGram : null,
                        warehouseId,
                        cancellationToken);
                }
                catch
                {
                    // best effort
                }

                if (line.ItemId.HasValue)
                {
                    var item = await context.GoldItems.FirstOrDefaultAsync(i => i.Id == line.ItemId.Value, cancellationToken);
                    if (item is not null && item.Status == GoldItemStatus.Sold)
                        item.Status = GoldItemStatus.InStock;
                }
            }

            // Reverse cash payments
            foreach (var payment in invoice.Payments)
            {
                try
                {
                    if (!payment.CashBoxId.HasValue)
                        continue;

                    var cashBox = await context.GoldCashBoxes.FirstOrDefaultAsync(c => c.Id == payment.CashBoxId.Value, cancellationToken);
                    if (cashBox is not null)
                        GoldCashService.AdjustCashBoxBalance(cashBox, -payment.Amount);
                }
                catch
                {
                    // best effort
                }
            }

            // Reverse remaining credit (money + credit grams from sale)
            if (invoice.Customer is not null)
            {
                try
                {
                    if (invoice.RemainingAmount > 0)
                    {
                        var credit = GoldCurrencyHelper.ConvertAmount(
                            invoice.RemainingAmount,
                            invoice.PricingCurrency,
                            invoice.PaymentCurrency,
                            invoice.FxRate > 0 ? invoice.FxRate : 1m);
                        GoldCustomerService.AdjustCredit(invoice.Customer, invoice.PaymentCurrency, -credit);
                    }

                    if (invoice.TotalWeightGrams > 0 &&
                        (invoice.PaymentMethod == GoldPaymentMethod.Credit || invoice.RemainingAmount > 0))
                    {
                        GoldCustomerService.AdjustGoldCreditGrams(invoice.Customer, -invoice.TotalWeightGrams);
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
        return await GoldInvoiceQueryHelper.GetNextInvoiceNumberAsync(context, GoldInvoiceType.Sale, cancellationToken);
    }

    public async Task<string> GetNextSaleReturnNumberAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await GoldInvoiceQueryHelper.GetNextInvoiceNumberAsync(context, GoldInvoiceType.SaleReturn, cancellationToken);
    }
}
