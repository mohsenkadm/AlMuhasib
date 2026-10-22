using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;

namespace AlMuhasib.Core.Entities.Customs;

public class CustomsVehicle : BaseEntity
{
    public int InvoiceId { get; set; }
    public CustomsInvoice Invoice { get; set; } = null!;

    public string SerialNumber { get; set; } = string.Empty;
    public string IraqiPlate { get; set; } = string.Empty;
    public string IraqiDriver { get; set; } = string.Empty;
    public string SyrianPlate { get; set; } = string.Empty;
    public string SyrianDriver { get; set; } = string.Empty;
    public string CargoType { get; set; } = string.Empty;
    public DateTime? LoadingDate { get; set; }
    public DateTime? ExitDate { get; set; }
    public DateTime? WarehouseArrivalDate { get; set; }
    public DateTime? UnloadingDate { get; set; }
    public decimal VehiclePrice { get; set; }
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
    public CustomsVehicleStatus Status { get; set; } = CustomsVehicleStatus.InProgress;
    public string Notes { get; set; } = string.Empty;
}
