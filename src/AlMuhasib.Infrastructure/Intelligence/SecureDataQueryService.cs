using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Intelligence;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Intelligence;

/// <summary>
/// ينفّذ الخطة عبر تقارير النظام المعتمدة فقط. الاستعلامات للقراءة، وبمعاملات، ومن دون SQL يكتبه المستخدم.
/// </summary>
public sealed class SecureDataQueryService
{
    private readonly IReportService _reports;
    private readonly IDbContextFactory<AppDbContext> _contexts;

    public SecureDataQueryService(IReportService reports, IDbContextFactory<AppDbContext> contexts)
    {
        _reports = reports;
        _contexts = contexts;
    }

    public async Task<AssistantAnswer> ExecuteAsync(QueryPlan plan, CancellationToken cancellationToken)
    {
        var (customerId, customerName, choice) = await ResolveCustomerAsync(plan, cancellationToken);
        if (choice is not null)
            return choice;

        return plan.Intent switch
        {
            AccountingIntent.SalesSummary or AccountingIntent.CustomerPurchases
                => await SalesAsync(plan, customerId, customerName, cancellationToken),
            AccountingIntent.SalesComparison => await SalesComparisonAsync(plan, cancellationToken),
            AccountingIntent.TopCustomers => await TopCustomersAsync(plan, cancellationToken),
            AccountingIntent.CustomerDebt => await CustomerDebtAsync(plan, customerId, cancellationToken),
            AccountingIntent.TopProducts => await TopProductsAsync(plan, cancellationToken),
            AccountingIntent.ExpensesSummary => await ExpensesAsync(plan, cancellationToken),
            AccountingIntent.ExpenseComparison => await ExpenseComparisonAsync(plan, cancellationToken),
            AccountingIntent.CashBalance => await CashAsync(plan, cancellationToken),
            AccountingIntent.PurchasesSummary => await PurchasesAsync(plan, cancellationToken),
            AccountingIntent.ProfitSummary or AccountingIntent.FinancialSummary
                => await ProfitAsync(plan, cancellationToken),
            AccountingIntent.ProfitComparison => await ProfitComparisonAsync(plan, cancellationToken),
            AccountingIntent.LowStock => await LowStockAsync(plan, cancellationToken),
            AccountingIntent.SupplierDebt => await SupplierDebtAsync(plan, cancellationToken),
            AccountingIntent.CustomerCollections => await CollectionsAsync(plan, customerId, cancellationToken),
            AccountingIntent.SupplierPayments => await SupplierPaymentsAsync(plan, cancellationToken),
            _ => ArabicResponseGenerator.Unknown()
        };
    }

    private async Task<AssistantAnswer> SalesAsync(QueryPlan plan, int? customerId, string? customerName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var report = await _reports.GetSalesReportAsync(plan.From, plan.To, customerId, null);
        var returns = report.Rows.Where(row => row.IsReturn && row.Currency == AccountingCurrency.IQD).Sum(row => row.NetAmount);
        var gross = report.Rows.Where(row => !row.IsReturn && row.Currency == AccountingCurrency.IQD).Sum(row => row.NetAmount);
        var snapshot = new SalesSnapshot
        {
            NetIqd = report.TotalSales,
            GrossIqd = gross,
            ReturnsIqd = returns,
            NetUsd = report.TotalSalesUsd,
            CashIqd = report.CashSales,
            CreditIqd = report.CreditSales,
            InstallmentIqd = report.InstallmentSales,
            InvoiceCount = report.InvoiceCount,
            ShowReturnSplit = report.TotalSalesUsd == 0 && ResultAnalysisService.CanSplitReturns(report.TotalSales, gross, returns),
            Daily = report.DailyChart.Select(point => new AssistantChartPoint
            {
                Label = point.Date.ToString("MM/dd"),
                Value = point.Amount
            }).ToList()
        };

        var answer = ArabicResponseGenerator.Sales(plan, snapshot, plan.PeriodLabel);
        if (!string.IsNullOrWhiteSpace(customerName))
        {
            answer.Text = $"مشتريات {customerName} — " + answer.Text;
            answer.PlainText = $"مشتريات {customerName}\n" + answer.PlainText;
        }
        answer.FocusCustomerId = customerId;
        answer.FocusCustomerName = customerName;
        return answer;
    }

