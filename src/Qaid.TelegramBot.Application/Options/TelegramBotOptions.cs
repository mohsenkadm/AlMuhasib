namespace Qaid.TelegramBot.Application.Options;

public sealed class TelegramBotOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
    public int RateLimitPerMinute { get; set; } = 20;
}

public sealed class QaidApiOptions
{
    public const string SectionName = "QaidApi";

    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}

public sealed class LinkOptions
{
    public const string SectionName = "Link";

    public int CodeLifetimeMinutes { get; set; } = 5;
    public int CodeLength { get; set; } = 8;
}
