using AlMuhasib.Core.Entities.CarTrade;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Car;
using AlMuhasib.Core.Models.CarTrade;
using AlMuhasib.Infrastructure.Data.CarTrade;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class CarTradeService : ICarTradeService
{
    private readonly IDbContextFactory<CarTradeDbContext> _contextFactory;

    public CarTradeService(IDbContextFactory<CarTradeDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<(IReadOnlyList<CarTradeListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CarTradeFilter filter,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = BuildQuery(context, filter);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => ToListItem(t))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<CarTradeListItem>> GetAllForExportAsync(
        CarTradeFilter filter,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await BuildQuery(context, filter)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .Select(t => ToListItem(t))
            .ToListAsync(cancellationToken);
    }

    public async Task<CarTradeTransaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.CarTradeTransactions
            .Include(t => t.Payments)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<CarTradeTransaction> CreateAsync(CarTradeTransaction transaction, CancellationToken cancellationToken = default)
    {
        ValidateTransaction(transaction);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        transaction.TransactionNumber = await CarTradeNumberHelper.GenerateNextAsync(context);
        ApplyAmounts(transaction);
        await context.CarTradeTransactions.AddAsync(transaction, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<CarTradeTransaction> UpdateAsync(CarTradeTransaction transaction, CancellationToken cancellationToken = default)
    {
        ValidateTransaction(transaction);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.CarTradeTransactions.FirstOrDefaultAsync(t => t.Id == transaction.Id, cancellationToken)
            ?? throw new InvalidOperationException("العملية غير موجودة");

        MapTransaction(existing, transaction);
        ApplyAmounts(existing);
        UpdateStatus(existing);
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var transaction = await context.CarTradeTransactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("العملية غير موجودة");

        transaction.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CarTradeTransaction> RecordPaymentAsync(
        int transactionId,
        decimal amount,
        DateTime paymentDate,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new InvalidOperationException("مبلغ التسديد يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var transaction = await context.CarTradeTransactions.FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException("العملية غير موجودة");

        if (transaction.Status == CarTradeStatus.Cancelled)
            throw new InvalidOperationException("لا يمكن تسديد عملية ملغاة");

        if (amount > transaction.RemainingAmount)
            throw new InvalidOperationException("مبلغ التسديد أكبر من المبلغ المتبقي");

        var remainingBefore = transaction.RemainingAmount;
        transaction.AmountPaid += amount;
        transaction.RemainingAmount = transaction.TotalAmount - transaction.AmountPaid;
        if (transaction.RemainingAmount < 0)
            transaction.RemainingAmount = 0;

        transaction.PaymentMode = transaction.RemainingAmount <= 0
            ? CarTradePaymentMode.FullCash
            : CarTradePaymentMode.Partial;

        UpdateStatus(transaction);

        await context.CarTradePayments.AddAsync(new CarTradePayment
        {
            TransactionId = transaction.Id,
            PaymentDate = paymentDate,
            Amount = amount,
            Notes = notes ?? string.Empty,
            RemainingBefore = remainingBefore,
            RemainingAfter = transaction.RemainingAmount
        }, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<CarTradeDashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var transactions = await context.CarTradeTransactions
            .Where(t => t.Status != CarTradeStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var buys = transactions.Where(t => t.TradeType == CarTradeType.Buy).ToList();
        var sells = transactions.Where(t => t.TradeType == CarTradeType.Sell).ToList();

        return new CarTradeDashboardStats
        {
            TodayTransactions = transactions.Count(t => t.TransactionDate.Date == today),
            MonthTransactions = transactions.Count(t => t.TransactionDate.Date >= monthStart),
            TotalTransactions = transactions.Count,
            UnpaidTransactions = transactions.Count(t => t.RemainingAmount > 0),
            BuyCount = buys.Count,
            SellCount = sells.Count,
            TotalBuyValue = buys.Sum(t => t.TotalAmount),
            TotalSellValue = sells.Sum(t => t.TotalAmount),
            TotalPaid = transactions.Sum(t => t.AmountPaid),
            TotalRemaining = transactions.Sum(t => t.RemainingAmount),
            MonthlyBuy = buys
                .GroupBy(t => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .TakeLast(12)
                .ToList(),
            MonthlySell = sells
                .GroupBy(t => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .TakeLast(12)
                .ToList(),
            PaymentStatusChart =
            [
                new NameAmountPoint { Name = "مسدد بالكامل", Amount = transactions.Count(t => t.RemainingAmount <= 0) },
                new NameAmountPoint { Name = "تسديد جزئي", Amount = transactions.Count(t => t.RemainingAmount > 0 && t.AmountPaid > 0) },
                new NameAmountPoint { Name = "غير مسدد", Amount = transactions.Count(t => t.AmountPaid <= 0 && t.RemainingAmount > 0) }
            ],
            TopCarTypes = transactions
                .GroupBy(t => string.IsNullOrWhiteSpace(t.CarType) ? "غير محدد" : t.CarType)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList(),
            RecentTransactions = transactions
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .Take(10)
                .Select(ToListItem)
                .ToList()
        };
    }

    public async Task<IReadOnlyList<string>> GetPartyNamesAsync(string? search, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var sellers = await context.CarTradeTransactions
            .Where(t => t.Status != CarTradeStatus.Cancelled && t.RemainingAmount > 0 && t.TradeType == CarTradeType.Buy)
            .Select(t => t.SellerName)
            .ToListAsync(cancellationToken);

        var buyers = await context.CarTradeTransactions
            .Where(t => t.Status != CarTradeStatus.Cancelled && t.RemainingAmount > 0 && t.TradeType == CarTradeType.Sell)
            .Select(t => t.BuyerName)
            .ToListAsync(cancellationToken);

        var names = sellers.Concat(buyers)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            names = names.Where(n => n.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return names;
    }

    public async Task<CarTradePartyStatementData> GetPartyStatementAsync(
        CarTradePartyStatementFilter filter,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filter.PartyName))
            throw new InvalidOperationException("يرجى اختيار الطرف");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var partyName = filter.PartyName.Trim();

        var query = context.CarTradeTransactions
            .Where(t => t.Status != CarTradeStatus.Cancelled && t.RemainingAmount > 0);

        if (filter.DateFrom.HasValue)
            query = query.Where(t => t.TransactionDate >= filter.DateFrom.Value.Date);
        if (filter.DateTo.HasValue)
            query = query.Where(t => t.TransactionDate <= filter.DateTo.Value.Date);

        var transactions = await query.ToListAsync(cancellationToken);

        var rows = new List<CarTradePartyStatementRow>();
        foreach (var t in transactions)
        {
            if (t.TradeType == CarTradeType.Buy &&
                string.Equals(t.SellerName.Trim(), partyName, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(filter.PartyPhone) ||
                 string.Equals(t.SellerPhone.Trim(), filter.PartyPhone.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                rows.Add(new CarTradePartyStatementRow
                {
                    TransactionDate = t.TransactionDate,
                    TransactionNumber = t.TransactionNumber,
                    TradeType = GetTradeTypeLabel(t.TradeType),
                    CarName = t.CarName,
                    TotalAmount = t.TotalAmount,
                    AmountPaid = t.AmountPaid,
                    RemainingAmount = t.RemainingAmount,
                    PartyRole = "بائع"
                });
            }
            else if (t.TradeType == CarTradeType.Sell &&
                     string.Equals(t.BuyerName.Trim(), partyName, StringComparison.OrdinalIgnoreCase) &&
                     (string.IsNullOrWhiteSpace(filter.PartyPhone) ||
                      string.Equals(t.BuyerPhone.Trim(), filter.PartyPhone.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                rows.Add(new CarTradePartyStatementRow
                {
                    TransactionDate = t.TransactionDate,
                    TransactionNumber = t.TransactionNumber,
                    TradeType = GetTradeTypeLabel(t.TradeType),
                    CarName = t.CarName,
                    TotalAmount = t.TotalAmount,
                    AmountPaid = t.AmountPaid,
                    RemainingAmount = t.RemainingAmount,
                    PartyRole = "مشتري"
                });
            }
        }

        rows = rows.OrderBy(r => r.TransactionDate).ThenBy(r => r.TransactionNumber).ToList();

        var totalDebit = rows.Where(r => r.PartyRole == "بائع").Sum(r => r.RemainingAmount);
        var totalCredit = rows.Where(r => r.PartyRole == "مشتري").Sum(r => r.RemainingAmount);

        return new CarTradePartyStatementData
        {
            PartyName = partyName,
            PartyPhone = filter.PartyPhone ?? string.Empty,
            Rows = rows,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            Balance = totalCredit - totalDebit
        };
    }

    private static IQueryable<CarTradeTransaction> BuildQuery(CarTradeDbContext context, CarTradeFilter filter)
    {
        var query = context.CarTradeTransactions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(t =>
                t.TransactionNumber.Contains(term) ||
                t.CarName.Contains(term) ||
                t.SellerName.Contains(term) ||
                t.BuyerName.Contains(term) ||
                t.PlateNumber.Contains(term) ||
                t.ChassisNumber.Contains(term) ||
                t.CarType.Contains(term));
        }

        if (filter.DateFrom.HasValue)
            query = query.Where(t => t.TransactionDate >= filter.DateFrom.Value.Date);

        if (filter.DateTo.HasValue)
            query = query.Where(t => t.TransactionDate <= filter.DateTo.Value.Date);

        if (filter.TradeType.HasValue)
            query = query.Where(t => t.TradeType == filter.TradeType.Value);

        query = filter.StatusFilter switch
        {
            CarTradeStatusFilter.Active => query.Where(t => t.Status == CarTradeStatus.Active),
            CarTradeStatusFilter.Completed => query.Where(t => t.Status == CarTradeStatus.Completed),
            CarTradeStatusFilter.Cancelled => query.Where(t => t.Status == CarTradeStatus.Cancelled),
            _ => query
        };

        if (filter.PaymentMode.HasValue)
            query = query.Where(t => t.PaymentMode == filter.PaymentMode.Value);

        if (filter.UnpaidOnly)
            query = query.Where(t => t.RemainingAmount > 0);

        return query;
    }

    private static void ValidateTransaction(CarTradeTransaction transaction)
    {
        if (string.IsNullOrWhiteSpace(transaction.CarName))
            throw new InvalidOperationException("اسم السيارة مطلوب");

        if (transaction.TradeType == CarTradeType.Buy)
        {
            if (string.IsNullOrWhiteSpace(transaction.SellerName))
                throw new InvalidOperationException("اسم البائع مطلوب عند الشراء");
            if (transaction.PurchasePrice <= 0)
                throw new InvalidOperationException("سعر الشراء يجب أن يكون أكبر من صفر");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(transaction.BuyerName))
                throw new InvalidOperationException("اسم المشتري مطلوب عند البيع");
            if (transaction.SalePrice <= 0)
                throw new InvalidOperationException("سعر البيع يجب أن يكون أكبر من صفر");
        }
    }

    private static void MapTransaction(CarTradeTransaction target, CarTradeTransaction source)
    {
        target.TransactionDate = source.TransactionDate;
        target.TradeType = source.TradeType;
        target.CarName = source.CarName;
        target.CarColor = source.CarColor;
        target.PlateNumber = source.PlateNumber;
        target.ChassisNumber = source.ChassisNumber;
        target.CarType = source.CarType;
        target.SellerName = source.SellerName;
        target.SellerPhone = source.SellerPhone;
        target.BuyerName = source.BuyerName;
        target.BuyerPhone = source.BuyerPhone;
        target.PurchasePrice = source.PurchasePrice;
        target.SalePrice = source.SalePrice;
        target.PaymentMode = source.PaymentMode;
        target.AmountPaid = source.AmountPaid;
        target.Notes = source.Notes;
        target.Status = source.Status;
    }

    internal static void ApplyAmounts(CarTradeTransaction transaction)
    {
        transaction.TotalAmount = transaction.TradeType == CarTradeType.Buy
            ? transaction.PurchasePrice
            : transaction.SalePrice;

        if (transaction.PaymentMode == CarTradePaymentMode.FullCash)
            transaction.AmountPaid = transaction.TotalAmount;

        if (transaction.AmountPaid > transaction.TotalAmount)
            transaction.AmountPaid = transaction.TotalAmount;

        transaction.RemainingAmount = transaction.TotalAmount - transaction.AmountPaid;
        if (transaction.RemainingAmount < 0)
            transaction.RemainingAmount = 0;

        UpdateStatus(transaction);
    }

    private static void UpdateStatus(CarTradeTransaction transaction)
    {
        if (transaction.Status == CarTradeStatus.Cancelled)
            return;

        transaction.Status = transaction.RemainingAmount <= 0
            ? CarTradeStatus.Completed
            : CarTradeStatus.Active;
    }

    internal static CarTradeListItem ToListItem(CarTradeTransaction t) => new()
    {
        Id = t.Id,
        TransactionNumber = t.TransactionNumber,
        TransactionDate = t.TransactionDate,
        TradeType = GetTradeTypeLabel(t.TradeType),
        TradeTypeValue = t.TradeType,
        CarName = t.CarName,
        CarColor = t.CarColor,
        PlateNumber = t.PlateNumber,
        ChassisNumber = t.ChassisNumber,
        CarType = t.CarType,
        SellerName = t.SellerName,
        SellerPhone = t.SellerPhone,
        BuyerName = t.BuyerName,
        BuyerPhone = t.BuyerPhone,
        PurchasePrice = t.PurchasePrice,
        SalePrice = t.SalePrice,
        TotalAmount = t.TotalAmount,
        PaymentMode = GetPaymentModeLabel(t.PaymentMode),
        AmountPaid = t.AmountPaid,
        RemainingAmount = t.RemainingAmount,
        Status = GetStatusLabel(t.Status),
        Notes = t.Notes
    };

    internal static string GetTradeTypeLabel(CarTradeType type) => type switch
    {
        CarTradeType.Sell => "بيع",
        _ => "شراء"
    };

    internal static string GetPaymentModeLabel(CarTradePaymentMode mode) => mode switch
    {
        CarTradePaymentMode.FullCash => "نقد كامل",
        _ => "دفع جزئي"
    };

    internal static string GetStatusLabel(CarTradeStatus status) => status switch
    {
        CarTradeStatus.Completed => "مكتمل",
        CarTradeStatus.Cancelled => "ملغى",
        _ => "نشط"
    };
}