    private async Task<AssistantAnswer> SalesComparisonAsync(QueryPlan plan, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var current = await _reports.GetSalesReportAsync(plan.From, plan.To, null, null);
        var previous = await _reports.GetSalesReportAsync(plan.CompareFrom, plan.CompareTo, null, null);
        var chart = ResultAnalysisService.CompareChart(plan.PeriodLabel, current.TotalSales, plan.CompareLabel ?? "السابق", previous.TotalSales);
        return ArabicResponseGenerator.Comparison(plan, "مبيعات", current.TotalSales, previous.TotalSales, chart);
    }

    private async Task<AssistantAnswer> TopCustomersAsync(QueryPlan plan, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var overview = await _reports.GetCustomersOverviewReportAsync(plan.From, plan.To);
        var top = overview.Rows
            .OrderByDescending(row => row.SalesAmount)
            .ThenBy(row => row.CustomerName)
            .Take(plan.TopCount)
            .ToList();
        if (top.Count == 0 || top.All(row => row.SalesAmount == 0))
        {
            return ArabicResponseGenerator.Ranked(
                plan,
                "لا توجد مبيعات على العملاء في هذه الفترة.",
                [new AssistantMetricCard { Label = "عدد العملاء", Value = AssistantMoney.Count(overview.CustomerCount) }],
                [],
                []);
        }

        var headers = new[] { "العميل", "الفواتير", "المبيعات", "الرصيد" };
        var rows = top.Select(row => (IReadOnlyList<string>)
        [
            row.CustomerName,
            AssistantMoney.Count(row.InvoiceCount),
            AssistantMoney.Iqd(row.SalesAmount),
            AssistantMoney.Iqd(row.OutstandingBalance)
        ]).ToList();
        var first = top[0];
        return ArabicResponseGenerator.Ranked(
            plan,
            $"أكثر العملاء شراءً خلال {plan.PeriodLabel} هو {first.CustomerName} بمبيعات {AssistantMoney.Iqd(first.SalesAmount)}.",
            [new AssistantMetricCard { Label = "الأول", Value = first.CustomerName, Hint = AssistantMoney.Iqd(first.SalesAmount) }],
            headers,
            rows,
            top.Select(row => new AssistantChartPoint { Label = row.CustomerName, Value = row.SalesAmount }).ToList(),
            first.CustomerId,
            first.CustomerName);
    }

    private async Task<AssistantAnswer> CustomerDebtAsync(QueryPlan plan, int? customerId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var asOf = plan.AsOf ?? DateTime.Today;
        var aging = await _reports.GetReceivablesAgingReportAsync(asOf, customerId);
        var metrics = new List<AssistantMetricCard>
        {
            new() { Label = "الذمم المدينة", Value = AssistantMoney.Iqd(aging.TotalOutstanding), Hint = "حسب تقرير أعمار الذمم" },
            new() { Label = "عدد العملاء", Value = AssistantMoney.Count(aging.CustomerCount) }
        };
        if (aging.TotalOutstandingUsd != 0)
            metrics.Add(new() { Label = "ذمم الدولار", Value = AssistantMoney.Usd(aging.TotalOutstandingUsd), Hint = "منفصلة عن الدينار" });

        var rows = aging.Buckets.Select(bucket => (IReadOnlyList<string>)
        [
            bucket.BucketName,
            AssistantMoney.Count(bucket.Count),
            AssistantMoney.Iqd(bucket.Amount)
        ]).ToList();
        return ArabicResponseGenerator.Ranked(
            plan,
            $"إجمالي الأرصدة المدينة للعملاء حتى {ArabicTextNormalizer.FormatDate(asOf)}: {AssistantMoney.Iqd(aging.TotalOutstanding)}.",
            metrics,
            rows.Count == 0 ? [] : ["الفئة", "العدد", "المبلغ"],
            rows);
    }

    private async Task<AssistantAnswer> TopProductsAsync(QueryPlan plan, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var report = await _reports.GetTopProductsReportAsync(plan.From, plan.To, null, plan.TopCount, true);
        if (report.Rows.Count == 0)
        {
            return ArabicResponseGenerator.Ranked(plan, "لا توجد مبيعات منتجات في هذه الفترة.", [], [], []);
        }

        var rows = report.Rows.Take(plan.TopCount).Select(row => (IReadOnlyList<string>)
        [
            row.ProductName,
            AssistantMoney.Quantity(row.QuantitySold),
            AssistantMoney.Iqd(row.Revenue),
            AssistantMoney.Percent(row.SharePercent)
        ]).ToList();
        return ArabicResponseGenerator.Ranked(
            plan,
            $"أكثر منتج مبيعاً خلال {plan.PeriodLabel} هو {report.Rows[0].ProductName}.",
            [
                new() { Label = "إجمالي القيمة", Value = AssistantMoney.Iqd(report.TotalRevenue) },
                new() { Label = "عدد المنتجات", Value = AssistantMoney.Count(report.ProductCount) }
            ],
            ["المنتج", "الكمية", "القيمة", "النسبة"],
            rows,
            report.Rows.Take(plan.TopCount).Select(row => new AssistantChartPoint { Label = row.ProductName, Value = row.Revenue }).ToList());
    }

