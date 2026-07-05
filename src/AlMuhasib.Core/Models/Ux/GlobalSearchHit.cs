namespace AlMuhasib.Core.Models.Ux;

public enum GlobalSearchKind
{
    Menu,
    Customer,
    Supplier,
    Product,
    SalesInvoice,
    PurchaseInvoice,
    Voucher,
    Installment,
    OverdueCustomer,
    HotelGuest,
    HotelRoom,
    HotelReservation
}

public class GlobalSearchHit
{
    public GlobalSearchKind Kind { get; init; }
    public int? EntityId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string? ScreenName { get; init; }
    public string? ViewModelTypeName { get; init; }
}
