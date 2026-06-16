using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class DeveloperSystemSwitchViewModel : ViewModelBase
{
    private readonly ISystemProfileService _systemProfile;
    private readonly IDeveloperAccessService _developerAccess;
    private readonly IToastNotificationService _toast;

    [ObservableProperty] private bool _isUnlocked;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _currentSystemDisplay;
    [ObservableProperty] private string _currentDatabaseDisplay;

    public DeveloperSystemSwitchViewModel(
        ISystemProfileService systemProfile,
        IDeveloperAccessService developerAccess,
        IToastNotificationService toast)
    {
        _systemProfile = systemProfile;
        _developerAccess = developerAccess;
        _toast = toast;
        PageTitle = "تبديل النظام (مطور)";
        RefreshSystemInfo();
    }

    private void RefreshSystemInfo()
    {
        CurrentSystemDisplay = _systemProfile.ActiveSystem switch
        {
            ApplicationSystemType.CarContracts => "نظام عقود السيارات",
            ApplicationSystemType.HotelManagement => "نظام إدارة الفنادق",
            _ => "نظام المحاسبة"
        };
        CurrentDatabaseDisplay = _systemProfile.ActiveDatabaseName;
    }

    [RelayCommand]
    private void Unlock()
    {
        if (!_developerAccess.VerifyPassword(Password))
        {
            _toast.ShowError("كلمة مرور المطور غير صحيحة");
            return;
        }

        IsUnlocked = true;
        Password = string.Empty;
        _toast.ShowSuccess("تم الدخول إلى منطقة المطور");
    }

    [RelayCommand]
    private void Lock()
    {
        IsUnlocked = false;
        NewPassword = string.Empty;
    }

    [RelayCommand]
    private void UpdateDeveloperPassword()
    {
        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            _toast.ShowWarning("أدخل كلمة مرور جديدة");
            return;
        }

        try
        {
            _developerAccess.SetPassword(NewPassword);
            NewPassword = string.Empty;
            _toast.ShowSuccess("تم تحديث كلمة مرور المطور");
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void SwitchToAccounting()
    {
        if (_systemProfile.ActiveSystem == ApplicationSystemType.Accounting)
        {
            _toast.ShowInfo("أنت تستخدم نظام المحاسبة حالياً");
            return;
        }

        ConfirmAndSwitch(ApplicationSystemType.Accounting, "نظام المحاسبة");
    }

    [RelayCommand]
    private void SwitchToCarContracts()
    {
        if (_systemProfile.ActiveSystem == ApplicationSystemType.CarContracts)
        {
            _toast.ShowInfo("أنت تستخدم نظام عقود السيارات حالياً");
            return;
        }

        ConfirmAndSwitch(ApplicationSystemType.CarContracts, "نظام عقود السيارات");
    }

    [RelayCommand]
    private void SwitchToHotelManagement()
    {
        if (_systemProfile.ActiveSystem == ApplicationSystemType.HotelManagement)
        {
            _toast.ShowInfo("أنت تستخدم نظام إدارة الفنادق حالياً");
            return;
        }

        ConfirmAndSwitch(ApplicationSystemType.HotelManagement, "نظام إدارة الفنادق");
    }

    private void ConfirmAndSwitch(ApplicationSystemType target, string targetLabel)
    {
        var message =
            $"سيتم التبديل إلى {targetLabel} وإعادة تشغيل التطبيق.\n\n" +
            "كل نظام يستخدم قاعدة بيانات منفصلة — لن تُحذف بيانات النظام الآخر.\n\n" +
            $"الحالي: {CurrentDatabaseDisplay}\n" +
            $"بعد التبديل: {GetTargetDatabaseName(target)}\n\n" +
            "هل تريد المتابعة؟";

        if (!BeautifulMessageDialog.ShowConfirm(message, "تبديل نوع النظام"))
            return;

        _systemProfile.ChangeSystem(target);
        _toast.ShowInfo("جاري إعادة تشغيل التطبيق...");
        ApplicationRestartHelper.Restart();
    }

    private static string GetTargetDatabaseName(ApplicationSystemType target) => target switch
    {
        ApplicationSystemType.CarContracts => "AlMuhasibCarContractsDb",
        ApplicationSystemType.HotelManagement => "AlMuhasibHotelsDb",
        _ => "AlMuhasibDb"
    };
}
