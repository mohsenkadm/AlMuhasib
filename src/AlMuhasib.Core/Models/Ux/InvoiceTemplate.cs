using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Models.Ux;

public enum InvoiceTemplateKind
{
    Sale,
    Purchase,
    Installment
}

public class InvoiceTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public InvoiceTemplateKind Kind { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public int? WarehouseId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public DateTime? CreditDueDate { get; set; }
    public int? CashBoxId { get; set; }
    public string? Notes { get; set; }
    public List<InvoiceTemplateLine> Lines { get; set; } = [];
    public DateTime SavedAt { get; set; } = DateTime.Now;

    /// <summary>إعدادات فاتورة الأقساط (عند Kind = Installment).</summary>
    public InstallmentType? InstallmentType { get; set; }
    public int? NumberOfInstallments { get; set; }
  /// <summary>بداية الأقساط بعد كم شهراً من تاريخ التحميل.</summary>
    public int InstallmentStartMonthsOffset { get; set; } = 1;
    public string? FileNumber { get; set; }

    /// <summary>قالب مدمج في النظام (لا يُحذف).</summary>
    public bool IsBuiltIn { get; set; }

    public string LineCountText => Lines.Count > 0
        ? $"{Lines.Count} بند"
        : NumberOfInstallments is > 0
            ? $"{NumberOfInstallments} قسط"
            : "بدون بنود";

    public string InstallmentSummaryText =>
        Kind != InvoiceTemplateKind.Installment || NumberOfInstallments is null or <= 0
            ? string.Empty
            : $"{NumberOfInstallments} قسط — {(InstallmentType == global::AlMuhasib.Core.Enums.InstallmentType.Platform ? "منصة" : "يدوي")}";
}

public class InvoiceTemplateLine
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
