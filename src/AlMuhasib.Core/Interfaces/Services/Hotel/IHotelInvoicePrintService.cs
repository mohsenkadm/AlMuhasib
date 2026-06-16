using AlMuhasib.Core.Entities.Hotel;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IHotelInvoicePrintService
{
    void PrintReservationInvoice(Reservation reservation, int copies = 1);
}
