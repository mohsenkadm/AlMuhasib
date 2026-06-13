using AlMuhasib.Sync.Responses;

namespace AlMuhasib.Cloud.Application.Models.Mobile;

public sealed class MobileWriteResponse
{
    public Guid SyncId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string Message { get; set; } = "تم الحفظ بنجاح";
    public List<SyncConflict> Conflicts { get; set; } = [];
}