    private async Task<AssistantAnswer> ExpensesAsync(QueryPlan plan, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var report = await _reports.GetExpensesReportAsync(plan.From, plan.To, null, null);
        var rows = report.ByTypeChart
            .OrderByDescending(point => point.Amount)
            .Take(8)
            .Select(point => (IReadOnlyList<string>)[point.Name, AssistantMoney.Iqd(point.Amount)])
            .ToList();
        var metrics = new List<AssistantMetricCard>
        {
            new() { Label = "إجمالي المصروفات", Value = AssistantMoney.Iqd(report.TotalExpenses) }
        };
        if (!string.IsNullOrWhiteSpace(report.TopExpenseType) && report.TopExpenseType != "—")
            metrics.Add(new() { Label = "أعلى تصنيف", Value = report.TopExpenseType });

        return ArabicResponseGenerator.Ranked(
            plan,
            $"إجمالي المصروفات {plan.PeriodLabel}: {AssistantMoney.Iqd(report.TotalExpenses)}." +
            (report.TotalExpenses == 0 ? " لا توجد مصروفات مسجلة في هذه الفترة." : string.Empty),
            metrics,
            rows.Count == 0 ? [] : ["التصنيف", "المبلغ"],
            rows,
            report.DailyChart.Select(point => new AssistantChartPoint { Label = point.Date.ToString("MM/dd"), Value = point.Amount }).ToList());
    }

    private async Task<AssistantAnswer> ExpenseComparisonAsync(QueryPlan plan, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var current = await _reports.GetExpensesReportAsync(plan.From, plan.To, null, null);
        var previous = await _reports.GetExpensesReportAsync(plan.CompareFrom, plan.CompareTo, null, null);
        var chart = ResultAnalysisService.CompareChart(plan.PeriodLabel, current.TotalExpenses, plan.CompareLabel ?? "السابق", previous.TotalExpenses);
        return ArabicResponseGenerator.Comparison(plan, "المصروفات", current.TotalExpenses, previous.TotalExpenses, chart);
    }

    private async Task<AssistantAnswer> CashAsync(QueryPlan plan, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var report = await _reports.GetCashBalancesSummaryReportAsync();
        var rows = report.Rows.Take(12).Select(row => (IReadOnlyList<string>)
        [
            row.Name,
            row.AccountType,
            row.Currency == AccountingCurrency.USD ? AssistantMoney.Usd(row.Balance) : AssistantMoney.Iqd(row.Balance)
        ]).ToList();
        var metrics = new List<AssistantMetricCard>
        {
            new() { Label = "الصناديق", Value = AssistantMoney.Iqd(report.CashBoxesTotal) },
            new() { Label = "المصارف", Value = AssistantMoney.Iqd(report.BanksTotal) },
            new() { Label = "السيولة", Value = AssistantMoney.Iqd(report.TotalLiquid) }
        };
        if (report.TotalLiquidUsd != 0)
            metrics.Add(new() { Label = "سيولة الدولار", Value = AssistantMoney.Usd(report.TotalLiquidUsd), Hint = "منفصلة عن الدينار" });

        return ArabicResponseGenerator.Ranked(
            plan,
            $"رصيد الصناديق الحالي: {AssistantMoney.Iqd(report.CashBoxesTotal)}. الرقم مأخوذ الآن من أرصدة الصندوق والمصرف، وليس من نسخة محفوظة.",
            metrics,
            rows.Count == 0 ? [] : ["الحساب", "النوع", "الرصيد"],
            rows);
    }

