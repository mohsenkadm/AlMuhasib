using AlMuhasib.Core.Enums.Gold;

namespace AlMuhasib.Core.Entities.Gold;

public class GoldInvoiceLine : BaseEntity
{
    public int InvoiceId { get; set; }
    public GoldInvoice? Invoice { get; set; }
    public int? ItemId { get; set; }
    public int KaratValue { get; set; }
    public decimal WeightGrams { get; set; }
    public decimal MithqalPrice { get; set; }
    public decimal PricePerGram { get; set; }
    public decimal GoldValue { get; set; }
    public decimal MakingCharge { get; set; }
    public GoldMakingChargeMode MakingChargeMode { get; set; } = GoldMakingChargeMode.Fixed;
    /// <summary>Per-gram amount or percent-of-gold rate, depending on <see cref="MakingChargeMode"/>.</summary>
    public decimal MakingChargeRate { get; set; }
    public decimal LineTotal { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool WeightFromScale { get; set; }
    /// <summary>Default Out for sales; exchange uses In (stock+) and Out (stock-).</summary>
    public GoldInvoiceLineDirection LineDirection { get; set; } = GoldInvoiceLineDirection.Out;
}
