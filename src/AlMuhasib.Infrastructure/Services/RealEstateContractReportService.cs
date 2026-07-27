using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.RealEstate;
using AlMuhasib.Infrastructure.Data.RealEstate;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class RealEstateContractReportService : IRealEstateContractReportService
{
    private readonly IDbContextFactory<RealEstateDbContext> _contextFactory;
    private readonly IRealEstateContractService _contractService;

    public RealEstateContractReportService(
        IDbContextFactory<RealEstateDbContext> contextFactory,
        IRealEstateContractService contractService)
    {
        _contextFactory = contextFactory;
        _contractService = contractService;
    }

    public async Task<RealEstateContractReportData> GetReportAsync(
        RealEstateContractFilter filter,
        CancellationToken cancellationToken = default)
    {
        var rows = await _contractService.GetAllForExportAsync(filter, cancellationToken);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var contracts = await context.RealEstateContracts
            .Where(c => rows.Select(r => r.Id).Contains(c.Id))
            .ToListAsync(cancellationToken);

        return new RealEstateContractReportData
        {
            Rows = rows.ToList(),
            TotalValue = rows.Sum(r => r.TotalPrice),
            TotalReceived = rows.Sum(r => r.AmountPaid),
            TotalRemaining = rows.Sum(r => r.RemainingAmount),
            MonthlyContracts = contracts
                .GroupBy(c => new DateTime(c.ContractDate.Year, c.ContractDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .ToList(),
            CollectedVsRemaining =
            [
                new NameAmountPoint { Name = "المحصّل", Amount = rows.Sum(r => r.AmountPaid) },
                new NameAmountPoint { Name = "المتبقي", Amount = rows.Sum(r => r.RemainingAmount) }
            ],
            ByPropertyType = contracts
                .GroupBy(c => RealEstateContractService.GetPropertyTypeLabel(c.PropertyType))
                .OrderByDescending(g => g.Count())
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList(),
            ByContractType = contracts
                .GroupBy(c => RealEstateContractService.GetContractTypeLabel(c.ContractType))
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList()
        };
    }

    public async Task<RealEstateProfitReportData> GetProfitReportAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var from = (dateFrom ?? DateTime.Today.AddMonths(-1)).Date;
        var to = (dateTo ?? DateTime.Today).Date;

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var contracts = await context.RealEstateContracts
            .Where(c =>
                c.Status != RealEstateContractStatus.Cancelled &&
                c.ContractDate >= from &&
                c.ContractDate <= to)
            .OrderByDescending(c => c.ContractDate)
            .ToListAsync(cancellationToken);

        var expenses = await context.RealEstateExpenses
            .Include(e => e.ExpenseType)
            .Include(e => e.RelatedContract)
            .Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to)
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync(cancellationToken);

        var sales = contracts.Where(c => c.ContractType == RealEstateContractType.Sale).ToList();
        var purchases = contracts.Where(c => c.ContractType == RealEstateContractType.Purchase).ToList();

        var saleRevenue = sales.Sum(c => c.TotalPrice);
        var purchaseCost = purchases.Sum(c => c.TotalPrice);
        var gross = saleRevenue - purchaseCost;
        var totalExpenses = expenses.Sum(e => e.Amount);
        var net = gross - totalExpenses;

        var cashIn = sales.Sum(c => c.AmountPaid);
        var cashOutPurchases = purchases.Sum(c => c.AmountPaid);

        // Monthly breakdown (union of contract months and expense months)
        var monthKeys = contracts
            .Select(c => new DateTime(c.ContractDate.Year, c.ContractDate.Month, 1))
            .Concat(expenses.Select(e => new DateTime(e.ExpenseDate.Year, e.ExpenseDate.Month, 1)))
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        var monthly = monthKeys.Select(month =>
        {
            var monthEnd = month.AddMonths(1).AddDays(-1);
            var monthSales = sales.Where(c => c.ContractDate >= month && c.ContractDate <= monthEnd).Sum(c => c.TotalPrice);
            var monthPurchases = purchases.Where(c => c.ContractDate >= month && c.ContractDate <= monthEnd).Sum(c => c.TotalPrice);
            var monthExpenses = expenses.Where(e => e.ExpenseDate >= month && e.ExpenseDate <= monthEnd).Sum(e => e.Amount);
            var monthGross = monthSales - monthPurchases;
            return new RealEstateMonthlyProfitPoint
            {
                Period = month.ToString("yyyy/MM"),
                SaleRevenue = monthSales,
                PurchaseCost = monthPurchases,
                Expenses = monthExpenses,
                GrossProfit = monthGross,
                NetProfit = monthGross - monthExpenses
            };
        }).ToList();

        return new RealEstateProfitReportData
        {
            DateFrom = from,
            DateTo = to,
            SaleContractsCount = sales.Count,
            PurchaseContractsCount = purchases.Count,
            ExpenseCount = expenses.Count,
            SaleRevenue = saleRevenue,
            PurchaseCost = purchaseCost,
            GrossProfit = gross,
            TotalExpenses = totalExpenses,
            NetProfit = net,
            ProfitMarginPercent = saleRevenue > 0 ? Math.Round(gross / saleRevenue * 100m, 2) : 0m,
            CashInFromSales = cashIn,
            CashOutOnPurchases = cashOutPurchases,
            CashExpenses = totalExpenses,
            NetCash = cashIn - cashOutPurchases - totalExpenses,
            SaleReceivables = sales.Sum(c => c.RemainingAmount),
            PurchasePayables = purchases.Sum(c => c.RemainingAmount),
            ExpensesByType = expenses
                .GroupBy(e => e.ExpenseType.Name)
                .OrderByDescending(g => g.Sum(x => x.Amount))
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToList(),
            MonthlySeries = monthly,
            SaleRows = sales.Select(c => new RealEstateProfitContractRow
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,
                ContractDate = c.ContractDate,
                ContractType = "بيع",
                PartyName = c.BuyerName,
                PropertyLocation = c.PropertyLocation,
                TotalPrice = c.TotalPrice,
                AmountPaid = c.AmountPaid,
                RemainingAmount = c.RemainingAmount
            }).ToList(),
            PurchaseRows = purchases.Select(c => new RealEstateProfitContractRow
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,
                ContractDate = c.ContractDate,
                ContractType = "شراء",
                PartyName = c.SellerName,
                PropertyLocation = c.PropertyLocation,
                TotalPrice = c.TotalPrice,
                AmountPaid = c.AmountPaid,
                RemainingAmount = c.RemainingAmount
            }).ToList(),
            ExpenseRows = expenses.Select(e => new RealEstateExpenseListItem
            {
                Id = e.Id,
                ExpenseDate = e.ExpenseDate,
                ExpenseTypeId = e.ExpenseTypeId,
                ExpenseTypeName = e.ExpenseType.Name,
                Amount = e.Amount,
                Description = e.Description,
                Notes = e.Notes,
                RelatedContractId = e.RelatedContractId,
                RelatedContractNumber = e.RelatedContract?.ContractNumber ?? string.Empty
            }).ToList()
        };
    }
}
