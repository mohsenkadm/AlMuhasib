using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class MainWindowViewModel
{
    [ObservableProperty] private bool _isFeatureTourOpen;
    [ObservableProperty] private int _featureTourStepIndex;

    public IReadOnlyList<FeatureTourStep> FeatureTourSteps { get; } =
    [
        new()
        {
            Title = "مرحباً بك في المحاسب",
            Description = "جولة سريعة لتعرف أهم أدوات النظام. يمكنك تخطيها أو إعادة فتحها لاحقاً من أدوات المساعدة.",
            IconKind = "HandWave"
        },
        new()
        {
            Title = "شريط الإجراءات السريع",
            Description = "من أعلى الشاشة: بيع، شراء، سندات قبض وصرف، وتسديد أقساط — دون فتح القائمة الجانبية.",
            IconKind = "LightningBolt"
        },
        new()
        {
            Title = "بيع سريع (POS)",
            Description = "شاشة كاشير مبسطة: امسح الباركود أو اختر المنتج، ثم «إتمام البيع» نقداً.",
            IconKind = "PointOfSale"
        },
        new()
        {
            Title = "بحث شامل Ctrl+K",
            Description = "ابحث عن عميل، مورد، منتج، أو انتقل لأي شاشة بسرعة من لوحة المفاتيح.",
            IconKind = "Magnify"
        },
        new()
        {
            Title = "المساعد الذكي",
            Description = "تنبيهات يومية (أقساط متأخرة، مخزون منخفض، ذمم) مع اختصار للشاشة المناسبة.",
            IconKind = "RobotOutline"
        },
        new()
        {
            Title = "لوحة التحكم",
            Description = "ملخص المبيعات والأرباح والمهام اليومية — نقطة انطلاقك كل صباح.",
            IconKind = "ViewDashboard"
        }
    ];

    public FeatureTourStep CurrentFeatureTourStep =>
        FeatureTourSteps[Math.Clamp(FeatureTourStepIndex, 0, FeatureTourSteps.Count - 1)];

    public bool CanFeatureTourGoBack => FeatureTourStepIndex > 0;
    public bool IsFeatureTourLastStep => FeatureTourStepIndex >= FeatureTourSteps.Count - 1;
    public string FeatureTourProgressText => $"{FeatureTourStepIndex + 1} / {FeatureTourSteps.Count}";

    public void TryStartFeatureTour()
    {
        if (_userPreferences.Current.HasCompletedFeatureTour) return;
        FeatureTourStepIndex = 0;
        IsFeatureTourOpen = true;
        OnPropertyChanged(nameof(CurrentFeatureTourStep));
        OnPropertyChanged(nameof(CanFeatureTourGoBack));
        OnPropertyChanged(nameof(IsFeatureTourLastStep));
        OnPropertyChanged(nameof(FeatureTourProgressText));
    }

    [RelayCommand]
    private void FeatureTourNext()
    {
        if (IsFeatureTourLastStep)
        {
            CompleteFeatureTour();
            return;
        }

        FeatureTourStepIndex++;
        NotifyFeatureTourChanged();
    }

    [RelayCommand]
    private void FeatureTourBack()
    {
        if (FeatureTourStepIndex <= 0) return;
        FeatureTourStepIndex--;
        NotifyFeatureTourChanged();
    }

    [RelayCommand]
    private void SkipFeatureTour() => CompleteFeatureTour();

    [RelayCommand]
    private void RestartFeatureTour()
    {
        FeatureTourStepIndex = 0;
        IsFeatureTourOpen = true;
        NotifyFeatureTourChanged();
    }

    private void CompleteFeatureTour()
    {
        IsFeatureTourOpen = false;
        _userPreferences.Update(p => p.HasCompletedFeatureTour = true);
        _toast.ShowSuccess("تم إكمال جولة التعريف — بالتوفيق!");
    }

    private void NotifyFeatureTourChanged()
    {
        OnPropertyChanged(nameof(CurrentFeatureTourStep));
        OnPropertyChanged(nameof(CanFeatureTourGoBack));
        OnPropertyChanged(nameof(IsFeatureTourLastStep));
        OnPropertyChanged(nameof(FeatureTourProgressText));
    }
}
