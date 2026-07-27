using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Entities.RealEstate;
using AlMuhasib.Infrastructure.Data.RealEstate;
using AlMuhasib.Sync.Dtos;
using AlMuhasib.Sync.Requests;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.RealEstate;

internal static class RealEstateSyncMapper
{
    public static async Task<SyncDataBundle> BuildPushBundleAsync(
        RealEstateDbContext db,
        DateTime? since,
        CancellationToken ct)
    {
        var cutoff = since ?? DateTime.MinValue;
        bool ShouldSync(BaseEntity e) =>
            (e.UpdatedAt ?? e.CreatedAt) >= cutoff
            || (e.IsDeleted && (e.DeletedAt ?? e.UpdatedAt ?? e.CreatedAt) >= cutoff);

        var contracts = await db.RealEstateContracts.IgnoreQueryFilters()
            .Include(c => c.Payments)
            .Include(c => c.Clauses)
            .ToListAsync(ct);

        var contractMap = contracts.ToDictionary(c => c.Id, c => c.SyncId);
        var templates = await db.RealEstateClauseTemplates.IgnoreQueryFilters().ToListAsync(ct);
        var parties = await db.RealEstateParties.IgnoreQueryFilters().ToListAsync(ct);

        return new SyncDataBundle
        {
            RealEstateContracts = contracts.Where(ShouldSync).Select(MapContract).ToList(),
            RealEstateContractPayments = contracts
                .SelectMany(c => c.Payments)
                .Where(ShouldSync)
                .Where(p => contractMap.ContainsKey(p.ContractId))
                .Select(p => MapPayment(p, contractMap))
                .ToList(),
            RealEstateContractClauses = contracts
                .SelectMany(c => c.Clauses)
                .Where(ShouldSync)
                .Where(c => contractMap.ContainsKey(c.ContractId))
                .Select(c => MapClause(c, contractMap))
                .ToList(),
            RealEstateClauseTemplates = templates.Where(ShouldSync).Select(MapTemplate).ToList(),
            RealEstateParties = parties.Where(ShouldSync).Select(MapParty).ToList()
        };
    }

    public static async Task ApplyPullBundleAsync(RealEstateDbContext db, SyncDataBundle data, CancellationToken ct)
    {
        db.IsApplyingSyncPull = true;
        try
        {
            var contractMap = await ApplyContractsAsync(db, data.RealEstateContracts, ct);
            await ApplyPaymentsAsync(db, data.RealEstateContractPayments, contractMap, ct);
            await ApplyClausesAsync(db, data.RealEstateContractClauses, contractMap, ct);
            await ApplyTemplatesAsync(db, data.RealEstateClauseTemplates, ct);
            await ApplyPartiesAsync(db, data.RealEstateParties, ct);
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            db.IsApplyingSyncPull = false;
        }
    }

    private static void CopyBase(BaseEntity e, SyncDtoBase d)
    {
        d.SyncId = e.SyncId;
        d.CreatedAt = e.CreatedAt;
        d.CreatedBy = e.CreatedBy;
        d.UpdatedAt = e.UpdatedAt;
        d.UpdatedBy = e.UpdatedBy;
        d.IsDeleted = e.IsDeleted;
        d.DeletedAt = e.DeletedAt;
        d.DeletedBy = e.DeletedBy;
        d.RowVersion = e.RowVersion;
    }

    private static void ApplyBase(BaseEntity e, SyncDtoBase d)
    {
        e.SyncId = d.SyncId;
        e.CreatedAt = d.CreatedAt;
        e.CreatedBy = d.CreatedBy;
        e.UpdatedAt = d.UpdatedAt;
        e.UpdatedBy = d.UpdatedBy;
        e.IsDeleted = d.IsDeleted;
        e.DeletedAt = d.DeletedAt;
        e.DeletedBy = d.DeletedBy;
    }

