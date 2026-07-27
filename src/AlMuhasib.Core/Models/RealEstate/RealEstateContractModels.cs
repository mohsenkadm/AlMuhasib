using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Core.Models.RealEstate;

public class RealEstateContractFilter
{
    public string? SearchText { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public RealEstateContractStatusFilter StatusFilter { get; set; } = RealEstateContractStatusFilter.All;
    public RealEstateContractType? ContractType { get; set; }
    public RealEstatePropertyType? PropertyType { get; set; }
    public RealEstatePaymentMode? PaymentMode { get; set; }
    public bool UnpaidOnly { get; set; }
    public bool CreditOnly { get; set; }
}

public enum RealEstateContractStatusFilter
{
    All,
    Active,
    Completed,
    Cancelled
}

public class RealEstateContractListItem
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; }
    public string ContractType { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public string PropertyLocation { get; set; } = string.Empty;
    public decimal PropertyAreaSqm { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string DebtorParty { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class RealEstateContractDashboardStats
{
    public int TodayContracts { get; set; }
    public int MonthContracts { get; set; }
    public int TotalContracts { get; set; }
    public int UnpaidContracts { get; set; }
    public int OverdueDebts { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalRemaining { get; set; }
    public List<NameCountPoint> MonthlyContracts { get; set; } = [];
    public List<NameAmountPoint> PaymentStatusChart { get; set; } = [];
    public List<NameCountPoint> ByContractType { get; set; } = [];
    public List<NameCountPoint> ByPropertyType { get; set; } = [];
    public List<RealEstateContractListItem> RecentContracts { get; set; } = [];
}

public class RealEstateContractReportData
{
    public List<RealEstateContractListItem> Rows { get; set; } = [];
    public decimal TotalValue { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalRemaining { get; set; }
    public List<NameCountPoint> MonthlyContracts { get; set; } = [];
    public List<NameAmountPoint> CollectedVsRemaining { get; set; } = [];
    public List<NameCountPoint> ByPropertyType { get; set; } = [];
    public List<NameCountPoint> ByContractType { get; set; } = [];
}

public class RealEstateDebtItem
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; }
    public string DebtorName { get; set; } = string.Empty;
    public string DebtorPhone { get; set; } = string.Empty;
    public string DebtorParty { get; set; } = string.Empty;
    public string CounterpartyName { get; set; } = string.Empty;
    public decimal RemainingAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysOverdue { get; set; }
}

public class RealEstatePartyListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public decimal TotalDebt { get; set; }
    public int ContractCount { get; set; }
}

public class NameCountPoint
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
