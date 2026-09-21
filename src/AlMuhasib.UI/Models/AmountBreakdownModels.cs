namespace AlMuhasib.UI.Models;

public sealed class AmountBreakdownModel
{
    public string Title { get; init; } = "تفاصيل المبلغ";
    public string Subtitle { get; init; } = string.Empty;
    public string Formula { get; init; } = string.Empty;
    public decimal ResultAmount { get; init; }
    public string ResultLabel { get; init; } = "النتيجة";
    public string? Note { get; init; }
    public IReadOnlyList<AmountBreakdownLine> Lines { get; init; } = [];
    public string? EntitiesTitle { get; init; }
    public IReadOnlyList<AmountBreakdownEntityRow> Entities { get; init; } = [];
}

public sealed class AmountBreakdownLine
{
    public string Label { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Operator { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsResult { get; init; }
}

public sealed class AmountBreakdownEntityRow
{
    public string Name { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public decimal Amount { get; init; }
    public bool ShowAmount { get; init; } = true;
}
