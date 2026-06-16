using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Core.Models.Car;

public class CarContractFilter
{
    public string? SearchText { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public CarContractStatusFilter StatusFilter { get; set; } = CarContractStatusFilter.All;
    public bool UnpaidOnly { get; set; }
}

public enum CarContractStatusFilter
{
    All,
    Active,
    Completed,
    Cancelled
}

public class CarContractListItem
{
    public int Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string CarType { get; set; } = string.Empty;
    public string CarModel { get; set; } = string.Empty;
    public string ChassisNumber { get; set; } = string.Empty;
    public decimal CarPrice { get; set; }
    public decimal AmountReceived { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class CarContractDashboardStats
{
    public int TodayContracts { get; set; }
    public int MonthContracts { get; set; }
    public int TotalContracts { get; set; }
    public int UnpaidContracts { get; set; }
    public decimal TotalCarValue { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalRemaining { get; set; }
    public List<NameCountPoint> MonthlyContracts { get; set; } = [];
    public List<NameAmountPoint> PaymentStatusChart { get; set; } = [];
    public List<NameCountPoint> TopSellers { get; set; } = [];
    public List<NameCountPoint> TopBuyers { get; set; } = [];
    public List<CarContractListItem> RecentContracts { get; set; } = [];
}

public class CarContractReportData
{
    public List<CarContractListItem> Rows { get; set; } = [];
    public decimal TotalCarValue { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalRemaining { get; set; }
    public List<NameCountPoint> MonthlyContracts { get; set; } = [];
    public List<NameAmountPoint> CollectedVsRemaining { get; set; } = [];
    public List<NameCountPoint> ByCarType { get; set; } = [];
}

public class NameCountPoint
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
