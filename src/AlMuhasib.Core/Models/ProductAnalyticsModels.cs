using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Core.Models;

public enum ProductAnalyticsActivity
{
    All,
    WithMovement,
    NoSales,
    Stagnant
}

/// <summary>سطر فاتورة جاهز للتجميع. كمية المخزن بعد تحويل الوحدة، وقيمة السطر بعد خصم السطر فقط.</summary>
public sealed class ProductAnalyticsSourceLine
{
    public int ProductId { get; init; }
    public int InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public InvoiceType Type { get; init; }
    public DateTime Date { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
    public decimal HeaderDiscount { get; init; }
    /// <summary>مجموع بنود الفاتورة كاملة، حتى لو كان التقرير مفلتراً على صنف واحد.</summary>
    public decimal InvoiceLinesTotal { get; init; }
    public AccountingCurrency Currency { get; init; }
    public decimal FxRate { get; init; }
    public int WarehouseId { get; init; }
    public string WarehouseName { get; init; } = string.Empty;
}

public sealed class ProductAnalyticsProduct
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Barcode { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string UnitName { get; init; } = "—";
}

public sealed class ProductAnalyticsStock
{
    public int ProductId { get; init; }
    public int WarehouseId { get; init; }
    public decimal Quantity { get; init; }
    public decimal MinQuantity { get; init; }
}

public sealed class ProductAnalyticsQuery
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int? WarehouseId { get; init; }
    public int? CategoryId { get; init; }
    public int? ProductId { get; init; }
    public int? SupplierId { get; init; }
    public string? Search { get; init; }
    public ProductAnalyticsActivity Activity { get; init; } = ProductAnalyticsActivity.All;
    public int TopCount { get; init; } = 10;
}

