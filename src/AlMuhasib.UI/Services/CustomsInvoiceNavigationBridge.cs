namespace AlMuhasib.UI.Services;

public static class CustomsInvoiceNavigationBridge
{
    public static int? PendingEditInvoiceId { get; set; }

    public static int? TakePendingEditInvoiceId()
    {
        var id = PendingEditInvoiceId;
        PendingEditInvoiceId = null;
        return id;
    }
}