    private async Task<AssistantAnswer> PurchasesAsync(QueryPlan plan, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var report = await _reports.GetPurchasesReportAsync(plan.From, plan.To, null, null);
        var metrics = new List<AssistantMetricCard>
        {
            new() { Label = "صافي المشتريات", Value = AssistantMoney.Iqd(report.TotalPurchases) },
            new() { Label = "عدد الفواتير", Value = AssistantMoney.Count(report.InvoiceCount) }
        };
        if (report.TotalPurchasesUsd != 0)
            metrics.Add(new() { Label = "مشتريات الدولار", Value = AssistantMoney.Usd(report.TotalPurchasesUsd) });
        return ArabicResponseGenerator.Ranked(
            plan,
            $"إجمالي المشتريات {plan.PeriodLabel}: {AssistantMoney.Iqd(report.TotalPurchases)}.",
            metrics,
            [],
            [],
            report.DailyChart.Select(point => new AssistantChartPoint { Label = point.Date.ToString("MM/dd"), Value = point.Amount }).ToList());
    }

    private async Task<AssistantAnswer> ProfitAsync(QueryPlan plan, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var report = await _reports.GetProfitAndLossReportAsync(plan.From, plan.To);
        var metrics = new List<AssistantMetricCard>
        {
            new() { Label = "المبيعات", Value = AssistantMoney.Iqd(report.TotalSales) },
            new() { Label = "تكلفة المبيعات", Value = AssistantMoney.Iqd(report.CostOfGoodsSold) },
            new() { Label = "مجمل الربح", Value = AssistantMoney.Iqd(report.GrossProfit) },
            new() { Label = "المصروفات", Value = AssistantMoney.Iqd(report.TotalExpenses) },
            new() { Label = "صافي الربح", Value = AssistantMoney.Iqd(report.NetProfit) },
            new() { Label = "هامش صافي", Value = AssistantMoney.Percent(report.NetMarginPercent) }
        };
        var title = plan.Intent == AccountingIntent.FinancialSummary ? "الملخص المالي" : "نتيجة الأرباح";
        var note = report.NetProfit < 0 ? " النتيجة خسارة في هذه الفترة." : string.Empty;
        return ArabicResponseGenerator.Ranked(
            plan,
            $"{title} {plan.PeriodLabel}: صافي الربح {AssistantMoney.Iqd(report.NetProfit)}.{note}",
            metrics,
            [],
            [],
            report.MonthlyChart.Select(point => new AssistantChartPoint { Label = point.Date.ToString("MM/dd"), Value = point.Amount }).ToList());
    }

    private async Task<AssistantAnswer> ProfitComparisonAsync(QueryPlan plan, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var current = await _reports.GetProfitAndLossReportAsync(plan.From, plan.To);
        var previous = await _reports.GetProfitAndLossReportAsync(plan.CompareFrom, plan.CompareTo);
        var chart = ResultAnalysisService.CompareChart(plan.PeriodLabel, current.NetProfit, plan.CompareLabel ?? "السابق", previous.NetProfit);
        return ArabicResponseGenerator.Comparison(plan, "صافي الربح", current.NetProfit, previous.NetProfit, chart);
    }

    private async Task<AssistantAnswer> LowStockAsync(QueryPlan plan, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var report = await _reports.GetMinimumQuantityReportAsync(null, null, MinimumQuantityFilter.BelowMinimum);
        if (report.BelowMinimumCount == 0)
        {
            return ArabicResponseGenerator.Ranked(
                plan,
                "لا توجد منتجات تحت حد إعادة الطلب المسجل في المخزون. إذا لم يُضبط الحد الأدنى للصنف فلن يظهر هنا.",
                [new AssistantMetricCard { Label = "تحت الحد", Value = "0" }],
                [],
                []);
        }

        var rows = report.Rows.Where(row => row.Status == MinimumQuantityStatus.BelowMinimum).Take(12)
            .Select(row => (IReadOnlyList<string>)
            [
                row.ProductName,
                row.WarehouseName,
                AssistantMoney.Quantity(row.CurrentQuantity),
                AssistantMoney.Quantity(row.MinQuantity)
            ]).ToList();
        return ArabicResponseGenerator.Ranked(
            plan,
            $"هناك {AssistantMoney.Count(report.BelowMinimumCount)} صنفاً وصل إلى ما دون الحد الأدنى المسجل.",
            [
                new() { Label = "تحت الحد", Value = AssistantMoney.Count(report.BelowMinimumCount) },
                new() { Label = "إجمالي النقص", Value = AssistantMoney.Quantity(report.TotalShortage) }
            ],
            ["المنتج", "المخزن", "الكمية", "الحد"],
            rows);
    }

