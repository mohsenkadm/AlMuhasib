using AlMuhasib.Core.Intelligence;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Services;

/// <summary>يربط نافذة المحادثة بمحرك قيد الداخلي. لا يستدعي أي نموذج خارجي.</summary>
public sealed class QaidAssistantService : IAssistantService
{
    private readonly IQaidIntelligenceEngine _engine;

    public QaidAssistantService(IQaidIntelligenceEngine engine) => _engine = engine;

    public bool IsLiveServiceEnabled => true;

    public string StatusDescription => "متصل ببيانات النظام";

    public async Task<AssistantMessage> GetReplyAsync(
        string userMessage,
        IReadOnlyList<AssistantMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        var answer = await _engine.AskAsync(userMessage, cancellationToken);
        return Map(answer, userMessage);
    }

    public void ResetSession() => _engine.ResetSession();

    public static AssistantMessage Map(AssistantAnswer answer, string? relatedQuestion)
    {
        var headers = answer.TableHeaders;
        var message = new AssistantMessage
        {
            Role = AssistantMessageRole.Assistant,
            Content = answer.Text,
            PlainText = string.IsNullOrWhiteSpace(answer.PlainText) ? answer.Text : answer.PlainText,
            RelatedQuestion = relatedQuestion,
            Timestamp = DateTime.Now,
            IsError = answer.IsError,
            SourceLine = BuildSource(answer),
            Header1 = Header(headers, 0),
            Header2 = Header(headers, 1),
            Header3 = Header(headers, 2),
            Header4 = Header(headers, 3),
            ShowHeader2 = headers.Count > 1,
            ShowHeader3 = headers.Count > 2,
            ShowHeader4 = headers.Count > 3,
            Metrics = answer.Metrics,
            Rows = answer.TableRows.Take(15).Select(row => new AssistantTableRow
            {
                Col1 = Cell(row, 0),
                Col2 = Cell(row, 1),
                Col3 = Cell(row, 2),
                Col4 = Cell(row, 3),
                Show2 = row.Count > 1,
                Show3 = row.Count > 2,
                Show4 = row.Count > 3
            }).ToList(),
            ChartPoints = answer.Chart
        };
        message.AttachChart();
        return message;
    }

    private static string BuildSource(AssistantAnswer answer)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(answer.SourceReport))
            parts.Add(answer.SourceReport);
        if (!string.IsNullOrWhiteSpace(answer.PeriodLabel))
            parts.Add(answer.PeriodLabel);
        if (!string.IsNullOrWhiteSpace(answer.BranchLabel))
            parts.Add(answer.BranchLabel);
        if (!string.IsNullOrWhiteSpace(answer.ConnectionLabel))
            parts.Add(answer.ConnectionLabel);
        if (answer.UpdatesContext)
            parts.Add("استُخرجت " + answer.ExtractedAt.ToString("yyyy/MM/dd HH:mm"));
        return string.Join(" · ", parts);
    }

    private static string Header(IReadOnlyList<string> headers, int index) =>
        index < headers.Count ? headers[index] : string.Empty;

    private static string Cell(IReadOnlyList<string> row, int index) =>
        index < row.Count ? row[index] : string.Empty;
}
