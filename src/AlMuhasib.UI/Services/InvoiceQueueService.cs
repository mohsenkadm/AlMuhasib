using System.IO;
using System.Text.Json;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Services;

public sealed class InvoiceQueueService : IInvoiceQueueService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly string _root;
    private readonly string _indexPath;
    private readonly string _itemsPath;

    public InvoiceQueueService()
    {
        _root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMuhasib",
            "invoice-queue");
        _itemsPath = Path.Combine(_root, "items");
        _indexPath = Path.Combine(_root, "index.json");
        Directory.CreateDirectory(_itemsPath);
    }

    public IReadOnlyList<InvoiceQueueItem> GetItems(InvoiceQueueKind kind) =>
        ReadIndex()
            .Where(x => x.Kind == kind)
            .OrderByDescending(x => x.SavedAt)
            .ToList();

    public void Enqueue<T>(InvoiceQueueKind kind, string name, T payload, int lineCount, decimal totalAmount) where T : class
    {
        var item = new InvoiceQueueItem
        {
            Kind = kind,
            Name = string.IsNullOrWhiteSpace(name) ? "فاتورة بانتظار الإكمال" : name.Trim(),
            SavedAt = DateTime.Now,
            LineCount = Math.Max(0, lineCount),
            TotalAmount = totalAmount < 0 ? 0 : totalAmount
        };

        var payloadPath = Path.Combine(_itemsPath, $"{item.Id}.json");
        File.WriteAllText(payloadPath, JsonSerializer.Serialize(payload, JsonOptions));

        var index = ReadIndex();
        index.Add(item);
        WriteIndex(index);
    }

    public T? Load<T>(string id) where T : class
    {
        var path = Path.Combine(_itemsPath, $"{id}.json");
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Remove(string id)
    {
        var index = ReadIndex();
        var item = index.FirstOrDefault(x => x.Id == id);
        if (item is not null)
        {
            index.Remove(item);
            WriteIndex(index);
        }

        var path = Path.Combine(_itemsPath, $"{id}.json");
        if (File.Exists(path))
            File.Delete(path);
    }

    private List<InvoiceQueueItem> ReadIndex()
    {
        if (!File.Exists(_indexPath)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<InvoiceQueueItem>>(File.ReadAllText(_indexPath), JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void WriteIndex(List<InvoiceQueueItem> items)
    {
        File.WriteAllText(_indexPath, JsonSerializer.Serialize(items, JsonOptions));
    }
}
