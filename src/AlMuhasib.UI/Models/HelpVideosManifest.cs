namespace AlMuhasib.UI.Models;

public sealed class HelpVideosManifest
{
    public string SupportWhatsApp { get; set; } = "07505496065";
    public string SupportMessage { get; set; } = "السلام عليكم، أحتاج مساعدة في نظام المحاسب.";
    public string TelegramBotUrl { get; set; } = "https://t.me/qaidsystem_bot";
    public string TelegramSubscribeMessage { get; set; } =
        "السلام عليكم، أرغب بالاشتراك في بوت تقارير تيليجرام لنظام قيد — 10 دولار سنوياً.";
    public string AppStoreUrl { get; set; } =
        "https://apps.apple.com/sa/app/%D9%82%D9%8A%D8%AF-%D8%A7%D9%84%D9%85%D8%AD%D8%A7%D8%B3%D8%A8%D9%8A/id6804299551?l=ar";
    public string PlayStoreUrl { get; set; } =
        "https://play.google.com/store/apps/details?id=com.almuhasib.almuhasib_mobile&hl=ar";
    public string MobileSubscribeMessage { get; set; } =
        "السلام عليكم، أرغب بالاشتراك في تطبيق قيد المحاسبي للجوال — 10 دولار سنوياً.";
    public List<HelpVideoCategory> Categories { get; set; } = [];
}

public sealed class HelpVideoCategory
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = "PlayCircleOutline";
    public List<HelpVideoEntry> Videos { get; set; } = [];
}

public sealed class HelpVideoEntry
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string YoutubeUrl { get; set; } = string.Empty;
    public string? Duration { get; set; }
}

public sealed class HelpVideoItemVm
{
    public required string CategoryId { get; init; }
    public required string CategoryTitle { get; init; }
    public required string CategoryIcon { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string YoutubeUrl { get; init; }
    public string? Duration { get; init; }
    public string? VideoId { get; init; }
    public bool HasVideo => !string.IsNullOrWhiteSpace(VideoId);
}
