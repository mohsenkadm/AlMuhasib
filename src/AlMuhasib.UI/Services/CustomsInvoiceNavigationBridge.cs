using AlMuhasib.Core.Enums.Customs;

namespace AlMuhasib.UI.Services;

public static class CustomsInvoiceNavigationBridge
{
    public static int? PendingEditInvoiceId { get; set; }

    /// <summary>Preset service type when opening a new invoice form (dashboard / quick actions).</summary>
    public static CustomsServiceType? PendingServiceType { get; set; }

    /// <summary>
    /// Optional focus hint after opening an existing invoice:
    /// "expenses" | "attachments" | "vehicles" — UI-only banner, no backend change.
    /// </summary>
    public static string? PendingFocusSection { get; set; }

    public static int? TakePendingEditInvoiceId()
    {
        var id = PendingEditInvoiceId;
        PendingEditInvoiceId = null;
        return id;
    }

    public static CustomsServiceType? TakePendingServiceType()
    {
        var value = PendingServiceType;
        PendingServiceType = null;
        return value;
    }

    public static string? TakePendingFocusSection()
    {
        var value = PendingFocusSection;
        PendingFocusSection = null;
        return value;
    }
}
