using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Models.Customs;

namespace AlMuhasib.Core.Interfaces.Services.Customs;

public interface ICustomsAttachmentService
{
    Task<IReadOnlyList<CustomsAttachmentItem>> GetByOwnerAsync(
        CustomsAttachmentOwnerType ownerType,
        int ownerId,
        CancellationToken cancellationToken = default);

    Task<CustomsAttachmentItem> AddFromFileAsync(
        CustomsAttachmentOwnerType ownerType,
        int ownerId,
        string sourceFilePath,
        string? description,
        string createdBy,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(int attachmentId, string deletedBy, CancellationToken cancellationToken = default);
}
