using AlMuhasib.Core.Entities.Car;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Car;
using AlMuhasib.Infrastructure.Data.Car;
using AlMuhasib.Core.Utilities;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class CarContractService : ICarContractService
{
    private readonly IDbContextFactory<CarDbContext> _contextFactory;

    public CarContractService(IDbContextFactory<CarDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<(IReadOnlyList<CarContractListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CarContractFilter filter,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = BuildQuery(context, filter);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.ContractDate)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => ToListItem(c))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<CarContractListItem>> GetAllForExportAsync(
        CarContractFilter filter,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await BuildQuery(context, filter)
            .OrderByDescending(c => c.ContractDate)
            .ThenByDescending(c => c.Id)
            .Select(c => ToListItem(c))
            .ToListAsync(cancellationToken);
    }

    public async Task<CarSaleContract?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.CarSaleContracts
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<CarSaleContract> CreateAsync(CarSaleContract contract, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        contract.ContractNumber = await CarContractNumberHelper.GenerateNextAsync(context);
        ApplyAmounts(contract);
        await context.CarSaleContracts.AddAsync(contract, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return contract;
    }

    public async Task<CarSaleContract> UpdateAsync(CarSaleContract contract, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.CarSaleContracts.FirstOrDefaultAsync(c => c.Id == contract.Id, cancellationToken)
            ?? throw new InvalidOperationException("العقد غير موجود");

        MapContract(existing, contract);
        ApplyAmounts(existing);
        UpdateStatus(existing);
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var contract = await context.CarSaleContracts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("العقد غير موجود");

        contract.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CarSaleContract> RecordPaymentAsync(
        int contractId,
        decimal amount,
        DateTime paymentDate,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new InvalidOperationException("مبلغ التسديد يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var contract = await context.CarSaleContracts.FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken)
            ?? throw new InvalidOperationException("العقد غير موجود");

        if (contract.Status == CarContractStatus.Cancelled)
            throw new InvalidOperationException("لا يمكن تسديد عقد ملغى");

        if (amount > contract.RemainingAmount)
            throw new InvalidOperationException("مبلغ التسديد أكبر من المبلغ المتبقي");

        var remainingBefore = contract.RemainingAmount;
        contract.AmountReceived += amount;
        contract.RemainingAmount = contract.CarPrice - contract.AmountReceived;
        UpdateStatus(contract);

        await context.CarContractPayments.AddAsync(new CarContractPayment
        {
            ContractId = contract.Id,
            PaymentDate = paymentDate,
            Amount = amount,
            Notes = notes ?? string.Empty,
            RemainingBefore = remainingBefore,
            RemainingAfter = contract.RemainingAmount
        }, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        return contract;
    }

    public async Task<CarContractDashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var contracts = await context.CarSaleContracts
            .Where(c => c.Status != CarContractStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var stats = new CarContractDashboardStats
        {
            TodayContracts = contracts.Count(c => c.ContractDate.Date == today),
            MonthContracts = contracts.Count(c => c.ContractDate.Date >= monthStart),
            TotalContracts = contracts.Count,
            UnpaidContracts = contracts.Count(c => c.RemainingAmount > 0),
            TotalCarValue = contracts.Sum(c => c.CarPrice),
            TotalReceived = contracts.Sum(c => c.AmountReceived),
            TotalRemaining = contracts.Sum(c => c.RemainingAmount),
            MonthlyContracts = contracts
                .GroupBy(c => new DateTime(c.ContractDate.Year, c.ContractDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint
                {
                    Name = g.Key.ToString("yyyy/MM"),
                    Count = g.Count()
                })
                .TakeLast(12)
                .ToList(),
            PaymentStatusChart =
            [
                new NameAmountPoint { Name = "مسدد بالكامل", Amount = contracts.Count(c => c.RemainingAmount <= 0) },
                new NameAmountPoint { Name = "تسديد جزئي", Amount = contracts.Count(c => c.RemainingAmount > 0 && c.AmountReceived > 0) },
                new NameAmountPoint { Name = "غير مسدد", Amount = contracts.Count(c => c.AmountReceived <= 0 && c.RemainingAmount > 0) }
            ],
            TopSellers = contracts
                .GroupBy(c => c.SellerName)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList(),
            TopBuyers = contracts
                .GroupBy(c => c.BuyerName)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList(),
            RecentContracts = contracts
                .OrderByDescending(c => c.ContractDate)
                .ThenByDescending(c => c.Id)
                .Take(10)
                .Select(ToListItem)
                .ToList()
        };

        return stats;
    }

    private static IQueryable<CarSaleContract> BuildQuery(CarDbContext context, CarContractFilter filter)
    {
        var query = context.CarSaleContracts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(c =>
                c.ContractNumber.Contains(term) ||
                c.SellerName.Contains(term) ||
                c.BuyerName.Contains(term) ||
                c.PlateNumber.Contains(term) ||
                c.ChassisNumber.Contains(term) ||
                c.CarType.Contains(term) ||
                c.CarModel.Contains(term));
        }

        if (filter.DateFrom.HasValue)
            query = query.Where(c => c.ContractDate >= filter.DateFrom.Value.Date);

        if (filter.DateTo.HasValue)
            query = query.Where(c => c.ContractDate <= filter.DateTo.Value.Date);

        query = filter.StatusFilter switch
        {
            CarContractStatusFilter.Active => query.Where(c => c.Status == CarContractStatus.Active),
            CarContractStatusFilter.Completed => query.Where(c => c.Status == CarContractStatus.Completed),
            CarContractStatusFilter.Cancelled => query.Where(c => c.Status == CarContractStatus.Cancelled),
            _ => query
        };

        if (filter.UnpaidOnly)
            query = query.Where(c => c.RemainingAmount > 0);

        return query;
    }

    private static void MapContract(CarSaleContract target, CarSaleContract source)
    {
        target.ContractDate = source.ContractDate;
        target.SellerName = source.SellerName;
        target.SellerAddress = source.SellerAddress;
        target.SellerIdNumber = source.SellerIdNumber;
        target.SellerIdDate = source.SellerIdDate;
        target.SellerPhone = source.SellerPhone;
        target.BuyerName = source.BuyerName;
        target.BuyerAddress = source.BuyerAddress;
        target.BuyerIdNumber = source.BuyerIdNumber;
        target.BuyerIdDate = source.BuyerIdDate;
        target.BuyerPhone = source.BuyerPhone;
        target.AnnualOwnerName = source.AnnualOwnerName;
        target.AnnualOwnerAddress = source.AnnualOwnerAddress;
        target.PlateNumber = source.PlateNumber;
        target.CarType = source.CarType;
        target.CarModel = source.CarModel;
        target.CarColor = source.CarColor;
        target.ChassisNumber = source.ChassisNumber;
        target.CarPrice = source.CarPrice;
        target.AmountReceived = source.AmountReceived;
        target.Notes = source.Notes;
        target.Status = source.Status;
    }

    private static void ApplyAmounts(CarSaleContract contract)
    {
        contract.RemainingAmount = contract.CarPrice - contract.AmountReceived;
        if (contract.RemainingAmount < 0)
            contract.RemainingAmount = 0;

        contract.CarPriceInWords = ArabicAmountToWords.Convert(contract.CarPrice);
        UpdateStatus(contract);
    }

    private static void UpdateStatus(CarSaleContract contract)
    {
        if (contract.Status == CarContractStatus.Cancelled)
            return;

        contract.Status = contract.RemainingAmount <= 0
            ? CarContractStatus.Completed
            : CarContractStatus.Active;
    }

    private static CarContractListItem ToListItem(CarSaleContract c) => new()
    {
        Id = c.Id,
        ContractNumber = c.ContractNumber,
        ContractDate = c.ContractDate,
        SellerName = c.SellerName,
        BuyerName = c.BuyerName,
        PlateNumber = c.PlateNumber,
        CarType = c.CarType,
        CarModel = c.CarModel,
        ChassisNumber = c.ChassisNumber,
        CarPrice = c.CarPrice,
        AmountReceived = c.AmountReceived,
        RemainingAmount = c.RemainingAmount,
        Status = GetStatusLabel(c.Status),
        Notes = c.Notes
    };

    internal static string GetStatusLabel(CarContractStatus status) => status switch
    {
        CarContractStatus.Completed => "مكتمل",
        CarContractStatus.Cancelled => "ملغى",
        _ => "نشط"
    };
}
