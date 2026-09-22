using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Reports;

public sealed partial class CloudReportService
{
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
            return (await context.CashBoxes.FindAsync(id))?.Name ?? $"قاصة #{id}";
        return (await context.BankAccounts.FindAsync(id))?.Name ?? $"مصرف #{id}";
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
        var query = context.InstallmentPlans
            .Include(p => p.Customer)
            .Include(p => p.Installments)
            .Include(p => p.Invoice)
            .Where(p => p.InstallmentType == InstallmentType.OpeningBalance);

        if (customerId.HasValue) query = query.Where(p => p.CustomerId == customerId.Value);
        if (from.HasValue) query = query.Where(p => p.Invoice.Date >= from.Value);
        if (to.HasValue) query = query.Where(p => p.Invoice.Date < EndOfDay(to));

        var plans = await query.OrderByDescending(p => p.Invoice.Date).ToListAsync();
        var rows = plans.Select(p =>
        {
            var paid = p.Installments.Sum(i => i.PaidAmount);
            var remaining = p.Installments.Sum(i => i.RemainingAmount);
            return new OpeningInstallmentBalanceRow
            {
                PlanId = p.Id,
                InvoiceId = p.InvoiceId,
                CustomerName = p.Customer?.Name ?? "—",
                Phone = p.Customer?.Phone ?? "—",
                Date = p.Invoice?.Date ?? DateTime.MinValue,
                TotalAmount = p.TotalAmount,
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
        var query = context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InstallmentPlans)
            .Where(i => i.InvoiceType == InvoiceType.Installment);

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

            var fee = plan.CompanyFeeAmount > 0
                ? plan.CompanyFeeAmount
                : (i.CompanyFeeAmount > 0 ? i.CompanyFeeAmount : CompanyFeeHelper.CalculateAmount(i.NetAmount));
            var pct = plan.CompanyFeePercentage > 0
                ? plan.CompanyFeePercentage * 100
                : (i.CompanyFeePercentage > 0 ? i.CompanyFeePercentage * 100 : CompanyFeeHelper.DefaultPercentage * 100);

            rows.Add(new CompanyFeeRow
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                Date = i.Date,
                CustomerName = i.Customer?.Name ?? "—",
                NetAmount = i.NetAmount,
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
        var query = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .AsQueryable();

        if (from.HasValue) query = query.Where(i => i.DueDate >= from.Value);
        if (to.HasValue) query = query.Where(i => i.DueDate < EndOfDay(to));
        if (customerId.HasValue) query = query.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InstallmentStatus>(status, true, out var st))
            query = query.Where(i => i.Status == st);

        var items = await query.OrderBy(i => i.DueDate).ToListAsync();
        var rows = items.Select(i => new InstallmentScheduleReportRow
        {
            InstallmentId = i.Id,
            PlanId = i.InstallmentPlanId,
            InvoiceId = i.InstallmentPlan?.InvoiceId ?? 0,
            CustomerName = i.InstallmentPlan?.Customer?.Name ?? "—",
            Phone = i.InstallmentPlan?.Customer?.Phone ?? "—",
            DueDate = i.DueDate,
            Amount = i.Amount,
            PaidAmount = i.PaidAmount,
            RemainingAmount = i.RemainingAmount,
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
        var query = CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans);
        if (from.HasValue) query = query.Where(i => i.Date >= from.Value);
        if (to.HasValue) query = query.Where(i => i.Date < EndOfDay(to));
        if (warehouseId.HasValue) query = query.Where(i => i.WarehouseId == warehouseId.Value);

        var invoices = await query.ToListAsync();
        var total = invoices.Sum(i => i.NetAmount);
        var rows = invoices.GroupBy(i => i.PaymentMethod)
            .Select(g => new SalesByPaymentMethodRow
            {
                PaymentMethod = PaymentMethodLabel(g.Key),
                InvoiceCount = g.Count(),
                Amount = g.Sum(x => x.NetAmount),
                SharePercent = total > 0 ? Math.Round(g.Sum(x => x.NetAmount) / total * 100, 1) : 0
            })
            .OrderByDescending(r => r.Amount).ToList();

        return new SalesByPaymentMethodReportResult
        {
            TotalSales = total,
            CashSales = invoices.Where(i => i.PaymentMethod == PaymentMethod.Cash).Sum(i => i.NetAmount),
            CreditSales = invoices.Where(i => i.PaymentMethod == PaymentMethod.Credit).Sum(i => i.NetAmount),
            InstallmentSales = invoices.Where(i => i.PaymentMethod == PaymentMethod.Installment).Sum(i => i.NetAmount),
            InvoiceCount = invoices.Count,
            Rows = rows,
            MethodChart = rows.Select(r => new NameAmountPoint { Name = r.PaymentMethod, Amount = r.Amount }).ToList(),
            DailyChart = invoices.GroupBy(i => i.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.NetAmount) })
                .OrderBy(x => x.Date).ToList()
        };
    }

