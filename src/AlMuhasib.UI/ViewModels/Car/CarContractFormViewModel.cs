using AlMuhasib.Core.Entities.Car;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Utilities;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.UI.ViewModels.Car;

public partial class CarContractFormViewModel : ViewModelBase
{
    private readonly ICarContractService _contractService;
    private readonly ICarContractPrintService _printService;
    private readonly MainWindowViewModel _mainWindow;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private int? _editId;

    [ObservableProperty] private DateTime _contractDate = DateTime.Today;
    [ObservableProperty] private string _contractNumber = "يُنشأ تلقائياً";
    [ObservableProperty] private string _sellerName = string.Empty;
    [ObservableProperty] private string _sellerAddress = string.Empty;
    [ObservableProperty] private string _sellerIdNumber = string.Empty;
    [ObservableProperty] private DateTime? _sellerIdDate = DateTime.Today;
    [ObservableProperty] private string _sellerPhone = string.Empty;
    [ObservableProperty] private string _buyerName = string.Empty;
    [ObservableProperty] private string _buyerAddress = string.Empty;
    [ObservableProperty] private string _buyerIdNumber = string.Empty;
    [ObservableProperty] private DateTime? _buyerIdDate = DateTime.Today;
    [ObservableProperty] private string _buyerPhone = string.Empty;
    [ObservableProperty] private string _annualOwnerName = string.Empty;
    [ObservableProperty] private string _annualOwnerAddress = string.Empty;
    [ObservableProperty] private string _plateNumber = string.Empty;
    [ObservableProperty] private string _carType = string.Empty;
    [ObservableProperty] private string _carModel = string.Empty;
    [ObservableProperty] private string _carColor = string.Empty;
    [ObservableProperty] private string _chassisNumber = string.Empty;
    [ObservableProperty] private decimal _carPrice;
    [ObservableProperty] private string _carPriceInWords = string.Empty;
    [ObservableProperty] private decimal _amountReceived;
    [ObservableProperty] private decimal _remainingAmount;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private bool _isSaving;

    public bool IsEditMode => _editId.HasValue;
    public bool CanSave => IsEditMode ? CanEdit : CanAdd;

    public CarContractFormViewModel(
        ICarContractService contractService,
        ICarContractPrintService printService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        MainWindowViewModel mainWindow)
    {
        _contractService = contractService;
        _printService = printService;
        _currentUserService = currentUserService;
        _toast = toast;
        _mainWindow = mainWindow;
        PageTitle = "عقد جديد";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CarPermissionRegistry.CarContractForm);
        NotifySaveStateChanged();

