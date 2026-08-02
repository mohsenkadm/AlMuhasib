using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldExchangeService : IGoldExchangeService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;

    public GoldExchangeService(IDbContextFactory<GoldDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<(IReadOnlyList<GoldInvoiceListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        return await GoldInvoiceQueryHelper.GetPagedAsync(
            _contextFactory,
            GoldInvoiceType.Exchange,
            page,
            pageSize,
            search,
            dateFrom,
            dateTo,
            null,
            null,
            cancellationToken);
    }

    public async Task<GoldInvoice?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GoldInvoices
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Warehouse)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id && i.InvoiceType == GoldInvoiceType.Exchange, cancellationToken);
    }

    public async Task<string> GetNextInvoiceNumberAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await GoldInvoiceQueryHelper.GetNextInvoiceNumberAsync(context, GoldInvoiceType.Exchange, cancellationToken);
    }

    public async Task<GoldInvoice> CreateExchangeAsync(
        GoldExchangeRequest request,
        CancellationToken cancellationToken = default)
    {
        if ((request.InLines is null || request.InLines.Count == 0) &&
            (request.OutLines is null || request.OutLines.Count == 0))
            throw new InvalidOperationException("يجب إضافة بنود واردة أو صادرة للمبادلة");

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

            var warehouseId = await GoldWarehouseService.ResolveWarehouseIdAsync(
                context,
                request.WarehouseId,
                cancellationToken);

            var invoice = new GoldInvoice
            {
                InvoiceNumber = await GoldInvoiceQueryHelper.GetNextInvoiceNumberAsync(
                    context, GoldInvoiceType.Exchange, cancellationToken),
                InvoiceDate = request.InvoiceDate.Date,
                InvoiceType = GoldInvoiceType.Exchange,
                IsExchange = true,
                PaymentMethod = request.PaymentMethod,
                CustomerId = request.CustomerId,
                WarehouseId = warehouseId,
                PricingCurrency = request.PricingCurrency,
                PaymentCurrency = request.PaymentCurrency,
                FxRate = fx,
                ExchangeCashDifference = request.ExchangeCashDifference,
                Notes = request.Notes ?? string.Empty,
                WeightFromScale = request.WeightFromScale,
                CashBoxId = request.CashBoxId
            };

            decimal totalGold = 0, totalMaking = 0, totalWeight = 0;
            decimal inValue = 0, outValue = 0;

            foreach (var lineReq in request.InLines ?? [])
            {
                var line = BuildLine(lineReq, mithqalGrams, GoldInvoiceLineDirection.In, "كسر وارد");
                // Scrap in increases stock
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
                inValue += line.LineTotal;
            }

            foreach (var lineReq in request.OutLines ?? [])
            {
                var line = BuildLine(lineReq, mithqalGrams, GoldInvoiceLineDirection.Out, "ذهب صادر");

                if (lineReq.ItemId.HasValue)
                {
                    var item = await context.GoldItems.FirstOrDefaultAsync(i => i.Id == lineReq.ItemId.Value, cancellationToken)
                        ?? throw new InvalidOperationException($"القطعة رقم {lineReq.ItemId} غير موجودة");
                    if (item.Status != GoldItemStatus.InStock)
                        throw new InvalidOperationException($"القطعة «{item.Name}» غير متاحة");
                    if (item.TrackAsPiece)
                        item.Status = GoldItemStatus.Sold;
                }

                await GoldInventoryService.AdjustStockInternalAsync(
                    context,
                    line.KaratValue,
                    -line.WeightGrams,
                    null,
                    warehouseId,
                    cancellationToken);
                invoice.Lines.Add(line);
                totalGold += line.GoldValue;
                totalMaking += line.MakingCharge;
                totalWeight += line.WeightGrams;
                outValue += line.LineTotal;
            }

            // Net: customer pays difference when out > in (new gold more valuable than scrap).
            var computedDifference = GoldCurrencyHelper.Round(outValue - inValue);
            if (request.ExchangeCashDifference == 0)
                invoice.ExchangeCashDifference = computedDifference;

            invoice.TotalGoldValue = GoldCurrencyHelper.Round(totalGold);
            invoice.TotalMakingCharge = GoldCurrencyHelper.Round(totalMaking);
            invoice.TotalWeightGrams = GoldCurrencyHelper.Round(totalWeight);
            invoice.TotalAmount = GoldCurrencyHelper.Round(Math.Abs(invoice.ExchangeCashDifference));
            GoldCurrencyHelper.ApplyDualTotals(invoice);

            await GoldCashService.EnsureDefaultCashBoxesAsync(context, cancellationToken);

            var cashDiffInPayment = GoldCurrencyHelper.ConvertAmount(
                invoice.ExchangeCashDifference,
                request.PricingCurrency,
                request.PaymentCurrency,
                fx);

            if (cashDiffInPayment != 0)
            {
                if (request.PaymentMethod == GoldPaymentMethod.Credit)
                {
                    if (customer is null)
                        throw new InvalidOperationException("فرق المبادلة الآجل يتطلب زبون");

                    // Positive diff = customer owes shop → increase customer credit
                    GoldCustomerService.AdjustCredit(customer, request.PaymentCurrency, cashDiffInPayment);
                    invoice.PaidAmount = 0;
                    invoice.RemainingAmount = GoldCurrencyHelper.Round(Math.Abs(invoice.ExchangeCashDifference));
                    invoice.Status = GoldInvoiceStatus.Open;
                }
                else
                {
                    var cashBox = await GoldCashService.ResolveCashBoxAsync(
                        context,
                        request.CashBoxId,
                        request.PaymentCurrency,
                        cancellationToken);
                    invoice.CashBoxId = cashBox.Id;

                    // Positive = cash in; negative = cash out
                    GoldCashService.AdjustCashBoxBalance(cashBox, cashDiffInPayment);

                    var paidAbs = Math.Abs(cashDiffInPayment);
                    invoice.Payments.Add(new GoldPayment
                    {
                        PaymentDate = request.InvoiceDate.Date,
                        Amount = GoldCurrencyHelper.Round(paidAbs),
                        Currency = request.PaymentCurrency,
                        FxRate = fx,
                        CashBoxId = cashBox.Id,
                        Notes = cashDiffInPayment >= 0 ? "فرق مبادلة مستلم" : "فرق مبادلة مدفوع"
                    });

                    invoice.PaidAmount = invoice.TotalAmount;
                    invoice.RemainingAmount = 0;
                    invoice.Status = GoldInvoiceStatus.Completed;
                }
            }
            else
            {
                invoice.PaidAmount = 0;
                invoice.RemainingAmount = 0;
                invoice.Status = GoldInvoiceStatus.Completed;
            }

            await context.GoldInvoices.AddAsync(invoice, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return (await context.GoldInvoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Warehouse)
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

    private static GoldInvoiceLine BuildLine(
        GoldSaleLineRequest lineReq,
        decimal mithqalGrams,
        GoldInvoiceLineDirection direction,
        string defaultDescription)
    {
        if (lineReq.WeightGrams <= 0)
            throw new InvalidOperationException("وزن البند يجب أن يكون أكبر من صفر");
        if (lineReq.MithqalPrice <= 0)
            throw new InvalidOperationException("سعر المثقال يجب أن يكون أكبر من صفر");

        var pricePerGram = GoldCurrencyHelper.Round(lineReq.MithqalPrice / mithqalGrams, 6);
        var goldValue = GoldCurrencyHelper.Round(lineReq.WeightGrams * pricePerGram);
        var making = Math.Max(0, lineReq.MakingCharge);
        var lineTotal = GoldCurrencyHelper.Round(goldValue + making);

        return new GoldInvoiceLine
        {
            ItemId = lineReq.ItemId,
            KaratValue = lineReq.KaratValue,
            WeightGrams = lineReq.WeightGrams,
            MithqalPrice = lineReq.MithqalPrice,
            PricePerGram = pricePerGram,
            GoldValue = goldValue,
            MakingCharge = making,
            LineTotal = lineTotal,
            Description = string.IsNullOrWhiteSpace(lineReq.Description)
                ? $"{defaultDescription} عيار {lineReq.KaratValue}"
                : lineReq.Description,
            WeightFromScale = lineReq.WeightFromScale,
            LineDirection = direction
        };
    }
}
