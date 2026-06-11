using AlMuhasib.Core.Enums;
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
        var overdue = await context.Installments.AsNoTracking()
            .Include(i => i.InstallmentPlan)
            .Where(i => i.InstallmentPlan!.CustomerId == customerId
                        && i.Status == InstallmentStatus.Overdue
                        && i.RemainingAmount > 0)
            .ToListAsync(cancellationToken);

        return new CustomerQuickStatementResult
        {
            CustomerId = customerId,
            CustomerName = statement.CustomerName,
            Balance = statement.Balance,
            TotalDebit = statement.TotalDebit,
            TotalCredit = statement.TotalCredit,
            OverdueInstallmentCount = overdue.Count,
            OverdueInstallmentAmount = overdue.Sum(i => i.RemainingAmount),
            Lines = statement.Rows.Select(r => new CustomerQuickStatementLine
            {
                Date = r.Date,
                Description = r.Description,
                Debit = r.Debit,
                Credit = r.Credit,
                RunningBalance = r.RunningBalance
            }).ToList()
        };
    }

    public async Task<string> ExportToPdfAsync(int customerId, string filePath, CancellationToken cancellationToken = default)
    {
        var data = await GetStatementAsync(customerId, cancellationToken);
        var cols = new[] { "التاريخ", "البيان", "مدين", "دائن", "الرصيد" };
        var rows = data.Lines.Select(r => new object[]
        {
            r.Date.ToString("yyyy/MM/dd"), r.Description, r.Debit, r.Credit, r.RunningBalance
        }).ToList();

        _exportService.ExportToExcel(filePath.Replace(".pdf", ".xlsx"),
            $"كشف حساب {data.CustomerName}", cols, rows);

        // Use print-to-PDF path via table export — Excel as fallback; PDF via PrintTable if available
        if (filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            // QuestPDF not wired for statements — export excel alongside
            var xlsxPath = filePath.Replace(".pdf", ".xlsx");
            return xlsxPath;
        }

        return filePath;
    }

    public void Print(int customerId)
    {
        var data = GetStatementAsync(customerId).GetAwaiter().GetResult();
        var cols = new[] { "التاريخ", "البيان", "مدين", "دائن", "الرصيد" };
        var rows = data.Lines.Select(r => new object[]
        {
            r.Date.ToString("yyyy/MM/dd"), r.Description, r.Debit, r.Credit, r.RunningBalance
        }).ToList();
        _exportService.PrintTable($"كشف حساب {data.CustomerName}", cols, rows);
    }
}
