namespace AlMuhasib.UI.Services;

/// <summary>UI navigation bridge for Customs customers list (open add dialog).</summary>
public static class CustomsCustomersNavigationBridge
{
    public static bool PendingOpenAdd { get; set; }

    public static bool TakePendingOpenAdd()
    {
        var value = PendingOpenAdd;
        PendingOpenAdd = false;
        return value;
    }
}
