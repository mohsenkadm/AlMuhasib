using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class PersonProfileService : IPersonProfileService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IReportService _reportService;

    public PersonProfileService(IDbContextFactory<AppDbContext> contextFactory, IReportService reportService)
    {
        _contextFactory = contextFactory;
        _reportService = reportService;
    }

    private static DateTime? EndOfDay(DateTime? to) => to?.Date.AddDays(1);

    public async Task<List<PersonLookupItem>> SearchPeopleAsync(string? searchText = null, PersonPartyType? typeFilter = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var items = new List<PersonLookupItem>();
        var term = searchText?.Trim();
        var hasTerm = !string.IsNullOrWhiteSpace(term);

        if (typeFilter is null or PersonPartyType.Customer)
        {
            var customers = context.Customers.AsQueryable();
            if (hasTerm)
            {
                customers = customers.Where(c =>
                    c.Name.Contains(term!) ||
                    (c.Phone != null && c.Phone.Contains(term!)) ||
                    (c.FileNumber != null && c.FileNumber.Contains(term!)));
            }

            items.AddRange(await customers
                .OrderBy(c => c.Name)
                .Select(c => new PersonLookupItem
                {
                    PartyType = PersonPartyType.Customer,
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone,
                    TypeLabel = "عميل",
                    DisplayText = c.Phone != null && c.Phone != "" ? $"{c.Name} — {c.Phone}" : c.Name
                })
                .ToListAsync());
        }

        if (typeFilter is null or PersonPartyType.Supplier)
        {
            var suppliers = context.Suppliers.AsQueryable();
            if (hasTerm)
            {
                suppliers = suppliers.Where(s =>
                    s.Name.Contains(term!) ||
                    (s.Phone != null && s.Phone.Contains(term!)));
            }

            items.AddRange(await suppliers
                .OrderBy(s => s.Name)
                .Select(s => new PersonLookupItem
                {
                    PartyType = PersonPartyType.Supplier,
                    Id = s.Id,
                    Name = s.Name,
                    Phone = s.Phone,
                    TypeLabel = "مورد",
                    DisplayText = s.Phone != null && s.Phone != "" ? $"{s.Name} — {s.Phone}" : s.Name
                })
                .ToListAsync());
        }

        if (typeFilter is null or PersonPartyType.Investor)
        {
            var investors = context.Investors.AsQueryable();
            if (hasTerm)
            {
                investors = investors.Where(i =>
                    i.Name.Contains(term!) ||
                    (i.Phone != null && i.Phone.Contains(term!)));
            }

            items.AddRange(await investors
                .OrderBy(i => i.Name)
                .Select(i => new PersonLookupItem
                {
                    PartyType = PersonPartyType.Investor,
                    Id = i.Id,
                    Name = i.Name,
                    Phone = i.Phone,
                    TypeLabel = "مستثمر",
                    DisplayText = i.Phone != null && i.Phone != "" ? $"{i.Name} — {i.Phone}" : i.Name
                })
                .ToListAsync());
        }

        return items
            .OrderBy(i => i.Name)
            .ThenBy(i => i.TypeLabel)
            .ToList();
    }

    public async Task<PersonProfileResult?> GetProfileAsync(PersonPartyType type, int id, DateTime? from = null, DateTime? to = null)
    {
        return type switch
        {
            PersonPartyType.Customer => await BuildCustomerProfileAsync(id, from, to),
            PersonPartyType.Supplier => await BuildSupplierProfileAsync(id, from, to),
            PersonPartyType.Investor => await BuildInvestorProfileAsync(id, from, to),
            _ => null
        };
    }

    private async Task<PersonProfileResult?> BuildCustomerProfileAsync(int id, DateTime? from, DateTime? to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var customer = await context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (customer is null) return null;

        var statement = await _reportService.GetCustomerStatementAsync(id, from, to);
        var result = new PersonProfileResult
        {
            PartyType = PersonPartyType.Customer,
            Id = customer.Id,
            Name = customer.Name,
            TypeLabel = "عميل",
            Phone = customer.Phone,
            Address = customer.Address,
            Notes = customer.Notes,
            FileNumber = customer.FileNumber,
            MaxCreditLimit = customer.MaxCreditLimit,
            MaxInstallmentDebt = customer.MaxInstallmentDebt,
            ReliabilityScore = customer.ReliabilityScore,
            GuarantorName = customer.GuarantorName,
            GuarantorPhone = customer.GuarantorPhone,
            TotalDebit = statement.TotalDebit,
            TotalCredit = statement.TotalCredit,
            Balance = statement.Balance,
            TransactionCount = statement.TransactionCount,
            Timeline = statement.Rows.Select(r => MapTimeline(r.Date, r.Description, r.Debit, r.Credit, r.RunningBalance)).ToList()
        };

        var invoices = await context.Invoices.AsNoTracking()
            .Where(i => i.CustomerId == id &&
                        (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment))
            .Where(i => !from.HasValue || i.Date >= from.Value)
            .Where(i => !to.HasValue || i.Date < EndOfDay(to))
            .OrderByDescending(i => i.Date)
            .ToListAsync();

        result.Sections.Add(new PersonProfileSection
        {
            Key = "invoices",
            Title = "الفواتير",
            Count = invoices.Count,
            Rows = invoices.Select(i => new PersonProfileDetailRow
            {
                Date = i.Date,
                Title = i.InvoiceNumber,
                Subtitle = $"{InvoiceTypeLabel(i.InvoiceType)} — {PaymentMethodLabel(i.PaymentMethod)}",
                AmountLabel = $"{i.NetAmount:N0} د.ع",
                Status = i.PaymentMethod == PaymentMethod.Credit
                    ? (i.IsCreditPaid ? "مسدد" : $"متبقي {i.RemainingAmount:N0}")
                    : PaymentMethodLabel(i.PaymentMethod)
            }).ToList()
        });

        var vouchers = await context.Vouchers.AsNoTracking()
            .Where(v => v.CustomerId == id &&
                        (v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt))
            .Where(v => !from.HasValue || v.Date >= from.Value)
            .Where(v => !to.HasValue || v.Date < EndOfDay(to))
            .OrderByDescending(v => v.Date)
            .ToListAsync();

        result.Sections.Add(new PersonProfileSection
        {
            Key = "vouchers",
            Title = "السندات",
            Count = vouchers.Count,
            Rows = vouchers.Select(v => new PersonProfileDetailRow
            {
                Date = v.Date,
                Title = v.VoucherNumber,
                Subtitle = VoucherTypeLabel(v.VoucherType),
                AmountLabel = $"{v.Amount:N0} د.ع",
                Status = string.IsNullOrWhiteSpace(v.Notes) ? "—" : v.Notes!
            }).ToList()
        });

        var installmentDetail = await _reportService.GetInstallmentDetailAsync(id);
        result.Sections.Add(new PersonProfileSection
        {
            Key = "installments",
            Title = "الأقساط",
            Count = installmentDetail.Rows.Count,
            Rows = installmentDetail.Rows.Select(r => new PersonProfileDetailRow
            {
                Date = r.DueDate,
                Title = $"قسط — خطة {r.PlanNumber}",
                Subtitle = r.PaymentDate.HasValue
                    ? $"دفع بتاريخ {r.PaymentDate:yyyy/MM/dd}"
                    : "لم يُدفع بعد",
                AmountLabel = $"{r.Amount:N0} د.ع",
                Status = r.Status
            }).ToList()
        });

        var attachments = await context.CustomerAttachments.AsNoTracking()
            .Where(a => a.CustomerId == id)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        result.Sections.Add(new PersonProfileSection
        {
            Key = "attachments",
            Title = "المرفقات",
            Count = attachments.Count,
            IsExpanded = attachments.Count > 0,
            Rows = attachments.Select(a => new PersonProfileDetailRow
            {
                Date = a.CreatedAt,
                Title = a.FileName,
                Subtitle = string.IsNullOrWhiteSpace(a.Description) ? a.FilePath : a.Description!,
                AmountLabel = "—",
                Status = "مرفق"
            }).ToList()
        });

        result.CustomerInsights = await BuildCustomerInsightsAsync(context, id, from, to, invoices, vouchers, installmentDetail);
        return result;
    }

    private async Task<CustomerProfileInsights> BuildCustomerInsightsAsync(
        AppDbContext context,
        int customerId,
        DateTime? from,
        DateTime? to,
        List<Invoice> invoices,
        List<Voucher> vouchers,
        InstallmentDetailResult installmentDetail)
    {
        var insights = new CustomerProfileInsights
        {
            InvoiceCount = invoices.Count,
            OutstandingBalance = invoices.Sum(i => i.RemainingAmount),
            FinancialTransactions = vouchers.Select(v => new CustomerFinancialTxnRow
            {
                Date = v.Date,
                VoucherNumber = v.VoucherNumber,
                VoucherType = VoucherTypeLabel(v.VoucherType),
                Amount = v.Amount,
                Notes = v.Notes
            }).ToList()
        };

        var invoiceIds = invoices.Select(i => i.Id).ToList();
        List<InvoiceItem> items;
        if (invoiceIds.Count == 0)
            items = [];
        else
            items = await context.InvoiceItems.AsNoTracking()
                .Include(ii => ii.Invoice)
                .Where(ii => invoiceIds.Contains(ii.InvoiceId))
                .ToListAsync();

        var productIds = items.Where(i => i.ProductId != null).Select(i => i.ProductId!.Value).Distinct().ToList();
        var stocks = productIds.Count > 0
            ? await context.WarehouseStocks.Where(ws => productIds.Contains(ws.ProductId)).ToListAsync()
            : [];
        var purchasesByProduct = productIds.Count > 0
            ? await ProductCostHelper.GetPurchaseItemsByProductAsync(context, productIds)
            : new Dictionary<int, List<InvoiceItem>>();
        var avgCostByProduct = productIds.ToDictionary(
            pid => pid,
            pid => ProductCostHelper.ComputeAverageUnitCostForProduct(
                purchasesByProduct.GetValueOrDefault(pid) ?? [], stocks, pid));

        decimal sales = 0, cost = 0;
        var monthly = new Dictionary<(int Y, int M), (decimal Sales, decimal Cost)>();
        foreach (var item in items)
        {
            var lineSales = item.TotalPrice;
            var unitCost = item.ProductId is int pid && avgCostByProduct.TryGetValue(pid, out var c) ? c : 0m;
            var lineCost = unitCost * item.Quantity;
            sales += lineSales;
            cost += lineCost;
            var d = item.Invoice?.Date ?? DateTime.Today;
            var key = (d.Year, d.Month);
            if (!monthly.TryGetValue(key, out var cur)) cur = (0, 0);
            monthly[key] = (cur.Sales + lineSales, cur.Cost + lineCost);
        }

        insights.SalesAmount = sales;
        insights.CostAmount = cost;
        insights.NetProfit = sales - cost;
        insights.MarginPercent = sales > 0 ? Math.Round((sales - cost) / sales * 100m, 2) : 0;
        insights.ProfitByMonth = monthly
            .OrderBy(kv => kv.Key.Y).ThenBy(kv => kv.Key.M)
            .Select(kv => new CustomerProfitMonthPoint
            {
                Label = $"{kv.Key.Y}/{kv.Key.M:00}",
                Sales = kv.Value.Sales,
                Cost = kv.Value.Cost,
                Profit = kv.Value.Sales - kv.Value.Cost
            })
            .ToList();

        insights.Products = items
            .GroupBy(i => new { i.ProductId, Name = string.IsNullOrWhiteSpace(i.ItemName) ? "—" : i.ItemName })
            .Select(g =>
            {
                var last = g.OrderByDescending(x => x.Invoice?.Date ?? DateTime.MinValue).First();
                return new CustomerProductPurchaseRow
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    DealCount = g.Count(),
                    TotalAmount = g.Sum(x => x.TotalPrice),
                    LastDate = last.Invoice?.Date,
                    LastUnitPrice = last.UnitPrice
                };
            })
            .OrderByDescending(p => p.TotalQuantity)
            .ThenBy(p => p.ProductName)
            .ToList();

        var asOf = to?.Date ?? DateTime.Today;
        var aging = await _reportService.GetReceivablesAgingReportAsync(asOf, customerId);
        insights.AgingBuckets = aging.Buckets.Select(b => new CustomerAgingBucketRow
        {
            BucketName = b.BucketName,
            Amount = b.Amount,
            Count = b.Count
        }).ToList();
        insights.AgingDetails = aging.Rows.Select(r => new CustomerAgingDetailRow
        {
            SourceType = r.SourceType,
            Reference = $"#{r.ReferenceId}",
            DueDate = r.DueDate,
            RemainingAmount = r.RemainingAmount,
            DaysOverdue = r.DaysOverdue,
            AgingBucket = r.AgingBucket
        }).ToList();

        foreach (var inv in invoices.Where(i => i.RemainingAmount > 0))
        {
            insights.DueItems.Add(new CustomerDueItemRow
            {
                Kind = "فاتورة",
                Title = inv.InvoiceNumber,
                Subtitle = InvoiceTypeLabel(inv.InvoiceType),
                DueDate = inv.CreditDueDate ?? inv.Date,
                RemainingAmount = inv.RemainingAmount,
                Status = inv.IsCreditPaid ? "مسدد" : "مستحق",
                InvoiceId = inv.Id
            });
        }

        foreach (var r in installmentDetail.Rows.Where(x => x.RemainingAmount > 0))
        {
            insights.DueItems.Add(new CustomerDueItemRow
            {
                Kind = "قسط",
                Title = $"خطة {r.PlanNumber}",
                Subtitle = r.Status,
                DueDate = r.DueDate,
                RemainingAmount = r.RemainingAmount,
                Status = r.Status
            });
        }

        insights.DueItems = insights.DueItems
            .OrderBy(d => d.DueDate ?? DateTime.MaxValue)
            .ToList();

        return insights;
    }

    private async Task<PersonProfileResult?> BuildSupplierProfileAsync(int id, DateTime? from, DateTime? to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var supplier = await context.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (supplier is null) return null;

        var statement = await _reportService.GetSupplierStatementAsync(id, from, to);
        var result = new PersonProfileResult
        {
            PartyType = PersonPartyType.Supplier,
            Id = supplier.Id,
            Name = supplier.Name,
            TypeLabel = "مورد",
            Phone = supplier.Phone,
            Address = supplier.Address,
            Notes = supplier.Notes,
            TotalDebit = statement.TotalDebit,
            TotalCredit = statement.TotalCredit,
            Balance = statement.Balance,
            TransactionCount = statement.Rows.Count,
            Timeline = statement.Rows.Select(r => MapTimeline(r.Date, r.Description, r.Debit, r.Credit, r.RunningBalance)).ToList()
        };

        var invoices = await context.Invoices.AsNoTracking()
            .Where(i => i.SupplierId == id &&
                        (i.InvoiceType == InvoiceType.Purchase || i.InvoiceType == InvoiceType.PurchaseReturn))
            .Where(i => !from.HasValue || i.Date >= from.Value)
            .Where(i => !to.HasValue || i.Date < EndOfDay(to))
            .OrderByDescending(i => i.Date)
            .ToListAsync();

        result.Sections.Add(new PersonProfileSection
        {
            Key = "invoices",
            Title = "فواتير المشتريات",
            Count = invoices.Count,
            Rows = invoices.Select(i => new PersonProfileDetailRow
            {
                Date = i.Date,
                Title = i.InvoiceNumber,
                Subtitle = $"{InvoiceTypeLabel(i.InvoiceType)} — {PaymentMethodLabel(i.PaymentMethod)}",
                AmountLabel = $"{i.NetAmount:N0} د.ع",
                Status = i.PaymentMethod == PaymentMethod.Credit
                    ? (i.IsCreditPaid ? "مسدد" : $"متبقي {i.RemainingAmount:N0}")
                    : PaymentMethodLabel(i.PaymentMethod)
            }).ToList()
        });

        // Payment vouchers for suppliers are stored with CustomerId = supplierId (existing convention).
        var vouchers = await context.Vouchers.AsNoTracking()
            .Where(v => v.CustomerId == id && v.VoucherType == VoucherType.Payment)
            .Where(v => !from.HasValue || v.Date >= from.Value)
            .Where(v => !to.HasValue || v.Date < EndOfDay(to))
            .OrderByDescending(v => v.Date)
            .ToListAsync();

        result.Sections.Add(new PersonProfileSection
        {
            Key = "vouchers",
            Title = "سندات الصرف",
            Count = vouchers.Count,
            Rows = vouchers.Select(v => new PersonProfileDetailRow
            {
                Date = v.Date,
                Title = v.VoucherNumber,
                Subtitle = VoucherTypeLabel(v.VoucherType),
                AmountLabel = $"{v.Amount:N0} د.ع",
                Status = string.IsNullOrWhiteSpace(v.Notes) ? "—" : v.Notes!
            }).ToList()
        });

        return result;
    }

    private async Task<PersonProfileResult?> BuildInvestorProfileAsync(int id, DateTime? from, DateTime? to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var investor = await context.Investors.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        if (investor is null) return null;

        var statement = await _reportService.GetInvestorStatementAsync(id, from, to);
        var result = new PersonProfileResult
        {
            PartyType = PersonPartyType.Investor,
            Id = investor.Id,
            Name = investor.Name,
            TypeLabel = "مستثمر",
            Phone = investor.Phone,
            TotalDeposit = investor.TotalDeposit,
            OpeningBalance = investor.OpeningBalance,
            ProfitPercentage = investor.ProfitPercentage,
            TotalDebit = statement.TotalDebit,
            TotalCredit = statement.TotalCredit,
            Balance = statement.Balance,
            TransactionCount = statement.TransactionCount,
            Timeline = statement.Rows.Select(r => MapTimeline(r.Date, r.Description, r.Debit, r.Credit, r.RunningBalance)).ToList()
        };

        var transactions = await context.InvestorTransactions.AsNoTracking()
            .Where(t => t.InvestorId == id)
            .Where(t => !from.HasValue || t.Date >= from.Value)
            .Where(t => !to.HasValue || t.Date < EndOfDay(to))
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        result.Sections.Add(new PersonProfileSection
        {
            Key = "transactions",
            Title = "الإيداعات والسحوبات",
            Count = transactions.Count,
            Rows = transactions.Select(t => new PersonProfileDetailRow
            {
                Date = t.Date,
                Title = t.Type == InvestorTransactionType.Deposit ? "إيداع" : "سحب",
                Subtitle = string.IsNullOrWhiteSpace(t.Notes) ? "—" : t.Notes!,
                AmountLabel = $"{t.Amount:N0} د.ع",
                Status = t.Type == InvestorTransactionType.Deposit ? "دائن" : "مدين"
            }).ToList()
        });

        var profits = await context.ProfitDistributionDetails.AsNoTracking()
            .Include(d => d.ProfitDistribution)
            .Where(d => d.InvestorId == id)
            .Where(d => !from.HasValue || d.ProfitDistribution.Date >= from.Value)
            .Where(d => !to.HasValue || d.ProfitDistribution.Date < EndOfDay(to))
            .OrderByDescending(d => d.ProfitDistribution.Date)
            .ToListAsync();

        result.Sections.Add(new PersonProfileSection
        {
            Key = "profits",
            Title = "توزيعات الأرباح",
            Count = profits.Count,
            Rows = profits.Select(d => new PersonProfileDetailRow
            {
                Date = d.ProfitDistribution.Date,
                Title = "توزيع أرباح",
                Subtitle = "—",
                AmountLabel = $"{d.Amount:N0} د.ع",
                Status = "دائن"
            }).ToList()
        });

        var vouchers = await context.Vouchers.AsNoTracking()
            .Where(v => v.InvestorId == id)
            .Where(v => !from.HasValue || v.Date >= from.Value)
            .Where(v => !to.HasValue || v.Date < EndOfDay(to))
            .OrderByDescending(v => v.Date)
            .ToListAsync();

        result.Sections.Add(new PersonProfileSection
        {
            Key = "vouchers",
            Title = "سندات المستثمر",
            Count = vouchers.Count,
            IsExpanded = vouchers.Count > 0,
            Rows = vouchers.Select(v => new PersonProfileDetailRow
            {
                Date = v.Date,
                Title = v.VoucherNumber,
                Subtitle = VoucherTypeLabel(v.VoucherType),
                AmountLabel = $"{v.Amount:N0} د.ع",
                Status = string.IsNullOrWhiteSpace(v.Notes) ? "—" : v.Notes!
            }).ToList()
        });

        return result;
    }

    private static PersonTimelineItem MapTimeline(DateTime date, string description, decimal debit, decimal credit, decimal runningBalance)
    {
        var (category, label) = ClassifyTimeline(description);
        return new PersonTimelineItem
        {
            Date = date,
            Category = category,
            CategoryLabel = label,
            Description = description,
            Debit = debit,
            Credit = credit,
            RunningBalance = runningBalance
        };
    }

    private static (PersonTimelineCategory Category, string Label) ClassifyTimeline(string description)
    {
        if (description.Contains("فاتورة", StringComparison.Ordinal))
            return (PersonTimelineCategory.Invoice, "فاتورة");
        if (description.Contains("سند", StringComparison.Ordinal))
            return (PersonTimelineCategory.Voucher, "سند");
        if (description.Contains("قسط", StringComparison.Ordinal))
            return (PersonTimelineCategory.InstallmentPayment, "قسط");
        if (description.Contains("رصيد افتتاحي", StringComparison.Ordinal))
            return (PersonTimelineCategory.OpeningBalance, "افتتاحي");
        if (description.Contains("إيداع", StringComparison.Ordinal))
            return (PersonTimelineCategory.Deposit, "إيداع");
        if (description.Contains("سحب", StringComparison.Ordinal))
            return (PersonTimelineCategory.Withdrawal, "سحب");
        if (description.Contains("أرباح", StringComparison.Ordinal))
            return (PersonTimelineCategory.ProfitDistribution, "أرباح");
        return (PersonTimelineCategory.Other, "حركة");
    }

    private static string InvoiceTypeLabel(InvoiceType type) => type switch
    {
        InvoiceType.Sale => "مبيعات",
        InvoiceType.Purchase => "مشتريات",
        InvoiceType.Installment => "أقساط",
        InvoiceType.PurchaseReturn => "مرتجع مشتريات",
        _ => type.ToString()
    };

    private static string PaymentMethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "نقدي",
        PaymentMethod.Credit => "آجل",
        PaymentMethod.Installment => "أقساط",
        _ => method.ToString()
    };

    private static string VoucherTypeLabel(VoucherType type) => type switch
    {
        VoucherType.Receipt => "سند قبض",
        VoucherType.Payment => "سند صرف",
        VoucherType.BankReceipt => "سند قبض بنكي",
        VoucherType.InvestorDeposit => "إيداع مستثمر",
        VoucherType.InvestorWithdrawal => "سحب مستثمر",
        VoucherType.DebtReceipt => "سند تسديد دين",
        _ => type.ToString()
    };
}
