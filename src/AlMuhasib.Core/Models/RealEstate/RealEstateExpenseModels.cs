using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Core.Models.RealEstate;

public class RealEstateExpenseFilter
{
    public string? SearchText { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? ExpenseTypeId { get; set; }
}

public class RealEstateExpenseListItem
{
    public int Id { get; set; }
    public DateTime ExpenseDate { get; set; }
    public int ExpenseTypeId { get; set; }
    public string ExpenseTypeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int? RelatedContractId { get; set; }
    public string RelatedContractNumber { get; set; } = string.Empty;
}

public class RealEstateProfitReportData
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }

    public int SaleContractsCount { get; set; }
    public int PurchaseContractsCount { get; set; }
    public int ExpenseCount { get; set; }

    public decimal SaleRevenue { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitMarginPercent { get; set; }

    public decimal CashInFromSales { get; set; }
    public decimal CashOutOnPurchases { get; set; }
    public decimal CashExpenses { get; set; }
    public decimal NetCash { get; set; }

    public decimal SaleReceivables { get; set; }
    public decimal PurchasePayables { get; set; }

    public List<NameAmountPoint> ExpensesByType { get; set; } = [];
    public List<RealEstateMonthlyProfitPoint> MonthlySeries { get; set; } = [];
    public List<RealEstateProfitContractRow> SaleRows { get; set; } = [];
    public List<RealEstateProfitContractRow> PurchaseRows { get; set; } = [];
    public List<RealEstateExpenseListItem> ExpenseRows { get; set; } = [];
}

public class RealEstateMonthlyProfitPoint
{
    public string Period { get; set; } = string.Empty;
    public decimal SaleRevenue { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal Expenses { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal NetProfit { get; set; }
}

public class RealEstateProfitContractRow
{
    public int Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; }
    public string ContractType { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public string PropertyLocation { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
}
