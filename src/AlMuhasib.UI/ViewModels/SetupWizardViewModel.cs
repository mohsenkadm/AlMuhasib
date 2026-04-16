using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class SetupWizardViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;

    public SetupWizardViewModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        PageTitle = "إعداد النظام";

        // Pre-populate common expense types
        ExpenseTypes.Add(new ExpenseTypeRow { Name = "إيجار" });
        ExpenseTypes.Add(new ExpenseTypeRow { Name = "كهرباء" });
        ExpenseTypes.Add(new ExpenseTypeRow { Name = "ماء" });
        ExpenseTypes.Add(new ExpenseTypeRow { Name = "إنترنت" });
        ExpenseTypes.Add(new ExpenseTypeRow { Name = "رواتب" });
        ExpenseTypes.Add(new ExpenseTypeRow { Name = "صيانة" });
        ExpenseTypes.Add(new ExpenseTypeRow { Name = "نقل" });
        ExpenseTypes.Add(new ExpenseTypeRow { Name = "مصاريف متنوعة" });
    }

    /// <summary>Raised when setup completes successfully.</summary>
    public event Action? SetupCompleted;

    // ── Step tracking ──
    [ObservableProperty] private int _currentStep; // 0-3
    public int TotalSteps => 4;

    public bool IsStep0 => CurrentStep == 0;
    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool CanGoBack => CurrentStep > 0;
    public bool IsLastStep => CurrentStep == TotalSteps - 1;

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsStep0));
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(StepTitle));
    }

    public string StepTitle => CurrentStep switch
    {
        0 => "١ - إدخال رأس المال الأولي",
        1 => "٢ - إنشاء القاصات",
        2 => "٣ - إنشاء المخازن",
        3 => "٤ - أنواع المصاريف",
        _ => ""
    };

    // ══════════════════════════════════════════════════════
    // STEP 0: CAPITAL
    // ══════════════════════════════════════════════════════
    [ObservableProperty] private decimal _capitalAmount;
    [ObservableProperty] private DateTime _capitalDate = DateTime.Today;
    [ObservableProperty] private string _capitalNotes = string.Empty;

    // ══════════════════════════════════════════════════════
    // STEP 1: CASH BOXES
    // ══════════════════════════════════════════════════════
    public ObservableCollection<CashBoxRow> CashBoxes { get; } = [];
    [ObservableProperty] private string _newCashBoxName = string.Empty;
    [ObservableProperty] private decimal _newCashBoxBalance;

    [RelayCommand]
    private void AddCashBox()
    {
        var name = NewCashBoxName?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        CashBoxes.Add(new CashBoxRow { Name = name, Balance = NewCashBoxBalance });
        NewCashBoxName = string.Empty;
        NewCashBoxBalance = 0;
    }

    [RelayCommand]
    private void RemoveCashBox(CashBoxRow? row)
    {
        if (row is not null) CashBoxes.Remove(row);
    }

    // ══════════════════════════════════════════════════════
    // STEP 2: WAREHOUSES
    // ══════════════════════════════════════════════════════
    public ObservableCollection<WarehouseRow> Warehouses { get; } = [];
    [ObservableProperty] private string _newWarehouseName = string.Empty;
    [ObservableProperty] private string _newWarehouseLocation = string.Empty;

    [RelayCommand]
    private void AddWarehouse()
    {
        var name = NewWarehouseName?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        Warehouses.Add(new WarehouseRow { Name = name, Location = NewWarehouseLocation?.Trim() ?? "" });
        NewWarehouseName = string.Empty;
        NewWarehouseLocation = string.Empty;
    }

    [RelayCommand]
    private void RemoveWarehouse(WarehouseRow? row)
    {
        if (row is not null) Warehouses.Remove(row);
    }

    // ══════════════════════════════════════════════════════
    // STEP 3: EXPENSE TYPES
    // ══════════════════════════════════════════════════════
    public ObservableCollection<ExpenseTypeRow> ExpenseTypes { get; } = [];
    [ObservableProperty] private string _newExpenseTypeName = string.Empty;

    [RelayCommand]
    private void AddExpenseType()
    {
        var name = NewExpenseTypeName?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        ExpenseTypes.Add(new ExpenseTypeRow { Name = name });
        NewExpenseTypeName = string.Empty;
    }

    [RelayCommand]
    private void RemoveExpenseType(ExpenseTypeRow? row)
    {
        if (row is not null) ExpenseTypes.Remove(row);
    }

    // ══════════════════════════════════════════════════════
    // NAVIGATION
    // ══════════════════════════════════════════════════════
    [RelayCommand]
    private void NextStep()
    {
        // Validate current step
        if (CurrentStep == 0 && CapitalAmount <= 0)
        {
            BeautifulMessageDialog.ShowWarning("يرجى إدخال مبلغ رأس المال");
            return;
        }
        if (CurrentStep == 1 && CashBoxes.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("يرجى إضافة قاصة واحدة على الأقل");
            return;
        }
        if (CurrentStep == 2 && Warehouses.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("يرجى إضافة مخزن واحد على الأقل");
            return;
        }

        if (CurrentStep < TotalSteps - 1)
            CurrentStep++;
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep > 0)
            CurrentStep--;
    }

    [RelayCommand]
    private async Task FinishAsync()
    {
        if (ExpenseTypes.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("يرجى إضافة نوع مصاريف واحد على الأقل");
            return;
        }

        try
        {
            IsBusy = true;
            await _unitOfWork.BeginTransactionAsync();

            // 1. Capital entry
            await _unitOfWork.CapitalEntries.AddAsync(new CapitalEntry
            {
                Amount = CapitalAmount,
                Date = CapitalDate,
                Type = CapitalEntryType.Initial,
                Notes = string.IsNullOrWhiteSpace(CapitalNotes) ? "رأس المال الأولي" : CapitalNotes
            });

            // 2. Cash boxes
            foreach (var cb in CashBoxes)
            {
                await _unitOfWork.CashBoxes.AddAsync(new CashBox
                {
                    Name = cb.Name,
                    Balance = cb.Balance
                });
            }

            // 3. Warehouses
            foreach (var wh in Warehouses)
            {
                await _unitOfWork.Warehouses.AddAsync(new Warehouse
                {
                    Name = wh.Name,
                    Location = string.IsNullOrWhiteSpace(wh.Location) ? null : wh.Location
                });
            }

            // 4. Expense types
            foreach (var et in ExpenseTypes)
            {
                await _unitOfWork.ExpenseTypes.AddAsync(new ExpenseType
                {
                    Name = et.Name
                });
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            SetupCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الإعداد: {ex.Message}");
        }
        finally { IsBusy = false; }
    }
}

// ── Row models ──

public partial class CashBoxRow : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private decimal _balance;
}

public partial class WarehouseRow : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _location = string.Empty;
}

public partial class ExpenseTypeRow : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
}
