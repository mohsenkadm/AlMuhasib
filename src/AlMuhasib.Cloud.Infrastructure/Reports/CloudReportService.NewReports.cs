using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Cloud.Infrastructure.Mobile;
using AlMuhasib.Core;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Reports;

public sealed partial class CloudReportService
{
    private async Task<decimal> GetAsOfUsdToIqdRateAsync(DateTime date, CancellationToken ct = default)
    {
        var day = date.Date;
        var rate = await _db.ExchangeRates.AsNoTracking()
            .Where(r => r.RateDate.Date <= day)
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.Id)
            .Select(r => (decimal?)r.UsdToIqd)
            .FirstOrDefaultAsync(ct);
        if (rate is > 0)
            return rate.Value;

        rate = await _db.ExchangeRates.AsNoTracking()
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.Id)
            .Select(r => (decimal?)r.UsdToIqd)
            .FirstOrDefaultAsync(ct);
        return rate is > 0 ? rate.Value : 0m;
    }

    private static string ResolveAgingBucket(DateTime dueDate, DateTime asOfDate)
    {
        if (dueDate.Date >= asOfDate)
            return "غير مستحق";
        var days = (asOfDate - dueDate.Date).Days;
        return days switch
        {
            <= 30 => "1-30 يوم",
            <= 60 => "31-60 يوم",
            <= 90 => "61-90 يوم",
            _ => "+90 يوم"
        };
    }

    private static List<AgingBucketSummary> BuildAgingBuckets(IEnumerable<(string Bucket, decimal Amount)> items)
    {
        var list = items.ToList();
        var order = new[] { "غير مستحق", "1-30 يوم", "31-60 يوم", "61-90 يوم", "+90 يوم" };
        return order.Select(name => new AgingBucketSummary
        {
            BucketName = name,
            Count = list.Count(x => x.Bucket == name),
            Amount = list.Where(x => x.Bucket == name).Sum(x => x.Amount)
        }).ToList();
    }

    private static string PaymentMethodLabel(PaymentMethod m) => m switch
    {
        PaymentMethod.Cash => "نقدي",
        PaymentMethod.Credit => "آجل",
        PaymentMethod.Installment => "أقساط",
        _ => m.ToString()
    };

    private static string CapitalTypeLabel(CapitalEntryType t) => t switch
    {
        CapitalEntryType.Initial => "رأس مال ابتدائي",
        CapitalEntryType.Adjustment => "تعديل رأس المال",
        CapitalEntryType.ProfitOpeningBalance => "أرباح افتتاحية",
        _ => t.ToString()
    };

    private static string InstallmentStatusLabel(InstallmentStatus s) => s switch
    {
        InstallmentStatus.Paid => "مسدد",
        InstallmentStatus.PartiallyPaid => "جزئي",
        InstallmentStatus.Pending => "غير مسدد",
        InstallmentStatus.Overdue => "متأخر",
        _ => s.ToString()
    };

    private async Task<string> ResolveTransferAccountNameAsync(CloudDbContext context, TransferAccountType type, int id)
    {
        if (type == TransferAccountType.CashBox)
            return (await context.CashBoxes.FirstOrDefaultAsync(c => c.Id == id))?.Name ?? $"قاصة #{id}";
        return (await context.BankAccounts.FirstOrDefaultAsync(b => b.Id == id))?.Name ?? $"مصرف #{id}";
    }

    // ── Supervisory ──────────────────────────────────────────────

    public async Task<InvestorProfitDistributionsReportResult> GetInvestorProfitDistributionsReportAsync(
        DateTime? from, DateTime? to, int? investorId)
    {
        var context = _db;
        var query = context.ProfitDistributions.AsQueryable();
        if (from.HasValue) query = query.Where(d => d.Date >= from.Value);
        if (to.HasValue) query = query.Where(d => d.Date < EndOfDay(to));

        var distributions = await query.OrderByDescending(d => d.Date).ToListAsync();
        var distIds = distributions.Select(d => d.Id).ToList();
        var detailEntities = await context.ProfitDistributionDetails
            .Include(x => x.Investor)
            .Where(x => distIds.Contains(x.ProfitDistributionId))
            .ToListAsync();
        if (investorId.HasValue)
            detailEntities = detailEntities.Where(x => x.InvestorId == investorId.Value).ToList();

        var distById = distributions.ToDictionary(d => d.Id);
        var details = detailEntities.Select(x => new InvestorProfitDistributionDetailRow
        {
            DistributionId = x.ProfitDistributionId,
            Date = distById.TryGetValue(x.ProfitDistributionId, out var d) ? d.Date : default,
            InvestorId = x.InvestorId,
            InvestorName = x.Investor?.Name ?? "—",
            ProfitPercentage = x.ProfitPercentage,
            Amount = x.Amount
        }).ToList();

        var rows = distributions.Select(d => new InvestorProfitDistributionRow
        {
            DistributionId = d.Id,
            Date = d.Date,
            TotalProfit = d.TotalProfit,
            DistributedAmount = d.DistributedAmount,
            DetailCount = detailEntities.Count(x => x.ProfitDistributionId == d.Id)
        }).ToList();

        return new InvestorProfitDistributionsReportResult
        {
            TotalProfit = rows.Sum(r => r.TotalProfit),
            TotalDistributed = details.Sum(d => d.Amount),
            DistributionCount = rows.Count,
            InvestorCount = details.Select(d => d.InvestorId).Distinct().Count(),
            Rows = rows,
            Details = details,
            ByInvestorChart = details.GroupBy(d => d.InvestorName)
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Amount).Take(10).ToList(),
            DailyChart = details.GroupBy(d => d.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderBy(x => x.Date).ToList()
        };
    }

    public async Task<CapitalMovementReportResult> GetCapitalMovementReportAsync(DateTime? from, DateTime? to)
    {
        var context = _db;
        var query = context.CapitalEntries.AsQueryable();
        if (from.HasValue) query = query.Where(c => c.Date >= from.Value);
        if (to.HasValue) query = query.Where(c => c.Date < EndOfDay(to));

        var entries = await query.OrderByDescending(c => c.Date).ToListAsync();
        var rows = entries.Select(c => new CapitalMovementRow
        {
            Id = c.Id,
            Date = c.Date,
            TypeDisplay = CapitalTypeLabel(c.Type),
            Amount = c.Amount,
            Notes = c.Notes ?? "—",
            CreatedBy = c.CreatedBy ?? "—"
        }).ToList();

        var initial = entries.Where(c => c.Type == CapitalEntryType.Initial).Sum(c => c.Amount);
        var adj = entries.Where(c => c.Type == CapitalEntryType.Adjustment).Sum(c => c.Amount);
        var opening = entries.Where(c => c.Type == CapitalEntryType.ProfitOpeningBalance).Sum(c => c.Amount);

        return new CapitalMovementReportResult
        {
            InitialCapital = initial,
            Adjustments = adj,
            ProfitOpening = opening,
            EquityCapital = initial + adj + opening,
            Rows = rows,
            ByTypeChart =
            [
                new NameAmountPoint { Name = "رأس مال ابتدائي", Amount = initial },
                new NameAmountPoint { Name = "تعديلات", Amount = adj },
                new NameAmountPoint { Name = "أرباح افتتاحية", Amount = opening }
            ],
            DailyChart = entries.GroupBy(c => c.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderBy(x => x.Date).ToList()
        };
    }

    // ── Installments ─────────────────────────────────────────────

    public async Task<OpeningInstallmentBalancesReportResult> GetOpeningInstallmentBalancesReportAsync(
        DateTime? from, DateTime? to, int? customerId)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var query = context.InstallmentPlans
            .Include(p => p.Customer)
            .Include(p => p.Installments)
            .Include(p => p.Invoice)
            .Where(p => p.InstallmentType == InstallmentType.OpeningBalance
                        && (foldInUsd || p.Invoice!.Currency == AccountingCurrency.IQD));

        if (customerId.HasValue) query = query.Where(p => p.CustomerId == customerId.Value);
        if (from.HasValue) query = query.Where(p => p.Invoice.Date >= from.Value);
        if (to.HasValue) query = query.Where(p => p.Invoice.Date < EndOfDay(to));

        var plans = await query.OrderByDescending(p => p.Invoice.Date).ToListAsync();
        var rows = plans.Select(p =>
        {
            var inv = p.Invoice!;
            decimal InBase(decimal amount) =>
                FinancialReportIqdFoldIn.AmountInBaseIqd(amount, inv.Currency, inv.FxRate, foldInUsd);
            var paid = p.Installments.Sum(i => InBase(i.PaidAmount));
            var remaining = p.Installments.Sum(i => InBase(i.RemainingAmount));
            return new OpeningInstallmentBalanceRow
            {
                PlanId = p.Id,
                InvoiceId = p.InvoiceId,
                CustomerName = p.Customer?.Name ?? "—",
                Phone = p.Customer?.Phone ?? "—",
                Date = p.Invoice?.Date ?? DateTime.MinValue,
                TotalAmount = InBase(p.TotalAmount),
                PaidAmount = paid,
                RemainingAmount = remaining,
                InstallmentCount = p.Installments.Count,
                Status = remaining <= 0 ? "مسدد" : paid > 0 ? "جزئي" : "مفتوح"
            };
        }).ToList();

        return new OpeningInstallmentBalancesReportResult
        {
            TotalAmount = rows.Sum(r => r.TotalAmount),
            TotalPaid = rows.Sum(r => r.PaidAmount),
            TotalRemaining = rows.Sum(r => r.RemainingAmount),
            PlanCount = rows.Count,
            CustomerCount = rows.Select(r => r.CustomerName).Distinct().Count(),
            Rows = rows,
            StatusChart = rows.GroupBy(r => r.Status)
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(x => x.RemainingAmount) }).ToList()
        };
    }

    public async Task<CompanyFeeReportResult> GetCompanyFeeReportAsync(DateTime? from, DateTime? to, int? customerId)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var query = context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InstallmentPlans)
            .Where(i => i.InvoiceType == InvoiceType.Installment
                        && (foldInUsd || i.Currency == AccountingCurrency.IQD));

        if (from.HasValue) query = query.Where(i => i.Date >= from.Value);
        if (to.HasValue) query = query.Where(i => i.Date < EndOfDay(to));
        if (customerId.HasValue) query = query.Where(i => i.CustomerId == customerId.Value);

        var invoices = await query.OrderByDescending(i => i.Date).ToListAsync();
        var rows = new List<CompanyFeeRow>();
        foreach (var i in invoices)
        {
            var plan = i.InstallmentPlans.FirstOrDefault();
            if (plan is null || !CompanyFeeHelper.AppliesTo(plan.InstallmentType))
                continue;

            decimal InBase(decimal amount) =>
                FinancialReportIqdFoldIn.AmountInBaseIqd(amount, i.Currency, i.FxRate, foldInUsd);
            var netInBase = InBase(i.NetAmount);
            var fee = plan.CompanyFeeAmount > 0
                ? InBase(plan.CompanyFeeAmount)
                : (i.CompanyFeeAmount > 0 ? InBase(i.CompanyFeeAmount) : CompanyFeeHelper.CalculateAmount(netInBase));
            var pct = plan.CompanyFeePercentage > 0
                ? plan.CompanyFeePercentage * 100
                : (i.CompanyFeePercentage > 0 ? i.CompanyFeePercentage * 100 : CompanyFeeHelper.DefaultPercentage * 100);

            rows.Add(new CompanyFeeRow
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                Date = i.Date,
                CustomerName = i.Customer?.Name ?? "—",
                NetAmount = netInBase,
                FeePercent = pct,
                FeeAmount = fee,
                PlanNumber = plan.Id.ToString()
            });
        }

        var totalFees = rows.Sum(r => r.FeeAmount);
        var totalSales = rows.Sum(r => r.NetAmount);
        return new CompanyFeeReportResult
        {
            TotalFees = totalFees,
            TotalSales = totalSales,
            AverageFeePercent = totalSales > 0 ? Math.Round(totalFees / totalSales * 100, 1) : 0,
            InvoiceCount = rows.Count,
            Rows = rows,
            DailyChart = rows.GroupBy(r => r.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.FeeAmount) })
                .OrderBy(x => x.Date).ToList(),
            ByCustomerChart = rows.GroupBy(r => r.CustomerName)
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(x => x.FeeAmount) })
                .OrderByDescending(x => x.Amount).Take(10).ToList()
        };
    }

    public async Task<InstallmentScheduleReportResult> GetInstallmentScheduleReportAsync(
        DateTime? from, DateTime? to, int? customerId, string? status)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var query = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Invoice)
            .Where(i => foldInUsd || i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD);

        if (from.HasValue) query = query.Where(i => i.DueDate >= from.Value);
        if (to.HasValue) query = query.Where(i => i.DueDate < EndOfDay(to));
        if (customerId.HasValue) query = query.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InstallmentStatus>(status, true, out var st))
            query = query.Where(i => i.Status == st);

        var items = await query.OrderBy(i => i.DueDate).ToListAsync();
        decimal InBase(CloudInstallment i, decimal amount)
        {
            var inv = i.InstallmentPlan!.Invoice!;
            return FinancialReportIqdFoldIn.AmountInBaseIqd(amount, inv.Currency, inv.FxRate, foldInUsd);
        }

        var rows = items.Select(i => new InstallmentScheduleReportRow
        {
            InstallmentId = i.Id,
            PlanId = i.InstallmentPlanId,
            InvoiceId = i.InstallmentPlan?.InvoiceId ?? 0,
            CustomerName = i.InstallmentPlan?.Customer?.Name ?? "—",
            Phone = i.InstallmentPlan?.Customer?.Phone ?? "—",
            DueDate = i.DueDate,
            Amount = InBase(i, i.Amount),
            PaidAmount = InBase(i, i.PaidAmount),
            RemainingAmount = InBase(i, i.RemainingAmount),
            Status = InstallmentStatusLabel(i.Status),
            PaymentDate = i.PaymentDate
        }).ToList();

        return new InstallmentScheduleReportResult
        {
            TotalAmount = rows.Sum(r => r.Amount),
            TotalPaid = rows.Sum(r => r.PaidAmount),
            TotalRemaining = rows.Sum(r => r.RemainingAmount),
            InstallmentCount = rows.Count,
            Rows = rows,
            DueChart = rows.GroupBy(r => r.DueDate.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.RemainingAmount) })
                .OrderBy(x => x.Date).ToList(),
            StatusChart = rows.GroupBy(r => r.Status)
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(x => x.RemainingAmount) }).ToList()
        };
    }

    // ── Sales & Profit ───────────────────────────────────────────

    public async Task<SalesByPaymentMethodReportResult> GetSalesByPaymentMethodReportAsync(
        DateTime? from, DateTime? to, int? warehouseId)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var query = foldInUsd
            ? CloudInvoiceFilters.ForProfitAndSalesTotalsAll(context.Invoices, context.InstallmentPlans)
            : CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans);
        if (from.HasValue) query = query.Where(i => i.Date >= from.Value);
        if (to.HasValue) query = query.Where(i => i.Date < EndOfDay(to));
        if (warehouseId.HasValue) query = query.Where(i => i.WarehouseId == warehouseId.Value);

        var invoices = await query.ToListAsync();
        decimal SignedInBase(CloudInvoice i) =>
            FinancialReportIqdFoldIn.SignedSalesInBaseIqd(i.InvoiceType, i.NetAmount, i.Currency, i.FxRate, foldInUsd);
        var total = invoices.Sum(SignedInBase);
        var rows = invoices.GroupBy(i => i.PaymentMethod)
            .Select(g => new SalesByPaymentMethodRow
            {
                PaymentMethod = PaymentMethodLabel(g.Key),
                InvoiceCount = g.Count(),
                Amount = g.Sum(SignedInBase),
                SharePercent = total > 0 ? Math.Round(g.Sum(SignedInBase) / total * 100, 1) : 0
            })
            .OrderByDescending(r => r.Amount).ToList();

        return new SalesByPaymentMethodReportResult
        {
            TotalSales = total,
            CashSales = invoices.Where(i => i.PaymentMethod == PaymentMethod.Cash).Sum(SignedInBase),
            CreditSales = invoices.Where(i => i.PaymentMethod == PaymentMethod.Credit).Sum(SignedInBase),
            InstallmentSales = invoices.Where(i => i.PaymentMethod == PaymentMethod.Installment).Sum(SignedInBase),
            InvoiceCount = invoices.Count,
            Rows = rows,
            MethodChart = rows.Select(r => new NameAmountPoint { Name = r.PaymentMethod, Amount = r.Amount }).ToList(),
            DailyChart = invoices.GroupBy(i => i.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(SignedInBase) })
                .OrderBy(x => x.Date).ToList()
        };
    }

    public async Task<DailySalesReportResult> GetDailySalesReportAsync(
        DateTime? from, DateTime? to, int? warehouseId, PaymentMethod? method,
        ReportCurrencyScope currencyScope = ReportCurrencyScope.Iqd)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var effectiveScope = foldInUsd ? ReportCurrencyScope.All : currencyScope;
        IQueryable<CloudInvoice> query = (effectiveScope == ReportCurrencyScope.All
                ? CloudInvoiceFilters.ForProfitAndSalesTotalsAll(context.Invoices, context.InstallmentPlans)
                : CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans,
                    effectiveScope == ReportCurrencyScope.Usd ? AccountingCurrency.USD : AccountingCurrency.IQD))
            .Include(i => i.InstallmentPlans);
        if (from.HasValue) query = query.Where(i => i.Date >= from.Value);
        if (to.HasValue) query = query.Where(i => i.Date < EndOfDay(to));
        if (warehouseId.HasValue) query = query.Where(i => i.WarehouseId == warehouseId.Value);
        if (method.HasValue) query = query.Where(i => i.PaymentMethod == method.Value);

        var invoices = await query.ToListAsync();
        List<DailySalesRow> rows;
        if (foldInUsd)
        {
            rows = invoices.GroupBy(i => i.Date.Date).Select(g =>
            {
                decimal fees = 0;
                foreach (var inv in g.Where(x => x.InvoiceType == InvoiceType.Installment))
                {
                    var plan = inv.InstallmentPlans.FirstOrDefault();
                    if (plan is null || !CompanyFeeHelper.AppliesTo(plan.InstallmentType)) continue;
                    fees += FinancialReportIqdFoldIn.AmountInBaseIqd(
                        plan.CompanyFeeAmount > 0 ? plan.CompanyFeeAmount : CompanyFeeHelper.CalculateAmount(inv.NetAmount),
                        inv.Currency, inv.FxRate, foldInUsd: true);
                }

                decimal SignedFold(CloudInvoice x) =>
                    FinancialReportIqdFoldIn.SignedSalesInBaseIqd(x.InvoiceType, x.NetAmount, x.Currency, x.FxRate, foldInUsd: true);

                return new DailySalesRow
                {
                    Date = g.Key,
                    Currency = AccountingCurrency.IQD,
                    InvoiceCount = g.Count(),
                    CashSales = g.Where(x => x.PaymentMethod == PaymentMethod.Cash).Sum(SignedFold),
                    CreditSales = g.Where(x => x.PaymentMethod == PaymentMethod.Credit).Sum(SignedFold),
                    InstallmentSales = g.Where(x => x.PaymentMethod == PaymentMethod.Installment).Sum(SignedFold),
                    TotalSales = g.Sum(SignedFold),
                    DiscountAmount = g.Sum(x => FinancialReportIqdFoldIn.AmountInBaseIqd(x.DiscountAmount, x.Currency, x.FxRate, true)),
                    CompanyFees = fees
                };
            }).OrderByDescending(r => r.Date).ToList();
        }
        else
        {
            rows = invoices.GroupBy(i => new { Day = i.Date.Date, i.Currency }).Select(g =>
            {
                decimal fees = 0;
                foreach (var inv in g.Where(x => x.InvoiceType == InvoiceType.Installment))
                {
                    var plan = inv.InstallmentPlans.FirstOrDefault();
                    if (plan is null || !CompanyFeeHelper.AppliesTo(plan.InstallmentType)) continue;
                    fees += plan.CompanyFeeAmount > 0 ? plan.CompanyFeeAmount : CompanyFeeHelper.CalculateAmount(inv.NetAmount);
                }

                return new DailySalesRow
                {
                    Date = g.Key.Day,
                    Currency = g.Key.Currency,
                    InvoiceCount = g.Count(),
                    CashSales = g.Where(x => x.PaymentMethod == PaymentMethod.Cash).Sum(CloudInvoiceFilters.SignedNetAmount),
                    CreditSales = g.Where(x => x.PaymentMethod == PaymentMethod.Credit).Sum(CloudInvoiceFilters.SignedNetAmount),
                    InstallmentSales = g.Where(x => x.PaymentMethod == PaymentMethod.Installment).Sum(CloudInvoiceFilters.SignedNetAmount),
                    TotalSales = g.Sum(CloudInvoiceFilters.SignedNetAmount),
                    DiscountAmount = g.Sum(x => x.DiscountAmount),
                    CompanyFees = fees
                };
            }).OrderByDescending(r => r.Date).ThenBy(r => r.Currency).ToList();
        }

        decimal SignedInBase(CloudInvoice i) =>
            FinancialReportIqdFoldIn.SignedSalesInBaseIqd(i.InvoiceType, i.NetAmount, i.Currency, i.FxRate, foldInUsd);

        var totalIqd = rows.Where(r => r.Currency == AccountingCurrency.IQD).Sum(r => r.TotalSales);
        var totalUsd = foldInUsd ? 0m : rows.Where(r => r.Currency == AccountingCurrency.USD).Sum(r => r.TotalSales);
        var primaryTotal = foldInUsd
            ? invoices.Sum(SignedInBase)
            : effectiveScope == ReportCurrencyScope.Usd ? totalUsd : totalIqd;
        return new DailySalesReportResult
        {
            TotalSales = primaryTotal,
            TotalSalesUsd = totalUsd,
            CurrencyScope = effectiveScope,
            DayCount = rows.Select(r => r.Date).Distinct().Count(),
            InvoiceCount = invoices.Count,
            AverageDaily = rows.Count > 0
                ? Math.Round(primaryTotal / Math.Max(1, rows.Select(r => r.Date).Distinct().Count()),
                    effectiveScope == ReportCurrencyScope.Usd ? 2 : 0)
                : 0,
            Rows = rows,
            DailyChart = foldInUsd
                ? invoices.GroupBy(i => i.Date.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(SignedInBase) }).ToList()
                : rows.Where(r => ReportCurrencyScopeHelper.Matches(effectiveScope == ReportCurrencyScope.All
                        ? ReportCurrencyScope.Iqd : effectiveScope, r.Currency))
                    .GroupBy(r => r.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.TotalSales) }).ToList()
        };
    }

    public async Task<SalesByWarehouseUserReportResult> GetSalesByWarehouseUserReportAsync(
        DateTime? from, DateTime? to, int? warehouseId)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        IQueryable<CloudInvoice> query = (foldInUsd
                ? CloudInvoiceFilters.ForProfitAndSalesTotalsAll(context.Invoices, context.InstallmentPlans)
                : CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans))
            .Include(i => i.Warehouse);
        if (from.HasValue) query = query.Where(i => i.Date >= from.Value);
        if (to.HasValue) query = query.Where(i => i.Date < EndOfDay(to));
        if (warehouseId.HasValue) query = query.Where(i => i.WarehouseId == warehouseId.Value);

        var invoices = await query.ToListAsync();
        decimal SignedInBase(CloudInvoice i) =>
            FinancialReportIqdFoldIn.SignedSalesInBaseIqd(i.InvoiceType, i.NetAmount, i.Currency, i.FxRate, foldInUsd);
        var total = invoices.Sum(SignedInBase);

        var warehouseRows = invoices.GroupBy(i => i.Warehouse?.Name ?? "—")
            .Select(g => new SalesByWarehouseUserRow
            {
                GroupType = "مخزن",
                Name = g.Key,
                InvoiceCount = g.Count(),
                Amount = g.Sum(SignedInBase),
                SharePercent = total > 0 ? Math.Round(g.Sum(SignedInBase) / total * 100, 1) : 0
            }).OrderByDescending(r => r.Amount).ToList();

        var userRows = invoices.GroupBy(i => string.IsNullOrWhiteSpace(i.CreatedBy) ? "—" : i.CreatedBy!)
            .Select(g => new SalesByWarehouseUserRow
            {
                GroupType = "مستخدم",
                Name = g.Key,
                InvoiceCount = g.Count(),
                Amount = g.Sum(SignedInBase),
                SharePercent = total > 0 ? Math.Round(g.Sum(SignedInBase) / total * 100, 1) : 0
            }).OrderByDescending(r => r.Amount).ToList();

        return new SalesByWarehouseUserReportResult
        {
            TotalSales = total,
            WarehouseCount = warehouseRows.Count,
            UserCount = userRows.Count,
            InvoiceCount = invoices.Count,
            Rows = warehouseRows.Concat(userRows).ToList(),
            WarehouseChart = warehouseRows.Take(10)
                .Select(r => new NameAmountPoint { Name = r.Name, Amount = r.Amount }).ToList(),
            UserChart = userRows.Take(10)
                .Select(r => new NameAmountPoint { Name = r.Name, Amount = r.Amount }).ToList()
        };
    }

    public async Task<GrossProfitMarginReportResult> GetGrossProfitMarginReportAsync(DateTime? from, DateTime? to)
    {
        var context = _db;
        var details = await GetProfitInvoiceDetailsAsync(from, to);
        var sales = details.Sum(d => d.Revenue);
        var cogs = details.Sum(d => d.Cost);
        var gross = sales - cogs;

        var rows = details.Select(d => new GrossProfitMarginRow
        {
            Date = d.Date,
            InvoiceNumber = d.InvoiceNumber,
            CustomerName = d.CustomerName,
            Revenue = d.Revenue,
            Cost = d.Cost,
            GrossProfit = d.GrossProfit,
            MarginPercent = d.MarginPercent
        }).ToList();

        return new GrossProfitMarginReportResult
        {
            TotalSales = sales,
            CostOfGoodsSold = cogs,
            GrossProfit = gross,
            GrossMarginPercent = sales > 0 ? Math.Round(gross / sales * 100, 1) : 0,
            Rows = rows,
            DailySalesChart = rows.GroupBy(r => r.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.Revenue) })
                .OrderBy(x => x.Date).ToList(),
            DailyGrossChart = rows.GroupBy(r => r.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.GrossProfit) })
                .OrderBy(x => x.Date).ToList(),
            CompositionChart =
            [
                new NameAmountPoint { Name = "تكلفة البضاعة", Amount = cogs },
                new NameAmountPoint { Name = "إجمالي الربح", Amount = Math.Max(0, gross) }
            ]
        };
    }

    public async Task<OperatingProfitReportResult> GetOperatingProfitReportAsync(DateTime? from, DateTime? to)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));

        var salesQ = foldInUsd
            ? CloudInvoiceFilters.ForProfitAndSalesTotalsAll(context.Invoices, context.InstallmentPlans)
            : CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans);
        var expQ = foldInUsd
            ? context.Expenses.AsQueryable()
            : context.Expenses.Where(e => e.Currency == AccountingCurrency.IQD);
        var bankQ = foldInUsd
            ? context.Vouchers.Where(v => v.VoucherType == VoucherType.BankReceipt)
            : context.Vouchers.Where(v => v.VoucherType == VoucherType.BankReceipt && v.Currency == AccountingCurrency.IQD);
        if (from.HasValue)
        {
            salesQ = salesQ.Where(i => i.Date >= from.Value);
            expQ = expQ.Where(e => e.Date >= from.Value);
            bankQ = bankQ.Where(v => v.Date >= from.Value);
        }
        if (to.HasValue)
        {
            salesQ = salesQ.Where(i => i.Date < EndOfDay(to));
            expQ = expQ.Where(e => e.Date < EndOfDay(to));
            bankQ = bankQ.Where(v => v.Date < EndOfDay(to));
        }

        var salesRows = await salesQ
            .Select(i => new { i.InvoiceType, i.NetAmount, i.Currency, i.FxRate })
            .ToListAsync();
        var sales = FinancialReportIqdFoldIn.SumSignedSalesInBaseIqd(
            salesRows.Select(r => (r.InvoiceType, r.NetAmount, r.Currency, r.FxRate)), foldInUsd);
        var cogs = await CalculateCogsAsync(context, from, EndOfDay(to));
        var expenseRows = await expQ
            .Select(e => new { e.Amount, e.Currency, e.FxRate })
            .ToListAsync();
        var expenses = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            expenseRows.Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);
        var bankRows = await bankQ
            .Select(v => new { v.BankFees, v.Currency, v.FxRate })
            .ToListAsync();
        var bankFees = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            bankRows.Select(r => (r.BankFees, r.Currency, r.FxRate)), foldInUsd);
        var gross = sales - cogs;
        var operating = gross - expenses - bankFees;

        var lines = new List<OperatingProfitLineRow>
        {
            new() { LineName = "صافي المبيعات", Amount = sales },
            new() { LineName = "تكلفة البضاعة المباعة", Amount = -cogs },
            new() { LineName = "إجمالي الربح", Amount = gross, IsSubtotal = true },
            new() { LineName = "المصاريف التشغيلية", Amount = -expenses },
            new() { LineName = "الرسوم البنكية", Amount = -bankFees },
            new() { LineName = "صافي الربح التشغيلي", Amount = operating, IsSubtotal = true }
        };

        var monthly = await GetMonthlyProfitAsync(from, to);

        return new OperatingProfitReportResult
        {
            TotalSales = sales,
            CostOfGoodsSold = cogs,
            GrossProfit = gross,
            TotalExpenses = expenses,
            TotalBankFees = bankFees,
            OperatingProfit = operating,
            OperatingMarginPercent = sales > 0 ? Math.Round(operating / sales * 100, 1) : 0,
            Lines = lines,
            CompositionChart =
            [
                new NameAmountPoint { Name = "إجمالي الربح", Amount = Math.Max(0, gross) },
                new NameAmountPoint { Name = "مصاريف", Amount = expenses },
                new NameAmountPoint { Name = "رسوم بنكية", Amount = bankFees }
            ],
            DailyChart = monthly.Select(m =>
            {
                var parts = m.Month.Split('/');
                var year = int.Parse(parts[0]);
                var month = int.Parse(parts[1]);
                return new DailyAmountPoint { Date = new DateTime(year, month, 1), Amount = m.GrossProfit - m.Expenses };
            }).ToList()
        };
    }

    // ── Partners ─────────────────────────────────────────────────

    public async Task<ReceivablesAgingReportResult> GetReceivablesAgingReportAsync(DateTime asOfDate, int? customerId)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var asOf = asOfDate.Date;
        var asOfEnd = asOf.AddDays(1);
        var multiCurrency = await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId);
        var creditRows = new List<ReceivablesAgingRow>();

        var creditQ = context.Invoices.Include(i => i.Customer)
            .Where(i => i.InvoiceType == InvoiceType.Sale
                        && i.PaymentMethod == PaymentMethod.Credit
                        && (multiCurrency || i.Currency == AccountingCurrency.IQD)
                        && i.RemainingAmount > 0
                        && i.Date < asOfEnd);
        if (customerId.HasValue) creditQ = creditQ.Where(i => i.CustomerId == customerId.Value);
        foreach (var i in await creditQ.OrderBy(i => i.Date).ThenBy(i => i.Id).ToListAsync())
        {
            var due = i.CreditDueDate?.Date ?? i.Date.Date;
            var days = due < asOf ? (asOf - due).Days : 0;
            creditRows.Add(new ReceivablesAgingRow
            {
                SourceType = "آجل",
                ReferenceId = i.Id,
                CustomerId = i.CustomerId,
                CustomerName = i.Customer?.Name ?? "—",
                CustomerFileNumber = i.Customer?.FileNumber,
                Phone = i.Customer?.Phone ?? "—",
                DueDate = due,
                Amount = i.NetAmount,
                RemainingAmount = i.RemainingAmount,
                DaysOverdue = days,
                AgingBucket = ResolveAgingBucket(due, asOf),
                Currency = i.Currency
            });
        }

        var unappliedQ = context.Vouchers.AsNoTracking()
            .Where(v => v.CustomerId != null
                        && (multiCurrency || v.Currency == AccountingCurrency.IQD)
                        && v.Date < asOfEnd
                        && !v.InvoiceId.HasValue
                        && !v.InstallmentId.HasValue
                        && (v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt)
                        && (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)));
        if (customerId.HasValue) unappliedQ = unappliedQ.Where(v => v.CustomerId == customerId.Value);
        var unappliedReceipts = await unappliedQ
            .Select(v => new { CustomerId = v.CustomerId!.Value, v.Amount, v.Currency })
            .ToListAsync();

        var iqdCredit = SupplierBalanceHelper.ApplyUnappliedPaymentsToAgingRows(
            creditRows.Where(r => r.Currency == AccountingCurrency.IQD).ToList(),
            unappliedReceipts.Where(v => v.Currency == AccountingCurrency.IQD).Select(v => (v.CustomerId, v.Amount)),
            r => r.CustomerId,
            r => r.RemainingAmount,
            (r, rem) =>
            {
                r.RemainingAmount = rem;
                r.AgingBucket = ResolveAgingBucket(r.DueDate, asOf);
                return r;
            });
        var usdCredit = multiCurrency
            ? SupplierBalanceHelper.ApplyUnappliedPaymentsToAgingRows(
                creditRows.Where(r => r.Currency == AccountingCurrency.USD).ToList(),
                unappliedReceipts.Where(v => v.Currency == AccountingCurrency.USD).Select(v => (v.CustomerId, v.Amount)),
                r => r.CustomerId,
                r => r.RemainingAmount,
                (r, rem) =>
                {
                    r.RemainingAmount = rem;
                    r.AgingBucket = ResolveAgingBucket(r.DueDate, asOf);
                    return r;
                })
            : [];
        creditRows = iqdCredit.Concat(usdCredit).ToList();

        var rows = new List<ReceivablesAgingRow>(creditRows);

        var instQ = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Where(i => i.Status != InstallmentStatus.Paid
                        && i.RemainingAmount > 0
                        && i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD);
        if (customerId.HasValue) instQ = instQ.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);
        foreach (var i in await instQ.ToListAsync())
        {
            var due = i.DueDate.Date;
            var days = due < asOf ? (asOf - due).Days : 0;
            rows.Add(new ReceivablesAgingRow
            {
                SourceType = "أقساط",
                ReferenceId = i.Id,
                CustomerId = i.InstallmentPlan?.CustomerId,
                CustomerName = i.InstallmentPlan?.Customer?.Name ?? "—",
                CustomerFileNumber = i.InstallmentPlan?.Customer?.FileNumber,
                Phone = i.InstallmentPlan?.Customer?.Phone ?? "—",
                DueDate = due,
                Amount = i.Amount,
                RemainingAmount = i.RemainingAmount,
                DaysOverdue = days,
                AgingBucket = ResolveAgingBucket(due, asOf)
            });
        }

        rows = rows.OrderByDescending(r => r.DaysOverdue).ThenBy(r => r.DueDate).ToList();

        var totalOutstandingUsdCredit = await context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.Sale
                        && i.PaymentMethod == PaymentMethod.Credit
                        && i.Currency == AccountingCurrency.USD
                        && i.RemainingAmount > 0
                        && i.Date < asOfEnd
                        && (!customerId.HasValue || i.CustomerId == customerId.Value))
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0m;
        var totalOutstandingUsdInst = await context.Installments.AsNoTracking()
            .Where(i => i.Status != InstallmentStatus.Paid
                        && i.RemainingAmount > 0
                        && i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.USD
                        && (!customerId.HasValue || i.InstallmentPlan.CustomerId == customerId.Value))
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0m;
        var unappliedDebtUsd = await context.Vouchers.AsNoTracking()
            .Where(v => v.CustomerId != null
                        && v.VoucherType == VoucherType.DebtReceipt
                        && v.Currency == AccountingCurrency.USD
                        && v.Date < asOfEnd
                        && !v.InvoiceId.HasValue
                        && !v.InstallmentId.HasValue
                        && (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker))
                        && (!customerId.HasValue || v.CustomerId == customerId.Value))
            .SumAsync(v => (decimal?)v.Amount) ?? 0m;
        var unappliedReceiptsUsd = await context.Vouchers.AsNoTracking()
            .Where(v => v.CustomerId != null
                        && v.VoucherType == VoucherType.Receipt
                        && v.Currency == AccountingCurrency.USD
                        && v.Date < asOfEnd
                        && !v.InvoiceId.HasValue
                        && !v.InstallmentId.HasValue
                        && (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker))
                        && (!customerId.HasValue || v.CustomerId == customerId.Value))
            .SumAsync(v => (decimal?)v.Amount) ?? 0m;

        return new ReceivablesAgingReportResult
        {
            TotalOutstanding = rows.Where(r => r.Currency == AccountingCurrency.IQD).Sum(r => r.RemainingAmount),
            TotalOutstandingUsd = multiCurrency
                ? rows.Where(r => r.Currency == AccountingCurrency.USD).Sum(r => r.RemainingAmount)
                : CustomerBalanceHelper.ComputeOutstandingBalance(
                    totalOutstandingUsdCredit, totalOutstandingUsdInst, unappliedDebtUsd, unappliedReceiptsUsd),
            RowCount = rows.Count,
            CustomerCount = rows.Select(r => r.CustomerName).Distinct().Count(),
            Buckets = BuildAgingBuckets(rows.Where(r => r.Currency == AccountingCurrency.IQD)
                .Select(r => (r.AgingBucket, r.RemainingAmount))),
            Rows = rows
        };
    }

    public async Task<PayablesAgingReportResult> GetPayablesAgingReportAsync(DateTime asOfDate, int? supplierId)
    {
        var context = _db;
        var asOf = asOfDate.Date;
        var asOfEnd = asOf.AddDays(1);
        var query = context.Invoices.Include(i => i.Supplier)
            .Where(i => i.InvoiceType == InvoiceType.Purchase
                        && i.PaymentMethod == PaymentMethod.Credit
                        && i.Currency == AccountingCurrency.IQD
                        && i.RemainingAmount > 0
                        && i.Date < asOfEnd);
        if (supplierId.HasValue) query = query.Where(i => i.SupplierId == supplierId.Value);

        var invoices = await query.OrderBy(i => i.Date).ThenBy(i => i.Id).ToListAsync();
        var rows = invoices.Select(i =>
        {
            var due = i.CreditDueDate?.Date ?? i.Date.Date;
            var days = due < asOf ? (asOf - due).Days : 0;
            return new PayablesAgingRow
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                SupplierId = i.SupplierId,
                SupplierName = i.Supplier?.Name ?? "—",
                Phone = i.Supplier?.Phone ?? "—",
                DueDate = due,
                Amount = i.NetAmount,
                RemainingAmount = i.RemainingAmount,
                DaysOverdue = days,
                AgingBucket = ResolveAgingBucket(due, asOf)
            };
        }).ToList();

        var paymentQ = context.Vouchers.AsNoTracking()
            .Where(v => v.SupplierId != null
                        && v.VoucherType == VoucherType.Payment
                        && v.Currency == AccountingCurrency.IQD
                        && v.Date < asOfEnd
                        && !v.InvoiceId.HasValue
                        && (v.Notes == null || !v.Notes.Contains(SupplierBalanceHelper.PaymentAppliedMarker)));
        if (supplierId.HasValue) paymentQ = paymentQ.Where(v => v.SupplierId == supplierId.Value);
        var unappliedPayments = await paymentQ
            .Select(v => new { SupplierId = v.SupplierId!.Value, v.Amount })
            .ToListAsync();

        rows = SupplierBalanceHelper.ApplyUnappliedPaymentsToAgingRows(
            rows,
            unappliedPayments.Select(v => (v.SupplierId, v.Amount)),
            r => r.SupplierId,
            r => r.RemainingAmount,
            (r, rem) =>
            {
                r.RemainingAmount = rem;
                r.AgingBucket = ResolveAgingBucket(r.DueDate, asOf);
                return r;
            });

        rows = rows.OrderByDescending(r => r.DaysOverdue).ThenBy(r => r.DueDate).ToList();

        var totalOutstandingUsdCredit = await context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.Purchase
                        && i.PaymentMethod == PaymentMethod.Credit
                        && i.Currency == AccountingCurrency.USD
                        && i.RemainingAmount > 0
                        && i.Date < asOfEnd
                        && (!supplierId.HasValue || i.SupplierId == supplierId.Value))
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0m;
        var unappliedUsd = await context.Vouchers.AsNoTracking()
            .Where(v => v.SupplierId != null
                        && v.VoucherType == VoucherType.Payment
                        && v.Currency == AccountingCurrency.USD
                        && v.Date < asOfEnd
                        && !v.InvoiceId.HasValue
                        && (v.Notes == null || !v.Notes.Contains(SupplierBalanceHelper.PaymentAppliedMarker))
                        && (!supplierId.HasValue || v.SupplierId == supplierId.Value))
            .SumAsync(v => (decimal?)v.Amount) ?? 0m;

        return new PayablesAgingReportResult
        {
            TotalOutstanding = rows.Sum(r => r.RemainingAmount),
            TotalOutstandingUsd = SupplierBalanceHelper.ComputeOutstandingPayables(
                totalOutstandingUsdCredit, unappliedUsd),
            RowCount = rows.Count,
            SupplierCount = rows.Select(r => r.SupplierName).Distinct().Count(),
            Buckets = BuildAgingBuckets(rows.Select(r => (r.AgingBucket, r.RemainingAmount))),
            Rows = rows
        };
    }

    public async Task<CustomerCollectionsReportResult> GetCustomerCollectionsReportAsync(
        DateTime? from, DateTime? to, int? customerId, int? cashBoxId,
        ReportCurrencyScope currencyScope = ReportCurrencyScope.Iqd)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId), currencyScope);
        var rows = new List<CustomerCollectionRow>();
        var strict = foldInUsd ? null : ReportCurrencyScopeHelper.ToStrictFilter(currencyScope);

        var vouchQ = context.Vouchers.Include(v => v.Customer).Include(v => v.CashBox)
            .Where(v => v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt);
        if (strict.HasValue) vouchQ = vouchQ.Where(v => v.Currency == strict.Value);
        if (from.HasValue) vouchQ = vouchQ.Where(v => v.Date >= from.Value);
        if (to.HasValue) vouchQ = vouchQ.Where(v => v.Date < EndOfDay(to));
        if (customerId.HasValue) vouchQ = vouchQ.Where(v => v.CustomerId == customerId.Value);
        if (cashBoxId.HasValue) vouchQ = vouchQ.Where(v => v.CashBoxId == cashBoxId.Value);

        foreach (var v in await vouchQ.ToListAsync())
        {
            rows.Add(new CustomerCollectionRow
            {
                Date = v.Date,
                SourceType = v.VoucherType == VoucherType.DebtReceipt ? "تسديد دين" : "سند قبض",
                Reference = v.VoucherNumber,
                CustomerName = v.Customer?.Name ?? "—",
                CustomerFileNumber = v.Customer?.FileNumber,
                Amount = foldInUsd
                    ? FinancialReportIqdFoldIn.AmountInBaseIqd(v.Amount, v.Currency, v.FxRate, foldInUsd)
                    : v.Amount,
                Currency = foldInUsd ? AccountingCurrency.IQD : v.Currency,
                AccountName = v.CashBox?.Name ?? "—",
                Notes = v.Notes ?? "—"
            });
        }

        var instQ = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Include(i => i.InstallmentPlan).ThenInclude(p => p!.Invoice)
            .Include(i => i.CashBox)
            .Where(i => i.PaidAmount > 0 && i.PaymentDate != null);
        if (strict.HasValue)
            instQ = instQ.Where(i => i.InstallmentPlan!.Invoice!.Currency == strict.Value);
        if (from.HasValue) instQ = instQ.Where(i => i.PaymentDate >= from.Value);
        if (to.HasValue) instQ = instQ.Where(i => i.PaymentDate < EndOfDay(to));
        if (customerId.HasValue) instQ = instQ.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);
        if (cashBoxId.HasValue) instQ = instQ.Where(i => i.CashBoxId == cashBoxId.Value);

        foreach (var i in await instQ.ToListAsync())
        {
            var currency = i.InstallmentPlan?.Invoice?.Currency ?? AccountingCurrency.IQD;
            var fx = i.InstallmentPlan?.Invoice?.FxRate ?? 0m;
            rows.Add(new CustomerCollectionRow
            {
                Date = i.PaymentDate!.Value,
                SourceType = "تحصيل قسط",
                Reference = $"قسط #{i.Id}",
                CustomerName = i.InstallmentPlan?.Customer?.Name ?? "—",
                CustomerFileNumber = i.InstallmentPlan?.Customer?.FileNumber,
                Amount = foldInUsd
                    ? FinancialReportIqdFoldIn.AmountInBaseIqd(i.PaidAmount, currency, fx, foldInUsd)
                    : i.PaidAmount,
                Currency = foldInUsd ? AccountingCurrency.IQD : currency,
                AccountName = i.CashBox?.Name ?? "—",
                Notes = "—"
            });
        }

        rows = rows.OrderByDescending(r => r.Date).ToList();
        var iqdRows = rows.Where(r => r.Currency == AccountingCurrency.IQD).ToList();
        var usdRows = rows.Where(r => r.Currency == AccountingCurrency.USD).ToList();
        var primaryRows = foldInUsd
            ? rows
            : currencyScope == ReportCurrencyScope.Usd ? usdRows : iqdRows;

        return new CustomerCollectionsReportResult
        {
            TotalCollected = primaryRows.Sum(r => r.Amount),
            TotalCollectedUsd = foldInUsd ? 0m : usdRows.Sum(r => r.Amount),
            VoucherCollections = primaryRows.Where(r => r.SourceType is "سند قبض" or "تسديد دين").Sum(r => r.Amount),
            InstallmentCollections = primaryRows.Where(r => r.SourceType == "تحصيل قسط").Sum(r => r.Amount),
            CurrencyScope = foldInUsd ? ReportCurrencyScope.Iqd : currencyScope,
            RowCount = rows.Count,
            Rows = rows,
            DailyChart = primaryRows.GroupBy(r => r.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderBy(x => x.Date).ToList(),
            ByCustomerChart = primaryRows.GroupBy(r => r.CustomerName)
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Amount).Take(10).ToList()
        };
    }

    public async Task<OverdueCustomersReportResult> GetOverdueCustomersReportAsync(
        DateTime asOfDate, int? minDaysOverdue, int? customerId)
    {
        var aging = await GetReceivablesAgingReportAsync(asOfDate, customerId);
        var minDays = minDaysOverdue ?? 1;
        var overdue = aging.Rows.Where(r => r.DaysOverdue >= minDays && r.RemainingAmount > 0).ToList();

        var rows = overdue.Select(r => new OverdueCustomerRow
        {
            CustomerId = 0,
            CustomerName = r.CustomerName,
            Phone = r.Phone,
            SourceType = r.SourceType,
            ReferenceId = r.ReferenceId,
            DueDate = r.DueDate,
            OverdueAmount = r.RemainingAmount,
            DaysOverdue = r.DaysOverdue
        }).ToList();

        // Enrich customer ids from names is weak; re-query for ids when possible
        var context = _db;
        var customers = await context.Customers.ToListAsync();
        var byName = customers.GroupBy(c => c.Name).ToDictionary(g => g.Key, g => g.First().Id);
        foreach (var row in rows)
            if (byName.TryGetValue(row.CustomerName, out var id))
                row.CustomerId = id;

        // متأخرات الدولار منفصلة — فقط البنود المتأخرة فعلياً (لا كل الرصيد المستحق)
        var asOf = asOfDate.Date;
        var asOfEnd = asOf.AddDays(1);
        var dueOnOrBefore = asOf.AddDays(-minDays);
        var overdueUsdCredit = await context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.Sale
                        && i.PaymentMethod == PaymentMethod.Credit
                        && i.Currency == AccountingCurrency.USD
                        && i.RemainingAmount > 0
                        && i.Date < asOfEnd
                        && (!customerId.HasValue || i.CustomerId == customerId.Value)
                        && (i.CreditDueDate ?? i.Date) <= dueOnOrBefore)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0m;
        var overdueUsdInst = await context.Installments.AsNoTracking()
            .Where(i => i.Status != InstallmentStatus.Paid
                        && i.RemainingAmount > 0
                        && i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.USD
                        && i.DueDate <= dueOnOrBefore
                        && (!customerId.HasValue || i.InstallmentPlan.CustomerId == customerId.Value))
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0m;

        return new OverdueCustomersReportResult
        {
            TotalOverdue = rows.Sum(r => r.OverdueAmount),
            TotalOverdueUsd = overdueUsdCredit + overdueUsdInst,
            CustomerCount = rows.Select(r => r.CustomerName).Distinct().Count(),
            ItemCount = rows.Count,
            AverageDaysOverdue = rows.Count > 0 ? Math.Round((decimal)rows.Average(r => r.DaysOverdue), 0) : 0,
            Rows = rows,
            ByCustomerChart = rows.GroupBy(r => r.CustomerName)
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(x => x.OverdueAmount) })
                .OrderByDescending(x => x.Amount).Take(10).ToList(),
            Buckets = BuildAgingBuckets(overdue.Select(r => (r.AgingBucket, r.RemainingAmount)))
        };
    }

    public async Task<SupplierPaymentsReportResult> GetSupplierPaymentsReportAsync(
        DateTime? from, DateTime? to, int? supplierId)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var supplierNames = await context.Suppliers.ToDictionaryAsync(s => s.Id, s => s.Name);
        var rows = new List<SupplierPaymentRow>();

        var vouchQ = context.Vouchers.Include(v => v.CashBox)
            .Where(v => v.VoucherType == VoucherType.Payment
                        && v.SupplierId != null
                        && (foldInUsd || v.Currency == AccountingCurrency.IQD));
        if (from.HasValue) vouchQ = vouchQ.Where(v => v.Date >= from.Value);
        if (to.HasValue) vouchQ = vouchQ.Where(v => v.Date < EndOfDay(to));
        if (supplierId.HasValue) vouchQ = vouchQ.Where(v => v.SupplierId == supplierId.Value);

        foreach (var v in await vouchQ.ToListAsync())
        {
            rows.Add(new SupplierPaymentRow
            {
                Date = v.Date,
                SourceType = "سند صرف",
                Reference = v.VoucherNumber,
                SupplierName = supplierNames.GetValueOrDefault(v.SupplierId ?? 0, "—"),
                Amount = FinancialReportIqdFoldIn.AmountInBaseIqd(v.Amount, v.Currency, v.FxRate, foldInUsd),
                AccountName = v.CashBox?.Name ?? "—",
                Notes = v.Notes ?? "—"
            });
        }

        var purchQ = context.Invoices.Include(i => i.Supplier).Include(i => i.CashBox)
            .Where(i => i.InvoiceType == InvoiceType.Purchase
                        && i.PaymentMethod == PaymentMethod.Cash
                        && (foldInUsd || i.Currency == AccountingCurrency.IQD));
        if (from.HasValue) purchQ = purchQ.Where(i => i.Date >= from.Value);
        if (to.HasValue) purchQ = purchQ.Where(i => i.Date < EndOfDay(to));
        if (supplierId.HasValue) purchQ = purchQ.Where(i => i.SupplierId == supplierId.Value);

        foreach (var i in await purchQ.ToListAsync())
        {
            rows.Add(new SupplierPaymentRow
            {
                Date = i.Date,
                SourceType = "مشتريات نقدية",
                Reference = i.InvoiceNumber,
                SupplierName = i.Supplier?.Name ?? "—",
                Amount = FinancialReportIqdFoldIn.AmountInBaseIqd(i.NetAmount, i.Currency, i.FxRate, foldInUsd),
                AccountName = i.CashBox?.Name ?? "—",
                Notes = "—"
            });
        }

        var purchaseReturnQ = context.Invoices.Include(i => i.Supplier).Include(i => i.CashBox)
            .Where(i => i.InvoiceType == InvoiceType.PurchaseReturn
                        && i.PaymentMethod == PaymentMethod.Cash
                        && (foldInUsd || i.Currency == AccountingCurrency.IQD));
        if (from.HasValue) purchaseReturnQ = purchaseReturnQ.Where(i => i.Date >= from.Value);
        if (to.HasValue) purchaseReturnQ = purchaseReturnQ.Where(i => i.Date < EndOfDay(to));
        if (supplierId.HasValue) purchaseReturnQ = purchaseReturnQ.Where(i => i.SupplierId == supplierId.Value);

        foreach (var i in await purchaseReturnQ.ToListAsync())
        {
            rows.Add(new SupplierPaymentRow
            {
                Date = i.Date,
                SourceType = "مرتجع مشتريات",
                Reference = i.InvoiceNumber,
                SupplierName = i.Supplier?.Name ?? "—",
                Amount = -FinancialReportIqdFoldIn.AmountInBaseIqd(Math.Abs(i.NetAmount), i.Currency, i.FxRate, foldInUsd),
                AccountName = i.CashBox?.Name ?? "—",
                Notes = "استرداد نقد من المورد"
            });
        }

        rows = rows.OrderByDescending(r => r.Date).ToList();
        var vouchTotal = rows.Where(r => r.SourceType == "سند صرف").Sum(r => r.Amount);
        var cashTotal = rows.Where(r => r.SourceType is "مشتريات نقدية" or "مرتجع مشتريات").Sum(r => r.Amount);

        return new SupplierPaymentsReportResult
        {
            TotalPaid = rows.Sum(r => r.Amount),
            VoucherPayments = vouchTotal,
            CashPurchases = cashTotal,
            RowCount = rows.Count,
            Rows = rows,
            DailyChart = rows.GroupBy(r => r.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderBy(x => x.Date).ToList(),
            BySupplierChart = rows.GroupBy(r => r.SupplierName)
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Amount).Take(10).ToList()
        };
    }

    // ── Inventory & Finance ──────────────────────────────────────

    public async Task<BankAccountStatementReportResult> GetBankAccountStatementReportAsync(
        int? bankAccountId, DateTime? from, DateTime? to)
    {
        var context = _db;
        var rows = new List<BankAccountStatementRow>();

        var banks = bankAccountId.HasValue
            ? await context.BankAccounts.Where(b => b.Id == bankAccountId.Value).ToListAsync()
            : await context.BankAccounts.Where(b => b.Currency == AccountingCurrency.IQD).ToListAsync();
        var bankMap = banks.ToDictionary(b => b.Id, b => b.Name);
        var bankIds = banks.Select(b => b.Id).ToHashSet();
        var reportCurrency = banks.FirstOrDefault()?.Currency ?? AccountingCurrency.IQD;

        var vouchQ = context.Vouchers.Where(v =>
            v.BankAccountId != null && bankIds.Contains(v.BankAccountId.Value)
            && v.Currency == reportCurrency);
        if (from.HasValue) vouchQ = vouchQ.Where(v => v.Date >= from.Value);
        if (to.HasValue) vouchQ = vouchQ.Where(v => v.Date < EndOfDay(to));
        foreach (var v in await vouchQ.ToListAsync())
        {
            var isIn = v.VoucherType is VoucherType.BankReceipt or VoucherType.Receipt or VoucherType.DebtReceipt
                or VoucherType.InvestorDeposit;
            rows.Add(new BankAccountStatementRow
            {
                Date = v.Date,
                Type = "سند",
                Description = $"{v.VoucherNumber} {(v.BankFees > 0 ? $"(رسوم {v.BankFees:N0})" : "")}".Trim(),
                Incoming = isIn ? v.Amount : 0,
                Outgoing = !isIn ? v.Amount + v.BankFees : v.BankFees,
                AccountName = bankMap.GetValueOrDefault(v.BankAccountId ?? 0, "—")
            });
        }

        var transfers = await context.Transfers
            .Where(t => t.Currency == reportCurrency)
            .ToListAsync();
        foreach (var t in transfers.Where(t =>
                     (t.FromType == TransferAccountType.Bank && bankIds.Contains(t.FromId)) ||
                     (t.ToType == TransferAccountType.Bank && bankIds.Contains(t.ToId))))
        {
            if (from.HasValue && t.Date < from.Value) continue;
            if (to.HasValue && t.Date >= EndOfDay(to)) continue;

            if (t.ToType == TransferAccountType.Bank && bankIds.Contains(t.ToId)
                && (!bankAccountId.HasValue || t.ToId == bankAccountId.Value))
            {
                rows.Add(new BankAccountStatementRow
                {
                    Date = t.Date,
                    Type = "تحويل وارد",
                    Description = t.Notes ?? "تحويل إلى المصرف",
                    Incoming = t.Amount,
                    AccountName = bankMap.GetValueOrDefault(t.ToId, "—")
                });
            }

            if (t.FromType == TransferAccountType.Bank && bankIds.Contains(t.FromId)
                && (!bankAccountId.HasValue || t.FromId == bankAccountId.Value))
            {
                rows.Add(new BankAccountStatementRow
                {
                    Date = t.Date,
                    Type = "تحويل صادر",
                    Description = t.Notes ?? "تحويل من المصرف",
                    Outgoing = t.Amount,
                    AccountName = bankMap.GetValueOrDefault(t.FromId, "—")
                });
            }
        }

        rows = rows.OrderBy(r => r.Date).ToList();
        var closing = banks.Sum(b => b.Balance);
        var periodNet = rows.Sum(r => r.Incoming - r.Outgoing);
        var opening = closing - periodNet;
        decimal bal = opening;
        foreach (var r in rows)
        {
            bal += r.Incoming - r.Outgoing;
            r.Balance = bal;
        }

        return new BankAccountStatementReportResult
        {
            OpeningBalance = opening,
            TotalIn = rows.Sum(r => r.Incoming),
            TotalOut = rows.Sum(r => r.Outgoing),
            ClosingBalance = closing,
            Currency = reportCurrency,
            Rows = rows,
            DailyInChart = rows.Where(r => r.Incoming > 0).GroupBy(r => r.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.Incoming) })
                .OrderBy(x => x.Date).ToList(),
            DailyOutChart = rows.Where(r => r.Outgoing > 0).GroupBy(r => r.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.Outgoing) })
                .OrderBy(x => x.Date).ToList()
        };
    }

    public async Task<CashBoxMovementReportResult> GetCashBoxMovementReportAsync(
        int? cashBoxId, DateTime? from, DateTime? to)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var asOfUsdToIqd = foldInUsd
            ? await GetAsOfUsdToIqdRateAsync(to ?? DateTime.Today)
            : 0m;

        var baseFlow = await GetCashFlowReportAsync(cashBoxId, from, to);
        var rows = baseFlow.Rows.Select(r => new CashBoxMovementRow
        {
            Date = r.Date,
            Type = r.Type,
            Description = r.Description,
            Incoming = r.Incoming,
            Outgoing = r.Outgoing,
            Balance = r.Balance,
            AccountName = r.AccountName
        }).ToList();

        decimal ToIqd(decimal amount, AccountingCurrency currency, decimal fxRate) =>
            foldInUsd
                ? FinancialReportIqdFoldIn.AmountInBaseIqd(amount, currency, fxRate, true)
                : amount;

        var cashBoxes = cashBoxId.HasValue
            ? await context.CashBoxes.Where(c => c.Id == cashBoxId.Value).ToListAsync()
            : foldInUsd
                ? await context.CashBoxes.ToListAsync()
                : await context.CashBoxes.Where(c => c.Currency == AccountingCurrency.IQD).ToListAsync();
        var ids = cashBoxes.Select(c => c.Id).ToHashSet();
        var nameMap = cashBoxes.ToDictionary(c => c.Id, c => c.Name);

        IQueryable<CloudTransfer> transfersQ = context.Transfers.AsQueryable();
        if (!foldInUsd)
        {
            var boxCurrency = cashBoxes.Select(c => c.Currency).Distinct().ToList();
            transfersQ = transfersQ.Where(t => boxCurrency.Contains(t.Currency));
        }
        var transfers = await transfersQ.ToListAsync();
        foreach (var t in transfers)
        {
            if (from.HasValue && t.Date < from.Value) continue;
            if (to.HasValue && t.Date >= EndOfDay(to)) continue;

            var amount = ToIqd(t.Amount, t.Currency, t.FxRate);
            if (t.ToType == TransferAccountType.CashBox && ids.Contains(t.ToId)
                && (!cashBoxId.HasValue || t.ToId == cashBoxId.Value))
            {
                rows.Add(new CashBoxMovementRow
                {
                    Date = t.Date,
                    Type = "تحويل وارد",
                    Description = t.Notes ?? "تحويل إلى القاصة",
                    Incoming = amount,
                    AccountName = nameMap.GetValueOrDefault(t.ToId, "—")
                });
            }

            if (t.FromType == TransferAccountType.CashBox && ids.Contains(t.FromId)
                && (!cashBoxId.HasValue || t.FromId == cashBoxId.Value))
            {
                rows.Add(new CashBoxMovementRow
                {
                    Date = t.Date,
                    Type = "تحويل صادر",
                    Description = t.Notes ?? "تحويل من القاصة",
                    Outgoing = amount,
                    AccountName = nameMap.GetValueOrDefault(t.FromId, "—")
                });
            }
        }

        var selectedCurrency = foldInUsd
            ? AccountingCurrency.IQD
            : cashBoxes.FirstOrDefault()?.Currency ?? AccountingCurrency.IQD;
        var instQ = context.Installments.Include(i => i.CashBox)
            .Include(i => i.InstallmentPlan).ThenInclude(p => p!.Invoice)
            .Where(i => i.PaidAmount > 0 && i.PaymentDate != null && i.CashBoxId != null
                        && ids.Contains(i.CashBoxId.Value)
                        && (foldInUsd || i.InstallmentPlan!.Invoice!.Currency == selectedCurrency));
        if (cashBoxId.HasValue) instQ = instQ.Where(i => i.CashBoxId == cashBoxId.Value);
        if (from.HasValue) instQ = instQ.Where(i => i.PaymentDate >= from.Value);
        if (to.HasValue) instQ = instQ.Where(i => i.PaymentDate < EndOfDay(to));
        foreach (var i in await instQ.ToListAsync())
        {
            var inv = i.InstallmentPlan!.Invoice!;
            rows.Add(new CashBoxMovementRow
            {
                Date = i.PaymentDate!.Value,
                Type = "تحصيل قسط",
                Description = $"قسط #{i.Id}",
                Incoming = ToIqd(i.PaidAmount, inv.Currency, inv.FxRate),
                AccountName = i.CashBox?.Name ?? "—"
            });
        }

        rows = rows.OrderBy(r => r.Date).ToList();
        var closing = foldInUsd
            ? FinancialReportIqdFoldIn.SumCashOrBankInBaseIqd(
                cashBoxes.Select(c => (c.Balance, c.Currency)), asOfUsdToIqd, true)
            : cashBoxes.Sum(c => c.Balance);
        var periodNet = rows.Sum(r => r.Incoming - r.Outgoing);
        var opening = closing - periodNet;
        decimal bal = opening;
        foreach (var r in rows)
        {
            bal += r.Incoming - r.Outgoing;
            r.Balance = bal;
        }

        return new CashBoxMovementReportResult
        {
            OpeningBalance = opening,
            TotalIncoming = rows.Sum(r => r.Incoming),
            TotalOutgoing = rows.Sum(r => r.Outgoing),
            ClosingBalance = closing,
            Currency = selectedCurrency,
            Rows = rows,
            DailyIncomingChart = rows.Where(r => r.Incoming > 0).GroupBy(r => r.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.Incoming) })
                .OrderBy(x => x.Date).ToList(),
            DailyOutgoingChart = rows.Where(r => r.Outgoing > 0).GroupBy(r => r.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.Outgoing) })
                .OrderBy(x => x.Date).ToList()
        };
    }

    public async Task<CashBalancesSummaryReportResult> GetCashBalancesSummaryReportAsync()
    {
        var context = _db;
        var cashBoxes = await context.CashBoxes.ToListAsync();
        var banks = await context.BankAccounts.ToListAsync();

        var rows = cashBoxes.Select(c => new CashBalanceRow
        {
            AccountType = "قاصة",
            Name = c.Name,
            AccountNumber = "—",
            Balance = c.Balance,
            Currency = c.Currency
        }).Concat(banks.Select(b => new CashBalanceRow
        {
            AccountType = "مصرف",
            Name = b.Name,
            AccountNumber = b.AccountNumber ?? "—",
            Balance = b.Balance,
            Currency = b.Currency
        })).OrderBy(r => r.Currency).ThenBy(r => r.AccountType).ThenBy(r => r.Name).ToList();

        var cashTotal = cashBoxes.Where(c => c.Currency == AccountingCurrency.IQD).Sum(c => c.Balance);
        var bankTotal = banks.Where(b => b.Currency == AccountingCurrency.IQD).Sum(b => b.Balance);
        var cashTotalUsd = cashBoxes.Where(c => c.Currency == AccountingCurrency.USD).Sum(c => c.Balance);
        var bankTotalUsd = banks.Where(b => b.Currency == AccountingCurrency.USD).Sum(b => b.Balance);

        return new CashBalancesSummaryReportResult
        {
            CashBoxesTotal = cashTotal,
            BanksTotal = bankTotal,
            TotalLiquid = cashTotal + bankTotal,
            CashBoxesTotalUsd = cashTotalUsd,
            BanksTotalUsd = bankTotalUsd,
            TotalLiquidUsd = cashTotalUsd + bankTotalUsd,
            AccountCount = rows.Count,
            Rows = rows,
            CompositionChart =
            [
                new NameAmountPoint { Name = "قاصات د.ع", Amount = cashTotal },
                new NameAmountPoint { Name = "مصارف د.ع", Amount = bankTotal }
            ]
        };
    }

    public async Task<TransfersReportResult> GetTransfersReportAsync(DateTime? from, DateTime? to)
    {
        var context = _db;
        var query = context.Transfers.AsQueryable();
        if (from.HasValue) query = query.Where(t => t.Date >= from.Value);
        if (to.HasValue) query = query.Where(t => t.Date < EndOfDay(to));

        var transfers = await query.OrderByDescending(t => t.Date).ToListAsync();
        var rows = new List<TransferReportRow>();
        foreach (var t in transfers)
        {
            rows.Add(new TransferReportRow
            {
                Id = t.Id,
                Date = t.Date,
                FromAccount = await ResolveTransferAccountNameAsync(context, t.FromType, t.FromId),
                ToAccount = await ResolveTransferAccountNameAsync(context, t.ToType, t.ToId),
                Amount = t.Amount,
                Currency = t.Currency,
                Notes = t.Notes ?? "—",
                CreatedBy = t.CreatedBy ?? "—"
            });
        }

        var iqdRows = rows.Where(r => r.Currency == AccountingCurrency.IQD).ToList();
        var usdRows = rows.Where(r => r.Currency == AccountingCurrency.USD).ToList();

        return new TransfersReportResult
        {
            TotalAmount = iqdRows.Sum(r => r.Amount),
            TotalAmountUsd = usdRows.Sum(r => r.Amount),
            TransferCount = rows.Count,
            AverageAmount = iqdRows.Count > 0 ? Math.Round(iqdRows.Average(r => r.Amount), 0) : 0,
            Rows = rows,
            DailyChart = iqdRows.GroupBy(r => r.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderBy(x => x.Date).ToList(),
            ByTypeChart = iqdRows.GroupBy(r => $"{r.FromAccount} ←")
                .Select(g => new NameAmountPoint { Name = g.Key.TrimEnd(' ', '←'), Amount = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Amount).Take(10).ToList()
        };
    }

    public async Task<InventoryValuationReportResult> GetInventoryValuationReportAsync(
        int? warehouseId, bool includeZero = false)
    {
        var context = _db;
        var stockQ = context.WarehouseStocks
            .Include(ws => ws.Product).ThenInclude(p => p!.Category)
            .Include(ws => ws.Warehouse)
            .AsQueryable();
        if (warehouseId.HasValue) stockQ = stockQ.Where(ws => ws.WarehouseId == warehouseId.Value);
        if (!includeZero) stockQ = stockQ.Where(ws => ws.Quantity > 0);

        var stocks = await stockQ.ToListAsync();
        var productIds = stocks.Select(s => s.ProductId).Distinct().ToList();
        var purchasesByProduct = await CloudProductCostHelper.GetPurchaseItemsByProductAsync(context, productIds);
        var rows = new List<InventoryValuationRow>();
        foreach (var s in stocks)
        {
            var purchaseItems = purchasesByProduct.GetValueOrDefault(s.ProductId) ?? [];
            var avg = CloudProductCostHelper.ComputeAverageUnitCost(purchaseItems, s.OpeningQuantity, s.UnitCost);
            rows.Add(new InventoryValuationRow
            {
                ProductId = s.ProductId,
                ProductName = s.Product?.Name ?? "—",
                WarehouseName = s.Warehouse?.Name ?? "—",
                CategoryName = s.Product?.Category?.Name ?? "—",
                Quantity = s.Quantity,
                AverageCost = avg,
                TotalValue = Math.Round(s.Quantity * avg, 0)
            });
        }

        rows = rows.OrderByDescending(r => r.TotalValue).ToList();
        return new InventoryValuationReportResult
        {
            TotalValue = rows.Sum(r => r.TotalValue),
            TotalQuantity = rows.Sum(r => r.Quantity),
            ProductCount = rows.Select(r => r.ProductId).Distinct().Count(),
            WarehouseCount = rows.Select(r => r.WarehouseName).Distinct().Count(),
            Rows = rows,
            WarehouseChart = rows.GroupBy(r => r.WarehouseName)
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(x => x.TotalValue) }).ToList(),
            TopProductsChart = rows.Take(10)
                .Select(r => new NameAmountPoint { Name = r.ProductName, Amount = r.TotalValue }).ToList()
        };
    }

    public async Task<WarehouseProductProfitReportResult> GetWarehouseProductProfitReportAsync(
        int? warehouseId, bool includeZero = false)
    {
        var context = _db;
        var stockQ = context.WarehouseStocks
            .Include(ws => ws.Product).ThenInclude(p => p!.Category)
            .Include(ws => ws.Warehouse)
            .AsQueryable();
        if (warehouseId.HasValue) stockQ = stockQ.Where(ws => ws.WarehouseId == warehouseId.Value);
        if (!includeZero) stockQ = stockQ.Where(ws => ws.Quantity > 0);

        var stocks = await stockQ.ToListAsync();
        if (stocks.Count == 0)
            return new WarehouseProductProfitReportResult();

        var productIds = stocks.Select(s => s.ProductId).Distinct().ToList();
        var purchasesByProduct = await CloudProductCostHelper.GetPurchaseItemsByProductAsync(context, productIds);
        var prices = await context.ProductPrices.AsNoTracking()
            .Where(pp => productIds.Contains(pp.ProductId))
            .ToListAsync();
        var salePriceByProduct = prices
            .GroupBy(pp => pp.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Where(p => p.SalePrice > 0).Select(p => (decimal?)p.SalePrice).FirstOrDefault()
                     ?? g.Select(p => (decimal?)p.SalePrice).FirstOrDefault()
                     ?? 0m);

        var rows = new List<WarehouseProductProfitRow>();
        foreach (var s in stocks)
        {
            var purchaseItems = purchasesByProduct.GetValueOrDefault(s.ProductId) ?? [];
            var avg = CloudProductCostHelper.ComputeAverageUnitCost(purchaseItems, s.OpeningQuantity, s.UnitCost);
            var salePrice = salePriceByProduct.GetValueOrDefault(s.ProductId);
            var profit = Math.Round(s.Quantity * (salePrice - avg), 0);
            rows.Add(new WarehouseProductProfitRow
            {
                ProductId = s.ProductId,
                ProductName = s.Product?.Name ?? "—",
                WarehouseName = s.Warehouse?.Name ?? "—",
                CategoryName = s.Product?.Category?.Name ?? "—",
                Quantity = s.Quantity,
                AverageCost = avg,
                SalePrice = salePrice,
                PotentialProfit = profit
            });
        }

        rows = rows.OrderByDescending(r => r.PotentialProfit).ToList();
        var totalCost = rows.Sum(r => Math.Round(r.Quantity * r.AverageCost, 0));
        var totalSale = rows.Sum(r => Math.Round(r.Quantity * r.SalePrice, 0));
        return new WarehouseProductProfitReportResult
        {
            TotalPotentialProfit = rows.Sum(r => r.PotentialProfit),
            TotalSaleValue = totalSale,
            TotalCostValue = totalCost,
            TotalQuantity = rows.Sum(r => r.Quantity),
            ProductCount = rows.Select(r => r.ProductId).Distinct().Count(),
            WarehouseCount = rows.Select(r => r.WarehouseName).Distinct().Count(),
            Rows = rows,
            WarehouseChart = rows.GroupBy(r => r.WarehouseName)
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(x => x.PotentialProfit) }).ToList(),
            TopProductsChart = rows.Take(10)
                .Select(r => new NameAmountPoint { Name = r.ProductName, Amount = r.PotentialProfit }).ToList()
        };
    }

    public async Task<StockTakingReportResult> GetStockTakingReportAsync(int? warehouseId, bool includeZero = true)
    {
        var context = _db;
        var stockQ = context.WarehouseStocks
            .Include(ws => ws.Product).ThenInclude(p => p!.Category)
            .Include(ws => ws.Warehouse)
            .AsQueryable();
        if (warehouseId.HasValue) stockQ = stockQ.Where(ws => ws.WarehouseId == warehouseId.Value);
        if (!includeZero) stockQ = stockQ.Where(ws => ws.Quantity != 0);

        var stocks = await stockQ.OrderBy(ws => ws.Warehouse!.Name).ThenBy(ws => ws.Product!.Name).ToListAsync();
        var rows = stocks.Select(s => new StockTakingRow
        {
            ProductId = s.ProductId,
            ProductName = s.Product?.Name ?? "—",
            Barcode = s.Product?.Barcode,
            WarehouseName = s.Warehouse?.Name ?? "—",
            CategoryName = s.Product?.Category?.Name ?? "—",
            SystemQuantity = s.Quantity,
            CountedQuantity = null
        }).ToList();

        return new StockTakingReportResult
        {
            TotalQuantity = rows.Sum(r => r.SystemQuantity),
            ProductCount = rows.Select(r => r.ProductId).Distinct().Count(),
            WarehouseCount = rows.Select(r => r.WarehouseName).Distinct().Count(),
            Rows = rows,
            WarehouseChart = rows.GroupBy(r => r.WarehouseName)
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(x => x.SystemQuantity) }).ToList()
        };
    }

    public async Task<CogsReportResult> GetCogsReportAsync(
        DateTime? from, DateTime? to, int? warehouseId,
        ReportCurrencyScope currencyScope = ReportCurrencyScope.Iqd)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId), currencyScope);

        var soldQ = context.InvoiceItems
            .Include(ii => ii.Invoice)
            .Include(ii => ii.Product)
            .Where(ii => ii.ProductId != null
                         && ii.Invoice != null
                         && (ii.Invoice.InvoiceType == InvoiceType.Sale || ii.Invoice.InvoiceType == InvoiceType.Installment || ii.Invoice.InvoiceType == InvoiceType.SaleReturn));
        if (!foldInUsd)
        {
            var strict = ReportCurrencyScopeHelper.ToStrictFilter(currencyScope);
            if (strict.HasValue)
                soldQ = soldQ.Where(ii => ii.Invoice!.Currency == strict.Value);
        }
        if (from.HasValue) soldQ = soldQ.Where(ii => ii.Invoice!.Date >= from.Value);
        if (to.HasValue) soldQ = soldQ.Where(ii => ii.Invoice!.Date < EndOfDay(to));
        if (warehouseId.HasValue) soldQ = soldQ.Where(ii => ii.Invoice!.WarehouseId == warehouseId.Value);

        var soldItems = await soldQ.ToListAsync();
        var productIds = soldItems.Select(ii => ii.ProductId!.Value).Distinct().ToList();
        var stocks = await context.WarehouseStocks.Where(ws => productIds.Contains(ws.ProductId)).ToListAsync();
        var allPurchases = await CloudProductCostHelper.GetPurchaseItemsByProductAsync(context, productIds);
        var endExclusive = EndOfDay(to);
        var purchasesByProduct = allPurchases.ToDictionary(
            kv => kv.Key,
            kv => kv.Value
                .Where(ii => !endExclusive.HasValue || ii.Invoice == null || ii.Invoice.Date < endExclusive.Value)
                .ToList());

        decimal LineRevenueInBaseIqd(CloudInvoiceItem x)
        {
            var signed = InvoiceFilters.SignedSaleLineAmount(x.Invoice!.InvoiceType, x.TotalPrice);
            if (x.Invoice.Currency == AccountingCurrency.IQD)
                return signed;
            if (foldInUsd || currencyScope != ReportCurrencyScope.Iqd)
                return AccountingCurrencyRules.ToBaseIqdStrict(signed, AccountingCurrency.USD, x.Invoice.FxRate);
            return 0m;
        }

        var rows = soldItems.GroupBy(ii => ii.ProductId!.Value).Select(g =>
        {
            var avg = CloudProductCostHelper.ComputeAverageUnitCostForProduct(
                purchasesByProduct.GetValueOrDefault(g.Key) ?? [], stocks, g.Key);
            var qty = g.Sum(x => InvoiceFilters.SignedSaleLineQuantity(x.Invoice!.InvoiceType, x.Quantity));
            var revenueIqd = g.Sum(LineRevenueInBaseIqd);
            var cogs = Math.Round(qty * avg, 0);
            return new CogsReportRow
            {
                ProductId = g.Key,
                ProductName = g.First().Product?.Name ?? g.First().ItemName,
                QuantitySold = qty,
                AverageCost = avg,
                CogsAmount = cogs,
                Revenue = revenueIqd,
                GrossProfit = revenueIqd - cogs
            };
        }).OrderByDescending(r => r.CogsAmount).ToList();

        var totalCogs = rows.Sum(r => r.CogsAmount);
        var totalRev = rows.Sum(r => r.Revenue);
        var totalRevUsd = currencyScope == ReportCurrencyScope.All
            ? soldItems
                .Where(x => x.Invoice!.Currency == AccountingCurrency.USD)
                .Sum(x => InvoiceFilters.SignedSaleLineAmount(x.Invoice!.InvoiceType, x.TotalPrice))
            : 0m;

        return new CogsReportResult
        {
            TotalCogs = totalCogs,
            TotalRevenue = totalRev,
            TotalRevenueUsd = totalRevUsd,
            GrossProfit = totalRev - totalCogs,
            CurrencyScope = currencyScope,
            ProductCount = rows.Count,
            Rows = rows,
            TopProductsChart = rows.Take(10)
                .Select(r => new NameAmountPoint { Name = r.ProductName, Amount = r.CogsAmount }).ToList(),
            DailyChart = soldItems.GroupBy(ii => ii.Invoice!.Date.Date)
                .Select(g =>
                {
                    decimal dayCogs = 0;
                    foreach (var item in g)
                    {
                        var avg = CloudProductCostHelper.ComputeAverageUnitCostForProduct(
                            purchasesByProduct.GetValueOrDefault(item.ProductId!.Value) ?? [], stocks, item.ProductId!.Value);
                        dayCogs += Math.Round(InvoiceFilters.SignedSaleLineQuantity(item.Invoice!.InvoiceType, item.Quantity) * avg, 0);
                    }
                    return new DailyAmountPoint { Date = g.Key, Amount = dayCogs };
                }).OrderBy(x => x.Date).ToList()
        };
    }

    // ── Financial statements ─────────────────────────────────────

    public async Task<FinancialPositionSummaryReportResult> GetFinancialPositionSummaryReportAsync(DateTime? asOfDate)
    {
        var date = asOfDate?.Date ?? DateTime.Today;
        var bs = await GetStatementOfFinancialPositionReportAsync(date);
        var rows = new List<FinancialPositionLineRow>
        {
            new() { Section = "أصول", LineName = "نقد ومصارف", Amount = bs.CashAndBanks },
            new() { Section = "أصول", LineName = "ذمم مدينة", Amount = bs.Receivables },
            new() { Section = "أصول", LineName = "ذمم أقساط", Amount = bs.InstallmentReceivables },
            new() { Section = "أصول", LineName = "مخزون", Amount = bs.Inventory },
            new() { Section = "التزامات", LineName = "ذمم دائنة", Amount = bs.Payables },
            new() { Section = "التزامات", LineName = "رأس مال مستثمرين", Amount = bs.InvestorCapital },
            new() { Section = "حقوق ملكية", LineName = "رأس المال", Amount = bs.Capital },
            new() { Section = "حقوق ملكية", LineName = "تعديلات", Amount = bs.Adjustments },
            new() { Section = "حقوق ملكية", LineName = "أرباح متراكمة", Amount = bs.AccumulatedProfits }
        };

        return new FinancialPositionSummaryReportResult
        {
            TotalAssets = bs.TotalAssets,
            TotalLiabilities = bs.TotalLiabilities,
            TotalEquity = bs.TotalEquity,
            NetWorkingCapital = bs.CashAndBanks + bs.Receivables + bs.InstallmentReceivables - bs.Payables,
            Difference = bs.Difference,
            IsBalanced = bs.IsBalanced,
            Rows = rows,
            CompositionChart =
            [
                new NameAmountPoint { Name = "أصول", Amount = bs.TotalAssets },
                new NameAmountPoint { Name = "التزامات", Amount = bs.TotalLiabilities },
                new NameAmountPoint { Name = "حقوق ملكية", Amount = bs.TotalEquity }
            ]
        };
    }

    public async Task<ProfitAndLossReportResult> GetProfitAndLossReportAsync(
        DateTime? from, DateTime? to,
        ReportCurrencyScope currencyScope = ReportCurrencyScope.Iqd)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var multiCurrency = await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId);
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(multiCurrency, currencyScope);

        var salesBaseQ = foldInUsd
            ? CloudInvoiceFilters.ForProfitAndSalesTotalsAll(context.Invoices, context.InstallmentPlans)
            : CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans, AccountingCurrency.IQD);
        var salesUsdQ = CloudInvoiceFilters.ForProfitAndSalesTotals(
            context.Invoices, context.InstallmentPlans, AccountingCurrency.USD);
        var expBaseQ = foldInUsd
            ? context.Expenses.AsQueryable()
            : context.Expenses.Where(e => e.Currency == AccountingCurrency.IQD);
        var expUsdQ = context.Expenses.Where(e => e.Currency == AccountingCurrency.USD);
        var bankBaseQ = foldInUsd
            ? context.Vouchers.Where(v => v.VoucherType == VoucherType.BankReceipt)
            : context.Vouchers.Where(v => v.VoucherType == VoucherType.BankReceipt && v.Currency == AccountingCurrency.IQD);
        var distQ = context.ProfitDistributions.AsQueryable();
        if (from.HasValue)
        {
            salesBaseQ = salesBaseQ.Where(i => i.Date >= from.Value);
            salesUsdQ = salesUsdQ.Where(i => i.Date >= from.Value);
            expBaseQ = expBaseQ.Where(e => e.Date >= from.Value);
            expUsdQ = expUsdQ.Where(e => e.Date >= from.Value);
            bankBaseQ = bankBaseQ.Where(v => v.Date >= from.Value);
            distQ = distQ.Where(d => d.Date >= from.Value);
        }
        if (to.HasValue)
        {
            salesBaseQ = salesBaseQ.Where(i => i.Date < EndOfDay(to));
            salesUsdQ = salesUsdQ.Where(i => i.Date < EndOfDay(to));
            expBaseQ = expBaseQ.Where(e => e.Date < EndOfDay(to));
            expUsdQ = expUsdQ.Where(e => e.Date < EndOfDay(to));
            bankBaseQ = bankBaseQ.Where(v => v.Date < EndOfDay(to));
            distQ = distQ.Where(d => d.Date < EndOfDay(to));
        }

        var salesRows = await salesBaseQ
            .Select(i => new { i.InvoiceType, i.NetAmount, i.Currency, i.FxRate })
            .ToListAsync();
        var sales = FinancialReportIqdFoldIn.SumSignedSalesInBaseIqd(
            salesRows.Select(r => (r.InvoiceType, r.NetAmount, r.Currency, r.FxRate)), foldInUsd);
        var salesUsd = await CloudInvoiceFilters.SumSignedNetAsync(salesUsdQ);
        var cogs = await CalculateCogsAsync(context, from, EndOfDay(to));
        var expenseRows = await expBaseQ
            .Select(e => new { e.Amount, e.Currency, e.FxRate })
            .ToListAsync();
        var expenses = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            expenseRows.Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);
        var expensesUsd = await expUsdQ.SumAsync(e => (decimal?)e.Amount) ?? 0;
        var bankRows = await bankBaseQ
            .Select(v => new { v.BankFees, v.Currency, v.FxRate })
            .ToListAsync();
        var bankFees = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            bankRows.Select(r => (r.BankFees, r.Currency, r.FxRate)), foldInUsd);
        var distributed = await distQ.SumAsync(d => (decimal?)d.DistributedAmount) ?? 0;

        if (currencyScope == ReportCurrencyScope.Usd)
        {
            return new ProfitAndLossReportResult
            {
                TotalSales = 0,
                TotalSalesUsd = salesUsd,
                CostOfGoodsSold = 0,
                GrossProfit = 0,
                TotalExpenses = 0,
                TotalExpensesUsd = expensesUsd,
                TotalBankFees = 0,
                OperatingProfit = 0,
                DistributedProfits = 0,
                NetProfit = 0,
                CurrencyScope = currencyScope,
                Lines =
                [
                    new() { LineName = "صافي المبيعات ($)", Amount = salesUsd },
                    new() { LineName = "المصاريف ($)", Amount = -expensesUsd },
                    new() { LineName = "إفصاح دولار (بدون دمج مع تكلفة/ربح دينار)", Amount = salesUsd - expensesUsd, IsTotal = true }
                ],
                CompositionChart =
                [
                    new NameAmountPoint { Name = "مبيعات $", Amount = salesUsd },
                    new NameAmountPoint { Name = "مصاريف $", Amount = expensesUsd }
                ],
                MonthlyChart = []
            };
        }

        var gross = sales - cogs;
        var operating = gross - expenses - bankFees;
        var net = operating - distributed;

        var lines = new List<ProfitAndLossLineRow>
        {
            new() { LineName = "صافي المبيعات", Amount = sales },
            new() { LineName = "تكلفة البضاعة المباعة", Amount = -cogs },
            new() { LineName = "إجمالي الربح", Amount = gross, IsSubtotal = true },
            new() { LineName = "المصاريف", Amount = -expenses },
            new() { LineName = "الرسوم البنكية", Amount = -bankFees },
            new() { LineName = "صافي الربح التشغيلي", Amount = operating, IsSubtotal = true },
            new() { LineName = "توزيعات الأرباح", Amount = -distributed },
            new() { LineName = "صافي الربح / الخسارة", Amount = net, IsTotal = true }
        };
        if (currencyScope == ReportCurrencyScope.All && (salesUsd != 0 || expensesUsd != 0))
        {
            lines.Add(new ProfitAndLossLineRow { LineName = "إفصاح مبيعات دولار", Amount = salesUsd });
            lines.Add(new ProfitAndLossLineRow { LineName = "إفصاح مصاريف دولار", Amount = -expensesUsd });
        }

        var monthly = await GetMonthlyProfitAsync(from, to);

        return new ProfitAndLossReportResult
        {
            TotalSales = sales,
            TotalSalesUsd = salesUsd,
            CostOfGoodsSold = cogs,
            GrossProfit = gross,
            TotalExpenses = expenses,
            TotalExpensesUsd = expensesUsd,
            TotalBankFees = bankFees,
            OperatingProfit = operating,
            DistributedProfits = distributed,
            NetProfit = net,
            GrossMarginPercent = sales > 0 ? Math.Round(gross / sales * 100, 1) : 0,
            NetMarginPercent = sales > 0 ? Math.Round(net / sales * 100, 1) : 0,
            CurrencyScope = currencyScope,
            Lines = lines,
            CompositionChart =
            [
                new NameAmountPoint { Name = "مبيعات", Amount = sales },
                new NameAmountPoint { Name = "COGS", Amount = cogs },
                new NameAmountPoint { Name = "مصاريف", Amount = expenses },
                new NameAmountPoint { Name = "صافي", Amount = Math.Max(0, net) }
            ],
            MonthlyChart = monthly.Select(m =>
            {
                var parts = m.Month.Split('/');
                return new DailyAmountPoint
                {
                    Date = new DateTime(int.Parse(parts[0]), int.Parse(parts[1]), 1),
                    Amount = m.NetProfit
                };
            }).ToList()
        };
    }

    public async Task<StatementOfFinancialPositionReportResult> GetStatementOfFinancialPositionReportAsync(DateTime date)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var endOfDay = date.Date.AddDays(1).AddTicks(-1);
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var asOfUsdToIqd = foldInUsd
            ? await GetAsOfUsdToIqdRateAsync(date)
            : 0m;

        var capital = await context.CapitalEntries
            .Where(c => c.Type == CapitalEntryType.Initial && c.Date <= endOfDay)
            .SumAsync(c => c.Amount);
        var adjustments = await context.CapitalEntries
            .Where(c => c.Type == CapitalEntryType.Adjustment && c.Date <= endOfDay)
            .SumAsync(c => c.Amount);
        var profitOpening = await CloudProductCostHelper.GetProfitOpeningBalanceAsync(context, endOfDay);

        var salesBaseQ = foldInUsd
            ? CloudInvoiceFilters.ForProfitAndSalesTotalsAll(context.Invoices, context.InstallmentPlans)
            : CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans, AccountingCurrency.IQD);
        var salesRows = await salesBaseQ.Where(i => i.Date <= endOfDay)
            .Select(i => new { i.InvoiceType, i.NetAmount, i.Currency, i.FxRate })
            .ToListAsync();
        var sales = FinancialReportIqdFoldIn.SumSignedSalesInBaseIqd(
            salesRows.Select(r => (r.InvoiceType, r.NetAmount, r.Currency, r.FxRate)), foldInUsd);
        var cogs = await CalculateCogsAsync(context, null, endOfDay.AddTicks(1));
        var expenseQ = foldInUsd
            ? context.Expenses.Where(e => e.Date <= endOfDay)
            : context.Expenses.Where(e => e.Currency == AccountingCurrency.IQD && e.Date <= endOfDay);
        var expenseRows = await expenseQ.Select(e => new { e.Amount, e.Currency, e.FxRate }).ToListAsync();
        var expenses = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            expenseRows.Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);
        var distributed = await context.ProfitDistributions
            .Where(d => d.Date <= endOfDay).SumAsync(d => (decimal?)d.DistributedAmount) ?? 0;
        var accumulated = profitOpening + (sales - cogs) - expenses - distributed;
        var equity = capital + adjustments + accumulated;

        var cashBoxes = await context.CashBoxes
            .Where(c => foldInUsd || c.Currency == AccountingCurrency.IQD)
            .Select(c => new { c.Balance, c.Currency })
            .ToListAsync();
        var cash = FinancialReportIqdFoldIn.SumCashOrBankInBaseIqd(
            cashBoxes.Select(c => (c.Balance, c.Currency)), asOfUsdToIqd, foldInUsd);
        var bankAccounts = await context.BankAccounts
            .Where(b => foldInUsd || b.Currency == AccountingCurrency.IQD)
            .Select(b => new { b.Balance, b.Currency })
            .ToListAsync();
        var banks = FinancialReportIqdFoldIn.SumCashOrBankInBaseIqd(
            bankAccounts.Select(b => (b.Balance, b.Currency)), asOfUsdToIqd, foldInUsd);

        var creditInvQ = context.Invoices.Where(i =>
            (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) &&
            i.PaymentMethod == PaymentMethod.Credit &&
            i.Date <= endOfDay &&
            (foldInUsd || i.Currency == AccountingCurrency.IQD));
        var creditRemaining = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            (await creditInvQ.Select(i => new { i.RemainingAmount, i.Currency, i.FxRate }).ToListAsync())
            .Select(r => (r.RemainingAmount, r.Currency, r.FxRate)), foldInUsd);
        var saleReturnQ = context.Invoices.Where(i =>
            i.InvoiceType == InvoiceType.SaleReturn &&
            i.PaymentMethod == PaymentMethod.Credit &&
            i.Date <= endOfDay &&
            (foldInUsd || i.Currency == AccountingCurrency.IQD));
        var saleReturnCredits = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            (await saleReturnQ.Select(i => new { i.RemainingAmount, i.Currency, i.FxRate }).ToListAsync())
            .Select(r => (r.RemainingAmount, r.Currency, r.FxRate)), foldInUsd);
        var unappliedDebtQ = context.Vouchers.Where(v =>
            v.CustomerId != null &&
            v.VoucherType == VoucherType.DebtReceipt &&
            v.Date <= endOfDay &&
            !v.InvoiceId.HasValue &&
            !v.InstallmentId.HasValue &&
            (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)) &&
            (foldInUsd || v.Currency == AccountingCurrency.IQD));
        var unappliedDebt = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            (await unappliedDebtQ.Select(v => new { v.Amount, v.Currency, v.FxRate }).ToListAsync())
            .Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);
        var unappliedReceiptsQ = context.Vouchers.Where(v =>
            v.CustomerId != null &&
            v.VoucherType == VoucherType.Receipt &&
            v.Date <= endOfDay &&
            !v.InvoiceId.HasValue &&
            !v.InstallmentId.HasValue &&
            (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)) &&
            (foldInUsd || v.Currency == AccountingCurrency.IQD));
        var unappliedReceipts = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            (await unappliedReceiptsQ.Select(v => new { v.Amount, v.Currency, v.FxRate }).ToListAsync())
            .Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);
        var creditAr = CustomerBalanceHelper.ComputeOutstandingBalance(
            creditRemaining, 0, unappliedDebt, unappliedReceipts + saleReturnCredits);
        var installmentRows = await context.Installments
            .Where(i => i.RemainingAmount > 0 &&
                        (foldInUsd || i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD))
            .Select(i => new
            {
                i.RemainingAmount,
                Currency = i.InstallmentPlan!.Invoice!.Currency,
                FxRate = i.InstallmentPlan!.Invoice!.FxRate
            })
            .ToListAsync();
        var installmentAr = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            installmentRows.Select(r => (r.RemainingAmount, r.Currency, r.FxRate)), foldInUsd);

        var stocks = await context.WarehouseStocks.Include(ws => ws.Product).ToListAsync();
        var inventoryProductIds = stocks.Where(s => s.Quantity > 0).Select(s => s.ProductId).Distinct().ToList();
        var inventoryPurchases = await CloudProductCostHelper.GetPurchaseItemsByProductAsync(context, inventoryProductIds);
        decimal inventory = 0;
        foreach (var s in stocks.Where(s => s.Quantity > 0))
        {
            var purchaseItems = (inventoryPurchases.GetValueOrDefault(s.ProductId) ?? [])
                .Where(ii => ii.Invoice == null || ii.Invoice.Date <= endOfDay)
                .ToList();
            var avg = CloudProductCostHelper.ComputeAverageUnitCost(purchaseItems, s.OpeningQuantity, s.UnitCost);
            inventory += Math.Round(s.Quantity * avg, 0);
        }

        var supplierCreditQ = context.Invoices.Where(i =>
            i.InvoiceType == InvoiceType.Purchase
            && i.PaymentMethod == PaymentMethod.Credit
            && i.Date <= endOfDay
            && (foldInUsd || i.Currency == AccountingCurrency.IQD));
        var supplierCreditRemaining = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            (await supplierCreditQ.Select(i => new { i.RemainingAmount, i.Currency, i.FxRate }).ToListAsync())
            .Select(r => (r.RemainingAmount, r.Currency, r.FxRate)), foldInUsd);
        var supplierReturnQ = context.Invoices.Where(i =>
            i.InvoiceType == InvoiceType.PurchaseReturn
            && i.PaymentMethod == PaymentMethod.Credit
            && i.Date <= endOfDay
            && (foldInUsd || i.Currency == AccountingCurrency.IQD));
        var supplierReturnCredits = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            (await supplierReturnQ.Select(i => new { i.RemainingAmount, i.Currency, i.FxRate }).ToListAsync())
            .Select(r => (r.RemainingAmount, r.Currency, r.FxRate)), foldInUsd);
        var unappliedSupplierQ = context.Vouchers.Where(v =>
            v.SupplierId != null
            && v.VoucherType == VoucherType.Payment
            && v.Date <= endOfDay
            && !v.InvoiceId.HasValue
            && (v.Notes == null || !v.Notes.Contains(SupplierBalanceHelper.PaymentAppliedMarker))
            && (foldInUsd || v.Currency == AccountingCurrency.IQD));
        var unappliedSupplierPayments = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            (await unappliedSupplierQ.Select(v => new { v.Amount, v.Currency, v.FxRate }).ToListAsync())
            .Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);
        var payables = SupplierBalanceHelper.ComputeOutstandingPayables(
            supplierCreditRemaining, unappliedSupplierPayments + supplierReturnCredits);

        var invDep = await context.InvestorTransactions
            .Where(t => t.Type == InvestorTransactionType.Deposit && t.Date <= endOfDay)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;
        var invWd = await context.InvestorTransactions
            .Where(t => t.Type == InvestorTransactionType.Withdrawal && t.Date <= endOfDay)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;
        var investorCapital = Math.Max(0, invDep - invWd);

        var assets = cash + banks + creditAr + installmentAr + inventory;
        var liabilities = payables + investorCapital;
        var diff = (equity + liabilities) - assets;

        var rows = new List<StatementOfFinancialPositionLineRow>
        {
            new() { Section = "الأصول", LineName = "النقدية في القاصات", Amount = cash,
                Formula = "مجموع أرصدة القاصات الحالية" },
            new() { Section = "الأصول", LineName = "الأرصدة المصرفية", Amount = banks,
                Formula = "مجموع أرصدة الحسابات المصرفية" },
            new() { Section = "الأصول", LineName = "الذمم المدينة (آجل)", Amount = creditAr,
                Formula = "متبقي مبيعات الآجل − سندات قبض/دين غير مطبّقة" },
            new() { Section = "الأصول", LineName = "ذمم الأقساط", Amount = installmentAr,
                Formula = "مجموع المتبقي من أقساط غير مسددة بالكامل" },
            new() { Section = "الأصول", LineName = "المخزون بالتكلفة", Amount = inventory,
                Formula = "الكمية × متوسط تكلفة الشراء (حتى التاريخ)" },
            new() { Section = "الأصول", LineName = "إجمالي الأصول", Amount = assets, IsTotal = true,
                Formula = "نقد + مصارف + ذمم آجل + أقساط + مخزون" },
            new() { Section = "الالتزامات", LineName = "الذمم الدائنة", Amount = payables,
                Formula = "متبقي مشتريات الآجل − سندات صرف غير مطبّقة" },
            new() { Section = "الالتزامات", LineName = "ودائع المستثمرين", Amount = investorCapital,
                Formula = "إيداعات المستثمرين − السحوبات" },
            new() { Section = "الالتزامات", LineName = "إجمالي الالتزامات", Amount = liabilities, IsTotal = true,
                Formula = "ذمم دائنة + ودائع المستثمرين" },
            new() { Section = "حقوق الملكية", LineName = "رأس المال", Amount = capital,
                Formula = "مجموع قيود رأس المال الافتتاحي حتى التاريخ" },
            new() { Section = "حقوق الملكية", LineName = "تعديلات رأس المال", Amount = adjustments,
                Formula = "مجموع تعديلات رأس المال حتى التاريخ" },
            new() { Section = "حقوق الملكية", LineName = "الأرباح المتراكمة", Amount = accumulated,
                Formula = "رصيد أرباح افتتاحي + (مبيعات − تكلفة) − مصاريف − توزيعات" },
            new() { Section = "حقوق الملكية", LineName = "إجمالي حقوق الملكية", Amount = equity, IsTotal = true,
                Formula = "رأس المال + التعديلات + الأرباح المتراكمة" }
        };

        return new StatementOfFinancialPositionReportResult
        {
            CashAndBanks = cash + banks,
            Receivables = creditAr,
            InstallmentReceivables = installmentAr,
            Inventory = inventory,
            TotalAssets = assets,
            Payables = payables,
            InvestorCapital = investorCapital,
            TotalLiabilities = liabilities,
            Capital = capital,
            Adjustments = adjustments,
            AccumulatedProfits = accumulated,
            TotalEquity = equity,
            Difference = diff,
            IsBalanced = Math.Abs(diff) < 1m,
            Rows = rows,
            AssetsChart =
            [
                new NameAmountPoint { Name = "نقد ومصارف", Amount = cash + banks },
                new NameAmountPoint { Name = "ذمم", Amount = creditAr + installmentAr },
                new NameAmountPoint { Name = "مخزون", Amount = inventory }
            ],
            EquityLiabilitiesChart =
            [
                new NameAmountPoint { Name = "التزامات", Amount = liabilities },
                new NameAmountPoint { Name = "حقوق ملكية", Amount = equity }
            ]
        };
    }

    public async Task<WorkSummaryReportResult> GetWorkSummaryAsync(DateTime? from, DateTime? to)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var endExclusive = EndOfDay(to);

        var customersQ = context.Customers.AsNoTracking().AsQueryable();
        if (from.HasValue) customersQ = customersQ.Where(c => c.CreatedAt >= from.Value);
        if (endExclusive.HasValue) customersQ = customersQ.Where(c => c.CreatedAt < endExclusive.Value);
        var newCustomersCount = await customersQ.CountAsync();

        var salesInvoicesQ = (foldInUsd
                ? CloudInvoiceFilters.ForProfitAndSalesTotalsAll(context.Invoices, context.InstallmentPlans)
                : CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans))
            .AsNoTracking();
        if (from.HasValue) salesInvoicesQ = salesInvoicesQ.Where(i => i.Date >= from.Value);
        if (endExclusive.HasValue) salesInvoicesQ = salesInvoicesQ.Where(i => i.Date < endExclusive.Value);

        var salesInvoices = await salesInvoicesQ
            .Select(i => new { i.Id, i.Date, i.InvoiceType, i.NetAmount, i.Currency, i.FxRate, i.CustomerId, CustomerName = i.Customer != null ? i.Customer.Name : "—" })
            .ToListAsync();

        var salesInvoiceIds = salesInvoices.Select(i => i.Id).ToList();
        var salesItems = salesInvoiceIds.Count == 0
            ? new List<(int? ProductId, decimal Quantity)>()
            : (await context.InvoiceItems.AsNoTracking()
                .Where(ii => salesInvoiceIds.Contains(ii.InvoiceId))
                .Select(ii => new { ii.ProductId, ii.Quantity, InvoiceType = ii.Invoice!.InvoiceType })
                .ToListAsync())
              .Select(ii => (ProductId: ii.ProductId, Quantity: InvoiceFilters.SignedSaleLineQuantity(ii.InvoiceType, ii.Quantity)))
              .ToList();

        var allActivityQ = context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.Sale
                        || i.InvoiceType == InvoiceType.Installment
                        || i.InvoiceType == InvoiceType.SaleReturn
                        || i.InvoiceType == InvoiceType.Purchase
                        || i.InvoiceType == InvoiceType.PurchaseReturn);
        if (from.HasValue) allActivityQ = allActivityQ.Where(i => i.Date >= from.Value);
        if (endExclusive.HasValue) allActivityQ = allActivityQ.Where(i => i.Date < endExclusive.Value);

        var activityDates = await allActivityQ
            .Select(i => new { i.Date, i.NetAmount, i.InvoiceType, i.Currency, i.FxRate })
            .ToListAsync();

        decimal SaleAmount(InvoiceType type, decimal net, AccountingCurrency currency, decimal fx) =>
            FinancialReportIqdFoldIn.SignedSalesInBaseIqd(type, net, currency, fx, foldInUsd);

        var salesByYear = salesInvoices
            .GroupBy(i => i.Date.Year)
            .OrderBy(g => g.Key)
            .Select(g => new NameAmountPoint { Name = g.Key.ToString(), Amount = g.Sum(x => SaleAmount(x.InvoiceType, x.NetAmount, x.Currency, x.FxRate)) })
            .ToList();

        var topCustomers = salesInvoices
            .Where(i => i.CustomerId.HasValue)
            .GroupBy(i => new { i.CustomerId, i.CustomerName })
            .Select(g => new NameAmountPoint { Name = g.Key.CustomerName, Amount = g.Sum(x => SaleAmount(x.InvoiceType, x.NetAmount, x.Currency, x.FxRate)) })
            .OrderByDescending(x => x.Amount)
            .Take(10)
            .ToList();

        var hourGroups = activityDates
            .GroupBy(i => i.Date.Hour)
            .ToDictionary(g => g.Key, g => g.ToList());

        var hourRows = Enumerable.Range(0, 24)
            .Select(h =>
            {
                hourGroups.TryGetValue(h, out var list);
                list ??= [];
                var salesAmount = list
                    .Where(x => x.InvoiceType is InvoiceType.Sale or InvoiceType.Installment or InvoiceType.SaleReturn)
                    .Sum(x => SaleAmount(x.InvoiceType, x.NetAmount, x.Currency, x.FxRate));
                return new WorkSummaryHourRow
                {
                    Hour = h,
                    HourLabel = $"{h:00}:00",
                    ActivityCount = list.Count,
                    SalesAmount = salesAmount
                };
            })
            .ToList();

        var busiestHours = hourRows
            .Select(r => new NameAmountPoint { Name = r.HourLabel, Amount = r.ActivityCount })
            .ToList();

        return new WorkSummaryReportResult
        {
            NewCustomersCount = newCustomersCount,
            TotalSalesAmount = salesInvoices.Sum(i => SaleAmount(i.InvoiceType, i.NetAmount, i.Currency, i.FxRate)),
            DealCount = salesInvoices.Count,
            DistinctProductCount = salesItems.Where(i => i.ProductId.HasValue).Select(i => i.ProductId!.Value).Distinct().Count(),
            TotalProductQuantity = salesItems.Sum(i => i.Quantity),
            SalesByYearChart = salesByYear,
            TopCustomersChart = topCustomers,
            BusiestHoursChart = busiestHours,
            HourRows = hourRows
        };
    }

    public async Task<ExecutiveBusinessSummaryResult> GetExecutiveBusinessSummaryAsync(DateTime? from, DateTime? to)
    {
        var asOf = to?.Date ?? DateTime.Today;
        var endExclusive = EndOfDay(to);
        var asOfEndOfDay = asOf.Date.AddDays(1).AddTicks(-1);

        var bs = await GetStatementOfFinancialPositionReportAsync(asOf);
        var profit = await GetProfitReportAsync(from, to);
        var stock = await GetWarehouseProductProfitReportAsync(null, includeZero: false);
        var cash = await GetCashBalancesSummaryReportAsync();

        var context = _db;

        var salesQ = CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans);
        if (from.HasValue) salesQ = salesQ.Where(i => i.Date >= from.Value);
        if (endExclusive.HasValue) salesQ = salesQ.Where(i => i.Date < endExclusive.Value);

        var salesInvoices = await salesQ
            .Select(i => new { i.PaymentMethod, i.InvoiceType, i.NetAmount, i.CustomerId })
            .ToListAsync();
        var salesByMethod = salesInvoices
            .GroupBy(i => i.PaymentMethod)
            .Select(g => new
            {
                Method = g.Key,
                Amount = g.Sum(x => InvoiceFilters.SignedNetAmount(x.InvoiceType, x.NetAmount)),
                Count = g.Count()
            })
            .ToList();

        var cashSales = salesByMethod.FirstOrDefault(x => x.Method == PaymentMethod.Cash)?.Amount ?? 0;
        var creditSales = salesByMethod.FirstOrDefault(x => x.Method == PaymentMethod.Credit)?.Amount ?? 0;
        var installmentSales = salesByMethod.FirstOrDefault(x => x.Method == PaymentMethod.Installment)?.Amount ?? 0;
        var salesCount = salesByMethod.Sum(x => x.Count);
        var totalSales = salesByMethod.Sum(x => x.Amount);

        var purchaseQ = CloudInvoiceFilters.ForPurchasesTotals(context.Invoices.AsNoTracking());
        if (from.HasValue) purchaseQ = purchaseQ.Where(i => i.Date >= from.Value);
        if (endExclusive.HasValue) purchaseQ = purchaseQ.Where(i => i.Date < endExclusive.Value);

        var purchaseInvoices = await purchaseQ
            .Select(i => new { i.PaymentMethod, i.InvoiceType, i.NetAmount, i.SupplierId })
            .ToListAsync();
        var purchasesByMethod = purchaseInvoices
            .GroupBy(i => i.PaymentMethod)
            .Select(g => new
            {
                Method = g.Key,
                Amount = g.Sum(x => InvoiceFilters.SignedNetAmount(x.InvoiceType, x.NetAmount)),
                Count = g.Count()
            })
            .ToList();

        var cashPurchases = purchasesByMethod.FirstOrDefault(x => x.Method == PaymentMethod.Cash)?.Amount ?? 0;
        var creditPurchases = purchasesByMethod.FirstOrDefault(x => x.Method == PaymentMethod.Credit)?.Amount ?? 0;
        var purchaseCount = purchasesByMethod.Sum(x => x.Count);
        var totalPurchases = purchasesByMethod.Sum(x => x.Amount);

        var activeCustomers = salesInvoices
            .Where(i => i.CustomerId != null)
            .Select(i => i.CustomerId!.Value)
            .Distinct()
            .Count();

        var activeSuppliers = purchaseInvoices
            .Where(i => i.SupplierId != null)
            .Select(i => i.SupplierId!.Value)
            .Distinct()
            .Count();

        var receiptQ = context.Vouchers.AsNoTracking()
            .Where(v => (v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt)
                        && v.Currency == AccountingCurrency.IQD);
        if (from.HasValue) receiptQ = receiptQ.Where(v => v.Date >= from.Value);
        if (endExclusive.HasValue) receiptQ = receiptQ.Where(v => v.Date < endExclusive.Value);
        var receiptAmount = await receiptQ.SumAsync(v => (decimal?)v.Amount) ?? 0;

        var paymentQ = context.Vouchers.AsNoTracking()
            .Where(v => v.VoucherType == VoucherType.Payment && v.Currency == AccountingCurrency.IQD);
        if (from.HasValue) paymentQ = paymentQ.Where(v => v.Date >= from.Value);
        if (endExclusive.HasValue) paymentQ = paymentQ.Where(v => v.Date < endExclusive.Value);
        var paymentAmount = await paymentQ.SumAsync(v => (decimal?)v.Amount) ?? 0;

        var instPaidQ = context.Installments.AsNoTracking()
            .Where(i => i.PaidAmount > 0
                        && i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD);
        if (from.HasValue) instPaidQ = instPaidQ.Where(i => (i.PaymentDate ?? i.DueDate) >= from.Value);
        if (endExclusive.HasValue) instPaidQ = instPaidQ.Where(i => (i.PaymentDate ?? i.DueDate) < endExclusive.Value);
        var collectedInstallments = await instPaidQ.SumAsync(i => (decimal?)i.PaidAmount) ?? 0;

        var overdueQ = context.Installments.AsNoTracking()
            .Where(i => i.Status != InstallmentStatus.Paid
                        && i.DueDate < asOf
                        && i.RemainingAmount > 0
                        && i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD);
        var overdueCount = await overdueQ.CountAsync();
        var overdueAmount = await overdueQ.SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var transferQ = context.Transfers.AsNoTracking()
            .Where(t => t.Currency == AccountingCurrency.IQD);
        if (from.HasValue) transferQ = transferQ.Where(t => t.Date >= from.Value);
        if (endExclusive.HasValue) transferQ = transferQ.Where(t => t.Date < endExclusive.Value);
        var transfersCount = await transferQ.CountAsync();
        var transfersAmount = await transferQ.SumAsync(t => (decimal?)t.Amount) ?? 0;

        // رصيد الآجل للعملاء = متبقي فواتير الآجل − سندات قبض/دين غير مطبّقة − مرتجع آجل
        var customerCreditRemaining = await context.Invoices.AsNoTracking()
            .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment)
                        && i.PaymentMethod == PaymentMethod.Credit
                        && !i.IsCreditPaid
                        && i.Currency == AccountingCurrency.IQD
                        && i.Date <= asOfEndOfDay)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var saleReturnCredits = await context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.SaleReturn
                        && i.PaymentMethod == PaymentMethod.Credit
                        && i.Date <= asOfEndOfDay)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var unappliedDebt = await context.Vouchers.AsNoTracking()
            .Where(v => v.VoucherType == VoucherType.DebtReceipt
                        && v.Currency == AccountingCurrency.IQD
                        && v.Date <= asOfEndOfDay
                        && (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;

        var unappliedReceipts = await context.Vouchers.AsNoTracking()
            .Where(v => v.VoucherType == VoucherType.Receipt
                        && v.Currency == AccountingCurrency.IQD
                        && v.Date <= asOfEndOfDay
                        && !v.InvoiceId.HasValue
                        && !v.InstallmentId.HasValue
                        && (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;

        var customerReceivables = Math.Max(0, customerCreditRemaining - unappliedDebt - unappliedReceipts - saleReturnCredits);

        // رصيد الآجل للموردين = متبقي مشتريات الآجل − سندات صرف غير مطبّقة − مرتجع آجل
        var supplierCreditRemaining = await context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.Purchase
                        && i.PaymentMethod == PaymentMethod.Credit
                        && !i.IsCreditPaid
                        && i.Currency == AccountingCurrency.IQD
                        && i.Date <= asOfEndOfDay)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var supplierReturnCredits = await context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.PurchaseReturn
                        && i.PaymentMethod == PaymentMethod.Credit
                        && i.Date <= asOfEndOfDay)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var unappliedSupplierPayments = await context.Vouchers.AsNoTracking()
            .Where(v => v.VoucherType == VoucherType.Payment
                        && v.SupplierId != null
                        && v.Currency == AccountingCurrency.IQD
                        && v.Date <= asOfEndOfDay
                        && !v.InvoiceId.HasValue
                        && (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;

        var supplierPayables = SupplierBalanceHelper.ComputeOutstandingPayables(
            supplierCreditRemaining, unappliedSupplierPayments + supplierReturnCredits);

        var installmentReceivables = await context.Installments.AsNoTracking()
            .Where(i => i.RemainingAmount > 0
                        && i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var customerBalanceMap = new Dictionary<int, decimal>();
        var creditByCustomer = await context.Invoices.AsNoTracking()
            .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment)
                        && i.PaymentMethod == PaymentMethod.Credit
                        && !i.IsCreditPaid
                        && i.Currency == AccountingCurrency.IQD
                        && i.Date <= asOfEndOfDay
                        && i.CustomerId != null
                        && i.RemainingAmount > 0)
            .GroupBy(i => i.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Amount = g.Sum(x => x.RemainingAmount) })
            .ToListAsync();

        var unappliedDebtByCustomer = await context.Vouchers.AsNoTracking()
            .Where(v => v.VoucherType == VoucherType.DebtReceipt
                        && v.CustomerId != null
                        && v.Currency == AccountingCurrency.IQD
                        && v.Date <= asOfEndOfDay
                        && (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .GroupBy(v => v.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync();

        var unappliedReceiptByCustomer = await context.Vouchers.AsNoTracking()
            .Where(v => v.VoucherType == VoucherType.Receipt
                        && v.CustomerId != null
                        && v.Currency == AccountingCurrency.IQD
                        && v.Date <= asOfEndOfDay
                        && !v.InvoiceId.HasValue
                        && !v.InstallmentId.HasValue
                        && (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .GroupBy(v => v.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync();

        var returnCreditByCustomer = await context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.SaleReturn
                        && i.PaymentMethod == PaymentMethod.Credit
                        && i.Date <= asOfEndOfDay
                        && i.CustomerId != null
                        && i.RemainingAmount > 0)
            .GroupBy(i => i.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Amount = g.Sum(x => x.RemainingAmount) })
            .ToListAsync();

        var installmentByCustomer = await (
            from inst in context.Installments.AsNoTracking()
            join plan in context.InstallmentPlans.AsNoTracking() on inst.InstallmentPlanId equals plan.Id
            join inv in context.Invoices.AsNoTracking() on plan.InvoiceId equals inv.Id
            where inst.RemainingAmount > 0 && inv.Currency == AccountingCurrency.IQD
            group inst.RemainingAmount by plan.CustomerId into g
            select new { CustomerId = g.Key, Amount = g.Sum() }
        ).ToListAsync();

        foreach (var row in creditByCustomer)
            customerBalanceMap[row.CustomerId] = row.Amount;
        foreach (var row in installmentByCustomer)
            customerBalanceMap[row.CustomerId] = customerBalanceMap.GetValueOrDefault(row.CustomerId) + row.Amount;
        foreach (var row in unappliedDebtByCustomer)
            customerBalanceMap[row.CustomerId] = customerBalanceMap.GetValueOrDefault(row.CustomerId) - row.Amount;
        foreach (var row in unappliedReceiptByCustomer)
            customerBalanceMap[row.CustomerId] = customerBalanceMap.GetValueOrDefault(row.CustomerId) - row.Amount;
        foreach (var row in returnCreditByCustomer)
            customerBalanceMap[row.CustomerId] = customerBalanceMap.GetValueOrDefault(row.CustomerId) - row.Amount;

        foreach (var key in customerBalanceMap.Keys.ToList())
            customerBalanceMap[key] = Math.Max(0, customerBalanceMap[key]);

        var customersWithBalance = customerBalanceMap.Count(kv => kv.Value > 0);
        var highestCustomerBalance = customerBalanceMap.Count > 0 ? customerBalanceMap.Values.Max() : 0;

        var supplierBalanceMap = new Dictionary<int, decimal>();
        var creditBySupplier = await context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.Purchase
                        && i.PaymentMethod == PaymentMethod.Credit
                        && !i.IsCreditPaid
                        && i.Currency == AccountingCurrency.IQD
                        && i.Date <= asOfEndOfDay
                        && i.SupplierId != null
                        && i.RemainingAmount > 0)
            .GroupBy(i => i.SupplierId!.Value)
            .Select(g => new { SupplierId = g.Key, Amount = g.Sum(x => x.RemainingAmount) })
            .ToListAsync();

        var unappliedPayBySupplier = await context.Vouchers.AsNoTracking()
            .Where(v => v.VoucherType == VoucherType.Payment
                        && v.SupplierId != null
                        && v.Currency == AccountingCurrency.IQD
                        && v.Date <= asOfEndOfDay
                        && !v.InvoiceId.HasValue
                        && (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .GroupBy(v => v.SupplierId!.Value)
            .Select(g => new { SupplierId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync();

        var returnCreditBySupplier = await context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.PurchaseReturn
                        && i.PaymentMethod == PaymentMethod.Credit
                        && i.Date <= asOfEndOfDay
                        && i.SupplierId != null
                        && i.RemainingAmount > 0)
            .GroupBy(i => i.SupplierId!.Value)
            .Select(g => new { SupplierId = g.Key, Amount = g.Sum(x => x.RemainingAmount) })
            .ToListAsync();

        foreach (var row in creditBySupplier)
            supplierBalanceMap[row.SupplierId] = row.Amount;
        foreach (var row in unappliedPayBySupplier)
            supplierBalanceMap[row.SupplierId] = supplierBalanceMap.GetValueOrDefault(row.SupplierId) - row.Amount;
        foreach (var row in returnCreditBySupplier)
            supplierBalanceMap[row.SupplierId] = supplierBalanceMap.GetValueOrDefault(row.SupplierId) - row.Amount;
        foreach (var key in supplierBalanceMap.Keys.ToList())
            supplierBalanceMap[key] = Math.Max(0, supplierBalanceMap[key]);

        var suppliersWithBalance = supplierBalanceMap.Count(kv => kv.Value > 0);
        var highestSupplierBalance = supplierBalanceMap.Count > 0 ? supplierBalanceMap.Values.Max() : 0;

        var belowMinimum = await context.WarehouseStocks.AsNoTracking()
            .CountAsync(ws => ws.MinQuantity > 0 && ws.Quantity < ws.MinQuantity);

        var operating = profit.GrossProfit - profit.TotalExpenses - profit.TotalBankFees;
        var netMargin = profit.TotalSales > 0
            ? Math.Round(profit.NetProfit / profit.TotalSales * 100, 1)
            : 0;

        var netWorkingCapital = cash.CashBoxesTotal + cash.BanksTotal
                                + customerReceivables + installmentReceivables
                                - supplierPayables;

        const int detailLimit = 40;

        var activeCustomersDetail = await salesQ
            .Where(i => i.CustomerId != null)
            .GroupBy(i => new { i.CustomerId, Name = i.Customer != null ? i.Customer.Name : "—" })
            .Select(g => new NameAmountPoint { Name = g.Key.Name, Amount = g.Sum(x => x.NetAmount) })
            .OrderByDescending(x => x.Amount)
            .Take(detailLimit)
            .ToListAsync();

        var activeSuppliersDetail = await purchaseQ
            .Where(i => i.SupplierId != null)
            .GroupBy(i => new { i.SupplierId, Name = i.Supplier != null ? i.Supplier.Name : "—" })
            .Select(g => new NameAmountPoint { Name = g.Key.Name, Amount = g.Sum(x => x.NetAmount) })
            .OrderByDescending(x => x.Amount)
            .Take(detailLimit)
            .ToListAsync();

        var customerIdsWithBalance = customerBalanceMap.Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value).Take(detailLimit).Select(kv => kv.Key).ToList();
        var customerNames = customerIdsWithBalance.Count == 0
            ? new Dictionary<int, string>()
            : await context.Customers.AsNoTracking()
                .Where(c => customerIdsWithBalance.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);
        var customersWithBalanceDetail = customerIdsWithBalance
            .Select(id => new NameAmountPoint
            {
                Name = customerNames.GetValueOrDefault(id, $"عميل #{id}"),
                Amount = customerBalanceMap[id]
            }).ToList();

        var supplierIdsWithBalance = supplierBalanceMap.Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value).Take(detailLimit).Select(kv => kv.Key).ToList();
        var supplierNames = supplierIdsWithBalance.Count == 0
            ? new Dictionary<int, string>()
            : await context.Suppliers.AsNoTracking()
                .Where(s => supplierIdsWithBalance.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);
        var suppliersWithBalanceDetail = supplierIdsWithBalance
            .Select(id => new NameAmountPoint
            {
                Name = supplierNames.GetValueOrDefault(id, $"مورد #{id}"),
                Amount = supplierBalanceMap[id]
            }).ToList();

        var creditCustomerIds = creditByCustomer.OrderByDescending(x => x.Amount).Take(detailLimit).Select(x => x.CustomerId).ToList();
        var creditNames = creditCustomerIds.Count == 0
            ? new Dictionary<int, string>()
            : await context.Customers.AsNoTracking()
                .Where(c => creditCustomerIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);
        var customerCreditDetail = creditByCustomer
            .OrderByDescending(x => x.Amount)
            .Take(detailLimit)
            .Select(x => new NameAmountPoint
            {
                Name = creditNames.GetValueOrDefault(x.CustomerId, $"عميل #{x.CustomerId}"),
                Amount = x.Amount
            }).ToList();

        var creditSupplierIds = creditBySupplier.OrderByDescending(x => x.Amount).Take(detailLimit).Select(x => x.SupplierId).ToList();
        var creditSupplierNames = creditSupplierIds.Count == 0
            ? new Dictionary<int, string>()
            : await context.Suppliers.AsNoTracking()
                .Where(s => creditSupplierIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);
        var supplierCreditDetail = creditBySupplier
            .OrderByDescending(x => x.Amount)
            .Take(detailLimit)
            .Select(x => new NameAmountPoint
            {
                Name = creditSupplierNames.GetValueOrDefault(x.SupplierId, $"مورد #{x.SupplierId}"),
                Amount = x.Amount
            }).ToList();

        var overdueInstallmentsDetail = await (
            from inst in context.Installments.AsNoTracking()
            join plan in context.InstallmentPlans.AsNoTracking() on inst.InstallmentPlanId equals plan.Id
            join inv in context.Invoices.AsNoTracking() on plan.InvoiceId equals inv.Id
            join cust in context.Customers.AsNoTracking() on plan.CustomerId equals cust.Id
            where inst.Status != InstallmentStatus.Paid
                  && inst.DueDate < asOf
                  && inst.RemainingAmount > 0
                  && inv.Currency == AccountingCurrency.IQD
            group inst.RemainingAmount by cust.Name into g
            select new NameAmountPoint { Name = g.Key, Amount = g.Sum() }
        ).OrderByDescending(x => x.Amount).Take(detailLimit).ToListAsync();

        var belowMinimumDetail = await context.WarehouseStocks.AsNoTracking()
            .Where(ws => ws.MinQuantity > 0 && ws.Quantity < ws.MinQuantity)
            .OrderBy(ws => ws.Quantity - ws.MinQuantity)
            .Take(detailLimit)
            .Select(ws => new NameAmountPoint
            {
                Name = (ws.Product != null ? ws.Product.Name : "—") +
                       (ws.Warehouse != null ? " · " + ws.Warehouse.Name : ""),
                Amount = ws.Quantity
            })
            .ToListAsync();

        var cashBoxesDetail = cash.Rows
            .Where(r => r.AccountType == "قاصة" && r.Currency == AccountingCurrency.IQD)
            .Select(r => new NameAmountPoint { Name = r.Name, Amount = r.Balance })
            .OrderByDescending(x => x.Amount)
            .Take(detailLimit)
            .ToList();
        var banksDetail = cash.Rows
            .Where(r => r.AccountType == "مصرف" && r.Currency == AccountingCurrency.IQD)
            .Select(r => new NameAmountPoint { Name = r.Name, Amount = r.Balance })
            .OrderByDescending(x => x.Amount)
            .Take(detailLimit)
            .ToList();

        var topStockByValue = stock.Rows
            .OrderByDescending(r => Math.Round(r.Quantity * r.AverageCost, 0))
            .Take(detailLimit)
            .Select(r => new NameAmountPoint
            {
                Name = $"{r.ProductName} · {r.WarehouseName}",
                Amount = Math.Round(r.Quantity * r.AverageCost, 0)
            })
            .ToList();

        return new ExecutiveBusinessSummaryResult
        {
            DateFrom = from,
            DateTo = to ?? asOf,

            CustomerReceivables = customerReceivables,
            CustomerCreditInvoiceRemaining = customerCreditRemaining,
            CustomerUnappliedDebt = unappliedDebt,
            CustomerUnappliedReceipts = unappliedReceipts,
            InstallmentReceivables = installmentReceivables,
            SupplierPayables = supplierPayables,
            SupplierCreditInvoiceRemaining = supplierCreditRemaining,
            SupplierUnappliedPayments = unappliedSupplierPayments,
            InventoryCostValue = stock.TotalCostValue > 0 ? stock.TotalCostValue : bs.Inventory,
            InventoryQuantity = stock.TotalQuantity,
            CashBoxesBalance = cash.CashBoxesTotal,
            BankBalance = cash.BanksTotal,
            NetWorkingCapital = netWorkingCapital,
            TotalAssets = bs.TotalAssets,
            TotalLiabilities = bs.TotalLiabilities,
            TotalEquity = bs.TotalEquity,
            AccumulatedProfits = bs.AccumulatedProfits,

            TotalSales = totalSales > 0 ? totalSales : profit.TotalSales,
            CashSales = cashSales,
            CreditSales = creditSales,
            InstallmentSales = installmentSales,
            SalesInvoiceCount = salesCount,
            AverageSaleInvoice = salesCount > 0 ? Math.Round((totalSales > 0 ? totalSales : profit.TotalSales) / salesCount, 0) : 0,

            TotalPurchases = totalPurchases,
            CashPurchases = cashPurchases,
            CreditPurchases = creditPurchases,
            PurchaseInvoiceCount = purchaseCount,
            CostOfGoodsSold = profit.TotalPurchases,

            GrossProfit = profit.GrossProfit,
            GrossMarginPercent = profit.ProfitMargin,
            TotalExpenses = profit.TotalExpenses,
            TotalBankFees = profit.TotalBankFees,
            OperatingProfit = operating,
            DistributedProfits = profit.DistributedProfits,
            ProfitOpeningBalance = profit.ProfitOpeningBalance,
            NetProfit = profit.NetProfit,
            NetMarginPercent = netMargin,

            ActiveCustomersCount = activeCustomers,
            CustomerCollections = receiptAmount + collectedInstallments,
            CustomersWithBalanceCount = customersWithBalance,
            HighestCustomerBalance = highestCustomerBalance,

            ActiveSuppliersCount = activeSuppliers,
            SupplierPayments = paymentAmount + cashPurchases,
            SuppliersWithBalanceCount = suppliersWithBalance,
            HighestSupplierBalance = highestSupplierBalance,

            StockedProductCount = stock.ProductCount,
            InventorySaleValue = stock.TotalSaleValue,
            InventoryPotentialProfit = stock.TotalPotentialProfit,
            BelowMinimumStockCount = belowMinimum,

            OverdueInstallmentsCount = overdueCount,
            OverdueInstallmentsAmount = overdueAmount,
            CollectedInstallments = collectedInstallments,
            ReceiptVouchersAmount = receiptAmount,
            PaymentVouchersAmount = paymentAmount,
            TransfersAmount = transfersAmount,
            TransfersCount = transfersCount,

            ActiveCustomersDetail = activeCustomersDetail,
            ActiveSuppliersDetail = activeSuppliersDetail,
            CustomersWithBalanceDetail = customersWithBalanceDetail,
            SuppliersWithBalanceDetail = suppliersWithBalanceDetail,
            CustomerCreditDetail = customerCreditDetail,
            SupplierCreditDetail = supplierCreditDetail,
            OverdueInstallmentsDetail = overdueInstallmentsDetail,
            BelowMinimumStockDetail = belowMinimumDetail,
            CashBoxesDetail = cashBoxesDetail,
            BanksDetail = banksDetail,
            TopStockByValueDetail = topStockByValue
        };
    }

    public async Task<WarehouseTransfersReportResult> GetWarehouseTransfersReportAsync(
        DateTime? from, DateTime? to, int? fromWarehouseId, int? toWarehouseId, string? search)
    {
        var context = _db;
        var query = context.WarehouseTransfers.AsNoTracking()
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Include(t => t.Items)
            .AsQueryable();

        if (from.HasValue) query = query.Where(t => t.Date >= from.Value);
        if (to.HasValue) query = query.Where(t => t.Date < EndOfDay(to));
        if (fromWarehouseId.HasValue) query = query.Where(t => t.FromWarehouseId == fromWarehouseId.Value);
        if (toWarehouseId.HasValue) query = query.Where(t => t.ToWarehouseId == toWarehouseId.Value);

        var searchTerm = search?.Trim();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t =>
                t.TransferNumber.Contains(searchTerm)
                || (t.Notes != null && t.Notes.Contains(searchTerm))
                || (t.CreatedBy != null && t.CreatedBy.Contains(searchTerm)));
        }

        var transfers = await query.OrderByDescending(t => t.Date).ToListAsync();
        var rows = transfers.Select(t =>
        {
            var activeItems = t.Items.Where(i => !i.IsDeleted).ToList();
            return new WarehouseTransferReportRow
            {
                Id = t.Id,
                TransferNumber = t.TransferNumber,
                Date = t.Date,
                FromWarehouseName = t.FromWarehouse?.Name ?? "—",
                ToWarehouseName = t.ToWarehouse?.Name ?? "—",
                LineCount = activeItems.Count,
                TotalQuantity = activeItems.Sum(i => i.Quantity),
                Notes = t.Notes ?? string.Empty,
                CreatedBy = t.CreatedBy ?? "—"
            };
        }).ToList();

        return new WarehouseTransfersReportResult
        {
            TransferCount = rows.Count,
            TotalLineCount = rows.Sum(r => r.LineCount),
            TotalQuantity = rows.Sum(r => r.TotalQuantity),
            WarehouseCount = rows.Select(r => r.FromWarehouseName).Concat(rows.Select(r => r.ToWarehouseName)).Distinct().Count(),
            Rows = rows
        };
    }

    public async Task<WarehouseTransferDetailResult?> GetWarehouseTransferDetailAsync(int transferId)
    {
        var context = _db;
        var transfer = await context.WarehouseTransfers.AsNoTracking()
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(t => t.Id == transferId);
        if (transfer is null) return null;

        var lines = transfer.Items.Where(i => !i.IsDeleted).Select(i => new WarehouseTransferDetailLine
        {
            ProductName = i.Product?.Name ?? "—",
            Quantity = i.Quantity
        }).ToList();

        return new WarehouseTransferDetailResult
        {
            Id = transfer.Id,
            TransferNumber = transfer.TransferNumber,
            Date = transfer.Date,
            FromWarehouseName = transfer.FromWarehouse?.Name ?? "—",
            ToWarehouseName = transfer.ToWarehouse?.Name ?? "—",
            Notes = transfer.Notes,
            CreatedBy = transfer.CreatedBy ?? "—",
            LineCount = lines.Count,
            TotalQuantity = lines.Sum(l => l.Quantity),
            Lines = lines
        };
    }

    public async Task<DailyOperationsReportResult> GetDailyOperationsReportAsync(
        DateTime? from, DateTime? to,
        ReportCurrencyScope currencyScope = ReportCurrencyScope.Iqd)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId), currencyScope);
        var effectiveScope = foldInUsd ? ReportCurrencyScope.All : currencyScope;
        var fromDate = from?.Date;
        var toExclusive = EndOfDay(to);

        var cashSalesQ = context.Invoices.Where(i =>
            i.PaymentMethod == PaymentMethod.Cash &&
            ((i.InvoiceType == InvoiceType.Sale
              && (i.Notes == null || !i.Notes.StartsWith(OpeningCreditBalanceMarkers.NotesPrefix)))
             || (i.InvoiceType == InvoiceType.Installment
                 && !context.InstallmentPlans.Any(p =>
                     p.InvoiceId == i.Id && p.InstallmentType == InstallmentType.OpeningBalance))));
        cashSalesQ = ApplyCloudInvoiceCurrencyScope(cashSalesQ, effectiveScope, foldInUsd);
        if (fromDate.HasValue) cashSalesQ = cashSalesQ.Where(i => i.Date >= fromDate.Value);
        if (toExclusive.HasValue) cashSalesQ = cashSalesQ.Where(i => i.Date < toExclusive.Value);

        var cashSalesRows = await cashSalesQ
            .Select(i => new { i.BranchId, i.NetAmount, i.Currency, i.FxRate, i.CreatedBy })
            .ToListAsync();
        decimal Amt(decimal amount, AccountingCurrency currency, decimal fx) =>
            FinancialReportIqdFoldIn.AmountInBaseIqd(amount, currency, fx, foldInUsd);

        var cashSales = cashSalesRows.Sum(r => Amt(r.NetAmount, r.Currency, r.FxRate));
        var cashSalesCount = cashSalesRows.Count;

        var returnsQ = context.Invoices.Where(i => i.InvoiceType == InvoiceType.SaleReturn);
        returnsQ = ApplyCloudInvoiceCurrencyScope(returnsQ, effectiveScope, foldInUsd);
        if (fromDate.HasValue) returnsQ = returnsQ.Where(i => i.Date >= fromDate.Value);
        if (toExclusive.HasValue) returnsQ = returnsQ.Where(i => i.Date < toExclusive.Value);

        var returnRows = await returnsQ
            .Select(i => new { i.BranchId, i.NetAmount, i.Currency, i.FxRate })
            .ToListAsync();
        var salesReturns = returnRows.Sum(r => Amt(Math.Abs(r.NetAmount), r.Currency, r.FxRate));
        var salesReturnCount = returnRows.Count;

        var expQ = context.Expenses.AsQueryable();
        expQ = ApplyCloudExpenseCurrencyScope(expQ, effectiveScope, foldInUsd);
        if (fromDate.HasValue) expQ = expQ.Where(e => e.Date >= fromDate.Value);
        if (toExclusive.HasValue) expQ = expQ.Where(e => e.Date < toExclusive.Value);

        var expenseRows = await expQ
            .Select(e => new { e.BranchId, e.Amount, e.Currency, e.FxRate })
            .ToListAsync();
        var expenses = expenseRows.Sum(r => Amt(r.Amount, r.Currency, r.FxRate));
        var expenseCount = expenseRows.Count;

        var payVouchQ = context.Vouchers.Where(v =>
            v.VoucherType == VoucherType.Payment && v.SupplierId != null);
        payVouchQ = ApplyCloudVoucherCurrencyScope(payVouchQ, effectiveScope, foldInUsd);
        if (fromDate.HasValue) payVouchQ = payVouchQ.Where(v => v.Date >= fromDate.Value);
        if (toExclusive.HasValue) payVouchQ = payVouchQ.Where(v => v.Date < toExclusive.Value);

        var payVouchRows = await payVouchQ
            .Select(v => new { v.BranchId, v.Amount, v.Currency, v.FxRate })
            .ToListAsync();

        var cashPurchQ = context.Invoices.Where(i =>
            i.InvoiceType == InvoiceType.Purchase
            && i.PaymentMethod == PaymentMethod.Cash
            && (i.Notes == null || !i.Notes.StartsWith(OpeningCreditBalanceMarkers.NotesPrefix)));
        cashPurchQ = ApplyCloudInvoiceCurrencyScope(cashPurchQ, effectiveScope, foldInUsd);
        if (fromDate.HasValue) cashPurchQ = cashPurchQ.Where(i => i.Date >= fromDate.Value);
        if (toExclusive.HasValue) cashPurchQ = cashPurchQ.Where(i => i.Date < toExclusive.Value);

        var cashPurchRows = await cashPurchQ
            .Select(i => new { i.BranchId, i.NetAmount, i.Currency, i.FxRate })
            .ToListAsync();

        var supplierPayments =
            payVouchRows.Sum(r => Amt(r.Amount, r.Currency, r.FxRate))
            + cashPurchRows.Sum(r => Amt(r.NetAmount, r.Currency, r.FxRate));
        var supplierPaymentCount = payVouchRows.Count + cashPurchRows.Count;

        var receiptQ = context.Vouchers.Where(v =>
            v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt);
        receiptQ = ApplyCloudVoucherCurrencyScope(receiptQ, effectiveScope, foldInUsd);
        if (fromDate.HasValue) receiptQ = receiptQ.Where(v => v.Date >= fromDate.Value);
        if (toExclusive.HasValue) receiptQ = receiptQ.Where(v => v.Date < toExclusive.Value);

        var receiptRows = await receiptQ
            .Select(v => new { v.BranchId, v.Amount, v.Currency, v.FxRate })
            .ToListAsync();
        var receipts = receiptRows.Sum(r => Amt(r.Amount, r.Currency, r.FxRate));
        var receiptCount = receiptRows.Count;

        var net = DailyOperationsReportCalculator.ComputeNet(
            cashSales, salesReturns, expenses, supplierPayments, receipts);

        var branchIds = cashSalesRows.Select(r => r.BranchId)
            .Concat(returnRows.Select(r => r.BranchId))
            .Concat(expenseRows.Select(r => r.BranchId))
            .Concat(payVouchRows.Select(r => r.BranchId))
            .Concat(cashPurchRows.Select(r => r.BranchId))
            .Concat(receiptRows.Select(r => r.BranchId))
            .Distinct()
            .ToList();
        var branchNames = branchIds.Count == 0
            ? new Dictionary<int, string>()
            : await context.Branches.AsNoTracking()
                .Where(b => branchIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Name);

        var branchRows = branchIds
            .Select(id =>
            {
                var bCash = cashSalesRows.Where(r => r.BranchId == id).Sum(r => Amt(r.NetAmount, r.Currency, r.FxRate));
                var bRet = returnRows.Where(r => r.BranchId == id).Sum(r => Amt(Math.Abs(r.NetAmount), r.Currency, r.FxRate));
                var bExp = expenseRows.Where(r => r.BranchId == id).Sum(r => Amt(r.Amount, r.Currency, r.FxRate));
                var bPay = payVouchRows.Where(r => r.BranchId == id).Sum(r => Amt(r.Amount, r.Currency, r.FxRate))
                           + cashPurchRows.Where(r => r.BranchId == id).Sum(r => Amt(r.NetAmount, r.Currency, r.FxRate));
                var bRec = receiptRows.Where(r => r.BranchId == id).Sum(r => Amt(r.Amount, r.Currency, r.FxRate));
                return new DailyOperationsBranchRow
                {
                    BranchId = id,
                    BranchName = branchNames.GetValueOrDefault(id, "—"),
                    CashSales = bCash,
                    Expenses = bExp,
                    SalesReturns = bRet,
                    SupplierPayments = bPay,
                    Receipts = bRec,
                    NetAmount = DailyOperationsReportCalculator.ComputeNet(bCash, bRet, bExp, bPay, bRec)
                };
            })
            .OrderByDescending(r => r.CashSales)
            .ToList();

        IQueryable<CloudInvoice> salesQ = foldInUsd || effectiveScope == ReportCurrencyScope.All
            ? CloudInvoiceFilters.ForProfitAndSalesTotalsAll(context.Invoices, context.InstallmentPlans)
            : CloudInvoiceFilters.ForProfitAndSalesTotals(
                context.Invoices, context.InstallmentPlans,
                ReportCurrencyScopeHelper.ToStrictFilter(effectiveScope) ?? AccountingCurrency.IQD);
        if (fromDate.HasValue) salesQ = salesQ.Where(i => i.Date >= fromDate.Value);
        if (toExclusive.HasValue) salesQ = salesQ.Where(i => i.Date < toExclusive.Value);

        var salesInvoices = await salesQ
            .Select(i => new { i.CreatedBy, i.InvoiceType, i.NetAmount, i.Currency, i.FxRate, i.PaymentMethod })
            .ToListAsync();

        decimal SignedAmt(InvoiceType type, decimal netAmt, AccountingCurrency currency, decimal fx) =>
            FinancialReportIqdFoldIn.SignedSalesInBaseIqd(type, netAmt, currency, fx, foldInUsd);

        var salesTotal = salesInvoices.Sum(i => SignedAmt(i.InvoiceType, i.NetAmount, i.Currency, i.FxRate));
        var userRows = salesInvoices
            .GroupBy(i => string.IsNullOrWhiteSpace(i.CreatedBy) ? "—" : i.CreatedBy!)
            .Select(g =>
            {
                var amount = g.Sum(i => SignedAmt(i.InvoiceType, i.NetAmount, i.Currency, i.FxRate));
                return new DailyOperationsUserRow
                {
                    UserName = g.Key,
                    InvoiceCount = g.Count(),
                    Amount = amount,
                    SharePercent = DailyOperationsReportCalculator.SharePercent(amount, salesTotal)
                };
            })
            .OrderByDescending(r => r.Amount)
            .ToList();

        var paymentRows = salesInvoices
            .GroupBy(i => i.PaymentMethod)
            .Select(g =>
            {
                var amount = g.Sum(i => SignedAmt(i.InvoiceType, i.NetAmount, i.Currency, i.FxRate));
                return new DailyOperationsPaymentMethodRow
                {
                    PaymentMethod = PaymentMethodLabel(g.Key),
                    InvoiceCount = g.Count(),
                    Amount = amount,
                    SharePercent = DailyOperationsReportCalculator.SharePercent(amount, salesTotal)
                };
            })
            .OrderByDescending(r => r.Amount)
            .ToList();

        var itemsQ = context.InvoiceItems
            .Include(ii => ii.Product)!.ThenInclude(p => p!.Category)
            .Include(ii => ii.Invoice)
            .Where(ii => ii.ProductId != null
                         && ii.Product != null
                         && ii.Invoice != null
                         && (ii.Invoice.InvoiceType == InvoiceType.Sale
                             || ii.Invoice.InvoiceType == InvoiceType.Installment
                             || ii.Invoice.InvoiceType == InvoiceType.SaleReturn));
        if (!foldInUsd && effectiveScope != ReportCurrencyScope.All)
        {
            var strict = ReportCurrencyScopeHelper.ToStrictFilter(effectiveScope);
            if (strict.HasValue)
                itemsQ = itemsQ.Where(ii => ii.Invoice!.Currency == strict.Value);
        }
        else if (!foldInUsd)
        {
            itemsQ = itemsQ.Where(ii => ii.Invoice!.Currency == AccountingCurrency.IQD);
        }

        if (fromDate.HasValue) itemsQ = itemsQ.Where(ii => ii.Invoice!.Date >= fromDate.Value);
        if (toExclusive.HasValue) itemsQ = itemsQ.Where(ii => ii.Invoice!.Date < toExclusive.Value);

        var items = await itemsQ.ToListAsync();
        decimal LineAmt(CloudInvoiceItem x)
        {
            var signed = InvoiceFilters.SignedSaleLineAmount(x.Invoice!.InvoiceType, x.TotalPrice);
            return FinancialReportIqdFoldIn.AmountInBaseIqd(signed, x.Invoice.Currency, x.Invoice.FxRate, foldInUsd);
        }

        var productGroups = items
            .GroupBy(ii => ii.ProductId!.Value)
            .Select(g =>
            {
                var first = g.First();
                return new DailyOperationsProductRow
                {
                    ProductId = g.Key,
                    ProductName = first.Product?.Name ?? first.ItemName ?? "—",
                    CategoryName = first.Product?.Category?.Name ?? "—",
                    LineCount = g.Count(),
                    Quantity = g.Sum(x => InvoiceFilters.SignedSaleLineQuantity(x.Invoice!.InvoiceType, x.Quantity)),
                    Amount = g.Sum(LineAmt)
                };
            })
            .OrderByDescending(r => r.Amount)
            .ToList();

        var productTotal = productGroups.Sum(r => r.Amount);
        for (var i = 0; i < productGroups.Count; i++)
        {
            productGroups[i].Rank = i + 1;
            productGroups[i].SharePercent =
                DailyOperationsReportCalculator.SharePercent(productGroups[i].Amount, productTotal);
        }

        var categoryRows = items
            .GroupBy(ii => new
            {
                CategoryId = (int?)ii.Product?.CategoryId,
                Name = ii.Product?.Category?.Name ?? "—"
            })
            .Select(g =>
            {
                var amount = g.Sum(LineAmt);
                return new DailyOperationsCategoryRow
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.Name,
                    LineCount = g.Count(),
                    Quantity = g.Sum(x => InvoiceFilters.SignedSaleLineQuantity(x.Invoice!.InvoiceType, x.Quantity)),
                    Amount = amount,
                    SharePercent = DailyOperationsReportCalculator.SharePercent(amount, productTotal)
                };
            })
            .OrderByDescending(r => r.Amount)
            .ToList();

        return new DailyOperationsReportResult
        {
            DateFrom = fromDate ?? from,
            DateTo = to?.Date ?? to,
            CurrencyScope = currencyScope,
            CashSales = cashSales,
            Expenses = expenses,
            SalesReturns = salesReturns,
            SupplierPayments = supplierPayments,
            Receipts = receipts,
            NetAmount = net,
            CashSalesInvoiceCount = cashSalesCount,
            SalesReturnInvoiceCount = salesReturnCount,
            ExpenseCount = expenseCount,
            SupplierPaymentCount = supplierPaymentCount,
            ReceiptCount = receiptCount,
            BranchRows = branchRows,
            UserRows = userRows,
            PaymentMethodRows = paymentRows,
            CategoryRows = categoryRows,
            ProductRows = productGroups,
            PaymentMethodChart = paymentRows
                .Select(r => new NameAmountPoint { Name = r.PaymentMethod, Amount = r.Amount }).ToList(),
            UserChart = userRows.Take(10)
                .Select(r => new NameAmountPoint { Name = r.UserName, Amount = r.Amount }).ToList(),
            CategoryChart = categoryRows.Take(10)
                .Select(r => new NameAmountPoint { Name = r.CategoryName, Amount = r.Amount }).ToList(),
            ProductChart = productGroups.Take(10)
                .Select(r => new NameAmountPoint { Name = r.ProductName, Amount = r.Amount }).ToList(),
            SummaryChart =
            [
                new NameAmountPoint { Name = "مبيعات نقدية", Amount = cashSales },
                new NameAmountPoint { Name = "وصولات قبض", Amount = receipts },
                new NameAmountPoint { Name = "مصاريف", Amount = expenses },
                new NameAmountPoint { Name = "مرتجعات", Amount = salesReturns },
                new NameAmountPoint { Name = "مدفوعات موردين", Amount = supplierPayments }
            ]
        };
    }

    private static IQueryable<CloudInvoice> ApplyCloudInvoiceCurrencyScope(
        IQueryable<CloudInvoice> query, ReportCurrencyScope scope, bool foldInUsd)
    {
        if (foldInUsd || scope == ReportCurrencyScope.All)
            return query;
        var strict = ReportCurrencyScopeHelper.ToStrictFilter(scope);
        return strict is null ? query : query.Where(i => i.Currency == strict.Value);
    }

    private static IQueryable<CloudExpense> ApplyCloudExpenseCurrencyScope(
        IQueryable<CloudExpense> query, ReportCurrencyScope scope, bool foldInUsd)
    {
        if (foldInUsd || scope == ReportCurrencyScope.All)
            return query;
        var strict = ReportCurrencyScopeHelper.ToStrictFilter(scope);
        return strict is null ? query : query.Where(e => e.Currency == strict.Value);
    }

    private static IQueryable<CloudVoucher> ApplyCloudVoucherCurrencyScope(
        IQueryable<CloudVoucher> query, ReportCurrencyScope scope, bool foldInUsd)
    {
        if (foldInUsd || scope == ReportCurrencyScope.All)
            return query;
        var strict = ReportCurrencyScopeHelper.ToStrictFilter(scope);
        return strict is null ? query : query.Where(v => v.Currency == strict.Value);
    }
}
