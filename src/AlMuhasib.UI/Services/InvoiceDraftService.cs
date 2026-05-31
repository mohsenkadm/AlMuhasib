using System.IO;
using System.Text.Json;

namespace AlMuhasib.UI.Services;

public sealed class InvoiceDraftService : IInvoiceDraftService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _draftsFolder;

    public InvoiceDraftService()
    {
        _draftsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMuhasib", "drafts");
        Directory.CreateDirectory(_draftsFolder);
    }

    private string PathFor(string draftKey) => Path.Combine(_draftsFolder, $"{Sanitize(draftKey)}.json");

    private static string Sanitize(string key) =>
        string.Concat(key.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

    public void SaveDraft<T>(string draftKey, T draft) where T : class
    {
        var envelope = new DraftEnvelope<T>
        {
            SavedAt = DateTime.UtcNow,
            Payload = draft
        };
        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        File.WriteAllText(PathFor(draftKey), json);
    }

    public T? LoadDraft<T>(string draftKey) where T : class
    {
        var path = PathFor(draftKey);
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            var envelope = JsonSerializer.Deserialize<DraftEnvelope<T>>(json, JsonOptions);
            return envelope?.Payload;
        }
        catch
        {
            return null;
        }
    }

    public void ClearDraft(string draftKey)
    {
        var path = PathFor(draftKey);
        if (File.Exists(path))
            File.Delete(path);
    }

    public bool HasDraft(string draftKey) => File.Exists(PathFor(draftKey));

    public DateTime? GetDraftSavedAt(string draftKey)
    {
        var path = PathFor(draftKey);
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("savedAt", out var savedAt))
                return savedAt.GetDateTime().ToLocalTime();
        }
        catch { /* ignore */ }
        return File.GetLastWriteTime(path);
    }

    private sealed class DraftEnvelope<T>
    {
        public DateTime SavedAt { get; set; }
        public T? Payload { get; set; }
    }
}
