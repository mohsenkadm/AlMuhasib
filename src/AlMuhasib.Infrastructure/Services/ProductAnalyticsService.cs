using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class ProductAnalyticsService : IProductAnalyticsService
{
    private static readonly InvoiceType[] MovementTypes =
    [
        InvoiceType.Sale,
        InvoiceType.Installment,
        InvoiceType.SaleReturn,
        InvoiceType.Purchase,
        InvoiceType.PurchaseReturn,
        InvoiceType.Damage
    ];

    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ProductAnalyticsService(IDbContextFactory<AppDbContext> contextFactory) =>
        _contextFactory = contextFactory;

    public async Task<ProductAnalyticsReportResult> GetReportAsync(
        ProductAnalyticsQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var multiCurrency = await MultiCurrencyFeatureGate.IsEnabledAsync(context, cancellationToken).ConfigureAwait(false);
        var foldUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(multiCurrency);

        var products = await LoadProductsAsync(context, query, cancellationToken).ConfigureAwait(false);
        var productIds = products.Select(p => p.Id).ToList();
        if (productIds.Count == 0)
        {
            return ProductAnalyticsAggregator.Build(query, products, [], new Dictionary<int, decimal>(), [], foldUsd, multiCurrency);
        }

        HashSet<int>? productSet = productIds.Count > 500 ? productIds.ToHashSet() : null;
        var rangeStart = ComparisonStart(query);
        IReadOnlyCollection<int> lineFilter = productSet is not null ? productSet : productIds;
        var lines = await LoadLinesAsync(context, query, lineFilter, rangeStart, includeDamage: false, cancellationToken)
            .ConfigureAwait(false);
        await FillInvoiceTotalsAsync(context, lines, cancellationToken).ConfigureAwait(false);

        var stockQuery = context.WarehouseStocks.AsNoTracking();
        if (query.ProductId is int onlyProduct)
            stockQuery = stockQuery.Where(s => s.ProductId == onlyProduct);
        else if (productSet is null)
            stockQuery = stockQuery.Where(s => productIds.Contains(s.ProductId));

        var stocks = await stockQuery
            .Select(s => new { s.ProductId, s.WarehouseId, s.Quantity, s.MinQuantity, s.OpeningQuantity, s.UnitCost })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (productSet is not null)
            stocks = stocks.Where(s => productSet.Contains(s.ProductId)).ToList();

        var stockRows = stocks
            .Where(s => query.WarehouseId is null || s.WarehouseId == query.WarehouseId)
            .Select(s => new ProductAnalyticsStock
            {
                ProductId = s.ProductId,
                WarehouseId = s.WarehouseId,
                Quantity = s.Quantity,
                MinQuantity = s.MinQuantity
            })
            .ToList();

        var purchaseItems = await ProductCostHelper.GetPurchaseItemsByProductAsync(
            context,
            productSet is null ? productIds : null).ConfigureAwait(false);
        var costStocks = stocks
            .GroupBy(s => s.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(s => new WarehouseStock
                {
                    ProductId = s.ProductId,
                    WarehouseId = s.WarehouseId,
                    Quantity = s.Quantity,
                    OpeningQuantity = s.OpeningQuantity,
                    UnitCost = s.UnitCost
                }).ToList());

        var costs = new Dictionary<int, decimal>(productIds.Count);
        foreach (var id in productIds)
        {
            costs[id] = ProductCostHelper.ComputeAverageUnitCostForProduct(
                purchaseItems.GetValueOrDefault(id) ?? [],
                costStocks.GetValueOrDefault(id) ?? [],
                id);
        }

        return ProductAnalyticsAggregator.Build(query, products, lines, costs, stockRows, foldUsd, multiCurrency);
    }

    public async Task<ProductAnalyticsDetail> GetDetailAsync(
        int productId,
        DateTime? from,
        DateTime? to,
        int? warehouseId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var multiCurrency = await MultiCurrencyFeatureGate.IsEnabledAsync(context, cancellationToken);
        var foldUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(multiCurrency);
        var report = await GetReportAsync(new ProductAnalyticsQuery
        {
            From = from,
            To = to,
            WarehouseId = warehouseId,
            ProductId = productId
        }, cancellationToken);

        var row = report.Rows.FirstOrDefault();
        var product = await context.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.Id, p.Name, p.Barcode, Category = p.Category.Name })
            .FirstOrDefaultAsync(cancellationToken);
        var unit = await context.ProductUnits.AsNoTracking()
            .Where(u => u.ProductId == productId && u.IsDefault)
            .Select(u => u.UnitName)
            .FirstOrDefaultAsync(cancellationToken);

        var lines = await LoadLinesAsync(
            context,
            new ProductAnalyticsQuery { ProductId = productId, WarehouseId = warehouseId },
            [productId],
            null,
            includeDamage: true,
            cancellationToken);
        await FillInvoiceTotalsAsync(context, lines, cancellationToken);

        var transfers = await context.WarehouseTransferItems.AsNoTracking()
            .Where(i => i.ProductId == productId)
            .Select(i => new
            {
                i.Quantity,
                i.WarehouseTransfer.TransferNumber,
                i.WarehouseTransfer.Date,
                i.WarehouseTransfer.FromWarehouseId,
                FromName = i.WarehouseTransfer.FromWarehouse.Name,
                i.WarehouseTransfer.ToWarehouseId,
                ToName = i.WarehouseTransfer.ToWarehouse.Name
            })
            .ToListAsync(cancellationToken);

        var openings = await context.WarehouseStocks.AsNoTracking()
            .Where(s => s.ProductId == productId && s.OpeningQuantity != 0)
            .Select(s => new { s.WarehouseId, s.OpeningQuantity, s.Warehouse.Name })
            .ToListAsync(cancellationToken);

        var replay = new List<ReplayMove>();
        foreach (var opening in openings)
        {
            replay.Add(new ReplayMove(DateTime.MinValue, 0, "افتتاحي", "رصيد افتتاحي",
                opening.OpeningQuantity > 0 ? opening.OpeningQuantity : 0,
                opening.OpeningQuantity < 0 ? -opening.OpeningQuantity : 0,
                null, null, opening.Name, opening.WarehouseId));
        }

        foreach (var line in lines.OrderBy(l => l.Date).ThenBy(l => l.InvoiceId))
        {
            var qty = Math.Abs(line.Quantity);
            var inbound = line.Type is InvoiceType.Purchase or InvoiceType.SaleReturn;
            replay.Add(new ReplayMove(
                line.Date,
                line.InvoiceId,
                line.InvoiceNumber,
                TypeLabel(line.Type),
                inbound ? qty : 0,
                inbound ? 0 : qty,
                line.UnitPrice,
                ProductAnalyticsAggregator.AllocatedAmountInIqd(line, foldUsd),
                line.WarehouseName,
                line.WarehouseId));
        }

        foreach (var transfer in transfers)
        {
            replay.Add(new ReplayMove(transfer.Date, 0, transfer.TransferNumber, "نقل صادر",
                0, transfer.Quantity, null, null, transfer.FromName, transfer.FromWarehouseId));
            replay.Add(new ReplayMove(transfer.Date, 1, transfer.TransferNumber, "نقل وارد",
                transfer.Quantity, 0, null, null, transfer.ToName, transfer.ToWarehouseId));
        }

        decimal balance = 0;
        var ordered = replay
            .OrderBy(m => m.Date)
            .ThenBy(m => m.Order)
            .ToList();
        var withBalance = new List<ProductAnalyticsMovement>();
        foreach (var move in ordered)
        {
            if (warehouseId.HasValue && move.WarehouseId != warehouseId.Value)
                continue;
            balance += move.QuantityIn - move.QuantityOut;
            if (move.Date == DateTime.MinValue)
                continue;
            if (!InPeriod(move.Date, from, to))
                continue;
            withBalance.Add(new ProductAnalyticsMovement
            {
                Date = move.Date,
                DocumentNumber = move.DocumentNumber,
                MovementType = move.MovementType,
                QuantityIn = move.QuantityIn,
                QuantityOut = move.QuantityOut,
                UnitPrice = move.UnitPrice,
                Amount = move.Amount,
                WarehouseName = move.WarehouseName,
                BalanceAfter = balance
            });
        }

        var currentQty = warehouseId.HasValue
            ? await context.WarehouseStocks.Where(s => s.ProductId == productId && s.WarehouseId == warehouseId).SumAsync(s => (decimal?)s.Quantity, cancellationToken) ?? 0
            : await context.WarehouseStocks.Where(s => s.ProductId == productId).SumAsync(s => (decimal?)s.Quantity, cancellationToken) ?? 0;
        var balanceOk = Math.Abs(balance - currentQty) < 0.001m;
        if (!balanceOk)
        {
            foreach (var movement in withBalance)
                movement.BalanceAfter = null;
        }

        return new ProductAnalyticsDetail
        {
            ProductId = productId,
            ProductName = product?.Name ?? row?.ProductName ?? "—",
            Code = productId.ToString(),
            Barcode = string.IsNullOrWhiteSpace(product?.Barcode) ? "—" : product!.Barcode!,
            CategoryName = product?.Category ?? "—",
            UnitName = string.IsNullOrWhiteSpace(unit) ? "—" : unit,
            StockQuantity = currentQty,
            SoldQuantity = row?.SoldQuantity ?? 0,
            NetQuantitySold = row?.NetQuantitySold ?? 0,
            SalesAmount = row?.SalesAmount ?? 0,
            PurchaseValue = row?.PurchaseValue ?? 0,
            ReturnValue = row?.ReturnValue ?? 0,
            GrossProfit = row?.GrossProfit ?? 0,
            MarginPercent = row?.MarginPercent ?? 0,
            ProfitNote = report.ProfitNote,
            BalanceAvailable = balanceOk,
            BalanceNote = balanceOk
                ? null
                : "الرصيد بعد الحركة غير معروض لأن إعادة بناء الحركات لا تطابق رصيد المخزن الحالي.",
            Trend = report.Trend,
            Movements = withBalance.OrderByDescending(m => m.Date).ToList()
        };
    }

    private static async Task<List<ProductAnalyticsProduct>> LoadProductsAsync(
        AppDbContext context,
        ProductAnalyticsQuery query,
        CancellationToken cancellationToken)
    {
        var products = context.Products.AsNoTracking().AsQueryable();
        if (query.ProductId is int productId)
            products = products.Where(p => p.Id == productId);
        if (query.CategoryId is int categoryId)
            products = products.Where(p => p.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            var code = int.TryParse(search, out var parsedId) ? parsedId : (int?)null;
            products = products.Where(p =>
                p.Name.Contains(search)
                || (p.Barcode != null && p.Barcode.Contains(search))
                || (code != null && p.Id == code));
        }

        if (query.SupplierId is int supplierId)
        {
            var supplierProductIds = context.InvoiceItems.AsNoTracking()
                .Where(ii => ii.ProductId != null
                             && ii.Invoice.SupplierId == supplierId
                             && (ii.Invoice.InvoiceType == InvoiceType.Purchase
                                 || ii.Invoice.InvoiceType == InvoiceType.PurchaseReturn)
                             && (ii.Invoice.Notes == null
                                 || !ii.Invoice.Notes.StartsWith(OpeningCreditBalanceMarkers.NotesPrefix)))
                .Select(ii => ii.ProductId!.Value);
            products = products.Where(p => supplierProductIds.Contains(p.Id));
        }

        var rows = await products
            .OrderBy(p => p.Name)
            .Select(p => new ProductAnalyticsProduct
            {
                Id = p.Id,
                Name = p.Name,
                Barcode = p.Barcode,
                CategoryName = p.Category.Name
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var ids = rows.Select(p => p.Id).ToHashSet();
        var units = await context.ProductUnits.AsNoTracking()
            .Where(u => u.IsDefault)
            .Select(u => new { u.ProductId, u.UnitName })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        units = units.Where(u => ids.Contains(u.ProductId)).ToList();
        var unitMap = units.GroupBy(u => u.ProductId).ToDictionary(g => g.Key, g => g.First().UnitName);
        return rows.Select(p => new ProductAnalyticsProduct
        {
            Id = p.Id,
            Name = p.Name,
            Barcode = p.Barcode,
            CategoryName = p.CategoryName,
            UnitName = unitMap.GetValueOrDefault(p.Id) ?? "—"
        }).ToList();
    }

    private static async Task<List<ProductAnalyticsSourceLine>> LoadLinesAsync(
        AppDbContext context,
        ProductAnalyticsQuery query,
        IReadOnlyCollection<int> productIds,
        DateTime? rangeStart,
        bool includeDamage,
        CancellationToken cancellationToken)
    {
        var openingInvoiceIds = (await context.InstallmentPlans.AsNoTracking()
            .Where(p => p.InstallmentType == InstallmentType.OpeningBalance)
            .Select(p => p.InvoiceId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false)).ToHashSet();

        var lines = context.InvoiceItems.AsNoTracking()
            .Where(ii => ii.ProductId != null
                         && ii.Invoice.HoldStatus != InvoiceHoldStatus.Held
                         && ii.Invoice.InvoiceType != InvoiceType.Quote
                         && (ii.Invoice.Notes == null
                             || !ii.Invoice.Notes.StartsWith(OpeningCreditBalanceMarkers.NotesPrefix)));

        if (query.ProductId is int onlyProduct)
            lines = lines.Where(ii => ii.ProductId == onlyProduct);
        else if (productIds.Count <= 500)
            lines = lines.Where(ii => productIds.Contains(ii.ProductId!.Value));

        lines = includeDamage
            ? lines.Where(ii => MovementTypes.Contains(ii.Invoice.InvoiceType))
            : lines.Where(ii => ii.Invoice.InvoiceType == InvoiceType.Sale
                                || ii.Invoice.InvoiceType == InvoiceType.Installment
                                || ii.Invoice.InvoiceType == InvoiceType.SaleReturn
                                || ii.Invoice.InvoiceType == InvoiceType.Purchase
                                || ii.Invoice.InvoiceType == InvoiceType.PurchaseReturn);

        if (rangeStart.HasValue)
            lines = lines.Where(ii => ii.Invoice.Date >= rangeStart.Value);
        if (query.To is DateTime to)
            lines = lines.Where(ii => ii.Invoice.Date < to.Date.AddDays(1));
        if (query.WarehouseId is int warehouseId)
            lines = lines.Where(ii => (ii.WarehouseId ?? ii.Invoice.WarehouseId) == warehouseId);

        var loaded = await lines.Select(ii => new ProductAnalyticsSourceLine
        {
            ProductId = ii.ProductId!.Value,
            InvoiceId = ii.InvoiceId,
            InvoiceNumber = ii.Invoice.InvoiceNumber,
            Type = ii.Invoice.InvoiceType,
            Date = ii.Invoice.Date,
            Quantity = ii.Quantity,
            UnitPrice = ii.UnitPrice,
            LineTotal = ii.TotalPrice,
            HeaderDiscount = ii.Invoice.DiscountAmount,
            Currency = ii.Invoice.Currency,
            FxRate = ii.Invoice.FxRate,
            WarehouseId = ii.WarehouseId ?? ii.Invoice.WarehouseId,
            WarehouseName = ii.Warehouse != null ? ii.Warehouse.Name : ii.Invoice.Warehouse.Name
        }).ToListAsync(cancellationToken).ConfigureAwait(false);

        if (openingInvoiceIds.Count > 0)
            loaded = loaded.Where(l => !openingInvoiceIds.Contains(l.InvoiceId)).ToList();
        if (productIds.Count > 500)
        {
            var allowed = productIds as HashSet<int> ?? productIds.ToHashSet();
            loaded = loaded.Where(l => allowed.Contains(l.ProductId)).ToList();
        }

        return loaded;
    }

    private static async Task FillInvoiceTotalsAsync(
        AppDbContext context,
        List<ProductAnalyticsSourceLine> lines,
        CancellationToken cancellationToken)
    {
        var invoiceIds = lines.Select(l => l.InvoiceId).Distinct().ToList();
        if (invoiceIds.Count == 0)
            return;

        var totals = await context.InvoiceItems.AsNoTracking()
            .Where(ii => invoiceIds.Contains(ii.InvoiceId))
            .GroupBy(ii => ii.InvoiceId)
            .Select(g => new { Id = g.Key, Total = g.Sum(x => x.TotalPrice) })
            .ToListAsync(cancellationToken);
        var map = totals.ToDictionary(x => x.Id, x => x.Total);
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            lines[i] = new ProductAnalyticsSourceLine
            {
                ProductId = line.ProductId,
                InvoiceId = line.InvoiceId,
                InvoiceNumber = line.InvoiceNumber,
                Type = line.Type,
                Date = line.Date,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineTotal = line.LineTotal,
                HeaderDiscount = line.HeaderDiscount,
                InvoiceLinesTotal = map.GetValueOrDefault(line.InvoiceId),
                Currency = line.Currency,
                FxRate = line.FxRate,
                WarehouseId = line.WarehouseId,
                WarehouseName = line.WarehouseName
            };
        }
    }

    private static DateTime? ComparisonStart(ProductAnalyticsQuery query)
    {
        if (query.From is not DateTime from || query.To is not DateTime to)
            return query.From?.Date;
        var days = (to.Date.AddDays(1) - from.Date).Days;
        if (days <= 0)
            return from.Date;
        return from.Date.AddDays(-days);
    }

    private static bool InPeriod(DateTime date, DateTime? from, DateTime? to) =>
        (!from.HasValue || date >= from.Value.Date) && (!to.HasValue || date < to.Value.Date.AddDays(1));

    private static string TypeLabel(InvoiceType type) => type switch
    {
        InvoiceType.Sale => "مبيعات",
        InvoiceType.Installment => "أقساط",
        InvoiceType.SaleReturn => "مرتجع مبيعات",
        InvoiceType.Purchase => "مشتريات",
        InvoiceType.PurchaseReturn => "مرتجع مشتريات",
        InvoiceType.Damage => "تلف",
        _ => type.ToString()
    };

    private sealed record ReplayMove(
        DateTime Date,
        int Order,
        string DocumentNumber,
        string MovementType,
        decimal QuantityIn,
        decimal QuantityOut,
        decimal? UnitPrice,
        decimal? Amount,
        string WarehouseName,
        int WarehouseId);
}
