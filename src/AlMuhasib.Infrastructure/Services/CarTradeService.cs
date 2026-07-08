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
        transaction.TradeType = CarTradeType.Buy;
        transaction.IsSold = false;
        ValidatePurchaseTransaction(transaction);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        transaction.TransactionNumber = await CarTradeNumberHelper.GenerateNextAsync(context);
        ApplyPurchaseAmounts(transaction);
        await context.CarTradeTransactions.AddAsync(transaction, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<CarTradeTransaction> UpdateAsync(CarTradeTransaction transaction, CancellationToken cancellationToken = default)
    {
        ValidatePurchaseTransaction(transaction);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.CarTradeTransactions.FirstOrDefaultAsync(t => t.Id == transaction.Id, cancellationToken)
            ?? throw new InvalidOperationException("العملية غير موجودة");

        if (existing.IsSold)
            throw new InvalidOperationException("لا يمكن تعديل بيانات الشراء بعد بيع السيارة");

        MapPurchaseTransaction(existing, transaction);
        ApplyPurchaseAmounts(existing);
        UpdatePurchaseStatus(existing);
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

    public Task<CarTradeTransaction> RecordPaymentAsync(
        int transactionId,
        decimal amount,
        DateTime paymentDate,
        string? notes,
        CancellationToken cancellationToken = default) =>
        RecordPurchasePaymentAsync(transactionId, amount, paymentDate, notes, cancellationToken);

    public async Task<CarTradeTransaction> RecordPurchasePaymentAsync(
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
            throw new InvalidOperationException("مبلغ التسديد أكبر من المبلغ المتبقي للبائع");

        var remainingBefore = transaction.RemainingAmount;
        transaction.AmountPaid += amount;
        transaction.RemainingAmount = transaction.PurchasePrice - transaction.AmountPaid;
        if (transaction.RemainingAmount < 0)
            transaction.RemainingAmount = 0;

        transaction.PaymentMode = transaction.RemainingAmount <= 0
            ? CarTradePaymentMode.FullCash
            : CarTradePaymentMode.Partial;

        UpdatePurchaseStatus(transaction);

        await context.CarTradePayments.AddAsync(new CarTradePayment
        {
            TransactionId = transaction.Id,
            PaymentKind = CarTradePaymentKind.Purchase,
            PaymentDate = paymentDate,
            Amount = amount,
            Notes = notes ?? string.Empty,
            RemainingBefore = remainingBefore,
            RemainingAfter = transaction.RemainingAmount
        }, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<CarTradeTransaction> RecordSalePaymentAsync(
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

        if (!transaction.IsSold)
            throw new InvalidOperationException("السيارة غير مباعة بعد");

        if (transaction.Status == CarTradeStatus.Cancelled)
            throw new InvalidOperationException("لا يمكن تسديد عملية ملغاة");

        if (amount > transaction.SaleRemainingAmount)
            throw new InvalidOperationException("مبلغ التسديد أكبر من المبلغ المتبقي على المشتري");

        var remainingBefore = transaction.SaleRemainingAmount;
        transaction.SaleAmountPaid += amount;
        transaction.SaleRemainingAmount = transaction.SalePrice - transaction.SaleAmountPaid;
        if (transaction.SaleRemainingAmount < 0)
            transaction.SaleRemainingAmount = 0;

        transaction.SalePaymentMode = transaction.SaleRemainingAmount <= 0
            ? CarTradePaymentMode.FullCash
            : CarTradePaymentMode.Partial;

        await context.CarTradePayments.AddAsync(new CarTradePayment
        {
            TransactionId = transaction.Id,
            PaymentKind = CarTradePaymentKind.Sale,
            PaymentDate = paymentDate,
            Amount = amount,
            Notes = notes ?? string.Empty,
            RemainingBefore = remainingBefore,
            RemainingAfter = transaction.SaleRemainingAmount
        }, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<CarTradeTransaction> SellCarAsync(
        int transactionId,
        CarTradeSellRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BuyerName))
            throw new InvalidOperationException("اسم المشتري مطلوب");
        if (request.SalePrice <= 0)
            throw new InvalidOperationException("سعر البيع يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var transaction = await context.CarTradeTransactions.FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException("العملية غير موجودة");

        if (transaction.IsSold)
            throw new InvalidOperationException("السيارة مباعة مسبقاً");

        if (transaction.Status == CarTradeStatus.Cancelled)
            throw new InvalidOperationException("لا يمكن بيع عملية ملغاة");

        transaction.BuyerName = request.BuyerName.Trim();
        transaction.BuyerPhone = request.BuyerPhone?.Trim() ?? string.Empty;
        transaction.SalePrice = request.SalePrice;
        transaction.SaleDate = request.SaleDate.Date;
        transaction.SalePaymentMode = request.SalePaymentMode;
        transaction.IsSold = true;

        if (!string.IsNullOrWhiteSpace(request.Notes))
            transaction.Notes = string.IsNullOrWhiteSpace(transaction.Notes)
                ? request.Notes.Trim()
                : $"{transaction.Notes.Trim()}\n{request.Notes.Trim()}";

        ApplySaleAmounts(transaction, request.SaleAmountPaid);
        await context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<CarTradeDashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var transactions = await context.CarTradeTransactions
            .Where(t => t.Status != CarTradeStatus.Cancelled && t.TradeType == CarTradeType.Buy)
            .ToListAsync(cancellationToken);

        var sold = transactions.Where(t => t.IsSold).ToList();

        return new CarTradeDashboardStats
        {
            TodayTransactions = transactions.Count(t => t.TransactionDate.Date == today),
            MonthTransactions = transactions.Count(t => t.TransactionDate.Date >= monthStart),
            TotalTransactions = transactions.Count,
            UnpaidTransactions = transactions.Count(t => t.RemainingAmount > 0 || t.SaleRemainingAmount > 0),
            BuyCount = transactions.Count,
            SellCount = sold.Count,
            AvailableCount = transactions.Count(t => !t.IsSold),
            SoldCount = sold.Count,
            TotalBuyValue = transactions.Sum(t => t.PurchasePrice),
            TotalSellValue = sold.Sum(t => t.SalePrice),
            TotalPaid = transactions.Sum(t => t.AmountPaid) + sold.Sum(t => t.SaleAmountPaid),
            TotalRemaining = transactions.Sum(t => t.RemainingAmount),
            TotalSaleRemaining = sold.Sum(t => t.SaleRemainingAmount),
            MonthlyBuy = transactions
                .GroupBy(t => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .TakeLast(12)
                .ToList(),
            MonthlySell = sold
                .Where(t => t.SaleDate.HasValue)
                .GroupBy(t => new DateTime(t.SaleDate!.Value.Year, t.SaleDate.Value.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .TakeLast(12)
                .ToList(),
            PaymentStatusChart =
            [
                new NameAmountPoint { Name = "ديون بائعين", Amount = transactions.Sum(t => t.RemainingAmount) },
                new NameAmountPoint { Name = "ديون مشترين", Amount = sold.Sum(t => t.SaleRemainingAmount) }
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
            .Where(t => t.Status != CarTradeStatus.Cancelled && t.IsSold && t.SaleRemainingAmount > 0)
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
        var transactions = await context.CarTradeTransactions
            .Where(t => t.Status != CarTradeStatus.Cancelled && t.TradeType == CarTradeType.Buy)
            .ToListAsync(cancellationToken);

        var rows = new List<CarTradePartyStatementRow>();
        foreach (var t in transactions)
        {
            if (t.RemainingAmount > 0 &&
                string.Equals(t.SellerName.Trim(), partyName, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(filter.PartyPhone) ||
                 string.Equals(t.SellerPhone.Trim(), filter.PartyPhone.Trim(), StringComparison.OrdinalIgnoreCase)) &&
                IsWithinDateRange(t.TransactionDate, filter.DateFrom, filter.DateTo))
            {
                rows.Add(new CarTradePartyStatementRow
                {
                    TransactionDate = t.TransactionDate,
                    TransactionNumber = t.TransactionNumber,
                    TradeType = "شراء",
                    CarName = t.CarName,
                    TotalAmount = t.PurchasePrice,
                    AmountPaid = t.AmountPaid,
                    RemainingAmount = t.RemainingAmount,
                    PartyRole = "بائع",
                    DebtKind = "دين بائع"
                });
            }

            if (t.IsSold &&
                t.SaleRemainingAmount > 0 &&
                string.Equals(t.BuyerName.Trim(), partyName, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(filter.PartyPhone) ||
                 string.Equals(t.BuyerPhone.Trim(), filter.PartyPhone.Trim(), StringComparison.OrdinalIgnoreCase)) &&
                IsWithinDateRange(t.SaleDate ?? t.TransactionDate, filter.DateFrom, filter.DateTo))
            {
                rows.Add(new CarTradePartyStatementRow
                {
                    TransactionDate = t.SaleDate ?? t.TransactionDate,
                    TransactionNumber = t.TransactionNumber,
                    TradeType = "بيع",
                    CarName = t.CarName,
                    TotalAmount = t.SalePrice,
                    AmountPaid = t.SaleAmountPaid,
                    RemainingAmount = t.SaleRemainingAmount,
                    PartyRole = "مشتري",
                    DebtKind = "دين مشتري"
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

    public async Task<IReadOnlyList<CarTradeDebtSummaryRow>> GetSellerDebtsSummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var transactions = await context.CarTradeTransactions
            .Where(t => t.Status != CarTradeStatus.Cancelled &&
                        t.TradeType == CarTradeType.Buy &&
                        t.RemainingAmount > 0 &&
                        !string.IsNullOrWhiteSpace(t.SellerName))
            .ToListAsync(cancellationToken);

        return transactions
            .GroupBy(t => new { Name = t.SellerName.Trim(), Phone = t.SellerPhone.Trim() })
            .Select(g => new CarTradeDebtSummaryRow
            {
                PartyName = g.Key.Name,
                PartyPhone = g.Key.Phone,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(t => t.PurchasePrice),
                AmountPaid = g.Sum(t => t.AmountPaid),
                RemainingAmount = g.Sum(t => t.RemainingAmount)
            })
            .OrderByDescending(r => r.RemainingAmount)
            .ThenBy(r => r.PartyName)
            .ToList();
    }

    public async Task<IReadOnlyList<CarTradeDebtSummaryRow>> GetBuyerDebtsSummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var transactions = await context.CarTradeTransactions
            .Where(t => t.Status != CarTradeStatus.Cancelled &&
                        t.IsSold &&
                        t.SaleRemainingAmount > 0 &&
                        !string.IsNullOrWhiteSpace(t.BuyerName))
            .ToListAsync(cancellationToken);

        return transactions
            .GroupBy(t => new { Name = t.BuyerName.Trim(), Phone = t.BuyerPhone.Trim() })
            .Select(g => new CarTradeDebtSummaryRow
            {
                PartyName = g.Key.Name,
                PartyPhone = g.Key.Phone,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(t => t.SalePrice),
                AmountPaid = g.Sum(t => t.SaleAmountPaid),
                RemainingAmount = g.Sum(t => t.SaleRemainingAmount)
            })
            .OrderByDescending(r => r.RemainingAmount)
            .ThenBy(r => r.PartyName)
            .ToList();
    }

    private static bool IsWithinDateRange(DateTime date, DateTime? from, DateTime? to)
    {
        if (from.HasValue && date.Date < from.Value.Date)
            return false;
        if (to.HasValue && date.Date > to.Value.Date)
            return false;
        return true;
    }

    private static IQueryable<CarTradeTransaction> BuildQuery(CarTradeDbContext context, CarTradeFilter filter)
    {
        var query = context.CarTradeTransactions.Where(t => t.TradeType == CarTradeType.Buy);

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
            query = query.Where(t => t.RemainingAmount > 0 || t.SaleRemainingAmount > 0);

        query = filter.SoldFilter switch
        {
            CarTradeSoldFilter.Available => query.Where(t => !t.IsSold),
            CarTradeSoldFilter.Sold => query.Where(t => t.IsSold),
            _ => query
        };

        return query;
    }

    private static void ValidatePurchaseTransaction(CarTradeTransaction transaction)
    {
        if (string.IsNullOrWhiteSpace(transaction.CarName))
            throw new InvalidOperationException("اسم السيارة مطلوب");

        if (string.IsNullOrWhiteSpace(transaction.SellerName))
            throw new InvalidOperationException("اسم البائع مطلوب");

        if (transaction.PurchasePrice <= 0)
            throw new InvalidOperationException("سعر الشراء يجب أن يكون أكبر من صفر");

        if (transaction.AmountPaid < 0)
            throw new InvalidOperationException("المبلغ المدفوع غير صالح");

        if (transaction.AmountPaid > transaction.PurchasePrice)
            throw new InvalidOperationException("المبلغ المدفوع أكبر من سعر الشراء");
    }

    private static void MapPurchaseTransaction(CarTradeTransaction target, CarTradeTransaction source)
    {
        target.TransactionDate = source.TransactionDate;
        target.TradeType = CarTradeType.Buy;
        target.CarName = source.CarName;
        target.CarColor = source.CarColor;
        target.PlateNumber = source.PlateNumber;
        target.ChassisNumber = source.ChassisNumber;
        target.CarType = source.CarType;
        target.SellerName = source.SellerName;
        target.SellerPhone = source.SellerPhone;
        target.PurchasePrice = source.PurchasePrice;
        target.PaymentMode = source.PaymentMode;
        target.AmountPaid = source.AmountPaid;
        target.Notes = source.Notes;
    }

    internal static void ApplyPurchaseAmounts(CarTradeTransaction transaction)
    {
        transaction.TotalAmount = transaction.PurchasePrice;

        if (transaction.PaymentMode == CarTradePaymentMode.FullCash)
            transaction.AmountPaid = transaction.PurchasePrice;

        if (transaction.AmountPaid > transaction.PurchasePrice)
            transaction.AmountPaid = transaction.PurchasePrice;

        transaction.RemainingAmount = transaction.PurchasePrice - transaction.AmountPaid;
        if (transaction.RemainingAmount < 0)
            transaction.RemainingAmount = 0;

        UpdatePurchaseStatus(transaction);
    }

    internal static void ApplySaleAmounts(CarTradeTransaction transaction, decimal saleAmountPaid)
    {
        if (transaction.SalePaymentMode == CarTradePaymentMode.FullCash)
            transaction.SaleAmountPaid = transaction.SalePrice;
        else
            transaction.SaleAmountPaid = saleAmountPaid;

        if (transaction.SaleAmountPaid > transaction.SalePrice)
            transaction.SaleAmountPaid = transaction.SalePrice;

        transaction.SaleRemainingAmount = transaction.SalePrice - transaction.SaleAmountPaid;
        if (transaction.SaleRemainingAmount < 0)
            transaction.SaleRemainingAmount = 0;

        if (transaction.SaleRemainingAmount <= 0)
            transaction.SalePaymentMode = CarTradePaymentMode.FullCash;
    }

    private static void UpdatePurchaseStatus(CarTradeTransaction transaction)
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
        IsSold = t.IsSold,
        SoldStatus = t.IsSold ? "مباعة" : "متوفرة",
        SaleDate = t.SaleDate,
        SalePaymentMode = GetPaymentModeLabel(t.SalePaymentMode),
        SaleAmountPaid = t.SaleAmountPaid,
        SaleRemainingAmount = t.SaleRemainingAmount,
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
        CarTradePaymentMode.FullCash => "نقدي",
        _ => "آجل"
    };

    internal static string GetStatusLabel(CarTradeStatus status) => status switch
    {
        CarTradeStatus.Completed => "مكتمل",
        CarTradeStatus.Cancelled => "ملغى",
        _ => "نشط"
    };

    // Backward-compatible alias used by sync mapper.
    internal static void ApplyAmounts(CarTradeTransaction transaction) => ApplyPurchaseAmounts(transaction);
}
