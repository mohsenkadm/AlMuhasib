namespace AlMuhasib.Core.Models.Ux;

public class UserAppPreferences
{
    public bool IsDarkTheme { get; set; }
    public double FontScale { get; set; } = 1.0;
    public List<string> HiddenMenuScreens { get; set; } = [];
    public List<string> PinnedMenuScreens { get; set; } = [];

    /// <summary>قوالب فواتير محفوظة (مبيعات، مشتريات، أقساط).</summary>
    public List<InvoiceTemplate> InvoiceTemplates { get; set; } = [];

    /// <summary>عميل افتراضي لفاتورة مبيعات جديدة.</summary>
    public int? DefaultSalesCustomerId { get; set; }

    /// <summary>مورد افتراضي لفاتورة مشتريات جديدة.</summary>
    public int? DefaultPurchaseSupplierId { get; set; }

    /// <summary>عميل افتراضي لفاتورة أقساط جديدة.</summary>
    public int? DefaultInstallmentCustomerId { get; set; }

    /// <summary>هل أكمل المستخدم جولة التعريف بالميزات.</summary>
    public bool HasCompletedFeatureTour { get; set; }

    /// <summary>مخزن افتراضي لشاشة البيع السريع.</summary>
    public int? DefaultPosWarehouseId { get; set; }

    /// <summary>قاصة افتراضية لشاشة البيع السريع.</summary>
    public int? DefaultPosCashBoxId { get; set; }

    /// <summary>معرّفات المنتجات المفضلة (POS والبيع السريع).</summary>
    public List<int> FavoriteProductIds { get; set; } = [];

    /// <summary>ملف العمل لتبسيط الشريط السريع.</summary>
    public WorkspaceProfile WorkspaceProfile { get; set; } = WorkspaceProfile.Full;

    /// <summary>تفعيل أصوات التفاعل (حفظ، حذف، تنبيه، إلخ).</summary>
    public bool SoundEnabled { get; set; } = true;

    public BusinessFeatureFlags FeatureFlags { get; set; } = new();
    public BackupPreferences Backup { get; set; } = new();
    public ReminderPreferences Reminders { get; set; } = new();

    /// <summary>قفل الشاشة بعد دقائق خمول (0 = معطّل).</summary>
    public int IdleLockMinutes { get; set; }

    /// <summary>الحد الأدنى لمبلغ التقسيط من POS.</summary>
    public decimal PosMinInstallmentAmount { get; set; } = 50_000m;

    /// <summary>وضع عرض القوائم (جدول / بطاقات) لكل شاشة بيانات رئيسية.</summary>
    public Dictionary<string, MasterDataListViewMode> ListViewModes { get; set; } = [];
}
