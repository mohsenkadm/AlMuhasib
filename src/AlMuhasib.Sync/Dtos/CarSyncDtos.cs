using AlMuhasib.Core.Enums;

namespace AlMuhasib.Sync.Dtos;

public sealed class CarSaleContractSyncDto : SyncDtoBase
{
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; } = DateTime.Today;

    public string SellerName { get; set; } = string.Empty;
    public string SellerAddress { get; set; } = string.Empty;
    public string SellerIdNumber { get; set; } = string.Empty;
    public DateTime? SellerIdDate { get; set; }
    public string SellerPhone { get; set; } = string.Empty;

    public string BuyerName { get; set; } = string.Empty;
    public string BuyerAddress { get; set; } = string.Empty;
    public string BuyerIdNumber { get; set; } = string.Empty;
    public DateTime? BuyerIdDate { get; set; }
    public string BuyerPhone { get; set; } = string.Empty;

    public string AnnualOwnerName { get; set; } = string.Empty;
    public string AnnualOwnerAddress { get; set; } = string.Empty;

    public string PlateNumber { get; set; } = string.Empty;
    public string CarType { get; set; } = string.Empty;
    public string CarModel { get; set; } = string.Empty;
    public string CarColor { get; set; } = string.Empty;
    public string ChassisNumber { get; set; } = string.Empty;

    public decimal CarPrice { get; set; }
    public bool IsAgreedPrice { get; set; }
    public string CarPriceInWords { get; set; } = string.Empty;
    public decimal AmountReceived { get; set; }
    public decimal RemainingAmount { get; set; }

    public string WitnessOneName { get; set; } = string.Empty;
    public string WitnessTwoName { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
    public CarContractStatus Status { get; set; } = CarContractStatus.Active;
}

public sealed class CarContractPaymentSyncDto : SyncDtoBase
{
    public Guid ContractSyncId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public decimal RemainingBefore { get; set; }
    public decimal RemainingAfter { get; set; }
}
