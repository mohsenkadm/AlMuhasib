namespace AlMuhasib.Core.Models.Ux;

public enum WhatsAppApiProvider
{
    UltraMsg = 0,
    MetaCloud = 1,
    CustomWebhook = 2
}

/// <summary>إعدادات إرسال واتساب عبر API — تُخزَّن في user-preferences.json</summary>
public class WhatsAppApiSettings
{
    public WhatsAppApiProvider Provider { get; set; } = WhatsAppApiProvider.UltraMsg;

    /// <summary>عنوان الأساس لـ API (اختياري حسب المزوّد).</summary>
    public string? BaseUrl { get; set; }

    /// <summary>مفتاح API / Access Token.</summary>
    public string? ApiKey { get; set; }

    /// <summary>معرّف المثيل (UltraMsg) أو Phone Number Id (Meta).</summary>
    public string? InstanceId { get; set; }

    /// <summary>معرّف إضافي للمرسل عند الحاجة.</summary>
    public string? SenderId { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        (Provider != WhatsAppApiProvider.UltraMsg || !string.IsNullOrWhiteSpace(InstanceId)) &&
        (Provider != WhatsAppApiProvider.MetaCloud || !string.IsNullOrWhiteSpace(InstanceId)) &&
        (Provider != WhatsAppApiProvider.CustomWebhook || !string.IsNullOrWhiteSpace(BaseUrl));
}
