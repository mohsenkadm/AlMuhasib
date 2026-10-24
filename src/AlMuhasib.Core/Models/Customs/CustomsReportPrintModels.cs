using AlMuhasib.Core.Enums.Customs;

namespace AlMuhasib.Core.Models.Customs;

/// <summary>
/// Shared Customs staff/list report form (تقارير وقوائم التخليص).
/// Branding header from print settings is applied by the document/PDF builders.
/// </summary>
public sealed class CustomsReportPrintData
{
    public string Title { get; set; } = "تقرير التخليص الكمركي";
    public string Subtitle { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime PrintedAt { get; set; } = DateTime.Now;
    public string PartyName { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public string[] Columns { get; set; } = [];
    public IList<object[]> Rows { get; set; } = [];
    public IList<string> SummaryLines { get; set; } = [];
    public CustomsPrintDocumentKind? Kind { get; set; }
    public bool UseLandscape { get; set; } = true;
}
