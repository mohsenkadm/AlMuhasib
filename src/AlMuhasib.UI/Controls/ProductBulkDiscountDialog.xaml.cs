using System.Windows;
using System.Windows.Input;
using AlMuhasib.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.Controls;

public partial class ProductBulkDiscountDialog : Window
{
    private readonly ProductBulkDiscountDialogViewModel _vm;

    public bool Cleared { get; private set; }
    public DiscountType DiscountType { get; private set; } = DiscountType.None;
    public decimal DiscountValue { get; private set; }
    public DateTime? DiscountExpiresAt { get; private set; }
    public bool Confirmed { get; private set; }

    public ProductBulkDiscountDialog(int selectedCount)
    {
        InitializeComponent();
        _vm = new ProductBulkDiscountDialogViewModel(selectedCount);
        DataContext = _vm;
    }

    public static (bool confirmed, bool cleared, DiscountType type, decimal value, DateTime? expiresAt)? Show(
        Window? owner, int selectedCount)
    {
        var dlg = new ProductBulkDiscountDialog(selectedCount) { Owner = owner };
        dlg.ShowDialog();
        if (!dlg.Confirmed)
            return null;
        return (true, dlg.Cleared, dlg.DiscountType, dlg.DiscountValue, dlg.DiscountExpiresAt);
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        DialogResult = false;
        Close();
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!_vm.Validate(out var error))
        {
            _vm.ErrorMessage = error;
            return;
        }

        Cleared = false;
        DiscountType = _vm.SelectedDiscountType;
        DiscountValue = _vm.DiscountValue;
        DiscountExpiresAt = _vm.HasExpiry && _vm.ExpiryDate is DateTime d
            ? DateTime.SpecifyKind(d.Date.AddDays(1).AddTicks(-1), DateTimeKind.Local).ToUniversalTime()
            : null;
        Confirmed = true;
        DialogResult = true;
        Close();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        Cleared = true;
        DiscountType = DiscountType.None;
        DiscountValue = 0;
        DiscountExpiresAt = null;
        Confirmed = true;
        DialogResult = true;
        Close();
    }
}

public partial class ProductBulkDiscountDialogViewModel : ObservableObject
{
    public int SelectedCount { get; }

    public IReadOnlyList<DiscountTypeOption> DiscountTypeOptions { get; } =
    [
        new(DiscountType.Percentage, "نسبة مئوية (%)"),
        new(DiscountType.FixedAmount, "قيمة ثابتة (د.ع لكل وحدة)")
    ];

    [ObservableProperty] private DiscountTypeOption? _selectedOption;
    [ObservableProperty] private decimal _discountValue;
    [ObservableProperty] private bool _hasExpiry;
    [ObservableProperty] private DateTime? _expiryDate = DateTime.Today.AddMonths(1);
    [ObservableProperty] private string _errorMessage = string.Empty;

    public DiscountType SelectedDiscountType => SelectedOption?.Type ?? DiscountType.Percentage;

    public ProductBulkDiscountDialogViewModel(int selectedCount)
    {
        SelectedCount = selectedCount;
        SelectedOption = DiscountTypeOptions[0];
    }

    public bool Validate(out string error)
    {
        error = string.Empty;
        if (SelectedOption is null)
        {
            error = "اختر نوع الخصم";
            return false;
        }

        if (DiscountValue <= 0)
        {
            error = "أدخل قيمة خصم أكبر من صفر";
            return false;
        }

        if (SelectedOption.Type == DiscountType.Percentage && DiscountValue > 100)
        {
            error = "النسبة لا تتجاوز 100%";
            return false;
        }

        if (HasExpiry && ExpiryDate is null)
        {
            error = "حدد تاريخ انتهاء الخصم أو ألغِ خيار الانتهاء";
            return false;
        }

        return true;
    }
}

public sealed record DiscountTypeOption(DiscountType Type, string Label)
{
    public override string ToString() => Label;
}
