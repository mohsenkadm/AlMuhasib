using AlMuhasib.Core.Enums;

namespace AlMuhasib.Sync.Dtos;

public sealed class RealEstateContractSyncDto : SyncDtoBase
{
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; } = DateTime.Today;
    public RealEstateContractType ContractType { get; set; } = RealEstateContractType.Sale;
    public RealEstatePropertyType PropertyType { get; set; } = RealEstatePropertyType.House;
    public string PropertyLocation { get; set; } = string.Empty;
    public string PropertyAddress { get; set; } = string.Empty;
    public decimal PropertyAreaSqm { get; set; }
    public string PropertyDescription { get; set; } = string.Empty;

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

    public decimal TotalPrice { get; set; }
    public string TotalPriceInWords { get; set; } = string.Empty;
    public decimal DownPayment { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }

    public RealEstatePaymentMode PaymentMode { get; set; } = RealEstatePaymentMode.Cash;
    public RealEstateDebtorParty DebtorParty { get; set; } = RealEstateDebtorParty.None;
    public DateTime? DueDate { get; set; }

    public string WitnessOneName { get; set; } = string.Empty;
    public string WitnessTwoName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public RealEstateContractStatus Status { get; set; } = RealEstateContractStatus.Active;
}

public sealed class RealEstateContractPaymentSyncDto : SyncDtoBase
{
    public Guid ContractSyncId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public decimal RemainingBefore { get; set; }
    public decimal RemainingAfter { get; set; }
}

public sealed class RealEstateContractClauseSyncDto : SyncDtoBase
{
    public Guid ContractSyncId { get; set; }
    public int SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public sealed class RealEstateClauseTemplateSyncDto : SyncDtoBase
{
    public int SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class RealEstatePartySyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public DateTime? IdDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class RealEstateExpenseTypeSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class RealEstateExpenseSyncDto : SyncDtoBase
{
    public Guid ExpenseTypeSyncId { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public Guid? RelatedContractSyncId { get; set; }
}
