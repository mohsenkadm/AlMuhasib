namespace AlMuhasib.UI.Services;

/// <summary>Pass customer selection into the Customs customer file tab after navigation.</summary>
public static class CustomsNavigationContext
{
    public static int? PendingCustomerId { get; set; }

    public static int? TakePendingCustomerId()
    {
        var id = PendingCustomerId;
        PendingCustomerId = null;
        return id;
    }
}
