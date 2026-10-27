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
    /// <summary>Delay penalty (غرامات تأخيرية) — used on internal transport invoices.</summary>
    public decimal DelayPenaltyAmount { get; set; }

    /// <summary>External transport destination (الوجهة).</summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>Clearance daily-form fee columns (per vehicle row).</summary>
    public decimal SealAmount { get; set; }
    public decimal RadiationAmount { get; set; }
    public decimal DutyAmount { get; set; }
    public decimal QualityControlAmount { get; set; }
    public decimal LandTransportAmount { get; set; }
    public decimal ClearanceFeeAmount { get; set; }
    public decimal ExpensesAmount { get; set; }

    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
    public CustomsVehicleStatus Status { get; set; } = CustomsVehicleStatus.InProgress;
    public string Notes { get; set; } = string.Empty;
}
