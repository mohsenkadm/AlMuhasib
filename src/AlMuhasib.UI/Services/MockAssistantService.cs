using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Services;

/// <summary>
/// UI-stage placeholder. Does not call any AI API or touch accounting data.
/// Replace via DI with a live implementation when ready.
/// </summary>
public sealed class MockAssistantService : IAssistantService
{
    public bool IsLiveServiceEnabled => false;

    public string StatusDescription => "جاهز للمساعدة";

    public async Task<AssistantMessage> GetReplyAsync(
        string userMessage,
        IReadOnlyList<AssistantMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(700, cancellationToken).ConfigureAwait(false);

        var trimmed = (userMessage ?? string.Empty).Trim();
        var reply =
            "واجهة المساعد الذكي جاهزة حالياً للتجربة فقط.\n\n" +
            "لم يتم ربط خدمة الذكاء الاصطناعي الحقيقية بعد، لذلك لا يمكن تحليل البيانات المحاسبية أو جلب التقارير في هذه المرحلة.\n\n" +
            (string.IsNullOrEmpty(trimmed)
                ? "اكتب سؤالك عندما تصبح الخدمة متاحة."
                : $"تم استلام سؤالك: «{trimmed}» — سيُعالَج عبر الخدمة الحقيقية لاحقاً.");

        return new AssistantMessage
        {
            Role = AssistantMessageRole.Assistant,
            Content = reply,
            Timestamp = DateTime.Now
        };
    }
}
