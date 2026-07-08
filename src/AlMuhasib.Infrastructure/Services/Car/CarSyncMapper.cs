using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Entities.Car;
using AlMuhasib.Infrastructure.Data.Car;
using AlMuhasib.Sync.Dtos;
using AlMuhasib.Sync.Requests;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Car;

internal static class CarSyncMapper
{
    public static async Task<SyncDataBundle> BuildPushBundleAsync(
        CarDbContext db,
        DateTime? since,
        CancellationToken ct)
    {
        var cutoff = since ?? DateTime.MinValue;
        bool ShouldSync(BaseEntity e) =>
            (e.UpdatedAt ?? e.CreatedAt) >= cutoff
            || (e.IsDeleted && (e.DeletedAt ?? e.UpdatedAt ?? e.CreatedAt) >= cutoff);

        var contracts = await db.CarSaleContracts.IgnoreQueryFilters()
            .Include(c => c.Payments)
            .ToListAsync(ct);

        var contractMap = contracts.ToDictionary(c => c.Id, c => c.SyncId);
        var changedContracts = contracts.Where(ShouldSync).ToList();
        var changedPayments = contracts
            .SelectMany(c => c.Payments)
            .Where(ShouldSync)
            .ToList();

        return new SyncDataBundle
        {
            CarSaleContracts = changedContracts.Select(MapContract).ToList(),
            CarContractPayments = changedPayments
                .Where(p => contractMap.ContainsKey(p.ContractId))
                .Select(p => MapPayment(p, contractMap))
                .ToList()
        };
    }

    public static async Task ApplyPullBundleAsync(CarDbContext db, SyncDataBundle data, CancellationToken ct)
    {
        db.IsApplyingSyncPull = true;
        try
        {
            var contractMap = await ApplyContractsAsync(db, data.CarSaleContracts, ct);
            await ApplyPaymentsAsync(db, data.CarContractPayments, contractMap, ct);
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

    private static CarSaleContractSyncDto MapContract(CarSaleContract c)
    {
        var dto = new CarSaleContractSyncDto
        {
            ContractNumber = c.ContractNumber,
            ContractDate = c.ContractDate,
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
            AnnualOwnerName = c.AnnualOwnerName,
            AnnualOwnerAddress = c.AnnualOwnerAddress,
            PlateNumber = c.PlateNumber,
            CarType = c.CarType,
            CarModel = c.CarModel,
            CarColor = c.CarColor,
            ChassisNumber = c.ChassisNumber,
            CarPrice = c.CarPrice,
            IsAgreedPrice = c.IsAgreedPrice,
            CarPriceInWords = c.CarPriceInWords,
            AmountReceived = c.AmountReceived,
            RemainingAmount = c.RemainingAmount,
            WitnessOneName = c.WitnessOneName,
            WitnessTwoName = c.WitnessTwoName,
            Notes = c.Notes,
            Status = c.Status
        };
        CopyBase(c, dto);
        return dto;
    }

    private static CarContractPaymentSyncDto MapPayment(
        CarContractPayment p,
        Dictionary<int, Guid> contractMap) => new()
    {
        ContractSyncId = contractMap[p.ContractId],
        PaymentDate = p.PaymentDate,
        Amount = p.Amount,
        Notes = p.Notes,
        RemainingBefore = p.RemainingBefore,
        RemainingAfter = p.RemainingAfter,
        SyncId = p.SyncId,
        CreatedAt = p.CreatedAt,
        CreatedBy = p.CreatedBy,
        UpdatedAt = p.UpdatedAt,
        UpdatedBy = p.UpdatedBy,
        IsDeleted = p.IsDeleted,
        DeletedAt = p.DeletedAt,
        DeletedBy = p.DeletedBy,
        RowVersion = p.RowVersion
    };

    private static async Task<Dictionary<Guid, int>> ApplyContractsAsync(
        CarDbContext db,
        List<CarSaleContractSyncDto> dtos,
        CancellationToken ct)
    {
        var map = new Dictionary<Guid, int>();
        foreach (var dto in dtos)
        {
            var existing = await db.CarSaleContracts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new CarSaleContract();
                db.CarSaleContracts.Add(existing);
            }

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
            ApplyBase(existing, dto);
            await db.SaveChangesAsync(ct);
            map[dto.SyncId] = existing.Id;
        }

        return map;
    }

    private static async Task ApplyPaymentsAsync(
        CarDbContext db,
        List<CarContractPaymentSyncDto> dtos,
        Dictionary<Guid, int> contractMap,
        CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            if (!contractMap.TryGetValue(dto.ContractSyncId, out var contractId))
                continue;

            var existing = await db.CarContractPayments.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.SyncId == dto.SyncId, ct);
            if (existing is null)
            {
                existing = new CarContractPayment { ContractId = contractId };
                db.CarContractPayments.Add(existing);
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
}
