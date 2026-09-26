using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class CustomerStatementQuickService : ICustomerStatementQuickService
{
    private readonly IReportService _reportService;
    private readonly IExportService _exportService;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CustomerStatementQuickService(
        IReportService reportService,
        IExportService exportService,
        IDbContextFactory<AppDbContext> contextFactory)
    {
        _reportService = reportService;
        _exportService = exportService;
        _contextFactory = contextFactory;
    }

    public async Task<CustomerQuickStatementResult> GetStatementAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var from = DateTime.Today.AddYears(-2);
        var to = DateTime.Today;
        var statement = await _reportService.GetCustomerStatementAsync(customerId, from, to);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var phone = await context.Customers.AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => c.Phone)
            .FirstOrDefaultAsync(cancellationToken);

        var overdue = await context.Installments.AsNoTracking()
            .Include(i => i.InstallmentPlan).ThenInclude(p => p!.Invoice)
            .Where(i => i.InstallmentPlan!.CustomerId == customerId
                        && i.Status == InstallmentStatus.Overdue
                        && i.RemainingAmount > 0)
            .Select(i => new
            {
                i.RemainingAmount,
                Currency = i.InstallmentPlan!.Invoice!.Currency
            })
            .ToListAsync(cancellationToken);

        return new CustomerQuickStatementResult
        {
            CustomerId = customerId,
            CustomerName = statement.CustomerName,
            Phone = phone,
            Balance = statement.Balance,
            BalanceUsd = statement.BalanceUsd,
            TotalDebit = statement.TotalDebit,
            TotalCredit = statement.TotalCredit,
            TotalDebitUsd = statement.TotalDebitUsd,
            TotalCreditUsd = statement.TotalCreditUsd,
            OverdueInstallmentCount = overdue.Count,
            OverdueInstallmentAmount = overdue
                .Where(i => i.Currency == AccountingCurrency.IQD)
                .Sum(i => i.RemainingAmount),
            OverdueInstallmentAmountUsd = overdue
                .Where(i => i.Currency == AccountingCurrency.USD)
                .Sum(i => i.RemainingAmount),
            Lines = statement.Rows.Select(r => new CustomerQuickStatementLine
            {
                Date = r.Date,
                Description = r.Description,
                Debit = r.Debit,
                Credit = r.Credit,
                RunningBalance = r.RunningBalance,
                CurrencyLabel = AccountingCurrencyHelper.GetLabel(r.Currency)
            }).ToList()
        };
    }

    public async Task<string> ExportToPdfAsync(int customerId, string filePath, CancellationToken cancellationToken = default)
    {
        var data = await GetStatementAsync(customerId, cancellationToken);
        var model = BuildStatementModel(data);
        var generatedPath = _exportService.ExportStatementToPdf(model);

        if (!string.IsNullOrWhiteSpace(filePath) &&
            !string.Equals(generatedPath, filePath, StringComparison.OrdinalIgnoreCase))
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var target = filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                ? filePath
                : Path.ChangeExtension(filePath, ".pdf");
            File.Copy(generatedPath, target, overwrite: true);
            return target;
        }

        return generatedPath;
    }

    public void Print(int customerId)
    {
        var data = GetStatementAsync(customerId).GetAwaiter().GetResult();
        var cols = new[] { "التاريخ", "البيان", "العملة", "مدين", "دائن", "الرصيد" };
        var rows = data.Lines.Select(r => new object[]
        {
            r.Date.ToString("yyyy/MM/dd"), r.Description, r.CurrencyLabel, r.Debit, r.Credit, r.RunningBalance
        }).ToList();
        _exportService.PrintTable($"كشف حساب {data.CustomerName}", cols, rows, BuildSummaryLines(data));
    }

    public static StatementPrintModel BuildStatementModel(CustomerQuickStatementResult data)
    {
        return new StatementPrintModel
        {
            Title = $"كشف حساب — {data.CustomerName}",
            PartyName = data.CustomerName,
            PartyPhone = data.Phone,
            FromDate = DateTime.Today.AddYears(-2),
            ToDate = DateTime.Today,
            Columns = ["التاريخ", "البيان", "العملة", "مدين", "دائن", "الرصيد"],
            Rows = data.Lines.Select(r => new object[]
            {
                r.Date.ToString("yyyy/MM/dd"),
                r.Description,
                r.CurrencyLabel,
                r.Debit,
                r.Credit,
                r.RunningBalance
            }).ToList(),
            SummaryLines = BuildSummaryLines(data)
        };
    }

    private static List<string> BuildSummaryLines(CustomerQuickStatementResult data)
    {
        var summary = new List<string>
        {
            $"الرصيد د.ع: {data.Balance:N0}",
            $"مدين د.ع: {data.TotalDebit:N0}",
            $"دائن د.ع: {data.TotalCredit:N0}"
        };
        if (data.BalanceUsd != 0 || data.TotalDebitUsd != 0 || data.TotalCreditUsd != 0)
        {
            summary.Insert(1, $"الرصيد $: {data.BalanceUsd:N2}");
            summary.Add($"مدين $: {data.TotalDebitUsd:N2}");
            summary.Add($"دائن $: {data.TotalCreditUsd:N2}");
        }
        if (data.OverdueInstallmentCount > 0)
        {
            var overdue = $"أقساط متأخرة: {data.OverdueInstallmentCount} | {data.OverdueInstallmentAmount:N0} د.ع";
            if (data.OverdueInstallmentAmountUsd != 0)
                overdue += $" | {data.OverdueInstallmentAmountUsd:N2} $";
            summary.Add(overdue);
        }
        return summary;
    }
}
