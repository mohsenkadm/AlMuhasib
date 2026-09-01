using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.SalesRep;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class SalesRepService : ISalesRepService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUser;

    public SalesRepService(IDbContextFactory<AppDbContext> contextFactory, ICurrentUserService currentUser)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
    }

    public async Task<SalesRepCommissionEntry?> CalculateAndSaveCommissionAsync(int invoiceId, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        var invoice = await db.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && !i.IsDeleted, ct);

        if (invoice is null || invoice.SalesRepresentativeId is null || invoice.SalesRepresentativeId <= 0)
            return null;

        if (invoice.InvoiceType is not (InvoiceType.Sale or InvoiceType.Installment))
            return null;

        var existing = await db.SalesRepCommissionEntries
            .FirstOrDefaultAsync(e => e.InvoiceId == invoiceId && !e.IsDeleted, ct);

        var rules = await db.SalesRepCommissionRules
            .Where(r => r.SalesRepresentativeId == invoice.SalesRepresentativeId
                        && r.IsActive
                        && !r.IsDeleted)
            .ToListAsync(ct);

        var (type, baseAmount, commission) = await ComputeCommissionAsync(db, invoice, rules, ct);

        if (commission <= 0 && existing is null)
            return null;

        var user = string.IsNullOrWhiteSpace(_currentUser.Username) ? "system" : _currentUser.Username;
        var now = DateTime.UtcNow;

        if (existing is null)
        {
            existing = new SalesRepCommissionEntry
            {
                SalesRepresentativeId = invoice.SalesRepresentativeId.Value,
                InvoiceId = invoice.Id,
                CustomerId = invoice.CustomerId,
                InvoiceDate = invoice.Date,
                CommissionType = type,
                BaseAmount = baseAmount,
                CommissionAmount = commission,
                PaidAmount = 0,
                Status = SalesRepCommissionStatus.Unpaid,
                CreatedBy = user,
                CreatedAt = now
            };
            db.SalesRepCommissionEntries.Add(existing);
        }
        else
        {
            // لا نعيد حساب المدفوع — نحدّث مبلغ العمولة فقط
            existing.CommissionType = type;
            existing.BaseAmount = baseAmount;
            existing.CommissionAmount = commission;
            existing.CustomerId = invoice.CustomerId;
            existing.InvoiceDate = invoice.Date;
            existing.UpdatedAt = now;
            existing.UpdatedBy = user;
            existing.Status = ResolveStatus(existing.CommissionAmount, existing.PaidAmount);
        }

        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<SalesRepStatement> GetStatementAsync(
        int salesRepresentativeId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        var rep = await db.SalesRepresentatives
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == salesRepresentativeId && !r.IsDeleted, ct)
            ?? throw new InvalidOperationException("المندوب غير موجود");

        var fromDate = from?.Date;
        var toDate = to?.Date.AddDays(1);

        var invoicesQ = db.Invoices.AsNoTracking()
            .Where(i => !i.IsDeleted
                        && i.SalesRepresentativeId == salesRepresentativeId
                        && (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment));
        if (fromDate is not null) invoicesQ = invoicesQ.Where(i => i.Date >= fromDate);
        if (toDate is not null) invoicesQ = invoicesQ.Where(i => i.Date < toDate);

        var invoices = await invoicesQ
            .Include(i => i.Customer)
            .OrderByDescending(i => i.Date)
            .ToListAsync(ct);

        var commissionsQ = db.SalesRepCommissionEntries.AsNoTracking()
            .Where(e => !e.IsDeleted && e.SalesRepresentativeId == salesRepresentativeId);
        if (fromDate is not null) commissionsQ = commissionsQ.Where(e => e.InvoiceDate >= fromDate);
        if (toDate is not null) commissionsQ = commissionsQ.Where(e => e.InvoiceDate < toDate);

        var commissions = await commissionsQ
            .Include(e => e.Invoice)
            .Include(e => e.Customer)
            .OrderByDescending(e => e.InvoiceDate)
            .ToListAsync(ct);

        var collectionsQ = db.SalesRepCollections.AsNoTracking()
            .Where(c => !c.IsDeleted && c.SalesRepresentativeId == salesRepresentativeId);
        if (fromDate is not null) collectionsQ = collectionsQ.Where(c => c.CollectionDate >= fromDate);
        if (toDate is not null) collectionsQ = collectionsQ.Where(c => c.CollectionDate < toDate);
        var collections = await collectionsQ.ToListAsync(ct);

        var commissionByInvoice = commissions.ToDictionary(c => c.InvoiceId, c => c.CommissionAmount);

        var customerCount = await db.Customers.AsNoTracking()
            .CountAsync(c => !c.IsDeleted && c.SalesRepresentativeId == salesRepresentativeId, ct);

        return new SalesRepStatement
        {
            SalesRepresentativeId = rep.Id,
            SalesRepresentativeName = rep.Name,
            From = from,
            To = to,
            TotalSales = invoices.Sum(i => i.NetAmount),
            TotalCollections = invoices.Sum(i => i.PaidAmount) + collections.Sum(c => c.Amount),
            RemainingReceivables = invoices.Sum(i => i.RemainingAmount),
            TotalCommissions = commissions.Sum(c => c.CommissionAmount),
            PaidCommissions = commissions.Sum(c => c.PaidAmount),
            UnpaidCommissions = commissions.Sum(c => Math.Max(0, c.CommissionAmount - c.PaidAmount)),
            CollectedByRep = collections.Sum(c => c.Amount),
            HandedOverByRep = collections.Sum(c => c.HandedOverAmount),
            PendingHandover = collections.Sum(c => Math.Max(0, c.Amount - c.HandedOverAmount)),
            InvoiceCount = invoices.Count,
            CustomerCount = customerCount,
            RecentInvoices = invoices.Take(100).Select(i => new SalesRepStatementLine
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                Date = i.Date,
                CustomerName = i.Customer?.Name ?? "—",
                CustomerFileNumber = i.Customer?.FileNumber,
                NetAmount = i.NetAmount,
                PaidAmount = i.PaidAmount,
                RemainingAmount = i.RemainingAmount,
                CommissionAmount = commissionByInvoice.GetValueOrDefault(i.Id)
            }).ToList(),
            Commissions = commissions.Select(MapCommissionRow).ToList()
        };
    }

    public async Task<IReadOnlyList<SalesRepPerformanceRow>> GetPerformanceComparisonAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        var reps = await db.SalesRepresentatives.AsNoTracking()
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        var fromDate = from?.Date;
        var toDate = to?.Date.AddDays(1);
        var asOf = to?.Date ?? DateTime.Today;

        var rows = new List<SalesRepPerformanceRow>(reps.Count);
        foreach (var rep in reps)
        {
            var invoicesQ = db.Invoices.AsNoTracking()
                .Where(i => !i.IsDeleted
                            && i.SalesRepresentativeId == rep.Id
                            && (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment));
            if (fromDate is not null) invoicesQ = invoicesQ.Where(i => i.Date >= fromDate);
            if (toDate is not null) invoicesQ = invoicesQ.Where(i => i.Date < toDate);

            var invoiceStats = await invoicesQ
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Count = g.Count(),
                    Sales = g.Sum(i => i.NetAmount),
                    Paid = g.Sum(i => i.PaidAmount),
                    Remaining = g.Sum(i => i.RemainingAmount)
                })
                .FirstOrDefaultAsync(ct);

            var commissionsQ = db.SalesRepCommissionEntries.AsNoTracking()
                .Where(e => !e.IsDeleted && e.SalesRepresentativeId == rep.Id);
            if (fromDate is not null) commissionsQ = commissionsQ.Where(e => e.InvoiceDate >= fromDate);
            if (toDate is not null) commissionsQ = commissionsQ.Where(e => e.InvoiceDate < toDate);

            var commissionStats = await commissionsQ
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Sum(e => e.CommissionAmount),
                    Unpaid = g.Sum(e => e.CommissionAmount - e.PaidAmount)
                })
                .FirstOrDefaultAsync(ct);

            var customerCount = await db.Customers.AsNoTracking()
                .CountAsync(c => !c.IsDeleted && c.SalesRepresentativeId == rep.Id, ct);

            var target = await db.SalesRepTargets.AsNoTracking()
                .Where(t => !t.IsDeleted
                            && t.SalesRepresentativeId == rep.Id
                            && t.PeriodStart <= asOf
                            && t.PeriodEnd >= asOf)
                .OrderByDescending(t => t.PeriodStart)
                .FirstOrDefaultAsync(ct);

            var achieved = invoiceStats?.Sales ?? 0;
            var targetAmount = target?.TargetAmount ?? 0;

            rows.Add(new SalesRepPerformanceRow
            {
                SalesRepresentativeId = rep.Id,
                Name = rep.Name,
                Region = rep.Region,
                IsActive = rep.IsActive,
                InvoiceCount = invoiceStats?.Count ?? 0,
                CustomerCount = customerCount,
                TotalSales = achieved,
                TotalCollections = invoiceStats?.Paid ?? 0,
                RemainingReceivables = invoiceStats?.Remaining ?? 0,
                TotalCommissions = commissionStats?.Total ?? 0,
                UnpaidCommissions = Math.Max(0, commissionStats?.Unpaid ?? 0),
                TargetAmount = targetAmount,
                AchievedAmount = achieved,
                AchievementPercent = targetAmount > 0 ? Math.Round(achieved / targetAmount * 100m, 1) : 0
            });
        }

        return rows.OrderByDescending(r => r.TotalSales).ToList();
    }

    public async Task<IReadOnlyList<SalesRepTargetProgress>> GetTargetProgressAsync(
        int? salesRepresentativeId, DateTime? asOf, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var date = (asOf ?? DateTime.Today).Date;

        var targetsQ = db.SalesRepTargets.AsNoTracking()
            .Include(t => t.SalesRepresentative)
            .Where(t => !t.IsDeleted && t.PeriodStart <= date && t.PeriodEnd >= date);

        if (salesRepresentativeId is > 0)
            targetsQ = targetsQ.Where(t => t.SalesRepresentativeId == salesRepresentativeId);

        var targets = await targetsQ.OrderBy(t => t.SalesRepresentative!.Name).ToListAsync(ct);
        var result = new List<SalesRepTargetProgress>(targets.Count);

        foreach (var t in targets)
        {
            var achieved = await db.Invoices.AsNoTracking()
                .Where(i => !i.IsDeleted
                            && i.SalesRepresentativeId == t.SalesRepresentativeId
                            && (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment)
                            && i.Date >= t.PeriodStart
                            && i.Date < t.PeriodEnd.Date.AddDays(1))
                .SumAsync(i => (decimal?)i.NetAmount, ct) ?? 0;

            result.Add(new SalesRepTargetProgress
            {
                TargetId = t.Id,
                SalesRepresentativeId = t.SalesRepresentativeId,
                SalesRepresentativeName = t.SalesRepresentative?.Name ?? "—",
                PeriodStart = t.PeriodStart,
                PeriodEnd = t.PeriodEnd,
                TargetAmount = t.TargetAmount,
                AchievedAmount = achieved,
                RemainingAmount = Math.Max(0, t.TargetAmount - achieved),
                AchievementPercent = t.TargetAmount > 0 ? Math.Round(achieved / t.TargetAmount * 100m, 1) : 0
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<SalesRepCustomerRow>> GetCustomersByRepAsync(
        int salesRepresentativeId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        var customers = await db.Customers.AsNoTracking()
            .Where(c => !c.IsDeleted && c.SalesRepresentativeId == salesRepresentativeId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var fromDate = from?.Date;
        var toDate = to?.Date.AddDays(1);

        var rows = new List<SalesRepCustomerRow>(customers.Count);
        foreach (var customer in customers)
        {
            var invoicesQ = db.Invoices.AsNoTracking()
                .Where(i => !i.IsDeleted
                            && i.CustomerId == customer.Id
                            && (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment));
            if (fromDate is not null) invoicesQ = invoicesQ.Where(i => i.Date >= fromDate);
            if (toDate is not null) invoicesQ = invoicesQ.Where(i => i.Date < toDate);

            var invoices = await invoicesQ.OrderByDescending(i => i.Date).ToListAsync(ct);
            var lastInvoice = invoices.FirstOrDefault();

            var collectionsQ = db.SalesRepCollections.AsNoTracking()
                .Where(c => !c.IsDeleted
                            && c.SalesRepresentativeId == salesRepresentativeId
                            && c.CustomerId == customer.Id);
            if (fromDate is not null) collectionsQ = collectionsQ.Where(c => c.CollectionDate >= fromDate);
            if (toDate is not null) collectionsQ = collectionsQ.Where(c => c.CollectionDate < toDate);

            var lastCollection = await collectionsQ
                .OrderByDescending(c => c.CollectionDate)
                .FirstOrDefaultAsync(ct);

            // آخر تسديد من الفواتير (PaidAmount > 0) أو التحصيل
            DateTime? lastPaymentDate = lastCollection?.CollectionDate;
            decimal lastPaymentAmount = lastCollection?.Amount ?? 0;
            if (lastInvoice is { PaidAmount: > 0 } &&
                (lastPaymentDate is null || lastInvoice.Date >= lastPaymentDate))
            {
                lastPaymentDate = lastInvoice.UpdatedAt?.Date ?? lastInvoice.Date;
                lastPaymentAmount = lastInvoice.PaidAmount;
            }

            rows.Add(new SalesRepCustomerRow
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
            CustomerFileNumber = customer.FileNumber,
                Phone = customer.Phone,
                TotalSales = invoices.Sum(i => i.NetAmount),
                PaidAmount = invoices.Sum(i => i.PaidAmount),
                RemainingAmount = invoices.Sum(i => i.RemainingAmount),
                LastInvoiceDate = lastInvoice?.Date,
                LastInvoiceNumber = lastInvoice?.InvoiceNumber,
                LastPaymentDate = lastPaymentDate,
                LastPaymentAmount = lastPaymentAmount
            });
        }

        return rows;
    }

    public async Task MarkCommissionPaidAsync(int commissionEntryId, decimal amount, CancellationToken ct = default)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var entry = await db.SalesRepCommissionEntries
            .FirstOrDefaultAsync(e => e.Id == commissionEntryId && !e.IsDeleted, ct)
            ?? throw new InvalidOperationException("سجل العمولة غير موجود");

        entry.PaidAmount = Math.Min(entry.CommissionAmount, entry.PaidAmount + amount);
        entry.Status = ResolveStatus(entry.CommissionAmount, entry.PaidAmount);
        entry.UpdatedAt = DateTime.UtcNow;
        entry.UpdatedBy = string.IsNullOrWhiteSpace(_currentUser.Username) ? "system" : _currentUser.Username;
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkCollectionHandedOverAsync(int collectionId, decimal amount, CancellationToken ct = default)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var collection = await db.SalesRepCollections
            .FirstOrDefaultAsync(c => c.Id == collectionId && !c.IsDeleted, ct)
            ?? throw new InvalidOperationException("سجل التحصيل غير موجود");

        collection.HandedOverAmount = Math.Min(collection.Amount, collection.HandedOverAmount + amount);
        collection.HandedOverAt = DateTime.UtcNow;
        collection.UpdatedAt = DateTime.UtcNow;
        collection.UpdatedBy = string.IsNullOrWhiteSpace(_currentUser.Username) ? "system" : _currentUser.Username;
        await db.SaveChangesAsync(ct);
    }

    private static SalesRepCommissionRow MapCommissionRow(SalesRepCommissionEntry e) => new()
    {
        Id = e.Id,
        InvoiceId = e.InvoiceId,
        InvoiceNumber = e.Invoice?.InvoiceNumber ?? $"#{e.InvoiceId}",
        InvoiceDate = e.InvoiceDate,
        CustomerName = e.Customer?.Name ?? "—",
                CustomerFileNumber = e.Customer?.FileNumber,
        CommissionType = e.CommissionType,
        BaseAmount = e.BaseAmount,
        CommissionAmount = e.CommissionAmount,
        PaidAmount = e.PaidAmount,
        UnpaidAmount = Math.Max(0, e.CommissionAmount - e.PaidAmount),
        Status = e.Status
    };

    private static SalesRepCommissionStatus ResolveStatus(decimal commission, decimal paid)
    {
        if (paid <= 0) return SalesRepCommissionStatus.Unpaid;
        if (paid + 0.001m >= commission) return SalesRepCommissionStatus.Paid;
        return SalesRepCommissionStatus.Partial;
    }

    private static async Task<(SalesRepCommissionType Type, decimal BaseAmount, decimal Commission)> ComputeCommissionAsync(
        AppDbContext db,
        Invoice invoice,
        List<SalesRepCommissionRule> rules,
        CancellationToken ct)
    {
        if (rules.Count == 0)
            return (SalesRepCommissionType.PercentOfSales, invoice.NetAmount, 0);

        // 1) قاعدة حسب العميل
        if (invoice.CustomerId is int customerId)
        {
            var customerRule = rules.FirstOrDefault(r =>
                r.CommissionType == SalesRepCommissionType.ByCustomer && r.CustomerId == customerId);
            if (customerRule is not null)
            {
                if (customerRule.FixedAmount > 0)
                    return (SalesRepCommissionType.ByCustomer, invoice.NetAmount, RoundMoney(customerRule.FixedAmount));
                var amount = invoice.NetAmount * customerRule.Percentage / 100m;
                return (SalesRepCommissionType.ByCustomer, invoice.NetAmount, RoundMoney(amount));
            }
        }

        // 2) قواعد حسب المنتج
        var productRules = rules.Where(r => r.CommissionType == SalesRepCommissionType.ByProduct && r.ProductId is not null).ToList();
        if (productRules.Count > 0)
        {
            decimal total = 0;
            decimal baseSum = 0;
            foreach (var item in invoice.Items.Where(i => !i.IsDeleted && i.ProductId is not null))
            {
                var rule = productRules.FirstOrDefault(r => r.ProductId == item.ProductId);
                if (rule is null) continue;
                baseSum += item.TotalPrice;
                if (rule.FixedAmount > 0)
                    total += rule.FixedAmount * item.Quantity;
                else
                    total += item.TotalPrice * rule.Percentage / 100m;
            }

            if (total > 0)
                return (SalesRepCommissionType.ByProduct, baseSum, RoundMoney(total));
        }

        // 3) مبلغ ثابت لكل فاتورة
        var fixedRule = rules.FirstOrDefault(r => r.CommissionType == SalesRepCommissionType.FixedPerInvoice);
        if (fixedRule is not null && fixedRule.FixedAmount > 0)
            return (SalesRepCommissionType.FixedPerInvoice, invoice.NetAmount, RoundMoney(fixedRule.FixedAmount));

        // 4) نسبة من صافي الربح
        var profitRule = rules.FirstOrDefault(r => r.CommissionType == SalesRepCommissionType.PercentOfNetProfit);
        if (profitRule is not null && profitRule.Percentage > 0)
        {
            var profit = await EstimateInvoiceProfitAsync(db, invoice, ct);
            var amount = Math.Max(0, profit) * profitRule.Percentage / 100m;
            return (SalesRepCommissionType.PercentOfNetProfit, profit, RoundMoney(amount));
        }

        // 5) نسبة من المبيعات
        var salesRule = rules.FirstOrDefault(r => r.CommissionType == SalesRepCommissionType.PercentOfSales);
        if (salesRule is not null && salesRule.Percentage > 0)
        {
            var amount = invoice.NetAmount * salesRule.Percentage / 100m;
            return (SalesRepCommissionType.PercentOfSales, invoice.NetAmount, RoundMoney(amount));
        }

        return (SalesRepCommissionType.PercentOfSales, invoice.NetAmount, 0);
    }

    private static async Task<decimal> EstimateInvoiceProfitAsync(AppDbContext db, Invoice invoice, CancellationToken ct)
    {
        var productIds = invoice.Items
            .Where(i => !i.IsDeleted && i.ProductId is not null)
            .Select(i => i.ProductId!.Value)
            .Distinct()
            .ToList();

        if (productIds.Count == 0)
            return invoice.NetAmount;

        var prices = await db.ProductPrices.AsNoTracking()
            .Where(p => productIds.Contains(p.ProductId) && !p.IsDeleted)
            .ToListAsync(ct);

        var costByProduct = prices
            .GroupBy(p => p.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).First().PurchasePrice);

        // آخر سعر شراء من فواتير المشتريات للمنتجات بلا سعر كتالوج
        var missing = productIds.Where(id => !costByProduct.ContainsKey(id) || costByProduct[id] <= 0).ToList();
        if (missing.Count > 0)
        {
            var purchaseItems = await db.InvoiceItems.AsNoTracking()
                .Include(i => i.Invoice)
                .Where(i => !i.IsDeleted
                            && i.ProductId != null
                            && missing.Contains(i.ProductId.Value)
                            && i.Invoice.InvoiceType == InvoiceType.Purchase
                            && !i.Invoice.IsDeleted)
                .OrderByDescending(i => i.Invoice.Date)
                .ToListAsync(ct);

            foreach (var pid in missing)
            {
                var last = purchaseItems.FirstOrDefault(i => i.ProductId == pid);
                if (last is not null)
                    costByProduct[pid] = last.UnitPrice;
            }
        }

        decimal costTotal = 0;
        foreach (var item in invoice.Items.Where(i => !i.IsDeleted))
        {
            if (item.ProductId is int pid && costByProduct.TryGetValue(pid, out var unitCost))
                costTotal += unitCost * item.Quantity;
        }

        return invoice.NetAmount - costTotal;
    }

    private static decimal RoundMoney(decimal value) => Math.Round(value, 0, MidpointRounding.AwayFromZero);
}
