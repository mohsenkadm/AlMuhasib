using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldMithqalPricesViewModel : ViewModelBase
{
    private readonly IGoldPricingService _pricingService;
    private readonly ICurrentUserService _currentUserService;
    private readonly List<GoldMithqalPriceRow> _allPrices = [];
    private System.Timers.Timer? _debounceTimer;
    private int? _editingId;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private GoldMithqalPriceRow? _selectedPrice;

    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private int _editKaratValue = 21;
    [ObservableProperty] private decimal _editPricePerMithqal;
    [ObservableProperty] private GoldCurrency _editCurrency = GoldCurrency.USD;
    [ObservableProperty] private DateTime _editPriceDate = DateTime.Today;
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private string _dialogError = string.Empty;

    public ObservableCollection<GoldMithqalPriceRow> Prices { get; } = [];
    public ObservableCollection<GoldMithqalPriceRow> LatestPrices { get; } = [];
    public ObservableCollection<GoldKarat> Karats { get; } = [];
    public GoldCurrency[] Currencies { get; } = Enum.GetValues<GoldCurrency>();

    public GoldMithqalPricesViewModel(
        IGoldPricingService pricingService,
        ICurrentUserService currentUserService)
    {
        _pricingService = pricingService;
        _currentUserService = currentUserService;
        PageTitle = "أسعار المثقال";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.MithqalPrices);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            Karats.Clear();
            foreach (var karat in await _pricingService.GetKaratsAsync())
                Karats.Add(karat);

            LatestPrices.Clear();
            foreach (var price in await _pricingService.GetLatestPricesAsync())
                LatestPrices.Add(price);

            _allPrices.Clear();
            _allPrices.AddRange(await _pricingService.GetPricesAsync());
            ApplyPage();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل الأسعار:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyPage()
    {
        IEnumerable<GoldMithqalPriceRow> query = _allPrices;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var filter = SearchText.Trim();
            query = query.Where(p =>
                p.KaratName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                p.KaratValue.ToString().Contains(filter) ||
                p.Notes.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = query.ToList();
        TotalCount = filtered.Count;
        TotalPages = PaginationHelper.ComputeTotalPages(TotalCount, PageSize);
        if (CurrentPage > TotalPages) CurrentPage = Math.Max(1, TotalPages);
        PaginationText = PaginationHelper.BuildPaginationText(TotalCount, CurrentPage, PageSize);

        Prices.Clear();
        foreach (var row in filtered.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
            Prices.Add(row);
    }

    partial void OnSearchTextChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(350);
        _debounceTimer.Elapsed += (_, _) =>
        {
            _debounceTimer?.Stop();
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentPage = 1;
                ApplyPage();
            });
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    [RelayCommand]
    private void FirstPage() { CurrentPage = 1; ApplyPage(); }

    [RelayCommand]
    private void PreviousPage() { if (CurrentPage > 1) { CurrentPage--; ApplyPage(); } }

    [RelayCommand]
    private void NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; ApplyPage(); } }

    [RelayCommand]
    private void LastPage() { CurrentPage = TotalPages; ApplyPage(); }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void OpenAddDialog()
    {
        _editingId = null;
        IsEditMode = false;
        DialogTitle = "إضافة سعر مثقال";
        EditKaratValue = Karats.FirstOrDefault()?.KaratValue ?? 21;
        EditPricePerMithqal = 0;
        EditCurrency = GoldCurrency.USD;
        EditPriceDate = DateTime.Today;
        EditNotes = string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(GoldMithqalPriceRow? row)
    {
        if (row is null) return;
        _editingId = row.Id;
        IsEditMode = true;
        DialogTitle = "تعديل سعر المثقال";
        EditKaratValue = row.KaratValue;
        EditPricePerMithqal = row.PricePerMithqal;
        EditCurrency = row.Currency;
        EditPriceDate = row.PriceDate.Date;
        EditNotes = row.Notes;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task SavePriceAsync()
    {
        if (EditKaratValue <= 0)
        {
            DialogError = "اختر العيار";
            return;
        }

        if (EditPricePerMithqal <= 0)
        {
            DialogError = "أدخل سعراً صالحاً";
            return;
        }

        try
        {
            var entity = new GoldMithqalPrice
            {
                Id = _editingId ?? 0,
                KaratValue = EditKaratValue,
                PricePerMithqal = EditPricePerMithqal,
                Currency = EditCurrency,
                PriceDate = EditPriceDate.Date,
                Notes = EditNotes?.Trim() ?? string.Empty,
                CreatedBy = _currentUserService.Username
            };

            await _pricingService.SavePriceAsync(entity);
            IsDialogOpen = false;
            BeautifulMessageDialog.ShowSuccess("تم حفظ السعر");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            DialogError = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeletePriceAsync(GoldMithqalPriceRow? row)
    {
        if (row is null || !CanDelete) return;
        if (!BeautifulMessageDialog.ShowConfirm($"حذف سعر عيار {row.KaratValue} بتاريخ {row.PriceDate:yyyy/MM/dd}؟"))
            return;

        try
        {
            await _pricingService.DeletePriceAsync(row.Id, _currentUserService.Username);
            BeautifulMessageDialog.ShowSuccess("تم الحذف");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }
}
