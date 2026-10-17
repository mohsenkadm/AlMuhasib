using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Services;

/// <summary>
/// Abstraction for the AI assistant backend. Swap <see cref="MockAssistantService"/>
/// with a real implementation later without changing the chat UI.
/// </summary>
public interface IAssistantService
{
    /// <summary>True when a live AI provider is configured and reachable.</summary>
    bool IsLiveServiceEnabled { get; }

    string StatusDescription { get; }

    Task<AssistantMessage> GetReplyAsync(
        string userMessage,
        IReadOnlyList<AssistantMessage> conversationHistory,
        CancellationToken cancellationToken = default);
}
