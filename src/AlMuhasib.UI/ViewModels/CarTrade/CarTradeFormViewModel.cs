using AlMuhasib.Core.Entities.CarTrade;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Services;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.UI.ViewModels.CarTrade;

public partial class CarTradeFormViewModel : ViewModelBase
{
    private readonly ICarTradeService _tradeService;
    private readonly ICarTradePrintService _printService;
    private readonly MainWindowViewModel _mainWindow;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private int? _editId;

    [ObservableProperty] private DateTime _transactionDate = DateTime.Today;
    [ObservableProperty] private string _transactionNumber = "يُنشأ تلقائياً";
    [ObservableProperty] private string _carName = string.Empty;
    [ObservableProperty] private string _carColor = string.Empty;
    [ObservableProperty] private string _plateNumber = string.Empty;
    [ObservableProperty] private string _chassisNumber = string.Empty;
    [ObservableProperty] private string _carType = string.Empty;
    [ObservableProperty] private string _sellerName = string.Empty;
    [ObservableProperty] private string _sellerPhone = string.Empty;
    [ObservableProperty] private decimal _purchasePrice;
    [ObservableProperty] private CarTradePaymentMode _paymentMode = CarTradePaymentMode.FullCash;
    [ObservableProperty] private decimal _amountPaid;
    [ObservableProperty] private decimal _remainingAmount;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private bool _isSaving;

    public bool IsEditMode => _editId.HasValue;
    public bool CanSave => IsEditMode ? CanEdit : CanAdd;
    public bool IsCash => PaymentMode == CarTradePaymentMode.FullCash;
    public bool IsCredit => PaymentMode == CarTradePaymentMode.Partial;

    public CarTradeFormViewModel(
        ICarTradeService tradeService,
        ICarTradePrintService printService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        MainWindowViewModel mainWindow)
    {
        _tradeService = tradeService;
        _printService = printService;
        _currentUserService = currentUserService;
        _toast = toast;
        _mainWindow = mainWindow;
        PageTitle = "شراء سيارة للمعرض";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CarTradePermissionRegistry.CarTradeForm);
        NotifyStateChanged();

