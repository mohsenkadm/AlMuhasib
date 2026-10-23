using AlMuhasib.Core.Entities.Customs;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Interfaces.Services.Customs;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.Infrastructure.Data.Customs;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Customs;

public sealed class CustomsAttachmentService : ICustomsAttachmentService
{
    private readonly IDbContextFactory<CustomsDbContext> _contextFactory;
    private readonly CustomsPermissionGuard _permissions;

    public CustomsAttachmentService(
        IDbContextFactory<CustomsDbContext> contextFactory,
        CustomsPermissionGuard permissions)
    {
        _contextFactory = contextFactory;
        _permissions = permissions;
    }

    public static string GetOwnerFolder(CustomsAttachmentOwnerType ownerType, int ownerId) =>
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Attachments",
            "Customs",
            ownerType.ToString(),
            ownerId.ToString());

    public async Task<IReadOnlyList<CustomsAttachmentItem>> GetByOwnerAsync(
        CustomsAttachmentOwnerType ownerType,
        int ownerId,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(_permissions.ScreenForOwner(ownerType));
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var items = await db.CustomsAttachments.AsNoTracking()
            .Where(a => a.OwnerType == ownerType && a.OwnerId == ownerId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(ToItem).ToList();
    }

    public async Task<CustomsAttachmentItem> AddFromFileAsync(
        CustomsAttachmentOwnerType ownerType,
        int ownerId,
        string sourceFilePath,
        string? description,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureEdit(_permissions.ScreenForOwner(ownerType));
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            throw new InvalidOperationException("الملف غير موجود");

        var folder = GetOwnerFolder(ownerType, ownerId);
        Directory.CreateDirectory(folder);

        var originalName = Path.GetFileName(sourceFilePath);
        var uniqueName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{originalName}";
        var destPath = Path.Combine(folder, uniqueName);
        File.Copy(sourceFilePath, destPath, overwrite: true);

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = new CustomsAttachment
        {
            OwnerType = ownerType,
            OwnerId = ownerId,
            FileName = originalName,
            FilePath = destPath,
            Description = description?.Trim() ?? string.Empty,
            CreatedBy = createdBy
        };
        await db.CustomsAttachments.AddAsync(entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToItem(entity);
    }

    public async Task SoftDeleteAsync(int attachmentId, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.CustomsAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken)
            ?? throw new InvalidOperationException("المرفق غير موجود");

        _permissions.EnsureDelete(_permissions.ScreenForOwner(entity.OwnerType));
        var path = entity.FilePath;
        entity.MarkSoftDeleted(deletedBy);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Metadata already soft-deleted; ignore disk failures.
        }
    }

    private static CustomsAttachmentItem ToItem(CustomsAttachment a) => new()
    {
        Id = a.Id,
        FileName = a.FileName,
        FilePath = a.FilePath,
        Description = a.Description,
        CreatedAt = a.CreatedAt
    };
}
