using AlMuhasib.Core.Entities.RealEstate;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.RealEstate;
using AlMuhasib.Core.Utilities;
using AlMuhasib.Infrastructure.Data.RealEstate;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class RealEstateContractService : IRealEstateContractService
{
    private readonly IDbContextFactory<RealEstateDbContext> _contextFactory;

    public RealEstateContractService(IDbContextFactory<RealEstateDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<(IReadOnlyList<RealEstateContractListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        RealEstateContractFilter filter,
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

    public async Task<IReadOnlyList<RealEstateContractListItem>> GetAllForExportAsync(
        RealEstateContractFilter filter,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await BuildQuery(context, filter)
            .OrderByDescending(c => c.ContractDate)
            .ThenByDescending(c => c.Id)
            .Select(c => ToListItem(c))
            .ToListAsync(cancellationToken);
    }

    public async Task<RealEstateContract?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.RealEstateContracts
            .Include(c => c.Payments)
            .Include(c => c.Clauses)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<RealEstateContract> CreateAsync(RealEstateContract contract, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        contract.ContractNumber = await RealEstateContractNumberHelper.GenerateNextAsync(context);
        ApplyAmounts(contract);

        if (contract.Clauses.Count == 0)
        {
            var templates = await context.RealEstateClauseTemplates
                .Where(t => t.IsActive)
                .OrderBy(t => t.SortOrder)
                .ToListAsync(cancellationToken);

            foreach (var template in templates)
            {
                contract.Clauses.Add(new RealEstateContractClause
                {
                    SortOrder = template.SortOrder,
                    Title = template.Title,
                    Body = template.Body
                });
            }
        }

        await UpsertPartiesAsync(context, contract, cancellationToken);
        await context.RealEstateContracts.AddAsync(contract, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return contract;
    }

    public async Task<RealEstateContract> UpdateAsync(RealEstateContract contract, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.RealEstateContracts
            .Include(c => c.Clauses)
            .FirstOrDefaultAsync(c => c.Id == contract.Id, cancellationToken)
            ?? throw new InvalidOperationException("العقد غير موجود");

        MapContract(existing, contract);
        ApplyAmounts(existing);
        UpdateStatus(existing);

        if (contract.Clauses.Count > 0)
        {
            context.RealEstateContractClauses.RemoveRange(existing.Clauses);
            existing.Clauses.Clear();
            foreach (var clause in contract.Clauses.OrderBy(c => c.SortOrder))
            {
                existing.Clauses.Add(new RealEstateContractClause
                {
                    SortOrder = clause.SortOrder,
                    Title = clause.Title,
                    Body = clause.Body
                });
            }
        }

        await UpsertPartiesAsync(context, existing, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var contract = await context.RealEstateContracts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("العقد غير موجود");

        contract.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RealEstateContract> RecordPaymentAsync(
        int contractId,
        decimal amount,
        DateTime paymentDate,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new InvalidOperationException("مبلغ التسديد يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var contract = await context.RealEstateContracts.FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken)
            ?? throw new InvalidOperationException("العقد غير موجود");

        if (contract.Status == RealEstateContractStatus.Cancelled)
            throw new InvalidOperationException("لا يمكن تسديد عقد ملغى");

        if (amount > contract.RemainingAmount)
            throw new InvalidOperationException("مبلغ التسديد أكبر من المبلغ المتبقي");

        var remainingBefore = contract.RemainingAmount;
        contract.AmountPaid += amount;
        contract.RemainingAmount = Math.Max(0, contract.TotalPrice - contract.AmountPaid);
        UpdateStatus(contract);

        await context.RealEstateContractPayments.AddAsync(new RealEstateContractPayment
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

    public async Task<RealEstateContractDashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var contracts = await context.RealEstateContracts
            .Where(c => c.Status != RealEstateContractStatus.Cancelled)
            .ToListAsync(cancellationToken);

        return new RealEstateContractDashboardStats
        {
            TodayContracts = contracts.Count(c => c.ContractDate.Date == today),
            MonthContracts = contracts.Count(c => c.ContractDate.Date >= monthStart),
            TotalContracts = contracts.Count,
            UnpaidContracts = contracts.Count(c => c.RemainingAmount > 0),
            OverdueDebts = contracts.Count(c =>
                c.PaymentMode == RealEstatePaymentMode.Credit &&
                c.RemainingAmount > 0 &&
                c.DueDate.HasValue &&
                c.DueDate.Value.Date < today),
            TotalValue = contracts.Sum(c => c.TotalPrice),
            TotalReceived = contracts.Sum(c => c.AmountPaid),
            TotalRemaining = contracts.Sum(c => c.RemainingAmount),
            MonthlyContracts = contracts
                .GroupBy(c => new DateTime(c.ContractDate.Year, c.ContractDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .TakeLast(12)
                .ToList(),
            PaymentStatusChart =
            [
                new NameAmountPoint { Name = "مسدد بالكامل", Amount = contracts.Count(c => c.RemainingAmount <= 0) },
                new NameAmountPoint { Name = "آجل جزئي", Amount = contracts.Count(c => c.RemainingAmount > 0 && c.AmountPaid > 0) },
                new NameAmountPoint { Name = "غير مسدد", Amount = contracts.Count(c => c.AmountPaid <= 0 && c.RemainingAmount > 0) }
            ],
            ByContractType =
            [
                new NameCountPoint { Name = "بيع", Count = contracts.Count(c => c.ContractType == RealEstateContractType.Sale) },
                new NameCountPoint { Name = "شراء", Count = contracts.Count(c => c.ContractType == RealEstateContractType.Purchase) }
            ],
            ByPropertyType = contracts
                .GroupBy(c => GetPropertyTypeLabel(c.PropertyType))
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList(),
            RecentContracts = contracts
                .OrderByDescending(c => c.ContractDate)
                .ThenByDescending(c => c.Id)
                .Take(10)
                .Select(ToListItem)
                .ToList()
        };
    }

    public async Task<IReadOnlyList<RealEstateDebtItem>> GetDebtsAsync(
        bool overdueOnly = false,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var today = DateTime.Today;

        var contracts = await context.RealEstateContracts
            .Where(c =>
                c.Status != RealEstateContractStatus.Cancelled &&
                c.PaymentMode == RealEstatePaymentMode.Credit &&
                c.RemainingAmount > 0 &&
                c.DebtorParty != RealEstateDebtorParty.None)
            .OrderBy(c => c.DueDate)
            .ToListAsync(cancellationToken);

        var items = contracts.Select(c =>
        {
            var isBuyer = c.DebtorParty == RealEstateDebtorParty.Buyer;
            var due = c.DueDate?.Date;
            var overdue = due.HasValue && due.Value < today;
            return new RealEstateDebtItem
            {
                ContractId = c.Id,
                ContractNumber = c.ContractNumber,
                ContractDate = c.ContractDate,
                DebtorName = isBuyer ? c.BuyerName : c.SellerName,
                DebtorPhone = isBuyer ? c.BuyerPhone : c.SellerPhone,
                DebtorParty = isBuyer ? "المشتري" : "البائع",
                CounterpartyName = isBuyer ? c.SellerName : c.BuyerName,
                RemainingAmount = c.RemainingAmount,
                DueDate = c.DueDate,
                IsOverdue = overdue,
                DaysOverdue = overdue ? (today - due!.Value).Days : 0
            };
        });

        if (overdueOnly)
            items = items.Where(i => i.IsOverdue);

        return items.ToList();
    }

    private static IQueryable<RealEstateContract> BuildQuery(RealEstateDbContext context, RealEstateContractFilter filter)
    {
        var query = context.RealEstateContracts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(c =>
                c.ContractNumber.Contains(term) ||
                c.SellerName.Contains(term) ||
                c.BuyerName.Contains(term) ||
                c.PropertyLocation.Contains(term) ||
                c.PropertyAddress.Contains(term));
        }

        if (filter.DateFrom.HasValue)
            query = query.Where(c => c.ContractDate >= filter.DateFrom.Value.Date);

        if (filter.DateTo.HasValue)
            query = query.Where(c => c.ContractDate <= filter.DateTo.Value.Date);

        query = filter.StatusFilter switch
        {
            RealEstateContractStatusFilter.Active => query.Where(c => c.Status == RealEstateContractStatus.Active),
            RealEstateContractStatusFilter.Completed => query.Where(c => c.Status == RealEstateContractStatus.Completed),
            RealEstateContractStatusFilter.Cancelled => query.Where(c => c.Status == RealEstateContractStatus.Cancelled),
            _ => query
        };

        if (filter.ContractType.HasValue)
            query = query.Where(c => c.ContractType == filter.ContractType.Value);

        if (filter.PropertyType.HasValue)
            query = query.Where(c => c.PropertyType == filter.PropertyType.Value);

        if (filter.PaymentMode.HasValue)
            query = query.Where(c => c.PaymentMode == filter.PaymentMode.Value);

        if (filter.UnpaidOnly)
            query = query.Where(c => c.RemainingAmount > 0);

        if (filter.CreditOnly)
            query = query.Where(c => c.PaymentMode == RealEstatePaymentMode.Credit);

        return query;
    }

    private static async Task UpsertPartiesAsync(
        RealEstateDbContext context,
        RealEstateContract contract,
        CancellationToken cancellationToken)
    {
        await UpsertPartyAsync(context, contract.SellerName, contract.SellerPhone, contract.SellerAddress, contract.SellerIdNumber, contract.SellerIdDate, cancellationToken);
        await UpsertPartyAsync(context, contract.BuyerName, contract.BuyerPhone, contract.BuyerAddress, contract.BuyerIdNumber, contract.BuyerIdDate, cancellationToken);
    }

    private static async Task UpsertPartyAsync(
        RealEstateDbContext context,
        string name,
        string phone,
        string address,
        string idNumber,
        DateTime? idDate,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        var existing = await context.RealEstateParties
            .FirstOrDefaultAsync(p => p.Name == name && (string.IsNullOrEmpty(phone) || p.Phone == phone), cancellationToken);

        if (existing is null)
        {
            await context.RealEstateParties.AddAsync(new RealEstateParty
            {
                Name = name.Trim(),
                Phone = phone ?? string.Empty,
                Address = address ?? string.Empty,
                IdNumber = idNumber ?? string.Empty,
                IdDate = idDate
            }, cancellationToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(phone)) existing.Phone = phone;
        if (!string.IsNullOrWhiteSpace(address)) existing.Address = address;
        if (!string.IsNullOrWhiteSpace(idNumber)) existing.IdNumber = idNumber;
        if (idDate.HasValue) existing.IdDate = idDate;
    }

    private static void MapContract(RealEstateContract target, RealEstateContract source)
    {
        target.ContractDate = source.ContractDate;
        target.ContractType = source.ContractType;
        target.PropertyType = source.PropertyType;
        target.PropertyLocation = source.PropertyLocation;
        target.PropertyAddress = source.PropertyAddress;
        target.PropertyAreaSqm = source.PropertyAreaSqm;
        target.PropertyDescription = source.PropertyDescription;
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
        target.TotalPrice = source.TotalPrice;
        target.DownPayment = source.DownPayment;
        target.AmountPaid = source.AmountPaid;
        target.PaymentMode = source.PaymentMode;
        target.DebtorParty = source.DebtorParty;
        target.DueDate = source.DueDate;
        target.WitnessOneName = source.WitnessOneName;
        target.WitnessTwoName = source.WitnessTwoName;
        target.Notes = source.Notes;
        target.Status = source.Status;
    }

    private static void ApplyAmounts(RealEstateContract contract)
    {
        if (contract.PaymentMode == RealEstatePaymentMode.Cash)
        {
            contract.DebtorParty = RealEstateDebtorParty.None;
            if (contract.AmountPaid <= 0 && contract.DownPayment > 0)
                contract.AmountPaid = contract.DownPayment;
            if (contract.AmountPaid <= 0)
                contract.AmountPaid = contract.TotalPrice;
        }
        else
        {
            if (contract.DebtorParty == RealEstateDebtorParty.None)
                contract.DebtorParty = RealEstateDebtorParty.Buyer;
            if (contract.AmountPaid <= 0 && contract.DownPayment > 0)
                contract.AmountPaid = contract.DownPayment;
        }

        contract.RemainingAmount = Math.Max(0, contract.TotalPrice - contract.AmountPaid);
        contract.TotalPriceInWords = ArabicAmountToWords.Convert(contract.TotalPrice, "دينار", "فلس");
        UpdateStatus(contract);
    }

    private static void UpdateStatus(RealEstateContract contract)
    {
        if (contract.Status == RealEstateContractStatus.Cancelled)
            return;

        contract.Status = contract.RemainingAmount <= 0
            ? RealEstateContractStatus.Completed
            : RealEstateContractStatus.Active;
    }

    internal static RealEstateContractListItem ToListItem(RealEstateContract c) => new()
    {
        Id = c.Id,
        SyncId = c.SyncId,
        ContractNumber = c.ContractNumber,
        ContractDate = c.ContractDate,
        ContractType = GetContractTypeLabel(c.ContractType),
        PropertyType = GetPropertyTypeLabel(c.PropertyType),
        PropertyLocation = c.PropertyLocation,
        PropertyAreaSqm = c.PropertyAreaSqm,
        SellerName = c.SellerName,
        BuyerName = c.BuyerName,
        TotalPrice = c.TotalPrice,
        AmountPaid = c.AmountPaid,
        RemainingAmount = c.RemainingAmount,
        PaymentMode = c.PaymentMode == RealEstatePaymentMode.Credit ? "آجل" : "نقدي",
        DebtorParty = c.DebtorParty switch
        {
            RealEstateDebtorParty.Buyer => "المشتري",
            RealEstateDebtorParty.Seller => "البائع",
            _ => "-"
        },
        DueDate = c.DueDate,
        Status = GetStatusLabel(c.Status),
        Notes = c.Notes
    };

    public static string GetStatusLabel(RealEstateContractStatus status) => status switch
    {
        RealEstateContractStatus.Completed => "مكتمل",
        RealEstateContractStatus.Cancelled => "ملغى",
        _ => "نشط"
    };

    public static string GetContractTypeLabel(RealEstateContractType type) =>
        type == RealEstateContractType.Purchase ? "شراء" : "بيع";

    public static string GetPropertyTypeLabel(RealEstatePropertyType type) => type switch
    {
        RealEstatePropertyType.Land => "أرض",
        RealEstatePropertyType.Other => "أخرى",
        _ => "دار"
    };
}
