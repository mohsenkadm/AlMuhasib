using System.Collections.ObjectModel;
using System.IO;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class MainWindowViewModel
{
    private readonly ICustomerStatementQuickService _customerStatementQuick = null!;

    [ObservableProperty] private bool _isQuickStatementOpen;
    [ObservableProperty] private string _quickStatementCustomerName = string.Empty;
    [ObservableProperty] private decimal _quickStatementBalance;
    [ObservableProperty] private string _quickStatementOverdueText = string.Empty;

    public ObservableCollection<CustomerQuickStatementLine> QuickStatementLines { get; } = [];

    private int _quickStatementCustomerId;

    [RelayCommand]
    private void CloseQuickStatement() => IsQuickStatementOpen = false;

    public async Task OpenQuickStatementAsync(int customerId)
    {
        try
        {
            var data = await _customerStatementQuick.GetStatementAsync(customerId);
            _quickStatementCustomerId = customerId;
            QuickStatementCustomerName = data.CustomerName;
            QuickStatementBalance = data.Balance;
            QuickStatementOverdueText = data.OverdueInstallmentCount > 0
                ? $"{data.OverdueInstallmentCount} قسط متأخر — {data.OverdueInstallmentAmount:N0} د.ع"
                : "لا أقساط متأخرة";
            QuickStatementLines.Clear();
            foreach (var line in data.Lines.Take(50))
                QuickStatementLines.Add(line);
            IsQuickStatementOpen = true;
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void PrintQuickStatement()
    {
        if (_quickStatementCustomerId <= 0) return;
        _customerStatementQuick.Print(_quickStatementCustomerId);
    }

    [RelayCommand]
    private async Task ExportQuickStatementPdfAsync()
    {
        if (_quickStatementCustomerId <= 0) return;
        var dlg = new SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = $"كشف_{QuickStatementCustomerName}.xlsx"
        };
        if (dlg.ShowDialog() != true) return;
        await _customerStatementQuick.ExportToPdfAsync(_quickStatementCustomerId, dlg.FileName);
        _toast.ShowSuccess("تم التصدير");
    }

    [RelayCommand]
    private async Task ShareQuickStatementWhatsAppAsync()
    {
        if (_quickStatementCustomerId <= 0) return;
        var tempPath = Path.Combine(Path.GetTempPath(), $"كشف_{QuickStatementCustomerName}_{DateTime.Now:yyyyMMdd}.xlsx");
        await _customerStatementQuick.ExportToPdfAsync(_quickStatementCustomerId, tempPath);
        _toast.ShowInfo($"تم تجهيز الملف:\n{tempPath}\nأرسله للعميل عبر واتساب");
    }
}
