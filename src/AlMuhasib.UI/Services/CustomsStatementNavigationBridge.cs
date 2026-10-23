namespace AlMuhasib.UI.Services;

public static class CustomsStatementNavigationBridge
{
    public static int? PendingCustomerId { get; set; }

    public static int? TakePendingCustomerId()
    {
        var id = PendingCustomerId;
        PendingCustomerId = null;
        return id;
    }
}
