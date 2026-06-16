using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class HotelSetupWizardViewModel : ViewModelBase
{
    private readonly IHotelSettingsService _settingsService;
    private readonly IHotelMasterDataService _masterDataService;
    private readonly IHotelCashService _cashService;
    private readonly IHotelExpenseService _expenseService;
    private readonly IRatePlanService _ratePlanService;

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
        0 => "١ - بيانات الفندق",
        1 => "٢ - الطوابق",
        2 => "٣ - أنواع الغرف",
        3 => "٤ - الغرف",
        4 => "٥ - الصناديق وخطط الأسعار",
        5 => "٦ - أنواع المصاريف",
        _ => string.Empty
    };

    [ObservableProperty] private string _hotelName = string.Empty;
    [ObservableProperty] private string _hotelAddress = string.Empty;
    [ObservableProperty] private string _hotelPhone = string.Empty;
    [ObservableProperty] private string _hotelEmail = string.Empty;
    [ObservableProperty] private string _checkInTimeText = "14:00";
    [ObservableProperty] private string _checkOutTimeText = "12:00";
    [ObservableProperty] private string _currency = "IQD";

    public ObservableCollection<SetupFloorRow> Floors { get; } = [];
    [ObservableProperty] private string _newFloorName = string.Empty;

    public ObservableCollection<SetupRoomTypeRow> RoomTypes { get; } = [];
    [ObservableProperty] private string _newRoomTypeName = string.Empty;
    [ObservableProperty] private int _newRoomTypeCapacity = 2;
    [ObservableProperty] private decimal _newRoomTypePrice;

    [ObservableProperty] private int? _bulkFloorId;
    [ObservableProperty] private int? _bulkRoomTypeId;
    [ObservableProperty] private string _bulkPrefix = string.Empty;
    [ObservableProperty] private int _bulkFrom = 101;
    [ObservableProperty] private int _bulkTo = 110;

    public ObservableCollection<SetupCashBoxRow> CashBoxes { get; } = [];
    [ObservableProperty] private string _newCashBoxName = string.Empty;
    [ObservableProperty] private decimal _newCashBoxBalance;

    public ObservableCollection<SetupRatePlanRow> RatePlans { get; } = [];
    [ObservableProperty] private string _newRatePlanName = string.Empty;
    [ObservableProperty] private int? _newRatePlanRoomTypeIndex;
    [ObservableProperty] private decimal _newRatePlanPrice;

    public ObservableCollection<SetupExpenseTypeRow> ExpenseTypes { get; } = [];
    [ObservableProperty] private string _newExpenseTypeName = string.Empty;

    public HotelSetupWizardViewModel(
        IHotelSettingsService settingsService,
        IHotelMasterDataService masterDataService,
        IHotelCashService cashService,
        IHotelExpenseService expenseService,
        IRatePlanService ratePlanService)
    {
        _settingsService = settingsService;
        _masterDataService = masterDataService;
        _cashService = cashService;
        _expenseService = expenseService;
        _ratePlanService = ratePlanService;
        PageTitle = "إعداد الفندق";

        ExpenseTypes.Add(new SetupExpenseTypeRow { Name = "كهرباء" });
        ExpenseTypes.Add(new SetupExpenseTypeRow { Name = "ماء" });
        ExpenseTypes.Add(new SetupExpenseTypeRow { Name = "رواتب" });
        ExpenseTypes.Add(new SetupExpenseTypeRow { Name = "صيانة" });
        ExpenseTypes.Add(new SetupExpenseTypeRow { Name = "مستلزمات" });
    }

    [RelayCommand]
    private void NextStep()
    {
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
    private void AddFloor()
    {
        var name = NewFloorName?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        Floors.Add(new SetupFloorRow { Name = name, SortOrder = Floors.Count + 1 });
        NewFloorName = string.Empty;
    }

    [RelayCommand]
    private void RemoveFloor(SetupFloorRow? row)
    {
        if (row is not null) Floors.Remove(row);
    }

    [RelayCommand]
    private void AddRoomType()
    {
        var name = NewRoomTypeName?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        RoomTypes.Add(new SetupRoomTypeRow
        {
            Name = name,
            Capacity = NewRoomTypeCapacity,
            BasePrice = NewRoomTypePrice,
            SortOrder = RoomTypes.Count + 1
        });
        NewRoomTypeName = string.Empty;
        NewRoomTypeCapacity = 2;
        NewRoomTypePrice = 0;
    }

    [RelayCommand]
    private void RemoveRoomType(SetupRoomTypeRow? row)
    {
        if (row is not null) RoomTypes.Remove(row);
    }

    [RelayCommand]
    private void AddCashBox()
    {
        var name = NewCashBoxName?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        CashBoxes.Add(new SetupCashBoxRow { Name = name, Balance = NewCashBoxBalance });
        NewCashBoxName = string.Empty;
        NewCashBoxBalance = 0;
    }

    [RelayCommand]
    private void RemoveCashBox(SetupCashBoxRow? row)
    {
        if (row is not null) CashBoxes.Remove(row);
    }

    [RelayCommand]
    private void AddRatePlan()
    {
        var name = NewRatePlanName?.Trim();
        if (string.IsNullOrEmpty(name) || !NewRatePlanRoomTypeIndex.HasValue) return;
        if (NewRatePlanRoomTypeIndex.Value < 0 || NewRatePlanRoomTypeIndex.Value >= RoomTypes.Count) return;

        RatePlans.Add(new SetupRatePlanRow
        {
            Name = name,
            RoomTypeName = RoomTypes[NewRatePlanRoomTypeIndex.Value].Name,
            BasePrice = NewRatePlanPrice
        });
        NewRatePlanName = string.Empty;
        NewRatePlanPrice = 0;
    }

    [RelayCommand]
    private void RemoveRatePlan(SetupRatePlanRow? row)
    {
        if (row is not null) RatePlans.Remove(row);
    }

    [RelayCommand]
    private void AddExpenseType()
    {
        var name = NewExpenseTypeName?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        ExpenseTypes.Add(new SetupExpenseTypeRow { Name = name });
        NewExpenseTypeName = string.Empty;
    }

    [RelayCommand]
    private void RemoveExpenseType(SetupExpenseTypeRow? row)
    {
        if (row is not null) ExpenseTypes.Remove(row);
    }

    [RelayCommand]
    private async Task FinishAsync()
    {
        if (string.IsNullOrWhiteSpace(HotelName))
        {
            BeautifulMessageDialog.ShowWarning("أدخل اسم الفندق");
            CurrentStep = 0;
            return;
        }

        if (Floors.Count == 0 || RoomTypes.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("أضف طابقاً واحداً على الأقل ونوع غرفة واحد");
            return;
        }

        IsBusy = true;
        try
        {
            if (!TimeSpan.TryParse(CheckInTimeText, out var checkIn))
                checkIn = new TimeSpan(14, 0, 0);
            if (!TimeSpan.TryParse(CheckOutTimeText, out var checkOut))
                checkOut = new TimeSpan(12, 0, 0);

            await _settingsService.SaveSettingsAsync(new HotelSettings
            {
                HotelName = HotelName.Trim(),
                Address = HotelAddress.Trim(),
                Phone = HotelPhone.Trim(),
                Email = HotelEmail.Trim(),
                CheckInTime = checkIn,
                CheckOutTime = checkOut,
                Currency = Currency.Trim()
            });

            var floorIds = new Dictionary<string, int>();
            foreach (var floor in Floors)
            {
                var created = await _masterDataService.CreateFloorAsync(new Floor
                {
                    Name = floor.Name,
                    SortOrder = floor.SortOrder
                });
                floorIds[floor.Name] = created.Id;
            }

            var roomTypeIds = new Dictionary<string, int>();
            foreach (var rt in RoomTypes)
            {
                var created = await _masterDataService.CreateRoomTypeAsync(new RoomType
                {
                    Name = rt.Name,
                    Capacity = rt.Capacity,
                    BasePrice = rt.BasePrice,
                    SortOrder = rt.SortOrder
                });
                roomTypeIds[rt.Name] = created.Id;
            }

            if (BulkTo >= BulkFrom && floorIds.Count > 0 && roomTypeIds.Count > 0)
            {
                var floorId = BulkFloorId.HasValue && BulkFloorId.Value < Floors.Count
                    ? floorIds[Floors[BulkFloorId.Value].Name]
                    : floorIds.Values.First();
                var roomTypeId = BulkRoomTypeId.HasValue && BulkRoomTypeId.Value < RoomTypes.Count
                    ? roomTypeIds[RoomTypes[BulkRoomTypeId.Value].Name]
                    : roomTypeIds.Values.First();

                await _masterDataService.BulkAddRoomsAsync(new BulkAddRoomsRequest
                {
                    FloorId = floorId,
                    RoomTypeId = roomTypeId,
                    NumberPrefix = BulkPrefix,
                    FromNumber = BulkFrom,
                    ToNumber = BulkTo,
                    InitialStatus = RoomStatus.Available
                });
            }

            foreach (var box in CashBoxes)
            {
                await _cashService.CreateCashBoxAsync(new HotelCashBox
                {
                    Name = box.Name,
                    OpeningBalance = box.Balance,
                    CurrentBalance = box.Balance,
                    IsActive = true
                });
            }

            foreach (var plan in RatePlans)
            {
                if (!roomTypeIds.TryGetValue(plan.RoomTypeName, out var rtId))
                    continue;

                await _ratePlanService.CreateRatePlanAsync(new RatePlan
                {
                    Name = plan.Name,
                    RoomTypeId = rtId,
                    BasePrice = plan.BasePrice,
                    IsActive = true
                });
            }

            foreach (var et in ExpenseTypes)
            {
                await _expenseService.CreateExpenseTypeAsync(new HotelExpenseType { Name = et.Name });
            }

            await _settingsService.MarkConfiguredAsync();
            SetupCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الإعداد: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public partial class SetupFloorRow : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private int _sortOrder;
}

public partial class SetupRoomTypeRow : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private int _capacity = 2;
    [ObservableProperty] private decimal _basePrice;
    [ObservableProperty] private int _sortOrder;
}

public partial class SetupCashBoxRow : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private decimal _balance;
}

public partial class SetupRatePlanRow : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _roomTypeName = string.Empty;
    [ObservableProperty] private decimal _basePrice;
}

public partial class SetupExpenseTypeRow : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
}