    public async Task<DailySalesReportResult> GetDailySalesReportAsync(
        DateTime? from, DateTime? to, int? warehouseId, PaymentMethod? method)
    {
        var context = _db;
        IQueryable<CloudInvoice> query = CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans)
            .Include(i => i.InstallmentPlans);
        if (from.HasValue) query = query.Where(i => i.Date >= from.Value);
        if (to.HasValue) query = query.Where(i => i.Date < EndOfDay(to));
        if (warehouseId.HasValue) query = query.Where(i => i.WarehouseId == warehouseId.Value);
        if (method.HasValue) query = query.Where(i => i.PaymentMethod == method.Value);

        var invoices = await query.ToListAsync();
        var rows = invoices.GroupBy(i => i.Date.Date).Select(g =>
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
                Date = g.Key,
                InvoiceCount = g.Count(),
                CashSales = g.Where(x => x.PaymentMethod == PaymentMethod.Cash).Sum(x => x.NetAmount),
                CreditSales = g.Where(x => x.PaymentMethod == PaymentMethod.Credit).Sum(x => x.NetAmount),
                InstallmentSales = g.Where(x => x.PaymentMethod == PaymentMethod.Installment).Sum(x => x.NetAmount),
                TotalSales = g.Sum(x => x.NetAmount),
                DiscountAmount = g.Sum(x => x.DiscountAmount),
                CompanyFees = fees
            };
        }).OrderByDescending(r => r.Date).ToList();

        var total = rows.Sum(r => r.TotalSales);
        return new DailySalesReportResult
        {
            TotalSales = total,
            DayCount = rows.Count,
            InvoiceCount = invoices.Count,
            AverageDaily = rows.Count > 0 ? Math.Round(total / rows.Count, 0) : 0,
            Rows = rows,
            DailyChart = rows.OrderBy(r => r.Date)
                .Select(r => new DailyAmountPoint { Date = r.Date, Amount = r.TotalSales }).ToList()
        };
    }

    public async Task<SalesByWarehouseUserReportResult> GetSalesByWarehouseUserReportAsync(
        DateTime? from, DateTime? to, int? warehouseId)
    {
        var context = _db;
        IQueryable<CloudInvoice> query = CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans)
            .Include(i => i.Warehouse);
        if (from.HasValue) query = query.Where(i => i.Date >= from.Value);
        if (to.HasValue) query = query.Where(i => i.Date < EndOfDay(to));
        if (warehouseId.HasValue) query = query.Where(i => i.WarehouseId == warehouseId.Value);

        var invoices = await query.ToListAsync();
        var total = invoices.Sum(i => i.NetAmount);

        var warehouseRows = invoices.GroupBy(i => i.Warehouse?.Name ?? "—")
            .Select(g => new SalesByWarehouseUserRow
            {
                GroupType = "مخزن",
                Name = g.Key,
                InvoiceCount = g.Count(),
                Amount = g.Sum(x => x.NetAmount),
                SharePercent = total > 0 ? Math.Round(g.Sum(x => x.NetAmount) / total * 100, 1) : 0
            }).OrderByDescending(r => r.Amount).ToList();

        var userRows = invoices.GroupBy(i => string.IsNullOrWhiteSpace(i.CreatedBy) ? "—" : i.CreatedBy!)
            .Select(g => new SalesByWarehouseUserRow
            {
                GroupType = "مستخدم",
                Name = g.Key,
                InvoiceCount = g.Count(),
                Amount = g.Sum(x => x.NetAmount),
                SharePercent = total > 0 ? Math.Round(g.Sum(x => x.NetAmount) / total * 100, 1) : 0
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
        var salesQ = CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans);
        var expQ = context.Expenses.AsQueryable();
        var bankQ = context.Vouchers.Where(v => v.VoucherType == VoucherType.BankReceipt);
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

        var sales = await salesQ.SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        var cogs = await CalculateCogsAsync(context, from, EndOfDay(to));
        var expenses = await expQ.SumAsync(e => (decimal?)e.Amount) ?? 0;
        var bankFees = await bankQ.SumAsync(v => (decimal?)v.BankFees) ?? 0;
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
        var asOf = asOfDate.Date;
        var asOfEnd = asOf.AddDays(1);
        var creditRows = new List<ReceivablesAgingRow>();

        var creditQ = context.Invoices.Include(i => i.Customer)
            .Where(i => i.InvoiceType == InvoiceType.Sale
                        && i.PaymentMethod == PaymentMethod.Credit
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
                AgingBucket = ResolveAgingBucket(due, asOf)
            });
        }

        var unappliedQ = context.Vouchers.AsNoTracking()
            .Where(v => v.CustomerId != null
                        && v.Date < asOfEnd
                        && !v.InvoiceId.HasValue
                        && !v.InstallmentId.HasValue
                        && (v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt)
                        && (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)));
        if (customerId.HasValue) unappliedQ = unappliedQ.Where(v => v.CustomerId == customerId.Value);
        var unappliedReceipts = await unappliedQ
            .Select(v => new { CustomerId = v.CustomerId!.Value, v.Amount })
            .ToListAsync();

        creditRows = SupplierBalanceHelper.ApplyUnappliedPaymentsToAgingRows(
            creditRows,
            unappliedReceipts.Select(v => (v.CustomerId, v.Amount)),
            r => r.CustomerId,
            r => r.RemainingAmount,
            (r, rem) =>
            {
                r.RemainingAmount = rem;
                r.AgingBucket = ResolveAgingBucket(r.DueDate, asOf);
                return r;
            });

        var rows = new List<ReceivablesAgingRow>(creditRows);

        var instQ = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Where(i => i.Status != InstallmentStatus.Paid && i.RemainingAmount > 0);
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
        return new ReceivablesAgingReportResult
        {
            TotalOutstanding = rows.Sum(r => r.RemainingAmount),
            RowCount = rows.Count,
            CustomerCount = rows.Select(r => r.CustomerName).Distinct().Count(),
            Buckets = BuildAgingBuckets(rows.Select(r => (r.AgingBucket, r.RemainingAmount))),
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

        return new PayablesAgingReportResult
        {
            TotalOutstanding = rows.Sum(r => r.RemainingAmount),
            RowCount = rows.Count,
            SupplierCount = rows.Select(r => r.SupplierName).Distinct().Count(),
            Buckets = BuildAgingBuckets(rows.Select(r => (r.AgingBucket, r.RemainingAmount))),
            Rows = rows
        };
    }

    public async Task<CustomerCollectionsReportResult> GetCustomerCollectionsReportAsync(
        DateTime? from, DateTime? to, int? customerId, int? cashBoxId)
    {
        var context = _db;
        var rows = new List<CustomerCollectionRow>();

        var vouchQ = context.Vouchers.Include(v => v.Customer).Include(v => v.CashBox)
            .Where(v => v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt);
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
                Amount = v.Amount,
                AccountName = v.CashBox?.Name ?? "—",
                Notes = v.Notes ?? "—"
            });
        }

        var instQ = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Include(i => i.CashBox)
            .Where(i => i.PaidAmount > 0 && i.PaymentDate != null);
        if (from.HasValue) instQ = instQ.Where(i => i.PaymentDate >= from.Value);
        if (to.HasValue) instQ = instQ.Where(i => i.PaymentDate < EndOfDay(to));
        if (customerId.HasValue) instQ = instQ.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);
        if (cashBoxId.HasValue) instQ = instQ.Where(i => i.CashBoxId == cashBoxId.Value);

        foreach (var i in await instQ.ToListAsync())
        {
            rows.Add(new CustomerCollectionRow
            {
                Date = i.PaymentDate!.Value,
                SourceType = "تحصيل قسط",
                Reference = $"قسط #{i.Id}",
                CustomerName = i.InstallmentPlan?.Customer?.Name ?? "—",
                Amount = i.PaidAmount,
                AccountName = i.CashBox?.Name ?? "—",
                Notes = "—"
            });
        }

        rows = rows.OrderByDescending(r => r.Date).ToList();
        var voucherTotal = rows.Where(r => r.SourceType is "سند قبض" or "تسديد دين").Sum(r => r.Amount);
        var instTotal = rows.Where(r => r.SourceType == "تحصيل قسط").Sum(r => r.Amount);

        return new CustomerCollectionsReportResult
        {
            TotalCollected = rows.Sum(r => r.Amount),
            VoucherCollections = voucherTotal,
            InstallmentCollections = instTotal,
            RowCount = rows.Count,
            Rows = rows,
            DailyChart = rows.GroupBy(r => r.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderBy(x => x.Date).ToList(),
            ByCustomerChart = rows.GroupBy(r => r.CustomerName)
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

        return new OverdueCustomersReportResult
        {
            TotalOverdue = rows.Sum(r => r.OverdueAmount),
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
        var supplierNames = await context.Suppliers.ToDictionaryAsync(s => s.Id, s => s.Name);
        var rows = new List<SupplierPaymentRow>();

        var vouchQ = context.Vouchers.Include(v => v.CashBox)
            .Where(v => v.VoucherType == VoucherType.Payment && v.SupplierId != null);
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
                Amount = v.Amount,
                AccountName = v.CashBox?.Name ?? "—",
                Notes = v.Notes ?? "—"
            });
        }

        var purchQ = context.Invoices.Include(i => i.Supplier).Include(i => i.CashBox)
            .Where(i => i.InvoiceType == InvoiceType.Purchase && i.PaymentMethod == PaymentMethod.Cash);
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
                Amount = i.NetAmount,
                AccountName = i.CashBox?.Name ?? "—",
                Notes = "—"
            });
        }

        rows = rows.OrderByDescending(r => r.Date).ToList();
        var vouchTotal = rows.Where(r => r.SourceType == "سند صرف").Sum(r => r.Amount);
        var cashTotal = rows.Where(r => r.SourceType == "مشتريات نقدية").Sum(r => r.Amount);

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
            : await context.BankAccounts.ToListAsync();
        var bankMap = banks.ToDictionary(b => b.Id, b => b.Name);
        var bankIds = banks.Select(b => b.Id).ToHashSet();

        var vouchQ = context.Vouchers.Where(v => v.BankAccountId != null && bankIds.Contains(v.BankAccountId.Value));
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

        var transfers = await context.Transfers.ToListAsync();
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

        // Add transfers
        var cashBoxes = cashBoxId.HasValue
            ? await context.CashBoxes.Where(c => c.Id == cashBoxId.Value).ToListAsync()
            : await context.CashBoxes.ToListAsync();
        var ids = cashBoxes.Select(c => c.Id).ToHashSet();
        var nameMap = cashBoxes.ToDictionary(c => c.Id, c => c.Name);

        var transfers = await context.Transfers.ToListAsync();
        foreach (var t in transfers)
        {
            if (from.HasValue && t.Date < from.Value) continue;
            if (to.HasValue && t.Date >= EndOfDay(to)) continue;

            if (t.ToType == TransferAccountType.CashBox && ids.Contains(t.ToId)
                && (!cashBoxId.HasValue || t.ToId == cashBoxId.Value))
            {
                rows.Add(new CashBoxMovementRow
                {
                    Date = t.Date,
                    Type = "تحويل وارد",
                    Description = t.Notes ?? "تحويل إلى القاصة",
                    Incoming = t.Amount,
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
                    Outgoing = t.Amount,
                    AccountName = nameMap.GetValueOrDefault(t.FromId, "—")
                });
            }
        }

        // Installment collections with cash box
        var instQ = context.Installments.Include(i => i.CashBox)
            .Where(i => i.PaidAmount > 0 && i.PaymentDate != null && i.CashBoxId != null);
        if (cashBoxId.HasValue) instQ = instQ.Where(i => i.CashBoxId == cashBoxId.Value);
        if (from.HasValue) instQ = instQ.Where(i => i.PaymentDate >= from.Value);
        if (to.HasValue) instQ = instQ.Where(i => i.PaymentDate < EndOfDay(to));
        foreach (var i in await instQ.ToListAsync())
        {
            // Avoid double-count if already represented via vouchers; installment PaidAmount is source of truth for plans
            rows.Add(new CashBoxMovementRow
            {
                Date = i.PaymentDate!.Value,
                Type = "تحصيل قسط",
                Description = $"قسط #{i.Id}",
                Incoming = i.PaidAmount,
                AccountName = i.CashBox?.Name ?? "—"
            });
        }

        rows = rows.OrderBy(r => r.Date).ToList();
        var closing = cashBoxes.Sum(c => c.Balance);
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
            Balance = c.Balance
        }).Concat(banks.Select(b => new CashBalanceRow
        {
            AccountType = "مصرف",
            Name = b.Name,
            AccountNumber = b.AccountNumber ?? "—",
            Balance = b.Balance
        })).OrderBy(r => r.AccountType).ThenBy(r => r.Name).ToList();

        var cashTotal = cashBoxes.Sum(c => c.Balance);
        var bankTotal = banks.Sum(b => b.Balance);

        return new CashBalancesSummaryReportResult
        {
            CashBoxesTotal = cashTotal,
            BanksTotal = bankTotal,
            TotalLiquid = cashTotal + bankTotal,
            AccountCount = rows.Count,
            Rows = rows,
            CompositionChart =
            [
                new NameAmountPoint { Name = "قاصات", Amount = cashTotal },
                new NameAmountPoint { Name = "مصارف", Amount = bankTotal }
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
                Notes = t.Notes ?? "—",
                CreatedBy = t.CreatedBy ?? "—"
            });
        }

        return new TransfersReportResult
        {
            TotalAmount = rows.Sum(r => r.Amount),
            TransferCount = rows.Count,
            AverageAmount = rows.Count > 0 ? Math.Round(rows.Average(r => r.Amount), 0) : 0,
            Rows = rows,
            DailyChart = rows.GroupBy(r => r.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderBy(x => x.Date).ToList(),
            ByTypeChart = rows.GroupBy(r => $"{r.FromAccount} ←")
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
        var rows = new List<InventoryValuationRow>();
        foreach (var s in stocks)
        {
            var purchaseItems = await context.InvoiceItems
                .Include(ii => ii.Invoice)
                .Where(ii => ii.ProductId == s.ProductId && ii.Invoice!.InvoiceType == InvoiceType.Purchase)
                .ToListAsync();
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

    public async Task<CogsReportResult> GetCogsReportAsync(DateTime? from, DateTime? to, int? warehouseId)
    {
        var context = _db;
        var soldQ = context.InvoiceItems
            .Include(ii => ii.Invoice)
            .Include(ii => ii.Product)
            .Where(ii => ii.ProductId != null
                         && ii.Invoice != null
                         && (ii.Invoice.InvoiceType == InvoiceType.Sale || ii.Invoice.InvoiceType == InvoiceType.Installment));
        if (from.HasValue) soldQ = soldQ.Where(ii => ii.Invoice!.Date >= from.Value);
        if (to.HasValue) soldQ = soldQ.Where(ii => ii.Invoice!.Date < EndOfDay(to));
        if (warehouseId.HasValue) soldQ = soldQ.Where(ii => ii.Invoice!.WarehouseId == warehouseId.Value);

        var soldItems = await soldQ.ToListAsync();
        var productIds = soldItems.Select(ii => ii.ProductId!.Value).Distinct().ToList();
        var stocks = await context.WarehouseStocks.Where(ws => productIds.Contains(ws.ProductId)).ToListAsync();
        var purchaseItems = await context.InvoiceItems
            .Include(ii => ii.Invoice)
            .Where(ii => ii.ProductId != null
                         && productIds.Contains(ii.ProductId.Value)
                         && ii.Invoice != null
                         && ii.Invoice.InvoiceType == InvoiceType.Purchase
                         && (!to.HasValue || ii.Invoice.Date < EndOfDay(to)))
            .ToListAsync();
        var purchasesByProduct = purchaseItems.GroupBy(ii => ii.ProductId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = soldItems.GroupBy(ii => ii.ProductId!.Value).Select(g =>
        {
            var avg = CloudProductCostHelper.ComputeAverageUnitCostForProduct(
                purchasesByProduct.GetValueOrDefault(g.Key) ?? [], stocks, g.Key);
            var qty = g.Sum(x => x.Quantity);
            var revenue = g.Sum(x => x.TotalPrice);
            var cogs = Math.Round(qty * avg, 0);
            return new CogsReportRow
            {
                ProductId = g.Key,
                ProductName = g.First().Product?.Name ?? g.First().ItemName,
                QuantitySold = qty,
                AverageCost = avg,
                CogsAmount = cogs,
                Revenue = revenue,
                GrossProfit = revenue - cogs
            };
        }).OrderByDescending(r => r.CogsAmount).ToList();

        var totalCogs = rows.Sum(r => r.CogsAmount);
        var totalRev = rows.Sum(r => r.Revenue);

        return new CogsReportResult
        {
            TotalCogs = totalCogs,
            TotalRevenue = totalRev,
            GrossProfit = totalRev - totalCogs,
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
                        dayCogs += Math.Round(item.Quantity * avg, 0);
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

    public async Task<ProfitAndLossReportResult> GetProfitAndLossReportAsync(DateTime? from, DateTime? to)
    {
        var context = _db;
        var salesQ = CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans);
        var expQ = context.Expenses.AsQueryable();
        var bankQ = context.Vouchers.Where(v => v.VoucherType == VoucherType.BankReceipt);
        var distQ = context.ProfitDistributions.AsQueryable();
        if (from.HasValue)
        {
            salesQ = salesQ.Where(i => i.Date >= from.Value);
            expQ = expQ.Where(e => e.Date >= from.Value);
            bankQ = bankQ.Where(v => v.Date >= from.Value);
            distQ = distQ.Where(d => d.Date >= from.Value);
        }
        if (to.HasValue)
        {
            salesQ = salesQ.Where(i => i.Date < EndOfDay(to));
            expQ = expQ.Where(e => e.Date < EndOfDay(to));
            bankQ = bankQ.Where(v => v.Date < EndOfDay(to));
            distQ = distQ.Where(d => d.Date < EndOfDay(to));
        }

        var sales = await salesQ.SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        var cogs = await CalculateCogsAsync(context, from, EndOfDay(to));
        var expenses = await expQ.SumAsync(e => (decimal?)e.Amount) ?? 0;
        var bankFees = await bankQ.SumAsync(v => (decimal?)v.BankFees) ?? 0;
        var distributed = await distQ.SumAsync(d => (decimal?)d.DistributedAmount) ?? 0;
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

        var monthly = await GetMonthlyProfitAsync(from, to);

        return new ProfitAndLossReportResult
        {
            TotalSales = sales,
            CostOfGoodsSold = cogs,
            GrossProfit = gross,
            TotalExpenses = expenses,
            TotalBankFees = bankFees,
            OperatingProfit = operating,
            DistributedProfits = distributed,
            NetProfit = net,
            GrossMarginPercent = sales > 0 ? Math.Round(gross / sales * 100, 1) : 0,
            NetMarginPercent = sales > 0 ? Math.Round(net / sales * 100, 1) : 0,
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
        var endOfDay = date.Date.AddDays(1).AddTicks(-1);

        var capital = await context.CapitalEntries
            .Where(c => c.Type == CapitalEntryType.Initial && c.Date <= endOfDay)
            .SumAsync(c => c.Amount);
        var adjustments = await context.CapitalEntries
            .Where(c => c.Type == CapitalEntryType.Adjustment && c.Date <= endOfDay)
            .SumAsync(c => c.Amount);
        var profitOpening = await CloudProductCostHelper.GetProfitOpeningBalanceAsync(context, endOfDay);

        var sales = await CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans)
            .Where(i => i.Date <= endOfDay)
            .SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        var cogs = await CalculateCogsAsync(context, null, endOfDay.AddTicks(1));
        var expenses = await context.Expenses.Where(e => e.Date <= endOfDay).SumAsync(e => (decimal?)e.Amount) ?? 0;
        var distributed = await context.ProfitDistributions
            .Where(d => d.Date <= endOfDay).SumAsync(d => (decimal?)d.DistributedAmount) ?? 0;
        var accumulated = profitOpening + (sales - cogs) - expenses - distributed;
        var equity = capital + adjustments + accumulated;

        var cash = await context.CashBoxes.SumAsync(c => (decimal?)c.Balance) ?? 0;
        var banks = await context.BankAccounts.SumAsync(b => (decimal?)b.Balance) ?? 0;

        // AR = متبقي الآجل − سندات قبض/دين غير مطبّقة
        var creditRemaining = await context.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Sale && i.PaymentMethod == PaymentMethod.Credit && i.Date <= endOfDay)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
        var unappliedDebt = await context.Vouchers
            .Where(v => v.CustomerId != null &&
                        v.VoucherType == VoucherType.DebtReceipt &&
                        v.Date <= endOfDay &&
                        !v.InvoiceId.HasValue &&
                        !v.InstallmentId.HasValue &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;
        var unappliedReceipts = await context.Vouchers
            .Where(v => v.CustomerId != null &&
                        v.VoucherType == VoucherType.Receipt &&
                        v.Date <= endOfDay &&
                        !v.InvoiceId.HasValue &&
                        !v.InstallmentId.HasValue &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;
        var creditAr = CustomerBalanceHelper.ComputeOutstandingBalance(
            creditRemaining, 0, unappliedDebt, unappliedReceipts);
        var installmentAr = await context.Installments
            .Where(i => i.RemainingAmount > 0)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var stocks = await context.WarehouseStocks.Include(ws => ws.Product).ToListAsync();
        decimal inventory = 0;
        foreach (var s in stocks.Where(s => s.Quantity > 0))
        {
            var purchaseItems = await context.InvoiceItems
                .Include(ii => ii.Invoice)
                .Where(ii => ii.ProductId == s.ProductId && ii.Invoice!.InvoiceType == InvoiceType.Purchase
                             && ii.Invoice.Date <= endOfDay)
                .ToListAsync();
            var avg = CloudProductCostHelper.ComputeAverageUnitCost(purchaseItems, s.OpeningQuantity, s.UnitCost);
            inventory += Math.Round(s.Quantity * avg, 0);
        }

        // AP = متبقي المشتريات الآجلة − سندات صرف غير مطبّقة
        var supplierCreditRemaining = await context.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Purchase
                        && i.PaymentMethod == PaymentMethod.Credit
                        && i.Date <= endOfDay)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
        var unappliedSupplierPayments = await context.Vouchers
            .Where(v => v.SupplierId != null
                        && v.VoucherType == VoucherType.Payment
                        && v.Date <= endOfDay
                        && !v.InvoiceId.HasValue
                        && (v.Notes == null || !v.Notes.Contains(SupplierBalanceHelper.PaymentAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;
        var payables = SupplierBalanceHelper.ComputeOutstandingPayables(
            supplierCreditRemaining, unappliedSupplierPayments);

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
        var endExclusive = EndOfDay(to);

        var customersQ = context.Customers.AsNoTracking().AsQueryable();
        if (from.HasValue) customersQ = customersQ.Where(c => c.CreatedAt >= from.Value);
        if (endExclusive.HasValue) customersQ = customersQ.Where(c => c.CreatedAt < endExclusive.Value);
        var newCustomersCount = await customersQ.CountAsync();

        var salesInvoicesQ = context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment);
        if (from.HasValue) salesInvoicesQ = salesInvoicesQ.Where(i => i.Date >= from.Value);
        if (endExclusive.HasValue) salesInvoicesQ = salesInvoicesQ.Where(i => i.Date < endExclusive.Value);

        var salesInvoices = await salesInvoicesQ
            .Select(i => new { i.Id, i.Date, i.NetAmount, i.CustomerId, CustomerName = i.Customer != null ? i.Customer.Name : "—" })
            .ToListAsync();

        var salesInvoiceIds = salesInvoices.Select(i => i.Id).ToList();
        var salesItems = salesInvoiceIds.Count == 0
            ? new List<(int? ProductId, decimal Quantity)>()
            : (await context.InvoiceItems.AsNoTracking()
                .Where(ii => salesInvoiceIds.Contains(ii.InvoiceId))
                .Select(ii => new { ii.ProductId, ii.Quantity })
                .ToListAsync())
              .Select(ii => (ii.ProductId, ii.Quantity))
              .ToList();

        var allActivityQ = context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.Sale
                        || i.InvoiceType == InvoiceType.Installment
                        || i.InvoiceType == InvoiceType.Purchase);
        if (from.HasValue) allActivityQ = allActivityQ.Where(i => i.Date >= from.Value);
        if (endExclusive.HasValue) allActivityQ = allActivityQ.Where(i => i.Date < endExclusive.Value);

        var activityDates = await allActivityQ
            .Select(i => new { i.Date, i.NetAmount, i.InvoiceType })
            .ToListAsync();

        var salesByYear = salesInvoices
            .GroupBy(i => i.Date.Year)
            .OrderBy(g => g.Key)
            .Select(g => new NameAmountPoint { Name = g.Key.ToString(), Amount = g.Sum(x => x.NetAmount) })
            .ToList();

        var topCustomers = salesInvoices
            .Where(i => i.CustomerId.HasValue)
            .GroupBy(i => new { i.CustomerId, i.CustomerName })
            .Select(g => new NameAmountPoint { Name = g.Key.CustomerName, Amount = g.Sum(x => x.NetAmount) })
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
                    .Where(x => x.InvoiceType is InvoiceType.Sale or InvoiceType.Installment)
                    .Sum(x => x.NetAmount);
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
            TotalSalesAmount = salesInvoices.Sum(i => i.NetAmount),
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

        var salesByMethod = await salesQ
            .GroupBy(i => i.PaymentMethod)
            .Select(g => new { Method = g.Key, Amount = g.Sum(x => x.NetAmount), Count = g.Count() })
            .ToListAsync();

        var cashSales = salesByMethod.FirstOrDefault(x => x.Method == PaymentMethod.Cash)?.Amount ?? 0;
        var creditSales = salesByMethod.FirstOrDefault(x => x.Method == PaymentMethod.Credit)?.Amount ?? 0;
        var installmentSales = salesByMethod.FirstOrDefault(x => x.Method == PaymentMethod.Installment)?.Amount ?? 0;
        var salesCount = salesByMethod.Sum(x => x.Count);
        var totalSales = salesByMethod.Sum(x => x.Amount);

        var purchaseQ = context.Invoices.AsNoTracking().Where(i => i.InvoiceType == InvoiceType.Purchase);
        if (from.HasValue) purchaseQ = purchaseQ.Where(i => i.Date >= from.Value);
        if (endExclusive.HasValue) purchaseQ = purchaseQ.Where(i => i.Date < endExclusive.Value);

        var purchasesByMethod = await purchaseQ
            .GroupBy(i => i.PaymentMethod)
            .Select(g => new { Method = g.Key, Amount = g.Sum(x => x.NetAmount), Count = g.Count() })
            .ToListAsync();

        var cashPurchases = purchasesByMethod.FirstOrDefault(x => x.Method == PaymentMethod.Cash)?.Amount ?? 0;
        var creditPurchases = purchasesByMethod.FirstOrDefault(x => x.Method == PaymentMethod.Credit)?.Amount ?? 0;
        var purchaseCount = purchasesByMethod.Sum(x => x.Count);
        var totalPurchases = purchasesByMethod.Sum(x => x.Amount);

        var activeCustomers = await salesQ
            .Where(i => i.CustomerId != null)
            .Select(i => i.CustomerId!.Value)
            .Distinct()
            .CountAsync();

        var activeSuppliers = await purchaseQ
            .Where(i => i.SupplierId != null)
            .Select(i => i.SupplierId!.Value)
            .Distinct()
            .CountAsync();

        var receiptQ = context.Vouchers.AsNoTracking()
            .Where(v => v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt);
        if (from.HasValue) receiptQ = receiptQ.Where(v => v.Date >= from.Value);
        if (endExclusive.HasValue) receiptQ = receiptQ.Where(v => v.Date < endExclusive.Value);
        var receiptAmount = await receiptQ.SumAsync(v => (decimal?)v.Amount) ?? 0;

        var paymentQ = context.Vouchers.AsNoTracking().Where(v => v.VoucherType == VoucherType.Payment);
        if (from.HasValue) paymentQ = paymentQ.Where(v => v.Date >= from.Value);
        if (endExclusive.HasValue) paymentQ = paymentQ.Where(v => v.Date < endExclusive.Value);
        var paymentAmount = await paymentQ.SumAsync(v => (decimal?)v.Amount) ?? 0;

        var instPaidQ = context.Installments.AsNoTracking().Where(i => i.PaidAmount > 0);
        if (from.HasValue) instPaidQ = instPaidQ.Where(i => (i.PaymentDate ?? i.DueDate) >= from.Value);
        if (endExclusive.HasValue) instPaidQ = instPaidQ.Where(i => (i.PaymentDate ?? i.DueDate) < endExclusive.Value);
        var collectedInstallments = await instPaidQ.SumAsync(i => (decimal?)i.PaidAmount) ?? 0;

        var overdueQ = context.Installments.AsNoTracking()
            .Where(i => i.Status != InstallmentStatus.Paid && i.DueDate < asOf && i.RemainingAmount > 0);
        var overdueCount = await overdueQ.CountAsync();
        var overdueAmount = await overdueQ.SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var transferQ = context.Transfers.AsNoTracking().AsQueryable();
        if (from.HasValue) transferQ = transferQ.Where(t => t.Date >= from.Value);
        if (endExclusive.HasValue) transferQ = transferQ.Where(t => t.Date < endExclusive.Value);
        var transfersCount = await transferQ.CountAsync();
        var transfersAmount = await transferQ.SumAsync(t => (decimal?)t.Amount) ?? 0;

        // رصيد الآجل للعملاء = متبقي فواتير الآجل − سندات قبض/دين غير مطبّقة (نفس لوحة التحكم)
        var customerCreditRemaining = await context.Invoices.AsNoTracking()
            .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment)
                        && i.PaymentMethod == PaymentMethod.Credit
                        && !i.IsCreditPaid
                        && i.Date <= asOfEndOfDay)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var unappliedDebt = await context.Vouchers.AsNoTracking()
            .Where(v => v.VoucherType == VoucherType.DebtReceipt
                        && v.Date <= asOfEndOfDay
                        && (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;

        var unappliedReceipts = await context.Vouchers.AsNoTracking()
            .Where(v => v.VoucherType == VoucherType.Receipt
                        && v.Date <= asOfEndOfDay
                        && !v.InvoiceId.HasValue
                        && !v.InstallmentId.HasValue
                        && (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;

        var customerReceivables = Math.Max(0, customerCreditRemaining - unappliedDebt - unappliedReceipts);

        // رصيد الآجل للموردين = متبقي مشتريات الآجل − سندات صرف غير مطبّقة
        var supplierCreditRemaining = await context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.Purchase
                        && i.PaymentMethod == PaymentMethod.Credit
                        && !i.IsCreditPaid
                        && i.Date <= asOfEndOfDay)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var unappliedSupplierPayments = await context.Vouchers.AsNoTracking()
            .Where(v => v.VoucherType == VoucherType.Payment
                        && v.SupplierId != null
                        && v.Date <= asOfEndOfDay
                        && !v.InvoiceId.HasValue
                        && (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;

        var supplierPayables = SupplierBalanceHelper.ComputeOutstandingPayables(
            supplierCreditRemaining, unappliedSupplierPayments);

        var installmentReceivables = await context.Installments.AsNoTracking()
            .Where(i => i.RemainingAmount > 0)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var customerBalanceMap = new Dictionary<int, decimal>();
        var creditByCustomer = await context.Invoices.AsNoTracking()
            .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment)
                        && i.PaymentMethod == PaymentMethod.Credit
                        && !i.IsCreditPaid
                        && i.Date <= asOfEndOfDay
                        && i.CustomerId != null
                        && i.RemainingAmount > 0)
            .GroupBy(i => i.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Amount = g.Sum(x => x.RemainingAmount) })
            .ToListAsync();

        var unappliedDebtByCustomer = await context.Vouchers.AsNoTracking()
            .Where(v => v.VoucherType == VoucherType.DebtReceipt
                        && v.CustomerId != null
                        && v.Date <= asOfEndOfDay
                        && (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .GroupBy(v => v.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync();

        var unappliedReceiptByCustomer = await context.Vouchers.AsNoTracking()
            .Where(v => v.VoucherType == VoucherType.Receipt
                        && v.CustomerId != null
                        && v.Date <= asOfEndOfDay
                        && !v.InvoiceId.HasValue
                        && !v.InstallmentId.HasValue
                        && (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .GroupBy(v => v.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync();

        var installmentByCustomer = await (
            from inst in context.Installments.AsNoTracking()
            join plan in context.InstallmentPlans.AsNoTracking() on inst.InstallmentPlanId equals plan.Id
            where inst.RemainingAmount > 0
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

        foreach (var key in customerBalanceMap.Keys.ToList())
            customerBalanceMap[key] = Math.Max(0, customerBalanceMap[key]);

        var customersWithBalance = customerBalanceMap.Count(kv => kv.Value > 0);
        var highestCustomerBalance = customerBalanceMap.Count > 0 ? customerBalanceMap.Values.Max() : 0;

        var supplierBalanceMap = new Dictionary<int, decimal>();
        var creditBySupplier = await context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.Purchase
                        && i.PaymentMethod == PaymentMethod.Credit
                        && !i.IsCreditPaid
                        && i.Date <= asOfEndOfDay
                        && i.SupplierId != null
                        && i.RemainingAmount > 0)
            .GroupBy(i => i.SupplierId!.Value)
            .Select(g => new { SupplierId = g.Key, Amount = g.Sum(x => x.RemainingAmount) })
            .ToListAsync();

        var unappliedPayBySupplier = await context.Vouchers.AsNoTracking()
            .Where(v => v.VoucherType == VoucherType.Payment
                        && v.SupplierId != null
                        && v.Date <= asOfEndOfDay
                        && !v.InvoiceId.HasValue
                        && (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .GroupBy(v => v.SupplierId!.Value)
            .Select(g => new { SupplierId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync();

        foreach (var row in creditBySupplier)
            supplierBalanceMap[row.SupplierId] = row.Amount;
        foreach (var row in unappliedPayBySupplier)
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
            join cust in context.Customers.AsNoTracking() on plan.CustomerId equals cust.Id
            where inst.Status != InstallmentStatus.Paid && inst.DueDate < asOf && inst.RemainingAmount > 0
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
            .Where(r => r.AccountType == "قاصة")
            .Select(r => new NameAmountPoint { Name = r.Name, Amount = r.Balance })
            .OrderByDescending(x => x.Amount)
            .Take(detailLimit)
            .ToList();
        var banksDetail = cash.Rows
            .Where(r => r.AccountType == "مصرف")
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
}
