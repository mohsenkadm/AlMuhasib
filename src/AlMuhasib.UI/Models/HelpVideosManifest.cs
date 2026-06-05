namespace AlMuhasib.UI.Models;

public sealed class HelpVideosManifest
{
    public string SupportWhatsApp { get; set; } = "07505496065";
    public string SupportMessage { get; set; } = "السلام عليكم، أحتاج مساعدة في نظام المحاسب.";
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
