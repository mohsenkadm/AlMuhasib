using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class CapitalAdjustmentViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;

    public CapitalAdjustmentViewModel(IUnitOfWork unitOfWork, IExportService exportService, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "رأس المال والتسويات";
    }

    // ── Summary ──
    [ObservableProperty] private decimal _initialCapital;
    [ObservableProperty] private decimal _totalAdjustments;
    [ObservableProperty] private decimal _currentCapital;

    // ── History ──
    public ObservableCollection<CapitalEntryDisplay> Entries { get; } = [];

    // ── Add Adjustment Form ──
    [ObservableProperty] private decimal _adjustmentAmount;
    [ObservableProperty] private DateTime _adjustmentDate = DateTime.Today;
    [ObservableProperty] private string _adjustmentNotes = string.Empty;
    [ObservableProperty] private bool _isFormVisible;

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            var entries = (await _unitOfWork.CapitalEntries.GetAllAsync())
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.CreatedAt)
                .ToList();

            InitialCapital = entries.Where(e => e.Type == CapitalEntryType.Initial).Sum(e => e.Amount);
            TotalAdjustments = entries.Where(e => e.Type == CapitalEntryType.Adjustment).Sum(e => e.Amount);
            CurrentCapital = InitialCapital + TotalAdjustments;

            Entries.Clear();
            foreach (var e in entries)
            {
                Entries.Add(new CapitalEntryDisplay
                {
                    Id = e.Id,
                    Amount = e.Amount,
                    Date = e.Date,
                    TypeDisplay = e.Type == CapitalEntryType.Initial ? "رأس مال أولي" : "تسوية",
                    Type = e.Type,
                    Notes = e.Notes ?? "—",
                    CreatedBy = e.CreatedBy
                });
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void ShowForm()
    {
        AdjustmentAmount = 0;
        AdjustmentDate = DateTime.Today;
        AdjustmentNotes = string.Empty;
        IsFormVisible = true;
    }

    [RelayCommand]
    private void HideForm()
    {
        IsFormVisible = false;
    }

    [RelayCommand]
    private async Task SaveAdjustmentAsync()
    {
        if (AdjustmentAmount == 0)
        {
            BeautifulMessageDialog.ShowWarning("يرجى إدخال مبلغ التسوية (يمكن أن يكون سالباً)");
            return;
        }

        try
        {
            IsBusy = true;

            await _unitOfWork.CapitalEntries.AddAsync(new CapitalEntry
            {
                Amount = AdjustmentAmount,
                Date = AdjustmentDate,
                Type = CapitalEntryType.Adjustment,
                Notes = string.IsNullOrWhiteSpace(AdjustmentNotes) ? null : AdjustmentNotes.Trim()
            });
            await _unitOfWork.SaveChangesAsync();

            IsFormVisible = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (Entries.Count == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "رأس_المال_والتسويات.xlsx" };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "التاريخ", "النوع", "المبلغ", "الملاحظات", "بواسطة" };
        var rows = Entries.Select(e => new object[]
        {
            e.Date.ToString("yyyy/MM/dd"),
            e.TypeDisplay,
            e.Amount,
            e.Notes,
            e.CreatedBy
        }).ToList();

        _exportService.ExportToExcel(dlg.FileName, "رأس المال", cols, (IList<object[]>)rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void PrintCapital()
    {
        if (Entries.Count == 0) return;
        var cols = new[] { "التاريخ", "النوع", "المبلغ", "الملاحظات", "بواسطة" };
        var rows = Entries.Select(e => new object[]
        {
            e.Date.ToString("yyyy/MM/dd"),
            e.TypeDisplay,
            e.Amount.ToString("N0"),
            e.Notes,
            e.CreatedBy
        }).ToList();
        _exportService.PrintTable("رأس المال والتسويات", cols, (IList<object[]>)rows);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Capital");

        await LoadAsync();
    }
}

public class CapitalEntryDisplay
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string TypeDisplay { get; set; } = string.Empty;
    public CapitalEntryType Type { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}
