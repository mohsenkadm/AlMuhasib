using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Shared.Services;

public static class InstallmentPrintHelpers
{
    public static string StatusToArabic(InstallmentStatus status) => status switch
    {
        InstallmentStatus.Paid => "مسدد",
        InstallmentStatus.PartiallyPaid => "مسدد جزئياً",
        InstallmentStatus.Overdue => "متأخر",
        InstallmentStatus.Pending => "معلق",
        _ => status.ToString()
    };

    public static InstallmentPrintRow ToPrintRow(Installment inst, int number)
    {
        var remaining = inst.RemainingAmount > 0 ? inst.RemainingAmount : Math.Max(0, inst.Amount - inst.PaidAmount);
        int? delayDays = null;
        if (inst.Status is InstallmentStatus.Overdue or InstallmentStatus.Pending or InstallmentStatus.PartiallyPaid
            && inst.DueDate.Date < DateTime.Today && remaining > 0)
            delayDays = (DateTime.Today - inst.DueDate.Date).Days;

        return new InstallmentPrintRow
        {
            Number = number,
            DueDate = inst.DueDate,
            Amount = inst.Amount,
            PaidAmount = inst.PaidAmount,
            RemainingAmount = remaining,
            PaymentDate = inst.PaymentDate,
            StatusText = StatusToArabic(inst.Status),
            DelayDays = delayDays
        };
    }

    public static List<InstallmentPrintRow> ToPrintRows(IEnumerable<Installment> installments) =>
        installments
            .OrderBy(i => i.DueDate)
            .Select((inst, idx) => ToPrintRow(inst, idx + 1))
            .ToList();

    public static string InstallmentTypeLabel(InstallmentType type) => type switch
    {
        InstallmentType.Platform => "بيع منصة",
        InstallmentType.OpeningBalance => "رصيد افتتاحي",
        _ => "يدوي"
    };
}