        if (CarContractNavigationBridge.PendingEditContractId is int editId)
        {
            CarContractNavigationBridge.PendingEditContractId = null;
            await LoadContractAsync(editId);
        }
        else
        {
            ResetForNewContract();
        }
    }

    public async Task LoadContractAsync(int id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract is null)
        {
            _toast.ShowError("العقد غير موجود");
            return;
        }

        _editId = contract.Id;
        PageTitle = "تعديل عقد";
        ContractDate = contract.ContractDate;
        ContractNumber = contract.ContractNumber;
        SellerName = contract.SellerName;
        SellerAddress = contract.SellerAddress;
        SellerIdNumber = contract.SellerIdNumber;
        SellerIdDate = contract.SellerIdDate ?? DateTime.Today;
        SellerPhone = contract.SellerPhone;
        BuyerName = contract.BuyerName;
        BuyerAddress = contract.BuyerAddress;
        BuyerIdNumber = contract.BuyerIdNumber;
        BuyerIdDate = contract.BuyerIdDate ?? DateTime.Today;
        BuyerPhone = contract.BuyerPhone;
        AnnualOwnerName = contract.AnnualOwnerName;
        AnnualOwnerAddress = contract.AnnualOwnerAddress;
        PlateNumber = contract.PlateNumber;
        CarType = contract.CarType;
        CarModel = contract.CarModel;
        CarColor = contract.CarColor;
        ChassisNumber = contract.ChassisNumber;
        CarPrice = contract.CarPrice;
        AmountReceived = contract.AmountReceived;
        RemainingAmount = contract.RemainingAmount;
        CarPriceInWords = contract.CarPriceInWords;
        Notes = contract.Notes;
        NotifySaveStateChanged();
    }

    partial void OnCarPriceChanged(decimal value)
    {
        CarPriceInWords = ArabicAmountToWords.Convert(value);
        RemainingAmount = Math.Max(0, value - AmountReceived);
    }

    partial void OnAmountReceivedChanged(decimal value) =>
        RemainingAmount = Math.Max(0, CarPrice - value);

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
            CarSaleContract saved;
            if (_editId.HasValue)
            {
                entity.Id = _editId.Value;
                saved = await _contractService.UpdateAsync(entity);
                _toast.ShowSuccess("تم حفظ العقد بنجاح");
            }
            else
            {
                saved = await _contractService.CreateAsync(entity);
                _toast.ShowSuccess("تم حفظ العقد بنجاح");
                ResetForNewContract();
            }

            if (printAfterSave && CanPrint)
                _printService.PrintContract(saved);
        }
        catch (DbUpdateConcurrencyException)
        {
            _toast.ShowError("تعذر الحفظ — تم تعديل العقد من جهة أخرى. أعد تحميل البيانات وحاول مجدداً.");
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

    private void ResetForNewContract()
    {
        _editId = null;
        PageTitle = "عقد جديد";
        ContractDate = DateTime.Today;
        ContractNumber = "يُنشأ تلقائياً";
        SellerName = string.Empty;
        SellerAddress = string.Empty;
        SellerIdNumber = string.Empty;
        SellerIdDate = DateTime.Today;
        SellerPhone = string.Empty;
        BuyerName = string.Empty;
        BuyerAddress = string.Empty;
        BuyerIdNumber = string.Empty;
        BuyerIdDate = DateTime.Today;
        BuyerPhone = string.Empty;
        AnnualOwnerName = string.Empty;
        AnnualOwnerAddress = string.Empty;
        PlateNumber = string.Empty;
        CarType = string.Empty;
        CarModel = string.Empty;
        CarColor = string.Empty;
        ChassisNumber = string.Empty;
        CarPrice = 0;
        CarPriceInWords = string.Empty;
        AmountReceived = 0;
        RemainingAmount = 0;
        Notes = string.Empty;
        NotifySaveStateChanged();
    }

    [RelayCommand]
    private void Cancel() => _mainWindow.CloseTabForViewModel(this);

    private bool Validate(out string error)
    {
        if (string.IsNullOrWhiteSpace(SellerName)) { error = "اسم البائع مطلوب"; return false; }
        if (string.IsNullOrWhiteSpace(BuyerName)) { error = "اسم المشتري مطلوب"; return false; }
        if (CarPrice <= 0) { error = "سعر السيارة يجب أن يكون أكبر من صفر"; return false; }
        if (AmountReceived < 0) { error = "المبلغ الواصل غير صالح"; return false; }
        if (AmountReceived > CarPrice) { error = "المبلغ الواصل أكبر من سعر السيارة"; return false; }
        error = string.Empty;
        return true;
    }

    private CarSaleContract BuildEntity() => new()
    {
        ContractDate = ContractDate,
        SellerName = SellerName.Trim(),
        SellerAddress = SellerAddress.Trim(),
        SellerIdNumber = SellerIdNumber.Trim(),
        SellerIdDate = SellerIdDate,
        SellerPhone = SellerPhone.Trim(),
        BuyerName = BuyerName.Trim(),
        BuyerAddress = BuyerAddress.Trim(),
        BuyerIdNumber = BuyerIdNumber.Trim(),
        BuyerIdDate = BuyerIdDate,
        BuyerPhone = BuyerPhone.Trim(),
        AnnualOwnerName = AnnualOwnerName.Trim(),
        AnnualOwnerAddress = AnnualOwnerAddress.Trim(),
        PlateNumber = PlateNumber.Trim(),
        CarType = CarType.Trim(),
        CarModel = CarModel.Trim(),
        CarColor = CarColor.Trim(),
        ChassisNumber = ChassisNumber.Trim(),
        CarPrice = CarPrice,
        CarPriceInWords = CarPriceInWords,
        AmountReceived = AmountReceived,
        RemainingAmount = RemainingAmount,
        Notes = Notes.Trim()
    };

    private void NotifySaveStateChanged()
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(CanSave));
    }
}
