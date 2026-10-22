using AlMuhasib.Core.Enums.Customs;

namespace AlMuhasib.Core.Entities.Customs;

public class CustomsAttachment : BaseEntity
{
    public CustomsAttachmentOwnerType OwnerType { get; set; }
    public int OwnerId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
