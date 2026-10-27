using AlMuhasib.UI.ViewModels;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Services;

/// <summary>
/// يفتح شاشات النظام (مثل التخليص الكمركي) في نوافذ ويندوز منفصلة بملء الشاشة.
/// </summary>
public interface IModuleDocumentWindowService
{
    /// <summary>
    /// إن كانت الشاشة من شاشات المستندات المنفصلة، يفتحها ويرجع true.
    /// وإلا يرجع false ليُكمل المتصل مسار التبويبات.
    /// </summary>
    Task<bool> TryOpenAsync(
        Type viewModelType,
        string title,
        PackIconKind icon,
        bool activateIfExists,
        string? permissionScreenName);

    /// <summary>يغلق النافذة المستضيفة لهذا الـ ViewModel إن وُجدت.</summary>
    bool TryCloseForViewModel(ViewModelBase viewModel);

    /// <summary>هل توجد نافذة مفتوحة لهذا النوع؟</summary>
    bool IsOpen(Type viewModelType);

    /// <summary>يغلق كل نوافذ المستندات (مع فالديشن غير المحفوظ؛ يرجع false إن ألغى المستخدم).</summary>
    bool TryCloseAll();

    /// <summary>إغلاق قسري لكل النوافذ عند إنهاء التطبيق.</summary>
    void ForceCloseAll();
}