    private async Task<AssistantAnswer> SupplierDebtAsync(QueryPlan plan, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var asOf = plan.AsOf ?? DateTime.Today;
        var aging = await _reports.GetPayablesAgingReportAsync(asOf, null);
        var metrics = new List<AssistantMetricCard>
        {
            new() { Label = "ذمم الموردين", Value = AssistantMoney.Iqd(aging.TotalOutstanding) },
            new() { Label = "عدد الموردين", Value = AssistantMoney.Count(aging.SupplierCount) }
        };
        if (aging.TotalOutstandingUsd != 0)
            metrics.Add(new() { Label = "ذمم الدولار", Value = AssistantMoney.Usd(aging.TotalOutstandingUsd) });
        return ArabicResponseGenerator.Ranked(
            plan,
            $"إجمالي الأرصدة الدائنة للموردين حتى {ArabicTextNormalizer.FormatDate(asOf)}: {AssistantMoney.Iqd(aging.TotalOutstanding)}.",
            metrics,
            [],
            []);
    }

    private async Task<AssistantAnswer> CollectionsAsync(QueryPlan plan, int? customerId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var report = await _reports.GetCustomerCollectionsReportAsync(plan.From, plan.To, customerId, null);
        return ArabicResponseGenerator.Ranked(
            plan,
            $"تحصيلات العملاء {plan.PeriodLabel}: {AssistantMoney.Iqd(report.TotalCollected)}.",
            [
                new() { Label = "سندات القبض", Value = AssistantMoney.Iqd(report.VoucherCollections) },
                new() { Label = "أقساط محصّلة", Value = AssistantMoney.Iqd(report.InstallmentCollections) },
                new() { Label = "الإجمالي", Value = AssistantMoney.Iqd(report.TotalCollected) }
            ],
            [],
            [],
            report.DailyChart.Select(point => new AssistantChartPoint { Label = point.Date.ToString("MM/dd"), Value = point.Amount }).ToList());
    }

    private async Task<AssistantAnswer> SupplierPaymentsAsync(QueryPlan plan, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var report = await _reports.GetSupplierPaymentsReportAsync(plan.From, plan.To, null);
        return ArabicResponseGenerator.Ranked(
            plan,
            $"دفعات الموردين {plan.PeriodLabel}: {AssistantMoney.Iqd(report.TotalPaid)}.",
            [
                new() { Label = "سندات الصرف", Value = AssistantMoney.Iqd(report.VoucherPayments) },
                new() { Label = "مشتريات نقدية", Value = AssistantMoney.Iqd(report.CashPurchases) },
                new() { Label = "الإجمالي", Value = AssistantMoney.Iqd(report.TotalPaid) }
            ],
            [],
            []);
    }

    private async Task<(int? Id, string? Name, AssistantAnswer? Choice)> ResolveCustomerAsync(QueryPlan plan, CancellationToken cancellationToken)
    {
        if (plan.CustomerId is int existing)
            return (existing, plan.ContextCustomerName, null);

        if (string.IsNullOrWhiteSpace(plan.CustomerNameQuery))
        {
            if (plan.Intent == AccountingIntent.CustomerPurchases)
                return (null, null, ArabicResponseGenerator.NeedCustomer());
            return (null, null, null);
        }

        var term = Sanitize(plan.CustomerNameQuery);
        if (term.Length < 2)
            return (null, null, ArabicResponseGenerator.NeedCustomer());

        await using var context = await _contexts.CreateDbContextAsync(cancellationToken);
        var customers = await context.Customers.AsNoTracking()
            .Select(customer => new { customer.Id, customer.Name })
            .ToListAsync(cancellationToken);

        var matches = customers
            .Select(customer => new { customer.Id, customer.Name, Normalized = ArabicTextNormalizer.Normalize(customer.Name) })
            .Where(customer => customer.Normalized.Contains(term, StringComparison.Ordinal))
            .Take(8)
            .ToList();

        if (matches.Count == 0)
            return (null, null, ArabicResponseGenerator.CustomerNotFound(term));
        if (matches.Count > 1)
            return (null, null, ArabicResponseGenerator.ChooseCustomer(matches.Select(match => match.Name).ToList()));

        return (matches[0].Id, matches[0].Name, null);
    }

    private static string Sanitize(string name)
    {
        var cleaned = name.Replace("%", "", StringComparison.Ordinal)
            .Replace("_", "", StringComparison.Ordinal)
            .Replace("[", "", StringComparison.Ordinal)
            .Replace("]", "", StringComparison.Ordinal)
            .Trim();
        return cleaned.Length <= 40 ? cleaned : cleaned[..40];
    }
}
