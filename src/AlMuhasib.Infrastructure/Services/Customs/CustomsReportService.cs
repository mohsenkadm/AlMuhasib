using AlMuhasib.Core.Constants;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services.Customs;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.Infrastructure.Data.Customs;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Customs;

public sealed class CustomsReportService : ICustomsReportService
{
    private readonly IDbContextFactory<CustomsDbContext> _contextFactory;
    private readonly CustomsPermissionGuard _permissions;

    public CustomsReportService(
        IDbContextFactory<CustomsDbContext> contextFactory,
        CustomsPermissionGuard permissions)
    {
        _contextFactory = contextFactory;
        _permissions = permissions;
    }

    public async Task<IReadOnlyList<CustomsReportCustomerOption>> GetCustomerOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_permissions.CanView(CustomsScreenNames.Reports)
            && !_permissions.CanViewProfit)
        {
            _permissions.EnsureView(CustomsScreenNames.Reports);
        }

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.CustomsCustomers.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CustomsReportCustomerOption { Id = c.Id, Name = c.Name, Phone = c.Phone })
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomsDailyReportResult> GetDailyReportAsync(
        DateTime day,
        int? customerId = null,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Reports);
        var from = day.Date;
        var toEx = from.AddDays(1);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var invoices = await db.CustomsInvoices.AsNoTracking()
            .Where(i => i.Status != CustomsInvoiceStatus.Cancelled
                        && i.InvoiceDate >= from && i.InvoiceDate < toEx
                        && (!customerId.HasValue || i.CustomerId == customerId.Value))
            .Select(i => new
            {
                i.InvoiceNumber,
                i.InvoiceDate,
                i.Currency,
                i.TotalAmount,
                i.Description,
                CustomerName = i.Customer.Name
            })
            .ToListAsync(cancellationToken);

        var receipts = await db.CustomsReceipts.AsNoTracking()
            .Where(r => r.ReceiptDate >= from && r.ReceiptDate < toEx
                        && (!customerId.HasValue || r.CustomerId == customerId.Value))
            .Select(r => new
            {
                r.ReceiptNumber,
                r.ReceiptDate,
                r.Currency,
                r.Amount,
                r.Notes,
                CustomerName = r.Customer.Name
            })
            .ToListAsync(cancellationToken);

        var expenses = await db.CustomsExpenses.AsNoTracking()
            .Where(e => e.ExpenseDate >= from && e.ExpenseDate < toEx
                        && e.Invoice.Status != CustomsInvoiceStatus.Cancelled
                        && (!customerId.HasValue || e.Invoice.CustomerId == customerId.Value))
            .Select(e => new
            {
                e.ExpenseDate,
                e.Currency,
                e.Amount,
                e.Description,
                TypeName = e.ExpenseType.Name,
                InvoiceNumber = e.Invoice.InvoiceNumber,
                CustomerName = e.Invoice.Customer.Name
            })
            .ToListAsync(cancellationToken);

        var vehicleCount = await db.CustomsVehicles.AsNoTracking()
            .CountAsync(v => v.Invoice.Status != CustomsInvoiceStatus.Cancelled
                             && v.Invoice.InvoiceDate >= from && v.Invoice.InvoiceDate < toEx
                             && (!customerId.HasValue || v.Invoice.CustomerId == customerId.Value),
                cancellationToken);

        var rows = new List<CustomsDailyReportRow>();
        rows.AddRange(invoices.Select(i => new CustomsDailyReportRow
        {
            Date = i.InvoiceDate,
            Kind = "فاتورة",
            Reference = i.InvoiceNumber,
            CustomerName = i.CustomerName,
            CurrencyLabel = CurrencyLabel(i.Currency),
            Amount = i.TotalAmount,
            Description = i.Description
        }));
        rows.AddRange(receipts.Select(r => new CustomsDailyReportRow
        {
            Date = r.ReceiptDate,
            Kind = "قبض",
            Reference = r.ReceiptNumber,
            CustomerName = r.CustomerName,
            CurrencyLabel = CurrencyLabel(r.Currency),
            Amount = r.Amount,
            Description = r.Notes
        }));
        if (_permissions.CanViewExpenses)
        {
            rows.AddRange(expenses.Select(e => new CustomsDailyReportRow
            {
                Date = e.ExpenseDate,
                Kind = "مصروف",
                Reference = e.InvoiceNumber,
                CustomerName = e.CustomerName,
                CurrencyLabel = CurrencyLabel(e.Currency),
                Amount = e.Amount,
                Description = string.IsNullOrWhiteSpace(e.Description) ? e.TypeName : e.Description
            }));
        }

        var showExpenses = _permissions.CanViewExpenses;
        return new CustomsDailyReportResult
        {
            InvoiceCount = invoices.Count,
            InvoicesIqd = invoices.Where(i => i.Currency == AccountingCurrency.IQD).Sum(i => i.TotalAmount),
            InvoicesUsd = invoices.Where(i => i.Currency == AccountingCurrency.USD).Sum(i => i.TotalAmount),
            ReceiptsIqd = receipts.Where(r => r.Currency == AccountingCurrency.IQD).Sum(r => r.Amount),
            ReceiptsUsd = receipts.Where(r => r.Currency == AccountingCurrency.USD).Sum(r => r.Amount),
            ExpensesIqd = showExpenses ? expenses.Where(e => e.Currency == AccountingCurrency.IQD).Sum(e => e.Amount) : 0,
            ExpensesUsd = showExpenses ? expenses.Where(e => e.Currency == AccountingCurrency.USD).Sum(e => e.Amount) : 0,
            VehicleCount = vehicleCount,
            ByTypeChart =
            [
                new() { Name = "فواتير", Amount = invoices.Count },
                new() { Name = "قبض", Amount = receipts.Count },
                new() { Name = "مصاريف", Amount = showExpenses ? expenses.Count : 0 },
                new() { Name = "سيارات", Amount = vehicleCount }
            ],
            Rows = rows.OrderBy(r => r.Date).ThenBy(r => r.Kind).ToList()
        };
    }

    public async Task<CustomsMonthlyReportResult> GetMonthlyReportAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Reports);
        var from = fromDate.Date;
        var toEx = toDate.Date.AddDays(1);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var invoices = await db.CustomsInvoices.AsNoTracking()
            .Where(i => i.Status != CustomsInvoiceStatus.Cancelled
                        && i.InvoiceDate >= from && i.InvoiceDate < toEx)
            .Select(i => new { i.Id, i.InvoiceDate, i.Currency, i.TotalAmount })
            .ToListAsync(cancellationToken);

        var receipts = await db.CustomsReceipts.AsNoTracking()
            .Where(r => r.ReceiptDate >= from && r.ReceiptDate < toEx)
            .Select(r => new { r.ReceiptDate, r.Currency, r.Amount })
            .ToListAsync(cancellationToken);

        var invoiceIds = invoices.Select(i => i.Id).ToList();
        var expenses = invoiceIds.Count == 0
            ? []
            : await db.CustomsExpenses.AsNoTracking()
                .Where(e => invoiceIds.Contains(e.InvoiceId))
                .Select(e => new { e.InvoiceId, e.ExpenseDate, e.Currency, e.Amount })
                .ToListAsync(cancellationToken);

        // Per-invoice same-currency internal profit for daily rollup.
        var expenseByInvoice = expenses
            .GroupBy(e => e.InvoiceId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var dayRows = new Dictionary<DateTime, CustomsMonthlyReportRow>();
        void Ensure(DateTime d)
        {
            var key = d.Date;
            if (!dayRows.ContainsKey(key))
                dayRows[key] = new CustomsMonthlyReportRow { Date = key };
        }

        foreach (var i in invoices)
        {
            Ensure(i.InvoiceDate);
            var row = dayRows[i.InvoiceDate.Date];
            row.InvoiceCount++;
            if (i.Currency == AccountingCurrency.IQD) row.InvoicesIqd += i.TotalAmount;
            else row.InvoicesUsd += i.TotalAmount;

            var sameCurrencyExpense = 0m;
            if (expenseByInvoice.TryGetValue(i.Id, out var invExpenses))
                sameCurrencyExpense = invExpenses.Where(e => e.Currency == i.Currency).Sum(e => e.Amount);

            if (i.Currency == AccountingCurrency.IQD)
            {
                row.ExpensesIqd += sameCurrencyExpense;
                row.ProfitIqd += i.TotalAmount - sameCurrencyExpense;
            }
            else
            {
                row.ExpensesUsd += sameCurrencyExpense;
                row.ProfitUsd += i.TotalAmount - sameCurrencyExpense;
            }
        }

        foreach (var r in receipts)
        {
            Ensure(r.ReceiptDate);
            var row = dayRows[r.ReceiptDate.Date];
            if (r.Currency == AccountingCurrency.IQD) row.ReceiptsIqd += r.Amount;
            else row.ReceiptsUsd += r.Amount;
        }

        var ordered = dayRows.Values.OrderBy(r => r.Date).ToList();
        var showExpenses = _permissions.CanViewExpenses;
        var showProfit = _permissions.CanViewProfit;
        if (!showExpenses || !showProfit)
        {
            foreach (var row in ordered)
            {
                if (!showExpenses)
                {
                    row.ExpensesIqd = 0;
                    row.ExpensesUsd = 0;
                }

                if (!showProfit)
                {
                    row.ProfitIqd = 0;
                    row.ProfitUsd = 0;
                }
                else if (!showExpenses)
                {
                    // Profit without expense visibility must not leak expense-derived figures.
                    row.ProfitIqd = 0;
                    row.ProfitUsd = 0;
                }
            }
        }

        return new CustomsMonthlyReportResult
        {
            InvoicesIqd = ordered.Sum(r => r.InvoicesIqd),
            InvoicesUsd = ordered.Sum(r => r.InvoicesUsd),
            ReceiptsIqd = ordered.Sum(r => r.ReceiptsIqd),
            ReceiptsUsd = ordered.Sum(r => r.ReceiptsUsd),
            ExpensesIqd = showExpenses ? ordered.Sum(r => r.ExpensesIqd) : 0,
            ExpensesUsd = showExpenses ? ordered.Sum(r => r.ExpensesUsd) : 0,
            ProfitIqd = showProfit && showExpenses ? ordered.Sum(r => r.ProfitIqd) : 0,
            ProfitUsd = showProfit && showExpenses ? ordered.Sum(r => r.ProfitUsd) : 0,
            // Rule 2: single-currency chart series (IQD) — never InvoicesIqd + InvoicesUsd.
            DailyChart = ordered.Select(r => new CustomsReportNameAmountPoint
            {
                Name = r.Date.ToString("MM/dd"),
                Amount = r.InvoicesIqd
            }).ToList(),
            Rows = ordered
        };
    }

    public async Task<CustomsCustomerReportResult> GetCustomerReportAsync(
        DateTime fromDate,
        DateTime toDate,
        int? customerId = null,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Reports);
        var from = fromDate.Date;
        var toEx = toDate.Date.AddDays(1);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var customers = await db.CustomsCustomers.AsNoTracking()
            .Where(c => c.IsActive && (!customerId.HasValue || c.Id == customerId.Value))
            .Select(c => new { c.Id, c.Name, c.Phone, c.BalanceIqd, c.BalanceUsd })
            .ToListAsync(cancellationToken);

        var invoices = await db.CustomsInvoices.AsNoTracking()
            .Where(i => i.Status != CustomsInvoiceStatus.Cancelled
                        && i.InvoiceDate >= from && i.InvoiceDate < toEx
                        && (!customerId.HasValue || i.CustomerId == customerId.Value))
            .Select(i => new
            {
                i.CustomerId,
                i.Currency,
                i.TotalAmount,
                i.PaidAmount,
                i.RemainingAmount
            })
            .ToListAsync(cancellationToken);

        var byCustomer = invoices.GroupBy(i => i.CustomerId).ToDictionary(g => g.Key, g => g.ToList());

        var rows = customers.Select(c =>
        {
            byCustomer.TryGetValue(c.Id, out var inv);
            inv ??= [];
            return new CustomsCustomerReportRow
            {
                CustomerId = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                BalanceIqd = c.BalanceIqd,
                BalanceUsd = c.BalanceUsd,
                InvoiceCount = inv.Count,
                RevenueIqd = inv.Where(i => i.Currency == AccountingCurrency.IQD).Sum(i => i.TotalAmount),
                RevenueUsd = inv.Where(i => i.Currency == AccountingCurrency.USD).Sum(i => i.TotalAmount),
                PaidIqd = inv.Where(i => i.Currency == AccountingCurrency.IQD).Sum(i => i.PaidAmount),
                PaidUsd = inv.Where(i => i.Currency == AccountingCurrency.USD).Sum(i => i.PaidAmount),
                RemainingIqd = inv.Where(i => i.Currency == AccountingCurrency.IQD).Sum(i => i.RemainingAmount),
                RemainingUsd = inv.Where(i => i.Currency == AccountingCurrency.USD).Sum(i => i.RemainingAmount)
            };
        })
        .OrderByDescending(r => r.RemainingIqd)
        .ThenByDescending(r => r.RemainingUsd)
        .ThenBy(r => r.Name)
        .ToList();

        return new CustomsCustomerReportResult
        {
            CustomerCount = rows.Count,
            BalanceIqd = rows.Sum(r => r.BalanceIqd),
            BalanceUsd = rows.Sum(r => r.BalanceUsd),
            InvoiceCount = rows.Sum(r => r.InvoiceCount),
            // Rule 2: chart Amount is IQD remaining only (USD kept in dual KPI columns).
            TopCustomersChart = rows
                .OrderByDescending(r => r.RemainingIqd)
                .Take(8)
                .Select(r => new CustomsReportNameAmountPoint
                {
                    Name = r.Name,
                    Amount = r.RemainingIqd
                })
                .ToList(),
            Rows = rows
        };
    }

    public async Task<CustomsInvoiceReportResult> GetInvoiceReportAsync(
        DateTime fromDate,
        DateTime toDate,
        int? customerId = null,
        CustomsInvoiceStatus? status = null,
        AccountingCurrency? currency = null,
        CustomsServiceType? serviceType = null,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Reports);
        var from = fromDate.Date;
        var toEx = toDate.Date.AddDays(1);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = db.CustomsInvoices.AsNoTracking()
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate < toEx);

        if (customerId.HasValue)
            query = query.Where(i => i.CustomerId == customerId.Value);
        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);
        else
            query = query.Where(i => i.Status != CustomsInvoiceStatus.Cancelled);
        if (currency.HasValue)
            query = query.Where(i => i.Currency == currency.Value);
        if (serviceType.HasValue)
            query = query.Where(i => i.ServiceType == serviceType.Value);

        var data = await query
            .Select(i => new
            {
                i.InvoiceNumber,
                i.InvoiceDate,
                CustomerName = i.Customer.Name,
                i.Status,
                i.ServiceType,
                i.Currency,
                i.TotalAmount,
                i.PaidAmount,
                i.RemainingAmount
            })
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);

        var rows = data.Select(i => new CustomsInvoiceReportRow
        {
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            CustomerName = i.CustomerName,
            StatusLabel = CustomsInvoiceStatusDisplay.ToArabic(i.Status),
            ServiceTypeLabel = CustomsServiceTypeDisplay.ToArabic(i.ServiceType),
            CurrencyLabel = CurrencyLabel(i.Currency),
            TotalAmount = i.TotalAmount,
            PaidAmount = i.PaidAmount,
            RemainingAmount = i.RemainingAmount
        }).ToList();

        return new CustomsInvoiceReportResult
        {
            Count = rows.Count,
            TotalIqd = data.Where(i => i.Currency == AccountingCurrency.IQD).Sum(i => i.TotalAmount),
            TotalUsd = data.Where(i => i.Currency == AccountingCurrency.USD).Sum(i => i.TotalAmount),
            PaidIqd = data.Where(i => i.Currency == AccountingCurrency.IQD).Sum(i => i.PaidAmount),
            PaidUsd = data.Where(i => i.Currency == AccountingCurrency.USD).Sum(i => i.PaidAmount),
            RemainingIqd = data.Where(i => i.Currency == AccountingCurrency.IQD).Sum(i => i.RemainingAmount),
            RemainingUsd = data.Where(i => i.Currency == AccountingCurrency.USD).Sum(i => i.RemainingAmount),
            StatusChart = data
                .GroupBy(i => CustomsInvoiceStatusDisplay.ToArabic(i.Status))
                .Select(g => new CustomsReportNameAmountPoint { Name = g.Key, Amount = g.Count() })
                .ToList(),
            Rows = rows
        };
    }

    public async Task<CustomsVehiclesReportResult> GetVehiclesReportAsync(
        DateTime fromDate,
        DateTime toDate,
        int? customerId = null,
        CustomsVehicleStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Reports);
        var from = fromDate.Date;
        var toEx = toDate.Date.AddDays(1);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = db.CustomsVehicles.AsNoTracking()
            .Where(v => v.Invoice.Status != CustomsInvoiceStatus.Cancelled
                        && v.Invoice.InvoiceDate >= from && v.Invoice.InvoiceDate < toEx);

        if (customerId.HasValue)
            query = query.Where(v => v.Invoice.CustomerId == customerId.Value);
        if (status.HasValue)
            query = query.Where(v => v.Status == status.Value);

        var data = await query
            .Select(v => new
            {
                v.IraqiPlate,
                v.SyrianPlate,
                v.SerialNumber,
                v.Status,
                v.LoadingDate,
                v.ExitDate,
                InvoiceNumber = v.Invoice.InvoiceNumber,
                CustomerName = v.Invoice.Customer.Name,
                v.Currency,
                v.VehiclePrice
            })
            .OrderByDescending(v => v.LoadingDate)
            .ToListAsync(cancellationToken);

        var inProgress = data.Count(v => v.Status == CustomsVehicleStatus.InProgress);
        var exited = data.Count(v => v.Status == CustomsVehicleStatus.Exited);
        var arrived = data.Count(v => v.Status == CustomsVehicleStatus.Arrived);

        return new CustomsVehiclesReportResult
        {
            Total = data.Count,
            InProgress = inProgress,
            Exited = exited,
            Arrived = arrived,
            StatusChart =
            [
                new() { Name = "قيد الإجراء", Amount = inProgress },
                new() { Name = "خارجة", Amount = exited },
                new() { Name = "واصلة", Amount = arrived }
            ],
            Rows = data.Select(v => new CustomsVehiclesReportRow
            {
                IraqiPlate = v.IraqiPlate,
                SyrianPlate = v.SyrianPlate,
                SerialNumber = v.SerialNumber,
                StatusLabel = CustomsVehicleStatusDisplay.ToArabic(v.Status),
                LoadingDate = v.LoadingDate,
                ExitDate = v.ExitDate,
                InvoiceNumber = v.InvoiceNumber,
                CustomerName = v.CustomerName,
                CurrencyLabel = CurrencyLabel(v.Currency),
                VehiclePrice = v.VehiclePrice
            }).ToList()
        };
    }

    public async Task<CustomsDebtsReportResult> GetDebtsReportAsync(
        DateTime asOfDate,
        int? customerId = null,
        AccountingCurrency? currency = null,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Reports);
        var asOf = asOfDate.Date;
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = db.CustomsInvoices.AsNoTracking()
            .Where(i => i.Status == CustomsInvoiceStatus.Approved
                        && i.RemainingAmount > 0
                        && i.InvoiceDate <= asOf);

        if (customerId.HasValue)
            query = query.Where(i => i.CustomerId == customerId.Value);
        if (currency.HasValue)
            query = query.Where(i => i.Currency == currency.Value);

        var data = await query
            .Select(i => new
            {
                i.InvoiceNumber,
                i.InvoiceDate,
                CustomerName = i.Customer.Name,
                i.Currency,
                i.RemainingAmount
            })
            .OrderByDescending(i => i.RemainingAmount)
            .ToListAsync(cancellationToken);

        static string Bucket(int days) => days switch
        {
            <= 30 => "0–30",
            <= 60 => "31–60",
            <= 90 => "61–90",
            _ => "90+"
        };

        var rows = data.Select(i =>
        {
            var days = Math.Max(0, (asOf - i.InvoiceDate.Date).Days);
            return new CustomsDebtsReportRow
            {
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                CustomerName = i.CustomerName,
                CurrencyLabel = CurrencyLabel(i.Currency),
                RemainingAmount = i.RemainingAmount,
                DaysOverdue = days,
                AgingBucket = Bucket(days)
            };
        }).ToList();

        return new CustomsDebtsReportResult
        {
            DebtsIqd = data.Where(i => i.Currency == AccountingCurrency.IQD).Sum(i => i.RemainingAmount),
            DebtsUsd = data.Where(i => i.Currency == AccountingCurrency.USD).Sum(i => i.RemainingAmount),
            UnpaidCount = rows.Count,
            // Rule 2: aging chart uses one currency scope (filter currency, else IQD only).
            AgingChart = data
                .Where(i => currency.HasValue
                    ? i.Currency == currency.Value
                    : i.Currency == AccountingCurrency.IQD)
                .GroupBy(i =>
                {
                    var days = Math.Max(0, (asOf - i.InvoiceDate.Date).Days);
                    return Bucket(days);
                })
                .OrderBy(g => g.Key)
                .Select(g => new CustomsReportNameAmountPoint
                {
                    Name = g.Key,
                    Amount = g.Sum(x => x.RemainingAmount)
                })
                .ToList(),
            Rows = rows
        };
    }

    public async Task<CustomsProfitReportResult> GetProfitReportAsync(
        DateTime fromDate,
        DateTime toDate,
        int? customerId = null,
        AccountingCurrency? currency = null,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.ProfitReport);
        var from = fromDate.Date;
        var toEx = toDate.Date.AddDays(1);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = db.CustomsInvoices.AsNoTracking()
            .Where(i => i.Status != CustomsInvoiceStatus.Cancelled
                        && i.InvoiceDate >= from && i.InvoiceDate < toEx);

        if (customerId.HasValue)
            query = query.Where(i => i.CustomerId == customerId.Value);
        if (currency.HasValue)
            query = query.Where(i => i.Currency == currency.Value);

        var invoices = await query
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.InvoiceDate,
                CustomerName = i.Customer.Name,
                i.Currency,
                i.TotalAmount
            })
            .ToListAsync(cancellationToken);

        var ids = invoices.Select(i => i.Id).ToList();
        var expenses = ids.Count == 0
            ? []
            : await db.CustomsExpenses.AsNoTracking()
                .Where(e => ids.Contains(e.InvoiceId))
                .Select(e => new { e.InvoiceId, e.Currency, e.Amount })
                .ToListAsync(cancellationToken);

        var expenseMap = expenses.GroupBy(e => e.InvoiceId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = invoices.Select(i =>
        {
            expenseMap.TryGetValue(i.Id, out var invExp);
            var expenseTuples = (invExp ?? []).Select(e => (e.Currency, e.Amount));
            var profit = CustomsFinancialRules.ComputeInternalProfit(
                i.TotalAmount, i.Currency, expenseTuples, out var sameCurrency);
            return new CustomsProfitReportRow
            {
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                CustomerName = i.CustomerName,
                CurrencyLabel = CurrencyLabel(i.Currency),
                Revenue = i.TotalAmount,
                Expenses = sameCurrency,
                Profit = profit
            };
        })
        .OrderByDescending(r => r.InvoiceDate)
        .ToList();

        var revenueIqd = invoices.Where(i => i.Currency == AccountingCurrency.IQD).Sum(i => i.TotalAmount);
        var revenueUsd = invoices.Where(i => i.Currency == AccountingCurrency.USD).Sum(i => i.TotalAmount);
        var expensesIqd = rows.Where(r => r.CurrencyLabel == "د.ع").Sum(r => r.Expenses);
        var expensesUsd = rows.Where(r => r.CurrencyLabel == "$").Sum(r => r.Expenses);

        // Rule 2: monthly chart Amount is one currency (filter, else IQD profit only).
        var chartCurrency = currency ?? AccountingCurrency.IQD;
        var monthlyChart = invoices
            .Where(i => i.Currency == chartCurrency)
            .GroupBy(i => new DateTime(i.InvoiceDate.Year, i.InvoiceDate.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var profit = g.Sum(i =>
                {
                    expenseMap.TryGetValue(i.Id, out var invExp);
                    return CustomsFinancialRules.ComputeInternalProfit(
                        i.TotalAmount,
                        i.Currency,
                        (invExp ?? []).Select(e => (e.Currency, e.Amount)));
                });
                return new CustomsReportNameAmountPoint
                {
                    Name = g.Key.ToString("yyyy/MM"),
                    Amount = profit
                };
            })
            .ToList();

        return new CustomsProfitReportResult
        {
            RevenueIqd = revenueIqd,
            RevenueUsd = revenueUsd,
            ExpensesIqd = expensesIqd,
            ExpensesUsd = expensesUsd,
            ProfitIqd = revenueIqd - expensesIqd,
            ProfitUsd = revenueUsd - expensesUsd,
            MonthlyChart = monthlyChart,
            Rows = rows
        };
    }

    private static string CurrencyLabel(AccountingCurrency currency) =>
        AccountingCurrencyHelper.GetLabel(currency);
}
