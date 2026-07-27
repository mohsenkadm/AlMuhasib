using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Sync.Dtos;
using AlMuhasib.Sync.Requests;
using AlMuhasib.Sync.Responses;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Services;

public sealed partial class SyncEngine
{
    private async Task<SyncPushResponse> PushRealEstateAsync(int tenantId, SyncPushRequest request, CancellationToken ct)
    {
        var resolver = new SyncIdResolver(_db, tenantId);
        var response = new SyncPushResponse { ServerTime = DateTime.UtcNow };
        var accepted = 0;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var dto in request.Data.RealEstateParties)
                accepted += await UpsertRealEstatePartyAsync(tenantId, dto, response, ct);

            foreach (var dto in request.Data.RealEstateClauseTemplates)
                accepted += await UpsertRealEstateClauseTemplateAsync(tenantId, dto, response, ct);

            foreach (var dto in request.Data.RealEstateContracts)
                accepted += await UpsertRealEstateContractAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.RealEstateContracts, tenantId, request.Data.RealEstateContracts.Select(c => c.SyncId), resolver, ct);

            foreach (var dto in request.Data.RealEstateContractPayments)
            {
                var contractId = await resolver.ResolveRealEstateContractAsync(dto.ContractSyncId, ct);
                if (contractId is null)
                {
                    AddConflict(response, "RealEstateContractPayment", dto.SyncId, "Contract not found");
                    continue;
                }
                accepted += await UpsertRealEstateContractPaymentAsync(tenantId, dto, contractId.Value, response, ct);
            }

            foreach (var dto in request.Data.RealEstateContractClauses)
            {
                var contractId = await resolver.ResolveRealEstateContractAsync(dto.ContractSyncId, ct);
                if (contractId is null)
                {
                    AddConflict(response, "RealEstateContractClause", dto.SyncId, "Contract not found");
                    continue;
                }
                accepted += await UpsertRealEstateContractClauseAsync(tenantId, dto, contractId.Value, response, ct);
            }

            var tenant = await _db.Tenants.FindAsync([tenantId], ct);
            if (tenant is not null)
                tenant.LastSyncAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            response.AcceptedCount = accepted;
            response.RejectedCount = response.Conflicts.Count;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        return response;
    }

    private async Task<SyncPullResponse> PullRealEstateAsync(int tenantId, SyncPullRequest request, CancellationToken ct)
    {
        var since = request.Since ?? DateTime.MinValue;
        var bundle = new SyncDataBundle
        {
            RealEstateContracts = await PullEntitiesAsync(_db.RealEstateContracts, tenantId, since, MapRealEstateContract, ct),
            RealEstateClauseTemplates = await PullEntitiesAsync(_db.RealEstateClauseTemplates, tenantId, since, MapRealEstateClauseTemplate, ct),
            RealEstateParties = await PullEntitiesAsync(_db.RealEstateParties, tenantId, since, MapRealEstateParty, ct)
        };

        var contractMap = await _db.RealEstateContracts.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);

        var payments = await _db.RealEstateContractPayments.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.RealEstateContractPayments = payments.Select(p => new RealEstateContractPaymentSyncDto
        {
            SyncId = p.SyncId, CreatedAt = p.CreatedAt, CreatedBy = p.CreatedBy, UpdatedAt = p.UpdatedAt, UpdatedBy = p.UpdatedBy,
            IsDeleted = p.IsDeleted, DeletedAt = p.DeletedAt, DeletedBy = p.DeletedBy, RowVersion = p.RowVersion,
            ContractSyncId = contractMap.GetValueOrDefault(p.ContractId),
            PaymentDate = p.PaymentDate, Amount = p.Amount, Notes = p.Notes,
            RemainingBefore = p.RemainingBefore, RemainingAfter = p.RemainingAfter
        }).ToList();

        var clauses = await _db.RealEstateContractClauses.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.RealEstateContractClauses = clauses.Select(c => new RealEstateContractClauseSyncDto
        {
            SyncId = c.SyncId, CreatedAt = c.CreatedAt, CreatedBy = c.CreatedBy, UpdatedAt = c.UpdatedAt, UpdatedBy = c.UpdatedBy,
            IsDeleted = c.IsDeleted, DeletedAt = c.DeletedAt, DeletedBy = c.DeletedBy, RowVersion = c.RowVersion,
            ContractSyncId = contractMap.GetValueOrDefault(c.ContractId),
            SortOrder = c.SortOrder, Title = c.Title, Body = c.Body
        }).ToList();

        var serverTime = DateTime.UtcNow;
        return new SyncPullResponse
        {
            Data = bundle,
            Cursor = serverTime.Ticks.ToString(),
            ServerTime = serverTime,
            HasMore = false
        };
    }

    private static RealEstateContractSyncDto MapRealEstateContract(CloudRealEstateContract e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        ContractNumber = e.ContractNumber, ContractDate = e.ContractDate,
        ContractType = e.ContractType, PropertyType = e.PropertyType,
        PropertyLocation = e.PropertyLocation, PropertyAddress = e.PropertyAddress,
        PropertyAreaSqm = e.PropertyAreaSqm, PropertyDescription = e.PropertyDescription,
        SellerName = e.SellerName, SellerAddress = e.SellerAddress, SellerIdNumber = e.SellerIdNumber,
        SellerIdDate = e.SellerIdDate, SellerPhone = e.SellerPhone,
        BuyerName = e.BuyerName, BuyerAddress = e.BuyerAddress, BuyerIdNumber = e.BuyerIdNumber,
        BuyerIdDate = e.BuyerIdDate, BuyerPhone = e.BuyerPhone,
        TotalPrice = e.TotalPrice, TotalPriceInWords = e.TotalPriceInWords,
        DownPayment = e.DownPayment, AmountPaid = e.AmountPaid, RemainingAmount = e.RemainingAmount,
        PaymentMode = e.PaymentMode, DebtorParty = e.DebtorParty, DueDate = e.DueDate,
        WitnessOneName = e.WitnessOneName, WitnessTwoName = e.WitnessTwoName,
        Notes = e.Notes, Status = e.Status
    };

    private static RealEstateClauseTemplateSyncDto MapRealEstateClauseTemplate(CloudRealEstateClauseTemplate e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        SortOrder = e.SortOrder, Title = e.Title, Body = e.Body, IsActive = e.IsActive
    };

    private static RealEstatePartySyncDto MapRealEstateParty(CloudRealEstateParty e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, Phone = e.Phone, Address = e.Address, IdNumber = e.IdNumber, IdDate = e.IdDate, Notes = e.Notes
    };

    private async Task<int> UpsertRealEstateContractAsync(int tenantId, RealEstateContractSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.RealEstateContracts, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "RealEstateContract", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudRealEstateContract { TenantId = tenantId }; _db.RealEstateContracts.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
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
        return 1;
    }

    private async Task<int> UpsertRealEstateContractPaymentAsync(int tenantId, RealEstateContractPaymentSyncDto dto, int contractId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.RealEstateContractPayments, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "RealEstateContractPayment", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudRealEstateContractPayment { TenantId = tenantId }; _db.RealEstateContractPayments.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.ContractId = contractId;
        existing.PaymentDate = dto.PaymentDate;
        existing.Amount = dto.Amount;
        existing.Notes = dto.Notes;
        existing.RemainingBefore = dto.RemainingBefore;
        existing.RemainingAfter = dto.RemainingAfter;
        return 1;
    }

    private async Task<int> UpsertRealEstateContractClauseAsync(int tenantId, RealEstateContractClauseSyncDto dto, int contractId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.RealEstateContractClauses, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "RealEstateContractClause", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudRealEstateContractClause { TenantId = tenantId }; _db.RealEstateContractClauses.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.ContractId = contractId;
        existing.SortOrder = dto.SortOrder;
        existing.Title = dto.Title;
        existing.Body = dto.Body;
        return 1;
    }

    private async Task<int> UpsertRealEstateClauseTemplateAsync(int tenantId, RealEstateClauseTemplateSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.RealEstateClauseTemplates, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "RealEstateClauseTemplate", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudRealEstateClauseTemplate { TenantId = tenantId }; _db.RealEstateClauseTemplates.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.SortOrder = dto.SortOrder;
        existing.Title = dto.Title;
        existing.Body = dto.Body;
        existing.IsActive = dto.IsActive;
        return 1;
    }

    private async Task<int> UpsertRealEstatePartyAsync(int tenantId, RealEstatePartySyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.RealEstateParties, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "RealEstateParty", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudRealEstateParty { TenantId = tenantId }; _db.RealEstateParties.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name;
        existing.Phone = dto.Phone;
        existing.Address = dto.Address;
        existing.IdNumber = dto.IdNumber;
        existing.IdDate = dto.IdDate;
        existing.Notes = dto.Notes;
        return 1;
    }
}
