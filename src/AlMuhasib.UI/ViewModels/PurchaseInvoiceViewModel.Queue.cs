using System.Collections.ObjectModel;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class PurchaseInvoiceViewModel
{
    private readonly IInvoiceQueueService _queueService;

    [ObservableProperty] private bool _isQueuePickerOpen;
    [ObservableProperty] private string _queueSearchText = string.Empty;
    [ObservableProperty] private bool _queueSortNewestFirst = true;
    public ObservableCollection<InvoiceQueueItem> QueueItems { get; } = [];

    [RelayCommand]
    private void AddToQueue()
    {
        if (IsSaved)
        {
            BeautifulMessageDialog.ShowWarning("لا يمكن نقل فاتورة محفوظة إلى قائمة الانتظار.");
            return;
        }

        var hasLines = Items.Any(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0);
        if (!hasLines)
        {
            BeautifulMessageDialog.ShowWarning("أضف بنداً واحداً على الأقل قبل الإرسال إلى قائمة الانتظار.");
            return;
        }

        var queueName = string.IsNullOrWhiteSpace(SupplierSearchText)
            ? $"مشتريات - {DateTime.Now:HH:mm}"
            : $"مشتريات {SupplierSearchText.Trim()} - {DateTime.Now:HH:mm}";

        _queueService.Enqueue(
            InvoiceQueueKind.Purchase,
            queueName,
            BuildDraft(),
            Items.Count(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0),
            GrandTotal);
        BeautifulMessageDialog.ShowSuccess("تمت إضافة الفاتورة إلى قائمة الانتظار.");
        _ = NewInvoice();
    }

    [RelayCommand]
    private void OpenQueuePicker()
    {
        QueueSearchText = string.Empty;
        QueueSortNewestFirst = true;
        RefreshQueueItems();
        IsQueuePickerOpen = true;
    }

    [RelayCommand]
    private void CloseQueuePicker() => IsQueuePickerOpen = false;

    [RelayCommand]
    private void LoadFromQueue(InvoiceQueueItem? item)
    {
        if (item is null) return;
        var draft = _queueService.Load<PurchaseInvoiceDraft>(item.Id);
        if (draft is null)
        {
            BeautifulMessageDialog.ShowWarning("تعذر تحميل الفاتورة من قائمة الانتظار.");
            _queueService.Remove(item.Id);
            RefreshQueueItems();
            return;
        }

        ApplyDraft(draft);
        _queueService.Remove(item.Id);
        RefreshQueueItems();
        IsQueuePickerOpen = false;
        BeautifulMessageDialog.ShowSuccess("تم تحميل الفاتورة من قائمة الانتظار.");
    }

    [RelayCommand]
    private void DeleteFromQueue(InvoiceQueueItem? item)
    {
        if (item is null) return;
        _queueService.Remove(item.Id);
        RefreshQueueItems();
    }

    partial void OnQueueSearchTextChanged(string value) => RefreshQueueItems();
    partial void OnQueueSortNewestFirstChanged(bool value) => RefreshQueueItems();

    private void RefreshQueueItems()
    {
        QueueItems.Clear();
        IEnumerable<InvoiceQueueItem> items = _queueService.GetItems(InvoiceQueueKind.Purchase);
        if (!string.IsNullOrWhiteSpace(QueueSearchText))
        {
            var term = QueueSearchText.Trim();
            items = items.Where(x => x.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        items = QueueSortNewestFirst
            ? items.OrderByDescending(x => x.SavedAt)
            : items.OrderBy(x => x.SavedAt);

        foreach (var item in items)
            QueueItems.Add(item);
    }
}
