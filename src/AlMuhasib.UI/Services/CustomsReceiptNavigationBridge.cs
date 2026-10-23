namespace AlMuhasib.UI.Services;

public static class CustomsReceiptNavigationBridge
{
    public static int? PendingEditReceiptId { get; set; }

    public static int? TakePendingEditReceiptId()
    {
        var id = PendingEditReceiptId;
        PendingEditReceiptId = null;
        return id;
    }
}