public sealed class ProductAnalyticsReportResult
{
    public int ProductCount { get; set; }
    public decimal GrossQuantitySold { get; set; }
    public decimal NetQuantitySold { get; set; }
    public decimal ReturnQuantity { get; set; }
    public decimal NetSales { get; set; }
    public decimal PurchaseValue { get; set; }
    public decimal PurchaseQuantity { get; set; }
    public decimal Cogs { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal ReturnValue { get; set; }
    public decimal StockValue { get; set; }
    public string TopSellerName { get; set; } = "—";
    public decimal TopSellerAmount { get; set; }
    public int StagnantCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int LowStockCount { get; set; }
    public int MissingCostCount { get; set; }
    public bool HasPreviousPeriod { get; set; }
    public decimal? SalesChangePercent { get; set; }
    public decimal? QuantityChangePercent { get; set; }
    public decimal? PurchaseChangePercent { get; set; }
    public decimal? ProfitChangePercent { get; set; }
    public decimal? ReturnChangePercent { get; set; }
    public decimal? StagnantChangePercent { get; set; }
    public string CurrencyNote { get; set; } = string.Empty;
    public string ProfitNote { get; set; } = string.Empty;
    public string? StockAgeNote { get; set; }
    public List<ProductAnalyticsRow> Rows { get; set; } = [];
    public List<ProductAnalyticsTrendPoint> Trend { get; set; } = [];
    public List<NameAmountPoint> CategoryByAmount { get; set; } = [];
    public List<NameAmountPoint> CategoryByQuantity { get; set; } = [];
    public List<ProductAnalyticsCategoryStat> Categories { get; set; } = [];
    public List<NameAmountPoint> StockStatus { get; set; } = [];
    public List<ProductAnalyticsInsightGroup> Insights { get; set; } = [];
}

public sealed class ProductAnalyticsRow
{
    public int ProductId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Barcode { get; set; } = "—";
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string UnitName { get; set; } = "—";
    public decimal PurchasedQuantity { get; set; }
    public decimal PurchaseValue { get; set; }
    public decimal SoldQuantity { get; set; }
    public decimal ReturnQuantity { get; set; }
    public decimal ReturnValue { get; set; }
    public decimal NetQuantitySold { get; set; }
    public decimal? AveragePurchasePrice { get; set; }
    public decimal? AverageSalePrice { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal Cogs { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal MinQuantity { get; set; }
    public bool IsLowStock { get; set; }
    public decimal StockValue { get; set; }
    public bool CostMissing { get; set; }
    public decimal PreviousSalesAmount { get; set; }
    public decimal PreviousSoldQuantity { get; set; }
    public decimal PreviousNetQuantity { get; set; }
    public decimal PreviousPurchaseValue { get; set; }
    public decimal PreviousReturnValue { get; set; }
    public decimal PreviousCogs { get; set; }

    public string AveragePurchasePriceDisplay => AveragePurchasePrice is null ? "—" : AveragePurchasePrice.Value.ToString("N0");
    public string AverageSalePriceDisplay => AverageSalePrice is null ? "—" : AverageSalePrice.Value.ToString("N0");
}

public sealed class ProductAnalyticsTrendPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal SalesAmount { get; set; }
    public decimal SalesQuantity { get; set; }
    public decimal PreviousSalesAmount { get; set; }
    public decimal PreviousSalesQuantity { get; set; }
    public decimal PurchaseAmount { get; set; }
}

public sealed class ProductAnalyticsInsightGroup
{
    public string Title { get; set; } = string.Empty;
    public List<string> Lines { get; set; } = [];
}

public sealed class ProductAnalyticsCategoryStat
{
    public string Name { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal NetQuantity { get; set; }
    public decimal PurchaseValue { get; set; }
    public decimal ReturnValue { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal StockValue { get; set; }
    public int StagnantCount { get; set; }
}

public sealed class ProductAnalyticsDetail
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Barcode { get; set; } = "—";
    public string CategoryName { get; set; } = string.Empty;
    public string UnitName { get; set; } = "—";
    public decimal StockQuantity { get; set; }
    public decimal SoldQuantity { get; set; }
    public decimal NetQuantitySold { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal PurchaseValue { get; set; }
    public decimal ReturnValue { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPercent { get; set; }
    public string ProfitNote { get; set; } = string.Empty;
    public bool BalanceAvailable { get; set; }
    public string? BalanceNote { get; set; }
    public List<ProductAnalyticsTrendPoint> Trend { get; set; } = [];
    public List<ProductAnalyticsMovement> Movements { get; set; } = [];
}

public sealed class ProductAnalyticsMovement
{
    public DateTime Date { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? Amount { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal? BalanceAfter { get; set; }

    public string UnitPriceDisplay => UnitPrice is null ? "—" : UnitPrice.Value.ToString("N0");
    public string AmountDisplay => Amount is null ? "—" : Amount.Value.ToString("N0");
    public string BalanceDisplay => BalanceAfter is null ? "—" : BalanceAfter.Value.ToString("N2");
}

public static class ProductAnalyticsAggregator
{
    public static ProductAnalyticsReportResult Build(
        ProductAnalyticsQuery query,
        IReadOnlyList<ProductAnalyticsProduct> products,
        IReadOnlyList<ProductAnalyticsSourceLine> lines,
        IReadOnlyDictionary<int, decimal> averageUnitCost,
        IReadOnlyList<ProductAnalyticsStock> stocks,
        bool foldUsd,
        bool multiCurrencyEnabled)
    {
        var from = query.From?.Date;
        var toExclusive = query.To?.Date.AddDays(1);
        var hasPrevious = from.HasValue && toExclusive.HasValue && toExclusive > from;
        DateTime? previousFrom = null;
        DateTime? previousTo = null;
        if (hasPrevious)
        {
            var days = (toExclusive!.Value - from!.Value).Days;
            previousTo = from;
            previousFrom = from.Value.AddDays(-days);
        }

        var stockByProduct = stocks
            .GroupBy(s => s.ProductId)
            .ToDictionary(g => g.Key, g => (
                Qty: g.Sum(x => x.Quantity),
                Min: g.Sum(x => x.MinQuantity),
                Low: g.Any(x => x.MinQuantity > 0 && x.Quantity > 0 && x.Quantity <= x.MinQuantity)));

        var current = new Dictionary<int, Acc>();
        var previous = new Dictionary<int, Acc>();
        var trendCurrent = new Dictionary<string, TrendAcc>();
        var trendPrevious = new Dictionary<string, TrendAcc>();
        var monthly = (toExclusive - from)?.TotalDays > 62;

        foreach (var line in lines)
        {
            if (!foldUsd && line.Currency != AccountingCurrency.IQD)
                continue;

            var when = line.Date;
            var inCurrent = IsInRange(when, from, toExclusive);
            var inPrevious = hasPrevious && IsInRange(when, previousFrom, previousTo);
            if (!inCurrent && !inPrevious)
                continue;

            var amount = AllocatedAmountInIqd(line, foldUsd);
            var bucket = inCurrent ? current : previous;
            if (!bucket.TryGetValue(line.ProductId, out var acc))
            {
                acc = new Acc();
                bucket[line.ProductId] = acc;
            }
            Apply(acc, line.Type, Math.Abs(line.Quantity), amount);

            var key = monthly ? when.ToString("yyyy/MM") : when.ToString("yyyy/MM/dd");
            var series = inCurrent ? trendCurrent : trendPrevious;
            if (!series.TryGetValue(key, out var trend))
            {
                trend = new TrendAcc();
                series[key] = trend;
            }
            if (IsSale(line.Type))
            {
                var signedQty = line.Type == InvoiceType.SaleReturn ? -Math.Abs(line.Quantity) : Math.Abs(line.Quantity);
                trend.SalesAmount += amount;
                trend.SalesQuantity += signedQty;
            }
            else if (IsPurchase(line.Type))
            {
                trend.PurchaseAmount += amount;
            }
        }

        var rows = new List<ProductAnalyticsRow>();
        foreach (var product in products)
        {
            current.TryGetValue(product.Id, out var now);
            previous.TryGetValue(product.Id, out var before);
            now ??= new Acc();
            before ??= new Acc();
            stockByProduct.TryGetValue(product.Id, out var stock);
            var avg = averageUnitCost.GetValueOrDefault(product.Id);
            var netQty = now.SoldQuantity - now.ReturnQuantity;
            var costMissing = netQty != 0 && avg <= 0;
            var cogs = Math.Round(netQty * avg, 0);
            var profit = now.SalesAmount - cogs;
            var margin = now.SalesAmount != 0 ? Math.Round(profit / now.SalesAmount * 100m, 1) : 0m;
            var stagnant = stock.Qty > 0 && now.SoldQuantity == 0 && now.ReturnQuantity == 0;

            if (!MatchesActivity(query.Activity, now, stagnant))
                continue;

            var previousNetQty = before.SoldQuantity - before.ReturnQuantity;
            rows.Add(new ProductAnalyticsRow
            {
                ProductId = product.Id,
                Code = product.Id.ToString(),
                Barcode = string.IsNullOrWhiteSpace(product.Barcode) ? "—" : product.Barcode,
                ProductName = product.Name,
                CategoryName = string.IsNullOrWhiteSpace(product.CategoryName) ? "—" : product.CategoryName,
                UnitName = string.IsNullOrWhiteSpace(product.UnitName) ? "—" : product.UnitName,
                PurchasedQuantity = now.PurchasedQuantity,
                PurchaseValue = now.PurchaseAmount,
                SoldQuantity = now.SoldQuantity,
                ReturnQuantity = now.ReturnQuantity,
                ReturnValue = now.ReturnAmount,
                NetQuantitySold = netQty,
                AveragePurchasePrice = now.PurchasedQuantity != 0 ? Math.Round(now.PurchaseAmount / now.PurchasedQuantity, 0) : null,
                AverageSalePrice = netQty != 0 ? Math.Round(now.SalesAmount / netQty, 0) : null,
                SalesAmount = now.SalesAmount,
                Cogs = cogs,
                GrossProfit = profit,
                MarginPercent = margin,
                StockQuantity = stock.Qty,
                MinQuantity = stock.Min,
                IsLowStock = stock.Low,
                StockValue = Math.Round(Math.Max(0, stock.Qty) * avg, 0),
                CostMissing = costMissing,
                PreviousSalesAmount = before.SalesAmount,
                PreviousSoldQuantity = before.SoldQuantity,
                PreviousNetQuantity = previousNetQty,
                PreviousPurchaseValue = before.PurchaseAmount,
                PreviousReturnValue = before.ReturnAmount,
                PreviousCogs = Math.Round(previousNetQty * avg, 0)
            });
        }

        rows = rows.OrderByDescending(r => r.SalesAmount).ThenBy(r => r.ProductName).ToList();
        var result = Summarize(rows, hasPrevious, foldUsd, multiCurrencyEnabled);
        result.Rows = rows;
        result.Trend = BuildTrend(trendCurrent, trendPrevious);
        result.CategoryByAmount = rows
            .GroupBy(r => r.CategoryName)
            .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(x => x.SalesAmount) })
            .Where(p => p.Amount != 0)
            .OrderByDescending(p => p.Amount)
            .ToList();
        result.CategoryByQuantity = rows
            .GroupBy(r => r.CategoryName)
            .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(x => x.NetQuantitySold) })
            .Where(p => p.Amount != 0)
            .OrderByDescending(p => p.Amount)
            .ToList();
        result.Categories = BuildCategories(rows);
        result.StockStatus = BuildStockStatus(rows);
        result.Insights = BuildInsights(rows, query, hasPrevious);
        result.StockAgeNote = "متوسط مدة بقاء المنتج غير معروض: تاريخ دخول الرصيد الافتتاحي غير مسجل، والحساب بدونه يكون تقديرياً.";
        return result;
    }

    public static decimal AllocatedAmountInIqd(ProductAnalyticsSourceLine line, bool foldUsd)
    {
        var net = line.LineTotal;
        var basis = line.InvoiceLinesTotal > 0 ? line.InvoiceLinesTotal : line.LineTotal;
        if (line.HeaderDiscount > 0 && basis > 0)
            net -= line.HeaderDiscount * (line.LineTotal / basis);
        if (net < 0)
            net = 0;

        var signed = line.Type is InvoiceType.SaleReturn or InvoiceType.PurchaseReturn ? -net : net;
        return FinancialReportIqdFoldIn.AmountInBaseIqd(signed, line.Currency, line.FxRate, foldUsd);
    }

    private static ProductAnalyticsReportResult Summarize(
        List<ProductAnalyticsRow> rows,
        bool hasPrevious,
        bool foldUsd,
        bool multiCurrencyEnabled)
    {
        var netSales = rows.Sum(r => r.SalesAmount);
        var profit = rows.Sum(r => r.GrossProfit);
        var purchases = rows.Sum(r => r.PurchaseValue);
        var returns = rows.Sum(r => r.ReturnValue);
        var top = rows.OrderByDescending(r => r.SalesAmount).FirstOrDefault(r => r.SalesAmount != 0);
        var stagnant = rows.Count(r => r.StockQuantity > 0 && !r.IsLowStock && r.SoldQuantity == 0 && r.ReturnQuantity == 0);
        var previousSales = rows.Sum(r => r.PreviousSalesAmount);
        var previousSold = rows.Sum(r => r.PreviousSoldQuantity);
        var previousPurchases = rows.Sum(r => r.PreviousPurchaseValue);
        var previousReturns = rows.Sum(r => r.PreviousReturnValue);
        var previousProfit = rows.Sum(r => r.PreviousSalesAmount - r.PreviousCogs);
        var previousStagnant = rows.Count(r => r.StockQuantity > 0 && !r.IsLowStock && r.PreviousSoldQuantity == 0 && r.PreviousReturnValue == 0);
        return new ProductAnalyticsReportResult
        {
            ProductCount = rows.Count,
            GrossQuantitySold = rows.Sum(r => r.SoldQuantity),
            NetQuantitySold = rows.Sum(r => r.NetQuantitySold),
            ReturnQuantity = rows.Sum(r => r.ReturnQuantity),
            NetSales = netSales,
            PurchaseValue = purchases,
            PurchaseQuantity = rows.Sum(r => r.PurchasedQuantity),
            Cogs = rows.Sum(r => r.Cogs),
            GrossProfit = profit,
            MarginPercent = netSales != 0 ? Math.Round(profit / netSales * 100m, 1) : 0,
            ReturnValue = returns,
            StockValue = rows.Sum(r => r.StockValue),
            TopSellerName = top?.ProductName ?? "—",
            TopSellerAmount = top?.SalesAmount ?? 0,
            StagnantCount = stagnant,
            OutOfStockCount = rows.Count(r => r.StockQuantity <= 0),
            LowStockCount = rows.Count(r => r.IsLowStock),
            MissingCostCount = rows.Count(r => r.CostMissing),
            HasPreviousPeriod = hasPrevious,
            SalesChangePercent = hasPrevious ? Change(netSales, previousSales) : null,
            QuantityChangePercent = hasPrevious ? Change(rows.Sum(r => r.SoldQuantity), previousSold) : null,
            PurchaseChangePercent = hasPrevious ? Change(purchases, previousPurchases) : null,
            ProfitChangePercent = hasPrevious ? Change(profit, previousProfit) : null,
            ReturnChangePercent = hasPrevious ? Change(returns, previousReturns) : null,
            StagnantChangePercent = hasPrevious ? Change(stagnant, previousStagnant) : null,
            CurrencyNote = multiCurrencyEnabled && foldUsd
                ? "المبالغ بالدينار. فواتير الدولار مُحوَّلة بسعر صرف الفاتورة، وليست ربحاً نقدياً من الصندوق."
                : "المبالغ بالدينار فقط. فواتير الدولار غير داخلة لأن تعدد العملات غير مفعّل.",
            ProfitNote = "مجمل الربح = صافي المبيعات بعد المرتجعات وخصم السطر وخصم الفاتورة − (صافي الكمية × متوسط تكلفة النظام). أجور النقل ورسوم الشركة ومصاريف المشتريات وسندات القبض والصرف غير داخلة."
        };
    }

    private static List<ProductAnalyticsTrendPoint> BuildTrend(
        Dictionary<string, TrendAcc> current,
        Dictionary<string, TrendAcc> previous)
    {
        var currentKeys = current.Keys.OrderBy(k => k).ToList();
        var previousKeys = previous.Keys.OrderBy(k => k).ToList();
        var points = new List<ProductAnalyticsTrendPoint>();
        for (var i = 0; i < currentKeys.Count; i++)
        {
            var key = currentKeys[i];
            var now = current[key];
            var before = i < previousKeys.Count ? previous[previousKeys[i]] : new TrendAcc();
            points.Add(new ProductAnalyticsTrendPoint
            {
                Label = key,
                SalesAmount = now.SalesAmount,
                SalesQuantity = now.SalesQuantity,
                PreviousSalesAmount = before.SalesAmount,
                PreviousSalesQuantity = before.SalesQuantity,
                PurchaseAmount = now.PurchaseAmount
            });
        }
        return points;
    }

    private static List<ProductAnalyticsCategoryStat> BuildCategories(List<ProductAnalyticsRow> rows) =>
        rows.GroupBy(r => string.IsNullOrWhiteSpace(r.CategoryName) ? "—" : r.CategoryName)
            .Select(g =>
            {
                var sales = g.Sum(x => x.SalesAmount);
                var profit = g.Sum(x => x.GrossProfit);
                return new ProductAnalyticsCategoryStat
                {
                    Name = g.Key,
                    ProductCount = g.Count(),
                    SalesAmount = sales,
                    NetQuantity = g.Sum(x => x.NetQuantitySold),
                    PurchaseValue = g.Sum(x => x.PurchaseValue),
                    ReturnValue = g.Sum(x => x.ReturnValue),
                    GrossProfit = profit,
                    MarginPercent = sales != 0 ? Math.Round(profit / sales * 100m, 1) : 0,
                    StockValue = g.Sum(x => x.StockValue),
                    StagnantCount = g.Count(x => x.StockQuantity > 0 && !x.IsLowStock && x.SoldQuantity == 0 && x.ReturnQuantity == 0)
                };
            })
            .OrderByDescending(c => c.SalesAmount)
            .ThenBy(c => c.Name)
            .ToList();

    private static List<NameAmountPoint> BuildStockStatus(List<ProductAnalyticsRow> rows)
    {
        var available = 0;
        var low = 0;
        var empty = 0;
        var stagnant = 0;
        foreach (var row in rows)
        {
            if (row.StockQuantity <= 0)
                empty++;
            else if (row.IsLowStock)
                low++;
            else if (row.SoldQuantity == 0 && row.ReturnQuantity == 0)
                stagnant++;
            else
                available++;
        }

        return
        [
            new() { Name = "متوفر", Amount = available },
            new() { Name = "منخفض", Amount = low },
            new() { Name = "نافد", Amount = empty },
            new() { Name = "راكد خلال الفترة", Amount = stagnant }
        ];
    }

    private static List<ProductAnalyticsInsightGroup> BuildInsights(
        List<ProductAnalyticsRow> rows,
        ProductAnalyticsQuery query,
        bool hasPrevious)
    {
        var top = Math.Max(1, query.TopCount);
        string Line(ProductAnalyticsRow row, string metric) => $"{row.ProductName} — {metric}";
        var sold = rows.Where(r => r.NetQuantitySold > 0).ToList();
        var withSales = rows.Where(r => r.SalesAmount > 0).ToList();
        var median = Median(withSales.Select(r => r.SalesAmount).ToList());
        var overallMargin = rows.Sum(r => r.SalesAmount) != 0
            ? rows.Sum(r => r.GrossProfit) / rows.Sum(r => r.SalesAmount) * 100m
            : 0;

        var groups = new List<ProductAnalyticsInsightGroup>
        {
            Group("الأعلى كمية", sold.OrderByDescending(r => r.NetQuantitySold).Take(top)
                .Select(r => Line(r, $"{r.NetQuantitySold:N0}")).ToList()),
            Group("الأعلى مبيعاً", withSales.OrderByDescending(r => r.SalesAmount).Take(top)
                .Select(r => Line(r, $"{r.SalesAmount:N0}")).ToList()),
            Group("الأقل مبيعاً", sold.OrderBy(r => r.NetQuantitySold).Take(top)
                .Select(r => Line(r, $"{r.NetQuantitySold:N0}")).ToList()),
            Group("بدون مبيعات خلال الفترة", rows.Where(r => r.SoldQuantity == 0 && r.ReturnQuantity == 0).Take(top)
                .Select(r => r.ProductName).ToList(), $"{rows.Count(r => r.SoldQuantity == 0 && r.ReturnQuantity == 0):N0} منتجاً"),
            Group("الأعلى ربحاً", rows.OrderByDescending(r => r.GrossProfit).Take(top)
                .Select(r => Line(r, $"{r.GrossProfit:N0}")).ToList()),
            Group("الأعلى هامشاً", withSales.Where(r => r.NetQuantitySold > 0).OrderByDescending(r => r.MarginPercent).Take(top)
                .Select(r => Line(r, $"{r.MarginPercent:N1}%")).ToList()),
            Group("مبيعات مرتفعة وهامش منخفض", withSales
                .Where(r => r.SalesAmount >= median && r.MarginPercent < overallMargin && r.MarginPercent < 15)
                .OrderBy(r => r.MarginPercent).Take(top)
                .Select(r => Line(r, $"{r.MarginPercent:N1}%")).ToList()),
            Group("ربح سالب", rows.Where(r => r.GrossProfit < 0).OrderBy(r => r.GrossProfit).Take(top)
                .Select(r => Line(r, $"{r.GrossProfit:N0}")).ToList())
        };

        groups.Add(Group("وصلت حد إعادة الطلب", rows.Where(r => r.IsLowStock).OrderBy(r => r.StockQuantity).Take(top)
            .Select(r => Line(r, $"الرصيد {r.StockQuantity:N0}")).ToList()));
        groups.Add(Group("نافدة", rows.Where(r => r.StockQuantity <= 0).Take(top)
            .Select(r => r.ProductName).ToList(), $"{rows.Count(r => r.StockQuantity <= 0):N0} منتجاً"));
        groups.Add(Group("مخزون مرتفع", rows
            .Where(r => r.MinQuantity > 0 && r.StockQuantity >= r.MinQuantity * 3m)
            .OrderByDescending(r => r.StockQuantity).Take(top)
            .Select(r => Line(r, $"الرصيد {r.StockQuantity:N0}")).ToList()));
        groups.Add(Group("راكدة خلال الفترة", rows
            .Where(r => r.StockQuantity > 0 && !r.IsLowStock && r.SoldQuantity == 0 && r.ReturnQuantity == 0).Take(top)
            .Select(r => Line(r, $"الرصيد {r.StockQuantity:N0}")).ToList()));
        groups.Add(Group("قيمة المخزون حسب التصنيف", rows
            .GroupBy(r => r.CategoryName)
            .Select(g => (Name: g.Key, Value: g.Sum(x => x.StockValue)))
            .Where(x => x.Value > 0)
            .OrderByDescending(x => x.Value).Take(top)
            .Select(x => $"{x.Name} — {x.Value:N0}").ToList()));

        if (!hasPrevious)
        {
            groups.Add(Group("اتجاه المبيعات", [], "حدد من تاريخ وإلى تاريخ حتى تُحسب المقارنة مع فترة مساوية سابقة."));
        }
        else
        {
            groups.Add(Group("مبيعاتها ترتفع", rows
                .Where(r => r.PreviousSalesAmount > 0 && r.SalesAmount > r.PreviousSalesAmount * 1.15m)
                .OrderByDescending(r => r.SalesAmount - r.PreviousSalesAmount).Take(top)
                .Select(r => Line(r, $"+{r.SalesAmount - r.PreviousSalesAmount:N0}")).ToList()));
            groups.Add(Group("مبيعاتها تنخفض", rows
                .Where(r => r.PreviousSalesAmount > 0 && r.SalesAmount < r.PreviousSalesAmount * 0.85m)
                .OrderBy(r => r.SalesAmount - r.PreviousSalesAmount).Take(top)
                .Select(r => Line(r, $"{r.SalesAmount - r.PreviousSalesAmount:N0}")).ToList()));
        }

        return groups.Where(g => g.Lines.Count > 0 || g.Title.Contains("اتجاه")).ToList();
    }

    private static ProductAnalyticsInsightGroup Group(string title, IEnumerable<string> lines, string? empty = null)
    {
        var list = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (list.Count == 0 && !string.IsNullOrWhiteSpace(empty))
            list.Add(empty);
        if (list.Count == 0)
            list.Add("لا توجد بيانات كافية.");
        return new ProductAnalyticsInsightGroup { Title = title, Lines = list };
    }

    private static bool MatchesActivity(ProductAnalyticsActivity activity, Acc now, bool stagnant) => activity switch
    {
        ProductAnalyticsActivity.WithMovement => now.SoldQuantity != 0 || now.ReturnQuantity != 0 || now.PurchasedQuantity != 0,
        ProductAnalyticsActivity.NoSales => now.SoldQuantity == 0 && now.ReturnQuantity == 0,
        ProductAnalyticsActivity.Stagnant => stagnant,
        _ => true
    };

    private static void Apply(Acc acc, InvoiceType type, decimal quantity, decimal signedAmount)
    {
        switch (type)
        {
            case InvoiceType.Sale:
            case InvoiceType.Installment:
                acc.SoldQuantity += quantity;
                acc.SalesAmount += signedAmount;
                break;
            case InvoiceType.SaleReturn:
                acc.ReturnQuantity += quantity;
                acc.SalesAmount += signedAmount;
                acc.ReturnAmount += -signedAmount;
                break;
            case InvoiceType.Purchase:
                acc.PurchasedQuantity += quantity;
                acc.PurchaseAmount += signedAmount;
                break;
            case InvoiceType.PurchaseReturn:
                acc.PurchasedQuantity -= quantity;
                acc.PurchaseAmount += signedAmount;
                break;
        }
    }

    private static bool IsSale(InvoiceType type) =>
        type is InvoiceType.Sale or InvoiceType.Installment or InvoiceType.SaleReturn;

    private static bool IsPurchase(InvoiceType type) =>
        type is InvoiceType.Purchase or InvoiceType.PurchaseReturn;

    private static bool IsInRange(DateTime date, DateTime? from, DateTime? toExclusive) =>
        (!from.HasValue || date >= from.Value) && (!toExclusive.HasValue || date < toExclusive.Value);

    private static decimal? Change(decimal current, decimal previous)
    {
        if (previous == 0)
            return current == 0 ? 0 : null;
        return Math.Round((current - previous) / Math.Abs(previous) * 100m, 1);
    }

    private static decimal Median(List<decimal> values)
    {
        if (values.Count == 0)
            return 0;
        var ordered = values.OrderBy(v => v).ToList();
        var mid = ordered.Count / 2;
        return ordered.Count % 2 == 1 ? ordered[mid] : (ordered[mid - 1] + ordered[mid]) / 2m;
    }

    private sealed class Acc
    {
        public decimal SoldQuantity { get; set; }
        public decimal ReturnQuantity { get; set; }
        public decimal PurchasedQuantity { get; set; }
        public decimal SalesAmount { get; set; }
        public decimal PurchaseAmount { get; set; }
        public decimal ReturnAmount { get; set; }
    }

    private sealed class TrendAcc
    {
        public decimal SalesAmount { get; set; }
        public decimal SalesQuantity { get; set; }
        public decimal PurchaseAmount { get; set; }
    }
}
