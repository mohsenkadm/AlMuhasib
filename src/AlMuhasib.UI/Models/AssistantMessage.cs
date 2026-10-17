using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Models;

public enum AssistantMessageRole
{
    User,
    Assistant,
    System
}

public sealed class AssistantMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public AssistantMessageRole Role { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public bool IsError { get; init; }

    public bool IsFromUser => Role == AssistantMessageRole.User;
    public bool IsFromAssistant => Role == AssistantMessageRole.Assistant;
}

public sealed class AssistantSuggestion
{
    public required string Title { get; init; }
    public required string Prompt { get; init; }
    public required PackIconKind IconKind { get; init; }
    public required string AccentHex { get; init; }
    public required string BackgroundHex { get; init; }
}
