using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;

namespace AlMuhasib.Infrastructure.Services;

public class PrintBrandingService : IPrintBrandingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBranchContext? _branchContext;

    public PrintBrandingService(IUnitOfWork unitOfWork, IBranchContext? branchContext = null)
    {
        _unitOfWork = unitOfWork;
        _branchContext = branchContext;
    }

    public async Task<PrintBrandingSettings> GetOrCreateSettingsAsync()
    {
        var existing = await GetSingletonAsync();
        if (existing is not null)
            return existing;

        var created = new PrintBrandingSettings
        {
            ShowHeaderText = true,
            ShowFooterText = true,
            // AppDbContext write guards stamp BranchId from the current write context.
            BranchId = _branchContext?.HasWriteBranchContext == true
                ? _branchContext.CurrentBranchId!.Value
                : 0,
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.PrintBrandingSettings.AddAsync(created);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return created;
    }

    public async Task<PrintBrandingSnapshot> GetSnapshotAsync()
    {
        var settings = await GetOrCreateSettingsAsync();
        return ToSnapshot(settings);
    }

    public async Task SaveAsync(PrintBrandingSettings settings)
    {
        settings.UpdatedAt = DateTime.UtcNow;

        // معاملة نشطة تمنع Repository.Update من استدعاء SaveChanges المتزامن على خيط الواجهة.
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var existing = await GetSingletonAsync();
            PrintBrandingSettings saved;

            if (existing is null)
            {
                settings.Id = 0;
                if (settings.BranchId <= 0 && _branchContext?.HasWriteBranchContext == true)
                    settings.BranchId = _branchContext.CurrentBranchId!.Value;
                await _unitOfWork.PrintBrandingSettings.AddAsync(settings);
                saved = settings;
            }
            else
            {
                CopySettings(settings, existing);
                existing.UpdatedAt = settings.UpdatedAt;
                existing.UpdatedBy = settings.UpdatedBy;
                // الكيان متتبَّع داخل سياق المعاملة — لا تستدعِ Update() المنفصل.
                saved = existing;
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            settings.Id = saved.Id;
            PrintBrandingProvider.Update(ToSnapshot(saved));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task RefreshProviderAsync()
    {
        PrintBrandingProvider.Update(await GetSnapshotAsync());
    }

    private async Task<PrintBrandingSettings?> GetSingletonAsync()
    {
        var items = await _unitOfWork.PrintBrandingSettings.GetAllAsync();
        return items.FirstOrDefault();
    }

    private static void CopySettings(PrintBrandingSettings source, PrintBrandingSettings target)
    {
        target.CompanyName = source.CompanyName;
        target.Address = source.Address;
        target.PhonePrimary = source.PhonePrimary;
        target.PhoneSecondary = source.PhoneSecondary;
        target.Email = source.Email;
        target.Details = source.Details;
        target.CompanyIdNumber = source.CompanyIdNumber;
        target.CompanyIdIssuer = source.CompanyIdIssuer;
        target.ShowHeaderText = source.ShowHeaderText;
        target.ShowHeaderImage = source.ShowHeaderImage;
        target.HeaderImageData = source.HeaderImageData;
        target.HeaderImageContentType = source.HeaderImageContentType;
        target.ShowFooterText = source.ShowFooterText;
        target.FooterText = source.FooterText;
        target.ShowFooterImage = source.ShowFooterImage;
        target.FooterImageData = source.FooterImageData;
        target.FooterImageContentType = source.FooterImageContentType;
    }

    internal static PrintBrandingSnapshot ToSnapshot(PrintBrandingSettings s) => new()
    {
        CompanyName = s.CompanyName ?? string.Empty,
        Address = s.Address ?? string.Empty,
        PhonePrimary = s.PhonePrimary ?? string.Empty,
        PhoneSecondary = s.PhoneSecondary ?? string.Empty,
        Email = s.Email ?? string.Empty,
        Details = s.Details ?? string.Empty,
        CompanyIdNumber = s.CompanyIdNumber ?? string.Empty,
        CompanyIdIssuer = s.CompanyIdIssuer ?? string.Empty,
        ShowHeaderText = s.ShowHeaderText,
        ShowHeaderImage = s.ShowHeaderImage,
        HeaderImageData = s.HeaderImageData,
        ShowFooterText = s.ShowFooterText,
        FooterText = s.FooterText ?? string.Empty,
        ShowFooterImage = s.ShowFooterImage,
        FooterImageData = s.FooterImageData
    };
}
