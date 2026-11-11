using System.IO;
using System.Text.Json;
using AlMuhasib.Core.Intelligence;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Services;

/// <summary>سجل محلي لكل مستخدم. لا يُكتب في قاعدة المحاسبة ولا يُشارك بين المستخدمين.</summary>
public sealed class AssistantHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<IReadOnlyList<AssistantMessage>> LoadAsync(int? userId, CancellationToken cancellationToken = default)
    {
        if (userId is null)
            return [];

        var path = PathFor(userId.Value);
        if (!File.Exists(path))
            return [];

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var stream = File.OpenRead(path);
            var items = await JsonSerializer.DeserializeAsync<List<StoredMessage>>(stream, JsonOptions, cancellationToken);
            return items?.Select(ToMessage).Where(message => message is not null).Cast<AssistantMessage>().ToList()
                   ?? [];
        }
        catch
        {
            return [];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(int? userId, IReadOnlyList<AssistantMessage> messages, CancellationToken cancellationToken = default)
    {
        if (userId is null)
            return;

        var payload = messages.TakeLast(40).Select(StoredMessage.From).ToList();
        var path = PathFor(userId.Value);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, payload, JsonOptions, cancellationToken);
        }
        catch
        {
            // السجل اختياري؛ فشل الكتابة لا يوقف المحادثة.
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Delete(int? userId)
    {
        if (userId is null)
            return;
        try
        {
            var path = PathFor(userId.Value);
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignore
        }
    }

    private static string PathFor(int userId)
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AlMuhasib", "assistant");
        return Path.Combine(root, userId + ".json");
    }

    private static AssistantMessage? ToMessage(StoredMessage stored)
    {
        var message = new AssistantMessage
        {
            Role = stored.Role,
            Content = stored.Content,
            DisplayContent = stored.Content,
            PlainText = stored.PlainText,
            RelatedQuestion = stored.RelatedQuestion,
            Timestamp = stored.Timestamp,
            IsError = stored.IsError,
            SourceLine = stored.SourceLine,
            Header1 = stored.Header1,
            Header2 = stored.Header2,
            Header3 = stored.Header3,
            Header4 = stored.Header4,
            ShowHeader2 = stored.ShowHeader2,
            ShowHeader3 = stored.ShowHeader3,
            ShowHeader4 = stored.ShowHeader4,
            Metrics = stored.Metrics,
            Rows = stored.Rows,
            ChartPoints = stored.Chart
        };
        message.AttachChart();
        return message;
    }

    private sealed class StoredMessage
    {
        public AssistantMessageRole Role { get; set; }
        public string Content { get; set; } = string.Empty;
        public string PlainText { get; set; } = string.Empty;
        public string? RelatedQuestion { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsError { get; set; }
        public string? SourceLine { get; set; }
        public string Header1 { get; set; } = string.Empty;
        public string Header2 { get; set; } = string.Empty;
        public string Header3 { get; set; } = string.Empty;
        public string Header4 { get; set; } = string.Empty;
        public bool ShowHeader2 { get; set; }
        public bool ShowHeader3 { get; set; }
        public bool ShowHeader4 { get; set; }
        public List<AssistantMetricCard> Metrics { get; set; } = [];
        public List<AssistantTableRow> Rows { get; set; } = [];
        public List<AssistantChartPoint> Chart { get; set; } = [];

        public static StoredMessage From(AssistantMessage message) => new()
        {
            Role = message.Role,
            Content = message.Content,
            PlainText = message.PlainText,
            RelatedQuestion = message.RelatedQuestion,
            Timestamp = message.Timestamp,
            IsError = message.IsError,
            SourceLine = message.SourceLine,
            Header1 = message.Header1,
            Header2 = message.Header2,
            Header3 = message.Header3,
            Header4 = message.Header4,
            ShowHeader2 = message.ShowHeader2,
            ShowHeader3 = message.ShowHeader3,
            ShowHeader4 = message.ShowHeader4,
            Metrics = message.Metrics,
            Rows = message.Rows,
            Chart = message.ChartPoints
        };
    }
}
