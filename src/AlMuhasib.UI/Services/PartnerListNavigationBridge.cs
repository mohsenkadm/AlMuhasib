namespace AlMuhasib.UI.Services;

/// <summary>Deep-link from dashboard quick actions into customer/supplier add dialogs.</summary>
public static class PartnerListNavigationBridge
{
    public static bool PendingOpenAddForCustomers { get; set; }
    public static bool PendingOpenAddForSuppliers { get; set; }
}
