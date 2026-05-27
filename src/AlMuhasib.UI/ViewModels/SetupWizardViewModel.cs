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

        ExpenseTypes.Add(new ExpenseTypeRow { Name = "إيجار" });
        ExpenseTypes.Add(new ExpenseTypeRow { Name = "كهرباء" });
        ExpenseTypes.Add(new ExpenseTypeRow { Name = "ماء" });
        ExpenseTypes.Add(new ExpenseTypeRow { Name = "إنترنت" });
        ExpenseTypes.Add(new ExpenseTypeRow { Name = "رواتب" });
        ExpenseTypes.Add(new ExpenseTypeRow { Name = "صيانة" });
        ExpenseTypes.Add(new ExpenseTypeRow { Name = "نقل" });
        ExpenseTypes.Add(new ExpenseTypeRow { Name = "مصاريف متنوعة" });
    }

    public event Action? SetupCompleted;

    [ObservableProperty] private int _currentStep;
    public int TotalSteps => 6;

    public bool IsStep0 => CurrentStep == 0;
    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool IsStep4 => CurrentStep == 4;
    public bool IsStep5 => CurrentStep == 5;
    public bool CanGoBack => CurrentStep > 0;
    public bool IsLastStep => CurrentStep == TotalSteps - 1;

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsStep0));
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
        OnPropertyChanged(nameof(IsStep4));
        OnPropertyChanged(nameof(IsStep5));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(StepTitle));
    }

    public string StepTitle => CurrentStep switch
    {
        0 => "١ - رأس المال والأرباح الافتتاحية",
        1 => "٢ - إنشاء القاصات",
        2 => "٣ - أرصدة المستثمرين الافتتاحية",
        3 => "٤ - إنشاء المخازن",
        4 => "٥ - الأرصدة الافتتاحية للمنتجات",
        5 => "٦ - أنواع المصاريف",
        _ => ""
    };

    // STEP 0: CAPITAL
    [ObservableProperty] private decimal _capitalAmount;
    [ObservableProperty] private DateTime _capitalDate = DateTime.Today;
    [ObservableProperty] private string _capitalNotes = string.Empty;
    [ObservableProperty] private decimal _profitOpeningBalance;

    // STEP 1: CASH BOXES
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

    // STEP 2: OPENING INVESTORS
    public ObservableCollection<SetupOpeningInvestorRow> OpeningInvestorItems { get; } = [];
    [ObservableProperty] private string _newInvestorName = string.Empty;
    [ObservableProperty] private string _newInvestorPhone = string.Empty;
    [ObservableProperty] private decimal _newInvestorProfitPercentage;
    [ObservableProperty] private decimal _newInvestorOpeningBalance;

    [RelayCommand]
    private void AddOpeningInvestorItem()
    {
        var name = NewInvestorName?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        if (NewInvestorOpeningBalance < 0)
        {
            BeautifulMessageDialog.ShowWarning("الرصيد الافتتاحي لا يمكن أن يكون سالباً");
            return;
        }

        OpeningInvestorItems.Add(new SetupOpeningInvestorRow
        {
            Name = name,
            Phone = NewInvestorPhone?.Trim() ?? string.Empty,
            ProfitPercentage = NewInvestorProfitPercentage,
            OpeningBalance = NewInvestorOpeningBalance
        });
        NewInvestorName = string.Empty;
        NewInvestorPhone = string.Empty;
        NewInvestorProfitPercentage = 0;
        NewInvestorOpeningBalance = 0;
    }

    [RelayCommand]
    private void RemoveOpeningInvestorItem(SetupOpeningInvestorRow? row)
    {
        if (row is not null) OpeningInvestorItems.Remove(row);
    }

    // STEP 3: WAREHOUSES
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

    // STEP 4: OPENING STOCK
    public ObservableCollection<SetupOpeningStockRow> OpeningStockItems { get; } = [];
    [ObservableProperty] private string _newProductName = string.Empty;
    [ObservableProperty] private decimal _newProductQuantity;
    [ObservableProperty] private decimal _newProductUnitCost;

    [RelayCommand]
    private void AddOpeningStockItem()
    {
        var name = NewProductName?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        if (NewProductQuantity <= 0)
        {
            BeautifulMessageDialog.ShowWarning("يرجى إدخال كمية أكبر من صفر");
            return;
        }
        if (NewProductUnitCost <= 0)
        {
            BeautifulMessageDialog.ShowWarning("يرجى إدخال كلفة الشراء للمنتج");
            return;
        }

        OpeningStockItems.Add(new SetupOpeningStockRow
        {
            ProductName = name,
            Quantity = NewProductQuantity,
            UnitCost = NewProductUnitCost
        });
        NewProductName = string.Empty;
        NewProductQuantity = 0;
        NewProductUnitCost = 0;
    }

    [RelayCommand]
    private void RemoveOpeningStockItem(SetupOpeningStockRow? row)
    {
        if (row is not null) OpeningStockItems.Remove(row);
    }

    // STEP 5: EXPENSE TYPES
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

    [RelayCommand]
    private void NextStep()
    {
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
        if (CurrentStep == 2 && OpeningInvestorItems.Count > 0 &&
            OpeningInvestorItems.Any(i => string.IsNullOrWhiteSpace(i.Name)))
        {
            BeautifulMessageDialog.ShowWarning("تأكد من إدخال اسم كل مستثمر");
            return;
        }
        if (CurrentStep == 3 && Warehouses.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("يرجى إضافة مخزن واحد على الأقل");
            return;
        }
        if (CurrentStep == 4 && OpeningStockItems.Count > 0 &&
            OpeningStockItems.Any(i => i.UnitCost <= 0 || i.Quantity <= 0))
        {
            BeautifulMessageDialog.ShowWarning("تأكد من إدخال الكمية وكلفة الشراء لكل منتج");
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

            await _unitOfWork.CapitalEntries.AddAsync(new CapitalEntry
            {
                Amount = CapitalAmount,
                Date = CapitalDate,
                Type = CapitalEntryType.Initial,
                Notes = string.IsNullOrWhiteSpace(CapitalNotes) ? "رأس المال الأولي" : CapitalNotes
            });

            if (ProfitOpeningBalance != 0)
            {
                await _unitOfWork.CapitalEntries.AddAsync(new CapitalEntry
                {
                    Amount = ProfitOpeningBalance,
                    Date = CapitalDate,
                    Type = CapitalEntryType.ProfitOpeningBalance,
                    Notes = "الرصيد الافتتاحي للأرباح"
                });
            }

            foreach (var cb in CashBoxes)
            {
                await _unitOfWork.CashBoxes.AddAsync(new CashBox
                {
                    Name = cb.Name,
                    Balance = cb.Balance
                });
            }

            foreach (var inv in OpeningInvestorItems)
            {
                var investor = new Investor
                {
                    Name = inv.Name,
                    Phone = string.IsNullOrWhiteSpace(inv.Phone) ? null : inv.Phone,
                    ProfitPercentage = inv.ProfitPercentage,
                    OpeningBalance = inv.OpeningBalance,
                    TotalDeposit = inv.OpeningBalance
                };
                await _unitOfWork.Investors.AddAsync(investor);
                await _unitOfWork.SaveChangesAsync();

                if (inv.OpeningBalance > 0)
                {
                    await _unitOfWork.InvestorTransactions.AddAsync(new InvestorTransaction
                    {
                        InvestorId = investor.Id,
                        Type = InvestorTransactionType.OpeningBalance,
                        Amount = inv.OpeningBalance,
                        Date = CapitalDate,
                        Notes = "رصيد افتتاحي — معالج الإعداد",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            var warehouseIds = new List<int>();
            foreach (var wh in Warehouses)
            {
                var entity = new Warehouse
                {
                    Name = wh.Name,
                    Location = string.IsNullOrWhiteSpace(wh.Location) ? null : wh.Location
                };
                await _unitOfWork.Warehouses.AddAsync(entity);
                await _unitOfWork.SaveChangesAsync();
                warehouseIds.Add(entity.Id);
            }

            if (OpeningStockItems.Count > 0 && warehouseIds.Count > 0)
            {
                var defaultCategory = (await _unitOfWork.Categories.FindAsync(c => c.Name == "عام"))
                    .FirstOrDefault();
                if (defaultCategory is null)
                {
                    defaultCategory = new Category { Name = "عام" };
                    await _unitOfWork.Categories.AddAsync(defaultCategory);
                    await _unitOfWork.SaveChangesAsync();
                }

                var warehouseId = warehouseIds[0];
                foreach (var item in OpeningStockItems)
                {
                    var product = new Product
                    {
                        Name = item.ProductName,
                        CategoryId = defaultCategory.Id
                    };
                    await _unitOfWork.Products.AddAsync(product);
                    await _unitOfWork.SaveChangesAsync();

                    await _unitOfWork.WarehouseStocks.AddAsync(new WarehouseStock
                    {
                        WarehouseId = warehouseId,
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        OpeningQuantity = item.Quantity,
                        UnitCost = item.UnitCost,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            foreach (var et in ExpenseTypes)
            {
                await _unitOfWork.ExpenseTypes.AddAsync(new ExpenseType { Name = et.Name });
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

public partial class SetupOpeningStockRow : ObservableObject
{
    [ObservableProperty] private string _productName = string.Empty;
    [ObservableProperty] private decimal _quantity;
    [ObservableProperty] private decimal _unitCost;
}

public partial class ExpenseTypeRow : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
}

public partial class SetupOpeningInvestorRow : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private decimal _profitPercentage;
    [ObservableProperty] private decimal _openingBalance;
}
