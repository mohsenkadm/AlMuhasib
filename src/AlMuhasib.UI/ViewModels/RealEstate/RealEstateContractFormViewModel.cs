using AlMuhasib.Core.Entities.RealEstate;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Utilities;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.RealEstate;

public partial class RealEstateContractFormViewModel : ViewModelBase
{
    private readonly IRealEstateContractService _contractService;
    private readonly IRealEstateContractPrintService _printService;
    private readonly IRealEstateClauseTemplateService _clauseTemplates;
    private readonly MainWindowViewModel _mainWindow;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private int? _editId;

    [ObservableProperty] private DateTime _contractDate = DateTime.Today;
    [ObservableProperty] private string _contractNumber = "يُنشأ تلقائياً";
    [ObservableProperty] private RealEstateContractType _contractType = RealEstateContractType.Sale;
    [ObservableProperty] private RealEstatePropertyType _propertyType = RealEstatePropertyType.House;
    [ObservableProperty] private string _propertyLocation = string.Empty;
    [ObservableProperty] private string _propertyAddress = string.Empty;
    [ObservableProperty] private decimal _propertyAreaSqm;
    [ObservableProperty] private string _propertyDescription = string.Empty;
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
    [ObservableProperty] private decimal _totalPrice;
    [ObservableProperty] private string _totalPriceInWords = string.Empty;
    [ObservableProperty] private decimal _downPayment;
    [ObservableProperty] private decimal _amountPaid;
    [ObservableProperty] private decimal _remainingAmount;
    [ObservableProperty] private RealEstatePaymentMode _paymentMode = RealEstatePaymentMode.Cash;
    [ObservableProperty] private RealEstateDebtorParty _debtorParty = RealEstateDebtorParty.None;
    [ObservableProperty] private DateTime? _dueDate;
    [ObservableProperty] private string _witnessOneName = string.Empty;
    [ObservableProperty] private string _witnessTwoName = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private bool _isSaving;

    public ObservableCollection<RealEstateContractClause> Clauses { get; } = [];

    public Array ContractTypes => Enum.GetValues(typeof(RealEstateContractType));
    public Array PropertyTypes => Enum.GetValues(typeof(RealEstatePropertyType));
    public Array PaymentModes => Enum.GetValues(typeof(RealEstatePaymentMode));
    public Array DebtorParties => Enum.GetValues(typeof(RealEstateDebtorParty));

    public bool IsCredit => PaymentMode == RealEstatePaymentMode.Credit;
    public bool IsEditMode => _editId.HasValue;
    public bool CanSave => IsEditMode ? CanEdit : CanAdd;

    public RealEstateContractFormViewModel(
        IRealEstateContractService contractService,
        IRealEstateContractPrintService printService,
        IRealEstateClauseTemplateService clauseTemplates,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        MainWindowViewModel mainWindow)
    {
        _contractService = contractService;
        _printService = printService;
        _clauseTemplates = clauseTemplates;
        _currentUserService = currentUserService;
        _toast = toast;
        _mainWindow = mainWindow;
        PageTitle = "عقد جديد";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, RealEstatePermissionRegistry.ContractForm);
        NotifySaveStateChanged();
        await _clauseTemplates.EnsureDefaultsAsync();

        if (RealEstateContractNavigationBridge.PendingEditContractId is int editId)
        {
            RealEstateContractNavigationBridge.PendingEditContractId = null;
            await LoadContractAsync(editId);
        }
        else
        {
            await ResetForNewContractAsync();
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
        ContractType = contract.ContractType;
        PropertyType = contract.PropertyType;
        PropertyLocation = contract.PropertyLocation;
        PropertyAddress = contract.PropertyAddress;
        PropertyAreaSqm = contract.PropertyAreaSqm;
        PropertyDescription = contract.PropertyDescription;
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
        TotalPrice = contract.TotalPrice;
        TotalPriceInWords = contract.TotalPriceInWords;
        DownPayment = contract.DownPayment;
        AmountPaid = contract.AmountPaid;
        RemainingAmount = contract.RemainingAmount;
        PaymentMode = contract.PaymentMode;
        DebtorParty = contract.DebtorParty;
        DueDate = contract.DueDate;
        WitnessOneName = contract.WitnessOneName;
        WitnessTwoName = contract.WitnessTwoName;
        Notes = contract.Notes;
        Clauses.Clear();
        foreach (var c in contract.Clauses.OrderBy(x => x.SortOrder))
            Clauses.Add(c);
        NotifySaveStateChanged();
        OnPropertyChanged(nameof(IsCredit));
    }

    partial void OnTotalPriceChanged(decimal value)
    {
        TotalPriceInWords = ArabicAmountToWords.Convert(value, "دينار", "فلس");
        RemainingAmount = Math.Max(0, value - AmountPaid);
    }

