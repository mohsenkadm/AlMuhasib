namespace AlMuhasib.UI.Services;

public interface IInvoiceDraftService
{
    void SaveDraft<T>(string draftKey, T draft) where T : class;
    T? LoadDraft<T>(string draftKey) where T : class;
    void ClearDraft(string draftKey);
    bool HasDraft(string draftKey);
    DateTime? GetDraftSavedAt(string draftKey);
}
