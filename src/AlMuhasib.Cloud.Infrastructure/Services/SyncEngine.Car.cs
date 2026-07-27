using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Sync.Dtos;
using AlMuhasib.Sync.Requests;
using AlMuhasib.Sync.Responses;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Services;

public sealed partial class SyncEngine
{
    private async Task<SyncPushResponse> PushCarAsync(int tenantId, SyncPushRequest request, CancellationToken ct)
    {
        var resolver = new SyncIdResolver(_db, tenantId);
        var response = new SyncPushResponse { ServerTime = DateTime.UtcNow };
        var accepted = 0;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var dto in request.Data.CarSaleContracts)
                accepted += await UpsertCarSaleContractAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.CarSaleContracts, tenantId, request.Data.CarSaleContracts.Select(c => c.SyncId), resolver, ct);

            foreach (var dto in request.Data.CarContractPayments)
            {
                var contractId = await resolver.ResolveCarSaleContractAsync(dto.ContractSyncId, ct);
                if (contractId is null)
                {
                    AddConflict(response, "CarContractPayment", dto.SyncId, "Contract not found");
                    continue;
                }
                accepted += await UpsertCarContractPaymentAsync(tenantId, dto, contractId.Value, response, ct);
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

    private async Task<SyncPullResponse> PullCarAsync(int tenantId, SyncPullRequest request, CancellationToken ct)
    {
        var since = request.Since ?? DateTime.MinValue;
        var bundle = new SyncDataBundle
        {
            CarSaleContracts = await PullEntitiesAsync(_db.CarSaleContracts, tenantId, since, MapCarSaleContract, ct)
        };

        var contractMap = await _db.CarSaleContracts.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);

        var payments = await _db.CarContractPayments.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.CarContractPayments = payments.Select(p => new CarContractPaymentSyncDto
        {
            SyncId = p.SyncId, CreatedAt = p.CreatedAt, CreatedBy = p.CreatedBy, UpdatedAt = p.UpdatedAt, UpdatedBy = p.UpdatedBy,
            IsDeleted = p.IsDeleted, DeletedAt = p.DeletedAt, DeletedBy = p.DeletedBy, RowVersion = p.RowVersion,
            ContractSyncId = contractMap.GetValueOrDefault(p.ContractId),
            PaymentDate = p.PaymentDate, Amount = p.Amount, Notes = p.Notes,
            RemainingBefore = p.RemainingBefore, RemainingAfter = p.RemainingAfter
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

    private static CarSaleContractSyncDto MapCarSaleContract(CloudCarSaleContract e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        ContractNumber = e.ContractNumber, ContractDate = e.ContractDate,
        SellerName = e.SellerName, SellerAddress = e.SellerAddress, SellerIdNumber = e.SellerIdNumber,
        SellerIdDate = e.SellerIdDate, SellerPhone = e.SellerPhone,
        BuyerName = e.BuyerName, BuyerAddress = e.BuyerAddress, BuyerIdNumber = e.BuyerIdNumber,
        BuyerIdDate = e.BuyerIdDate, BuyerPhone = e.BuyerPhone,
        AnnualOwnerName = e.AnnualOwnerName, AnnualOwnerAddress = e.AnnualOwnerAddress,
        PlateNumber = e.PlateNumber, CarType = e.CarType, CarModel = e.CarModel, CarColor = e.CarColor,
        ChassisNumber = e.ChassisNumber, CarPrice = e.CarPrice, IsAgreedPrice = e.IsAgreedPrice, CarPriceInWords = e.CarPriceInWords,
        AmountReceived = e.AmountReceived, RemainingAmount = e.RemainingAmount,
        WitnessOneName = e.WitnessOneName, WitnessTwoName = e.WitnessTwoName,
        Notes = e.Notes, Status = e.Status
    };

    private async Task<int> UpsertCarSaleContractAsync(int tenantId, CarSaleContractSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.CarSaleContracts, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "CarSaleContract", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudCarSaleContract { TenantId = tenantId }; _db.CarSaleContracts.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.ContractNumber = dto.ContractNumber;
        existing.ContractDate = dto.ContractDate;
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
        existing.AnnualOwnerName = dto.AnnualOwnerName;
        existing.AnnualOwnerAddress = dto.AnnualOwnerAddress;
        existing.PlateNumber = dto.PlateNumber;
        existing.CarType = dto.CarType;
        existing.CarModel = dto.CarModel;
        existing.CarColor = dto.CarColor;
        existing.ChassisNumber = dto.ChassisNumber;
        existing.CarPrice = dto.CarPrice;
        existing.IsAgreedPrice = dto.IsAgreedPrice;
        existing.CarPriceInWords = dto.CarPriceInWords;
        existing.AmountReceived = dto.AmountReceived;
        existing.RemainingAmount = dto.RemainingAmount;
        existing.WitnessOneName = dto.WitnessOneName;
        existing.WitnessTwoName = dto.WitnessTwoName;
        existing.Notes = dto.Notes;
        existing.Status = dto.Status;
        return 1;
    }

    private async Task<int> UpsertCarContractPaymentAsync(int tenantId, CarContractPaymentSyncDto dto, int contractId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.CarContractPayments, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "CarContractPayment", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudCarContractPayment { TenantId = tenantId }; _db.CarContractPayments.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.ContractId = contractId;
        existing.PaymentDate = dto.PaymentDate;
        existing.Amount = dto.Amount;
        existing.Notes = dto.Notes;
        existing.RemainingBefore = dto.RemainingBefore;
        existing.RemainingAfter = dto.RemainingAfter;
        return 1;
    }
}
