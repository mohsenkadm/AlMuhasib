using AlMuhasib.Core.Entities.RealEstate;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.RealEstate;
using AlMuhasib.Infrastructure.Data.RealEstate;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class RealEstatePartyService : IRealEstatePartyService
{
    private readonly IDbContextFactory<RealEstateDbContext> _contextFactory;

    public RealEstatePartyService(IDbContextFactory<RealEstateDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<(IReadOnlyList<RealEstatePartyListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.RealEstateParties.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.Phone.Contains(term) ||
                p.IdNumber.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var parties = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var debts = await context.RealEstateContracts
            .Where(c =>
                c.Status != RealEstateContractStatus.Cancelled &&
                c.PaymentMode == RealEstatePaymentMode.Credit &&
                c.RemainingAmount > 0)
            .Select(c => new
            {
                c.SellerName,
                c.BuyerName,
                c.SellerPhone,
                c.BuyerPhone,
                c.DebtorParty,
                c.RemainingAmount
            })
            .ToListAsync(cancellationToken);

        var items = parties.Select(p =>
        {
            var totalDebt = debts
                .Where(d =>
                    (d.DebtorParty == RealEstateDebtorParty.Buyer && d.BuyerName == p.Name) ||
                    (d.DebtorParty == RealEstateDebtorParty.Seller && d.SellerName == p.Name))
                .Sum(d => d.RemainingAmount);

            var contractCount = debts.Count(d =>
                d.BuyerName == p.Name || d.SellerName == p.Name);

            return new RealEstatePartyListItem
            {
                Id = p.Id,
                Name = p.Name,
                Phone = p.Phone,
                Address = p.Address,
                IdNumber = p.IdNumber,
                TotalDebt = totalDebt,
                ContractCount = contractCount
            };
        }).ToList();

        return (items, totalCount);
    }

    public async Task<RealEstateParty?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.RealEstateParties.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<RealEstateParty> SaveAsync(RealEstateParty party, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        if (party.Id == 0)
        {
            await context.RealEstateParties.AddAsync(party, cancellationToken);
        }
        else
        {
            var existing = await context.RealEstateParties.FirstOrDefaultAsync(p => p.Id == party.Id, cancellationToken)
                ?? throw new InvalidOperationException("الزبون غير موجود");
            existing.Name = party.Name;
            existing.Phone = party.Phone;
            existing.Address = party.Address;
            existing.IdNumber = party.IdNumber;
            existing.IdDate = party.IdDate;
            existing.Notes = party.Notes;
            party = existing;
        }

        await context.SaveChangesAsync(cancellationToken);
        return party;
    }

    public async Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var party = await context.RealEstateParties.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("الزبون غير موجود");
        party.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }
}
