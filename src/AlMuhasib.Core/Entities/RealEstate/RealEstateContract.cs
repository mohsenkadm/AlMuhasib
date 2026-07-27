using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities.RealEstate;

public class RealEstateContract : BaseEntity
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

    public ICollection<RealEstateContractPayment> Payments { get; set; } = [];
    public ICollection<RealEstateContractClause> Clauses { get; set; } = [];
}