    private static RealEstateContractSyncDto MapContract(RealEstateContract c)
    {
        var dto = new RealEstateContractSyncDto
        {
            ContractNumber = c.ContractNumber,
            ContractDate = c.ContractDate,
            ContractType = c.ContractType,
            PropertyType = c.PropertyType,
            PropertyLocation = c.PropertyLocation,
            PropertyAddress = c.PropertyAddress,
            PropertyAreaSqm = c.PropertyAreaSqm,
            PropertyDescription = c.PropertyDescription,
            SellerName = c.SellerName,
            SellerAddress = c.SellerAddress,
            SellerIdNumber = c.SellerIdNumber,
            SellerIdDate = c.SellerIdDate,
            SellerPhone = c.SellerPhone,
            BuyerName = c.BuyerName,
            BuyerAddress = c.BuyerAddress,
            BuyerIdNumber = c.BuyerIdNumber,
            BuyerIdDate = c.BuyerIdDate,
            BuyerPhone = c.BuyerPhone,
            TotalPrice = c.TotalPrice,
            TotalPriceInWords = c.TotalPriceInWords,
            DownPayment = c.DownPayment,
            AmountPaid = c.AmountPaid,
            RemainingAmount = c.RemainingAmount,
            PaymentMode = c.PaymentMode,
            DebtorParty = c.DebtorParty,
            DueDate = c.DueDate,
            WitnessOneName = c.WitnessOneName,
            WitnessTwoName = c.WitnessTwoName,
            Notes = c.Notes,
            Status = c.Status
        };
        CopyBase(c, dto);
        return dto;
    }

    private static RealEstateContractPaymentSyncDto MapPayment(
        RealEstateContractPayment p,
        Dictionary<int, Guid> contractMap)
    {
        var dto = new RealEstateContractPaymentSyncDto
        {
            ContractSyncId = contractMap[p.ContractId],
            PaymentDate = p.PaymentDate,
            Amount = p.Amount,
            Notes = p.Notes,
            RemainingBefore = p.RemainingBefore,
            RemainingAfter = p.RemainingAfter
        };
        CopyBase(p, dto);
        return dto;
    }

    private static RealEstateContractClauseSyncDto MapClause(
        RealEstateContractClause c,
        Dictionary<int, Guid> contractMap)
    {
        var dto = new RealEstateContractClauseSyncDto
        {
            ContractSyncId = contractMap[c.ContractId],
            SortOrder = c.SortOrder,
            Title = c.Title,
            Body = c.Body
        };
        CopyBase(c, dto);
        return dto;
    }

    private static RealEstateClauseTemplateSyncDto MapTemplate(RealEstateClauseTemplate t)
    {
        var dto = new RealEstateClauseTemplateSyncDto
        {
            SortOrder = t.SortOrder,
            Title = t.Title,
            Body = t.Body,
            IsActive = t.IsActive
        };
        CopyBase(t, dto);
        return dto;
    }

    private static RealEstatePartySyncDto MapParty(RealEstateParty p)
    {
        var dto = new RealEstatePartySyncDto
        {
            Name = p.Name,
            Phone = p.Phone,
            Address = p.Address,
            IdNumber = p.IdNumber,
            IdDate = p.IdDate,
            Notes = p.Notes
        };
        CopyBase(p, dto);
        return dto;
    }

    private static async Task<Dictionary<Guid, int>> ApplyContractsAsync(
        RealEstateDbContext db,
        List<RealEstateContractSyncDto> dtos,
        CancellationToken ct)
    {
        var map = new Dictionary<Guid, int>();
        foreach (var dto in dtos)
        {
            var existing = await db.RealEstateContracts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new RealEstateContract();
                db.RealEstateContracts.Add(existing);
            }

            existing.ContractNumber = dto.ContractNumber;
            existing.ContractDate = dto.ContractDate;
            existing.ContractType = dto.ContractType;
            existing.PropertyType = dto.PropertyType;
            existing.PropertyLocation = dto.PropertyLocation;
            existing.PropertyAddress = dto.PropertyAddress;
            existing.PropertyAreaSqm = dto.PropertyAreaSqm;
            existing.PropertyDescription = dto.PropertyDescription;
            existing.SellerName = dto.SellerName;
            existing.SellerAddress = dto.SellerAddress;
            existing.SellerIdNumber = dto.SellerIdNumber;
            existing.SellerIdDate = dto.SellerIdDate;
            existing.SellerPhone = dto.SellerPhone;
            existing.BuyerName = dto.BuyerName;
            existing.BuyerAddress = dto.BuyerAddress;
            existing.BuyerIdNumber = dto.BuyerIdNumber;
            existing.BuyerIdDate = dto.BuyerIdDate;
            existing.BuyerPhone = dto.BuyerPhone;
            existing.TotalPrice = dto.TotalPrice;
            existing.TotalPriceInWords = dto.TotalPriceInWords;
            existing.DownPayment = dto.DownPayment;
            existing.AmountPaid = dto.AmountPaid;
            existing.RemainingAmount = dto.RemainingAmount;
            existing.PaymentMode = dto.PaymentMode;
            existing.DebtorParty = dto.DebtorParty;
            existing.DueDate = dto.DueDate;
            existing.WitnessOneName = dto.WitnessOneName;
            existing.WitnessTwoName = dto.WitnessTwoName;
            existing.Notes = dto.Notes;
            existing.Status = dto.Status;
            ApplyBase(existing, dto);
            await db.SaveChangesAsync(ct);
            map[dto.SyncId] = existing.Id;
        }

