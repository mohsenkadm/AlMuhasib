using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Models.Ux;

public sealed class VoiceCommandDefinition
{
    public required string Id { get; init; }
    public required string DisplayLabel { get; init; }
    public required IReadOnlyList<string> Phrases { get; init; }
    public required VoiceCommandActionType ActionType { get; init; }
    public Type? ViewModelType { get; init; }
    public string? ScreenName { get; init; }
    public string? TabTitle { get; init; }
    public string? SuccessMessage { get; init; }
}

public sealed class VoiceCommandMatch
{
    public required VoiceCommandDefinition Command { get; init; }
    public required double Score { get; init; }
    public string? RecognizedPhrase { get; init; }
}

public sealed class VoiceCommandResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool CloseAssistant { get; init; }
}