    partial void OnDownPaymentChanged(decimal value)
    {
        if (AmountPaid <= 0)
            AmountPaid = value;
    }

    partial void OnAmountPaidChanged(decimal value) =>
        RemainingAmount = Math.Max(0, TotalPrice - value);

    partial void OnPaymentModeChanged(RealEstatePaymentMode value)
    {
        if (value == RealEstatePaymentMode.Cash)
        {
            DebtorParty = RealEstateDebtorParty.None;
            DueDate = null;
            if (AmountPaid <= 0)
                AmountPaid = TotalPrice;
        }
        else if (DebtorParty == RealEstateDebtorParty.None)
        {
            DebtorParty = RealEstateDebtorParty.Buyer;
        }

        OnPropertyChanged(nameof(IsCredit));
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
            RealEstateContract saved;
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
                await ResetForNewContractAsync();
            }

            if (printAfterSave && CanPrint)
                _printService.PrintContract(saved);
        }
        catch (DbUpdateConcurrencyException)
        {
            _toast.ShowError("تعذر الحفظ — تم تعديل العقد من جهة أخرى.");
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

    private async Task ResetForNewContractAsync()
    {
        _editId = null;
        PageTitle = "عقد جديد";
        ContractDate = DateTime.Today;
        ContractNumber = "يُنشأ تلقائياً";
        ContractType = RealEstateContractType.Sale;
        PropertyType = RealEstatePropertyType.House;
        PropertyLocation = string.Empty;
        PropertyAddress = string.Empty;
        PropertyAreaSqm = 0;
        PropertyDescription = string.Empty;
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
        TotalPrice = 0;
        TotalPriceInWords = string.Empty;
        DownPayment = 0;
        AmountPaid = 0;
        RemainingAmount = 0;
        PaymentMode = RealEstatePaymentMode.Cash;
        DebtorParty = RealEstateDebtorParty.None;
        DueDate = null;
        WitnessOneName = string.Empty;
        WitnessTwoName = string.Empty;
        Notes = string.Empty;
        Clauses.Clear();
        var templates = await _clauseTemplates.GetActiveAsync();
        foreach (var t in templates)
        {
            Clauses.Add(new RealEstateContractClause
            {
                SortOrder = t.SortOrder,
                Title = t.Title,
                Body = t.Body
            });
        }
        NotifySaveStateChanged();
        OnPropertyChanged(nameof(IsCredit));
    }

    [RelayCommand]
    private void Cancel() => _mainWindow.CloseTabForViewModel(this);

    private bool Validate(out string error)
    {
        if (string.IsNullOrWhiteSpace(SellerName)) { error = "اسم البائع مطلوب"; return false; }
        if (string.IsNullOrWhiteSpace(BuyerName)) { error = "اسم المشتري مطلوب"; return false; }
        if (TotalPrice <= 0) { error = "السعر الكلي يجب أن يكون أكبر من صفر"; return false; }
        if (AmountPaid < 0) { error = "المبلغ المدفوع غير صالح"; return false; }
        if (AmountPaid > TotalPrice) { error = "المبلغ المدفوع أكبر من السعر الكلي"; return false; }
        if (PaymentMode == RealEstatePaymentMode.Credit && DebtorParty == RealEstateDebtorParty.None)
        { error = "حدد طرف المدين للعقد الآجل"; return false; }
        error = string.Empty;
        return true;
    }

    private RealEstateContract BuildEntity()
    {
        var entity = new RealEstateContract
        {
            ContractDate = ContractDate,
            ContractType = ContractType,
            PropertyType = PropertyType,
            PropertyLocation = PropertyLocation.Trim(),
            PropertyAddress = PropertyAddress.Trim(),
            PropertyAreaSqm = PropertyAreaSqm,
            PropertyDescription = PropertyDescription.Trim(),
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
            TotalPrice = TotalPrice,
            TotalPriceInWords = TotalPriceInWords,
            DownPayment = DownPayment,
            AmountPaid = AmountPaid,
            RemainingAmount = RemainingAmount,
            PaymentMode = PaymentMode,
            DebtorParty = DebtorParty,
            DueDate = DueDate,
            WitnessOneName = WitnessOneName.Trim(),
            WitnessTwoName = WitnessTwoName.Trim(),
            Notes = Notes.Trim()
        };
        foreach (var c in Clauses)
        {
            entity.Clauses.Add(new RealEstateContractClause
            {
                SortOrder = c.SortOrder,
                Title = c.Title,
                Body = c.Body
            });
        }
        return entity;
    }

    private void NotifySaveStateChanged()
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(CanSave));
    }
}