        return map;
    }

    private static async Task ApplyPaymentsAsync(
        RealEstateDbContext db,
        List<RealEstateContractPaymentSyncDto> dtos,
        Dictionary<Guid, int> contractMap,
        CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            if (!contractMap.TryGetValue(dto.ContractSyncId, out var contractId))
            {
                var contract = await db.RealEstateContracts.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.SyncId == dto.ContractSyncId, ct);
                if (contract is null) continue;
                contractId = contract.Id;
                contractMap[dto.ContractSyncId] = contractId;
            }

            var existing = await db.RealEstateContractPayments.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new RealEstateContractPayment { ContractId = contractId };
                db.RealEstateContractPayments.Add(existing);
            }

            existing.ContractId = contractId;
            existing.PaymentDate = dto.PaymentDate;
            existing.Amount = dto.Amount;
            existing.Notes = dto.Notes;
            existing.RemainingBefore = dto.RemainingBefore;
            existing.RemainingAfter = dto.RemainingAfter;
            ApplyBase(existing, dto);
        }
    }

    private static async Task ApplyClausesAsync(
        RealEstateDbContext db,
        List<RealEstateContractClauseSyncDto> dtos,
        Dictionary<Guid, int> contractMap,
        CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            if (!contractMap.TryGetValue(dto.ContractSyncId, out var contractId))
            {
                var contract = await db.RealEstateContracts.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.SyncId == dto.ContractSyncId, ct);
                if (contract is null) continue;
                contractId = contract.Id;
                contractMap[dto.ContractSyncId] = contractId;
            }

            var existing = await db.RealEstateContractClauses.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new RealEstateContractClause { ContractId = contractId };
                db.RealEstateContractClauses.Add(existing);
            }

            existing.ContractId = contractId;
            existing.SortOrder = dto.SortOrder;
            existing.Title = dto.Title;
            existing.Body = dto.Body;
            ApplyBase(existing, dto);
        }
    }

    private static async Task ApplyTemplatesAsync(
        RealEstateDbContext db,
        List<RealEstateClauseTemplateSyncDto> dtos,
        CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            var existing = await db.RealEstateClauseTemplates.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new RealEstateClauseTemplate();
                db.RealEstateClauseTemplates.Add(existing);
            }

            existing.SortOrder = dto.SortOrder;
            existing.Title = dto.Title;
            existing.Body = dto.Body;
            existing.IsActive = dto.IsActive;
            ApplyBase(existing, dto);
        }
    }

    private static async Task ApplyPartiesAsync(
        RealEstateDbContext db,
        List<RealEstatePartySyncDto> dtos,
        CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            var existing = await db.RealEstateParties.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new RealEstateParty();
                db.RealEstateParties.Add(existing);
            }

            existing.Name = dto.Name;
            existing.Phone = dto.Phone;
            existing.Address = dto.Address;
            existing.IdNumber = dto.IdNumber;
            existing.IdDate = dto.IdDate;
            existing.Notes = dto.Notes;
            ApplyBase(existing, dto);
        }
    }
}
