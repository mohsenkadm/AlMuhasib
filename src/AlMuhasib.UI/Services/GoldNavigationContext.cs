namespace AlMuhasib.UI.Services;

/// <summary>Pass party selection into gold statement screens after navigation.</summary>
public static class GoldNavigationContext
{
    public static int? PendingCustomerId { get; set; }
    public static int? PendingSupplierId { get; set; }

    public static int? TakePendingCustomerId()
    {
        var id = PendingCustomerId;
        PendingCustomerId = null;
        return id;
    }

    public static int? TakePendingSupplierId()
    {
        var id = PendingSupplierId;
        PendingSupplierId = null;
        return id;
    }
}