        if (CarTradeNavigationBridge.PendingEditTransactionId is int editId)
        {
            CarTradeNavigationBridge.PendingEditTransactionId = null;
            await LoadTransactionAsync(editId);
        }
        else
        {
            ResetForNewTransaction();
        }
    }

    public async Task LoadTransactionAsync(int id)
    {
        var transaction = await _tradeService.GetByIdAsync(id);
        if (transaction is null)
        {
            _toast.ShowError("العملية غير موجودة");
            return;
        }

        if (transaction.IsSold)
        {
            _toast.ShowWarning("لا يمكن تعديل عملية شراء بعد بيع السيارة");
            return;
        }

        _editId = transaction.Id;
        PageTitle = "تعديل عملية شراء";
        TransactionDate = transaction.TransactionDate;
        TransactionNumber = transaction.TransactionNumber;
        CarName = transaction.CarName;
        CarColor = transaction.CarColor;
        PlateNumber = transaction.PlateNumber;
        ChassisNumber = transaction.ChassisNumber;
        CarType = transaction.CarType;
        SellerName = transaction.SellerName;
        SellerPhone = transaction.SellerPhone;
        PurchasePrice = transaction.PurchasePrice;
        PaymentMode = transaction.PaymentMode;
        AmountPaid = transaction.AmountPaid;
        RemainingAmount = transaction.RemainingAmount;
        Notes = transaction.Notes;
        NotifyStateChanged();
    }

    partial void OnPurchasePriceChanged(decimal value) => RecalculateAmounts();

    partial void OnPaymentModeChanged(CarTradePaymentMode value)
    {
        RecalculateAmounts();
        OnPropertyChanged(nameof(IsCash));
        OnPropertyChanged(nameof(IsCredit));
    }

    partial void OnAmountPaidChanged(decimal value) => RecalculateAmounts();

    private void RecalculateAmounts()
    {
        if (PaymentMode == CarTradePaymentMode.FullCash)
            AmountPaid = PurchasePrice;
        else if (AmountPaid > PurchasePrice)
            AmountPaid = PurchasePrice;

        RemainingAmount = Math.Max(0, PurchasePrice - AmountPaid);
    }

    [RelayCommand]
    private void SetCash()
    {
        PaymentMode = CarTradePaymentMode.FullCash;
        RecalculateAmounts();
    }

    [RelayCommand]
    private void SetCredit()
    {
        PaymentMode = CarTradePaymentMode.Partial;
        RecalculateAmounts();
    }

    [RelayCommand]
    private Task SaveAsync() => SaveInternalAsync(false);

    [RelayCommand]
    private Task SaveAndPrintAsync() => SaveInternalAsync(true);

    private async Task SaveInternalAsync(bool printAfterSave)
    {
        if (!Validate(out var error))
        {
            _toast.ShowWarning(error);
            return;
        }

        if (!CanSave)
        {
            _toast.ShowWarning("ليس لديك صلاحية لهذه العملية");
            return;
        }

        IsSaving = true;
        try
        {
            var entity = BuildEntity();
            CarTradeTransaction saved;
            if (_editId.HasValue)
            {
                entity.Id = _editId.Value;
                saved = await _tradeService.UpdateAsync(entity);
                _toast.ShowSuccess("تم حفظ العملية بنجاح");
            }
            else
            {
                saved = await _tradeService.CreateAsync(entity);
                _toast.ShowSuccess("تم حفظ عملية الشراء بنجاح");
                ResetForNewTransaction();
            }

            if (printAfterSave && CanPrint)
                _printService.PrintTransaction(saved);
        }
        catch (DbUpdateConcurrencyException)
        {
            _toast.ShowError("تعذر الحفظ — تم تعديل العملية من جهة أخرى. أعد تحميل البيانات وحاول مجدداً.");
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.InnerException?.Message ?? ex.Message);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void ResetForNewTransaction()
    {
        _editId = null;
        PageTitle = "شراء سيارة للمعرض";
        TransactionDate = DateTime.Today;
        TransactionNumber = "يُنشأ تلقائياً";
        CarName = string.Empty;
        CarColor = string.Empty;
        PlateNumber = string.Empty;
        ChassisNumber = string.Empty;
        CarType = string.Empty;
        SellerName = string.Empty;
        SellerPhone = string.Empty;
        PurchasePrice = 0;
        PaymentMode = CarTradePaymentMode.FullCash;
        AmountPaid = 0;
        RemainingAmount = 0;
        Notes = string.Empty;
        NotifyStateChanged();
    }

    [RelayCommand]
    private void Cancel() => _mainWindow.CloseTabForViewModel(this);

    private bool Validate(out string error)
    {
        if (string.IsNullOrWhiteSpace(CarName)) { error = "اسم السيارة مطلوب"; return false; }
        if (string.IsNullOrWhiteSpace(SellerName)) { error = "اسم البائع مطلوب"; return false; }
        if (PurchasePrice <= 0) { error = "سعر الشراء يجب أن يكون أكبر من صفر"; return false; }
        if (AmountPaid < 0) { error = "المبلغ المدفوع غير صالح"; return false; }
        if (AmountPaid > PurchasePrice) { error = "المبلغ المدفوع أكبر من سعر الشراء"; return false; }

        error = string.Empty;
        return true;
    }

    private CarTradeTransaction BuildEntity() => new()
    {
        TransactionDate = TransactionDate,
        TradeType = CarTradeType.Buy,
        CarName = CarName.Trim(),
        CarColor = CarColor.Trim(),
        PlateNumber = PlateNumber.Trim(),
        ChassisNumber = ChassisNumber.Trim(),
        CarType = CarType.Trim(),
        SellerName = SellerName.Trim(),
        SellerPhone = SellerPhone.Trim(),
        PurchasePrice = PurchasePrice,
        PaymentMode = PaymentMode,
        AmountPaid = AmountPaid,
        RemainingAmount = RemainingAmount,
        Notes = Notes.Trim()
    };

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(IsCash));
        OnPropertyChanged(nameof(IsCredit));
    }
}
