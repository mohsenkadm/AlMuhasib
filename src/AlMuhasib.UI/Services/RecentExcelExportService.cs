using System.IO;
using System.Text.Json;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.UI.Services;

public sealed class RecentExcelExportService : IRecentExcelExportService
{
    private const int MaxEntries = 50;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;
    private readonly List<RecentExcelExportEntry> _entries = [];

    public event Action? ExportsChanged;

    public RecentExcelExportService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMuhasib");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "recent-excel-exports.json");
        Load();
    }

    public void RecordExport(string filePath, string sheetName)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        var normalizedPath = Path.GetFullPath(filePath);
        _entries.RemoveAll(e =>
            string.Equals(e.FilePath, normalizedPath, StringComparison.OrdinalIgnoreCase));

        _entries.Insert(0, new RecentExcelExportEntry
        {
            FilePath = normalizedPath,
            FileName = Path.GetFileName(normalizedPath),
            SheetName = string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName,
            ExportedAt = DateTime.Now
        });

        if (_entries.Count > MaxEntries)
            _entries.RemoveRange(MaxEntries, _entries.Count - MaxEntries);

        Save();
        ExportsChanged?.Invoke();
    }

    public IReadOnlyList<RecentExcelExportEntry> GetRecent(int count = 50) =>
        _entries.Take(Math.Max(1, count)).ToList();

    public void Remove(string id)
    {
        _entries.RemoveAll(e => e.Id == id);
        Save();
        ExportsChanged?.Invoke();
    }

    public void Clear()
    {
        _entries.Clear();
        Save();
        ExportsChanged?.Invoke();
    }

    private void Load()
    {
        if (!File.Exists(_path))
            return;

        try
        {
            var json = File.ReadAllText(_path);
            var loaded = JsonSerializer.Deserialize<List<RecentExcelExportEntry>>(json, JsonOptions);
            if (loaded is null)
                return;

            _entries.Clear();
            _entries.AddRange(loaded
                .Where(e => !string.IsNullOrWhiteSpace(e.FilePath))
                .OrderByDescending(e => e.ExportedAt)
                .Take(MaxEntries));
        }
        catch
        {
            _entries.Clear();
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_entries, JsonOptions);
            File.WriteAllText(_path, json);
        }
        catch
        {
            // ignore persistence failures
        }
    }
}
