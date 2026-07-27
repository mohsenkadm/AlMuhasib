namespace AlMuhasib.Sync;

public static class SyncEntityOrder
{
    public static readonly SyncEntityType[] PushOrder =
    [
        SyncEntityType.Category,
        SyncEntityType.Product,
        SyncEntityType.PricingType,
        SyncEntityType.ProductPrice,
        SyncEntityType.BusinessSettings,
        SyncEntityType.Warehouse,
        SyncEntityType.Customer,
        SyncEntityType.Supplier,
        SyncEntityType.CashBox,
        SyncEntityType.BankAccount,
        SyncEntityType.Investor,
        SyncEntityType.ExpenseType,
        SyncEntityType.PrintBrandingSettings,
        SyncEntityType.WarehouseStock,
        SyncEntityType.WarehouseTransfer,
        SyncEntityType.WarehouseTransferItem,
        SyncEntityType.Invoice,
        SyncEntityType.InvoiceItem,
        SyncEntityType.InstallmentPlan,
        SyncEntityType.Installment,
        SyncEntityType.Voucher,
        SyncEntityType.Expense,
        SyncEntityType.Transfer,
        SyncEntityType.InvestorTransaction,
        SyncEntityType.ProfitDistribution,
        SyncEntityType.ProfitDistributionDetail,
        SyncEntityType.CapitalEntry,
        SyncEntityType.CustomerAttachment
    ];
}
