namespace Qaid.TelegramBot.Application.Reports;

public sealed class ReportDocumentModel
{
    public required string Title { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string PeriodLabel { get; init; } = string.Empty;
    public string GeneratedAtLabel { get; init; } = string.Empty;
    public IReadOnlyList<ReportSummaryItem> Summary { get; init; } = [];
    public IReadOnlyList<ReportTableModel> Tables { get; init; } = [];
}

public sealed class ReportSummaryItem
{
    public required string Label { get; init; }
    public required string Value { get; init; }
}

public sealed class ReportTableModel
{
    public string Title { get; init; } = string.Empty;
    public required IReadOnlyList<string> Headers { get; init; }
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
}

public sealed class ReportPdfFile
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
    public required string Caption { get; init; }
}
