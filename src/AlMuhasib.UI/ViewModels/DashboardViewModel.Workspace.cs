using System.Collections.ObjectModel;
using System.Globalization;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.ViewModels;

public partial class DashboardViewModel
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPricingTypeService _pricingTypeService;

    [ObservableProperty] private bool _showDashboardQuickSales = true;
    [ObservableProperty] private bool _showDashboardQuickPurchase = true;
    [ObservableProperty] private bool _showDashboardQuickInstallment = true;
    [ObservableProperty] private bool _showCollectionDashboard;
    [ObservableProperty] private bool _showTodayPurchases = true;
    [ObservableProperty] private bool _showNetProfit = true;
    [ObservableProperty] private bool _showInvestorStats = true;
    [ObservableProperty] private bool _showFinanceCharts = true;

    // ── إعداد النظام / معالج النقل ─────────────────────────
    [ObservableProperty] private bool _showSetupProgress;
    [ObservableProperty] private int _setupCompletedSteps;
    [ObservableProperty] private int _setupTotalSteps;
    [ObservableProperty] private double _setupProgressPercent;
    [ObservableProperty] private string _setupProgressText = string.Empty;
    [ObservableProperty] private string _setupRemainingText = string.Empty;

    public ObservableCollection<SetupProgressDot> SetupProgressDots { get; } = [];

    private void ApplyDashboardProfile()
    {
        // ملف العمل (كاشير/محاسب) يخصّص شريط المساعد السريع فقط.
        // لوحة التحكم تعرض كل الإحصائيات والإجراءات؛ الصلاحيات تحدّد الظهور.
        ShowDashboardQuickSales = _currentUserService.CanView("SaleInvoice");
        ShowDashboardQuickPurchase = _currentUserService.CanView("PurchaseInvoice");
        ShowDashboardQuickInstallment = _currentUserService.CanView("InstallmentInvoice");
        ShowCollectionDashboard = _currentUserService.CanView("Installments");

        ShowTodayPurchases = true;
        ShowNetProfit = true;
        ShowInvestorStats = true;
        ShowFinanceCharts = true;
    }

    /// <summary>
    /// يحسب تقدّم إعداد النظام بما يطابق خطوات معالج النقل (١١ خطوة).
    /// الخطوات الاختيارية لا تمنع إخفاء الشريط إن اكتملت الأساسيات.
    /// </summary>
    private async Task RefreshSetupProgressAsync()
    {
        try
        {
            var hasCapital = await _unitOfWork.CapitalEntries.AnyAsync();
            var hasCategories = await _unitOfWork.Categories.AnyAsync();
            var hasWarehouses = await _unitOfWork.Warehouses.AnyAsync();
            var hasPricing = (await _pricingTypeService.GetActiveAsync()).Count > 0;
            var hasProducts = await _unitOfWork.Products.AnyAsync();
            var hasCash = await _unitOfWork.CashBoxes.AnyAsync() || await _unitOfWork.BankAccounts.AnyAsync();
            var hasInvestors = await _unitOfWork.Investors.AnyAsync();
            var hasCustomers = await _unitOfWork.Customers.AnyAsync();
            var hasSuppliers = await _unitOfWork.Suppliers.AnyAsync();
            var hasExpenseTypes = await _unitOfWork.ExpenseTypes.AnyAsync();
            var hasInstallments = await _unitOfWork.InstallmentPlans.AnyAsync();

            // الترتيب والعدد مطابقان لمعالج النقل — Required = يمنع إخفاء الشريط
            var checks = new (string Label, bool Done, bool Required)[]
            {
                ("الفروع ورأس المال", hasCapital, true),
                ("التصنيفات", hasCategories, false),
                ("المخازن", hasWarehouses, true),
                ("أنواع التسعير", hasPricing, true),
                ("المنتجات", hasProducts, false),
                ("القاصات والمصرف", hasCash, false),
                ("المستثمرون", hasInvestors, false),
                ("العملاء", hasCustomers, false),
                ("الموردون", hasSuppliers, false),
                ("أنواع المصاريف", hasExpenseTypes, false),
                ("الأقساط", hasInstallments, false),
            };

            var total = checks.Length;
            var done = checks.Count(c => c.Done);
            var remainingAll = total - done;
            var requiredRemaining = checks.Count(c => c.Required && !c.Done);

            // رقم الخطوة الحالية = أول خطوة غير مكتملة (١-based) كما في المعالج
            var currentIndex = Array.FindIndex(checks, c => !c.Done);
            var displayStep = currentIndex >= 0 ? currentIndex + 1 : total;

            SetupTotalSteps = total;
            SetupCompletedSteps = done;
            SetupProgressPercent = total == 0
                ? 100
                : Math.Round(done * 100.0 / total, 1);

            // أخفِ الشريط عند اكتمال الأساسيات (فروع/رأس مال + مخازن + تسعير)
            ShowSetupProgress = requiredRemaining > 0;

            SetupProgressText = requiredRemaining > 0
                ? $"الخطوة {ToArabicNumeral(displayStep)} من {ToArabicNumeral(total)}"
                : $"اكتمل الإعداد الأساسي ({ToArabicNumeral(done)}/{ToArabicNumeral(total)})";

            SetupRemainingText = requiredRemaining <= 0
                ? "اكتمل إعداد النظام"
                : remainingAll <= 0
                    ? "اكتمل إعداد النظام"
                    : remainingAll == 1
                        ? "متبقي خطوة واحدة"
                        : remainingAll == 2
                            ? "متبقي خطوتان"
                            : $"متبقي {ToArabicNumeral(remainingAll)} خطوات";

            SetupProgressDots.Clear();
            for (var i = 0; i < checks.Length; i++)
            {
                SetupProgressDots.Add(new SetupProgressDot
                {
                    IsCompleted = checks[i].Done,
                    IsCurrent = currentIndex >= 0 && i == currentIndex,
                    ToolTip = checks[i].Required
                        ? checks[i].Label
                        : $"{checks[i].Label} (اختياري)"
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Setup progress: {ex.Message}");
            ShowSetupProgress = false;
        }
    }

    private static string ToArabicNumeral(int n) => n switch
    {
        1 => "١", 2 => "٢", 3 => "٣", 4 => "٤", 5 => "٥", 6 => "٦",
        7 => "٧", 8 => "٨", 9 => "٩", 10 => "١٠", 11 => "١١", 12 => "١٢",
        _ => n.ToString(CultureInfo.InvariantCulture)
    };
}

public sealed class SetupProgressDot
{
    public bool IsCompleted { get; init; }
    public bool IsCurrent { get; init; }
    public string ToolTip { get; init; } = string.Empty;
}
