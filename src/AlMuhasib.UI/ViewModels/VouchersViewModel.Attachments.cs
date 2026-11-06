using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using AlMuhasib.Core.Entities;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class VouchersViewModel
{
    private string _pendingSessionId = Guid.NewGuid().ToString("N");

    public ObservableCollection<PendingVoucherAttachment> PendingAttachments { get; } = [];

    public ObservableCollection<VoucherAttachment> VoucherAttachments { get; } = [];

    [ObservableProperty]
    private bool _isAttachmentsDialogOpen;

    [ObservableProperty]
    private Voucher? _attachmentsVoucher;

    [ObservableProperty]
    private string _pendingAttachmentsSummary = "لا مرفقات بعد";

    private static string AttachmentsBaseFolder =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Attachments", "Vouchers");

    private string PendingSessionFolder =>
        Path.Combine(AttachmentsBaseFolder, "_pending", _pendingSessionId);

    private static string VoucherFolder(int voucherId) =>
        Path.Combine(AttachmentsBaseFolder, voucherId.ToString());

    private const string AttachmentFileFilter =
        "صور ومستندات|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.pdf;*.doc;*.docx;*.xls;*.xlsx|كل الملفات|*.*";

    // ── Create-form pending attachments ────────────────────

    [RelayCommand]
    private void AddPendingAttachmentFromFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = AttachmentFileFilter,
            Multiselect = true,
            Title = "اختر مرفقات السند"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            Directory.CreateDirectory(PendingSessionFolder);
            var added = 0;

            foreach (var filePath in dialog.FileNames)
            {
                if (!File.Exists(filePath)) continue;

                var originalName = Path.GetFileName(filePath);
                var uniqueName = $"{DateTime.Now:yyyyMMdd_HHmmssfff}_{originalName}";
                var destPath = Path.Combine(PendingSessionFolder, uniqueName);
                File.Copy(filePath, destPath, overwrite: true);

                PendingAttachments.Add(new PendingVoucherAttachment
                {
                    FileName = originalName,
                    TempPath = destPath
                });
                added++;
            }

            RefreshPendingSummary();
            if (added > 0)
                BeautifulMessageDialog.ShowSuccess($"تم تجهيز {added} مرفق للحفظ مع السند");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"خطأ أثناء إضافة المرفق: {ex.Message}");
        }
    }

    [RelayCommand]
    private void AddPendingAttachmentFromScanner()
    {
        try
        {
            Directory.CreateDirectory(PendingSessionFolder);
            var fileName = $"scan_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var destPath = Path.Combine(PendingSessionFolder, fileName);

            var result = WiaScannerHelper.AcquireJpegToFile(destPath);
            if (result.Cancelled)
            {
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    BeautifulMessageDialog.ShowWarning(result.ErrorMessage);
                return;
            }

            if (!result.Success || string.IsNullOrWhiteSpace(result.FilePath))
            {
                BeautifulMessageDialog.ShowError(result.ErrorMessage ?? "فشل المسح الضوئي");
                return;
            }

            PendingAttachments.Add(new PendingVoucherAttachment
            {
                FileName = fileName,
                TempPath = result.FilePath
            });
            RefreshPendingSummary();
            BeautifulMessageDialog.ShowSuccess("تم المسح الضوئي وإضافة المرفق");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"خطأ أثناء المسح الضوئي: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RemovePendingAttachment(PendingVoucherAttachment? item)
    {
        if (item is null) return;

        PendingAttachments.Remove(item);
        try
        {
            if (File.Exists(item.TempPath))
                File.Delete(item.TempPath);
        }
        catch
        {
            // ignore disk cleanup failures
        }

        RefreshPendingSummary();
    }

    private void RefreshPendingSummary()
    {
        var count = PendingAttachments.Count;
        PendingAttachmentsSummary = count == 0
            ? "لا مرفقات بعد"
            : count == 1
                ? "مرفق واحد جاهز للحفظ"
                : $"{count} مرفقات جاهزة للحفظ";
    }

    private void ClearPendingAttachments(bool deleteFiles)
    {
        if (deleteFiles)
        {
            foreach (var item in PendingAttachments.ToList())
            {
                try
                {
                    if (File.Exists(item.TempPath))
                        File.Delete(item.TempPath);
                }
                catch
                {
                    // ignore
                }
            }

            try
            {
                if (Directory.Exists(PendingSessionFolder))
                    Directory.Delete(PendingSessionFolder, recursive: true);
            }
            catch
            {
                // ignore
            }
        }

        PendingAttachments.Clear();
        _pendingSessionId = Guid.NewGuid().ToString("N");
        RefreshPendingSummary();
    }

    private async Task PersistPendingAttachmentsAsync(int voucherId)
    {
        if (PendingAttachments.Count == 0) return;

        var folder = VoucherFolder(voucherId);
        Directory.CreateDirectory(folder);

        foreach (var pending in PendingAttachments.ToList())
        {
            if (!File.Exists(pending.TempPath)) continue;

            var uniqueName = $"{DateTime.Now:yyyyMMdd_HHmmssfff}_{pending.FileName}";
            var destPath = Path.Combine(folder, uniqueName);

            File.Move(pending.TempPath, destPath, overwrite: true);

            await _unitOfWork.VoucherAttachments.AddAsync(new VoucherAttachment
            {
                VoucherId = voucherId,
                FileName = pending.FileName,
                FilePath = destPath,
                CreatedBy = _currentUserService.Username
            });
        }

        ClearPendingAttachments(deleteFiles: true);
    }

    // ── List: view / manage attachments ────────────────────

    [RelayCommand]
    private async Task OpenVoucherAttachmentsAsync(Voucher? voucher)
    {
        if (voucher is null) return;

        AttachmentsVoucher = voucher;
        await LoadVoucherAttachmentsAsync(voucher.Id);
        IsAttachmentsDialogOpen = true;
    }

    private async Task LoadVoucherAttachmentsAsync(int voucherId)
    {
        VoucherAttachments.Clear();
        var items = await _unitOfWork.VoucherAttachments.FindAsync(a => a.VoucherId == voucherId);
        foreach (var a in items.OrderByDescending(x => x.CreatedAt))
            VoucherAttachments.Add(a);
    }

    [RelayCommand]
    private async Task AddVoucherAttachmentFromFileAsync()
    {
        if (AttachmentsVoucher is null) return;

        var dialog = new OpenFileDialog
        {
            Filter = AttachmentFileFilter,
            Multiselect = true,
            Title = "اختر مرفقات السند"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var folder = VoucherFolder(AttachmentsVoucher.Id);
            Directory.CreateDirectory(folder);

            foreach (var filePath in dialog.FileNames)
            {
                var fileName = Path.GetFileName(filePath);
                var uniqueName = $"{DateTime.Now:yyyyMMdd_HHmmssfff}_{fileName}";
                var destPath = Path.Combine(folder, uniqueName);
                File.Copy(filePath, destPath, overwrite: true);

                await _unitOfWork.VoucherAttachments.AddAsync(new VoucherAttachment
                {
                    VoucherId = AttachmentsVoucher.Id,
                    FileName = fileName,
                    FilePath = destPath,
                    CreatedBy = _currentUserService.Username
                });
            }

            await LoadVoucherAttachmentsAsync(AttachmentsVoucher.Id);
            BeautifulMessageDialog.ShowSuccess($"تم إرفاق {dialog.FileNames.Length} ملف بنجاح");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"خطأ أثناء الإرفاق: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AddVoucherAttachmentFromScannerAsync()
    {
        if (AttachmentsVoucher is null) return;

        try
        {
            var folder = VoucherFolder(AttachmentsVoucher.Id);
            Directory.CreateDirectory(folder);

            var fileName = $"scan_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var destPath = Path.Combine(folder, fileName);

            var result = WiaScannerHelper.AcquireJpegToFile(destPath);
            if (result.Cancelled)
            {
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    BeautifulMessageDialog.ShowWarning(result.ErrorMessage);
                return;
            }

            if (!result.Success || string.IsNullOrWhiteSpace(result.FilePath))
            {
                BeautifulMessageDialog.ShowError(result.ErrorMessage ?? "فشل المسح الضوئي");
                return;
            }

            await _unitOfWork.VoucherAttachments.AddAsync(new VoucherAttachment
            {
                VoucherId = AttachmentsVoucher.Id,
                FileName = fileName,
                FilePath = result.FilePath,
                CreatedBy = _currentUserService.Username
            });

            await LoadVoucherAttachmentsAsync(AttachmentsVoucher.Id);
            BeautifulMessageDialog.ShowSuccess("تم المسح الضوئي والإرفاق بنجاح");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"خطأ أثناء المسح الضوئي: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenVoucherAttachmentFile(VoucherAttachment? attachment)
    {
        if (attachment is null || !File.Exists(attachment.FilePath))
        {
            BeautifulMessageDialog.ShowWarning("الملف غير موجود على القرص");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = attachment.FilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"لا يمكن فتح الملف: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteVoucherAttachmentAsync(VoucherAttachment? attachment)
    {
        if (attachment is null || AttachmentsVoucher is null) return;

        if (!BeautifulMessageDialog.ShowConfirm($"حذف المرفق «{attachment.FileName}»؟"))
            return;

        try
        {
            await Task.Run(() =>
                _unitOfWork.VoucherAttachments.SoftDelete(attachment, _currentUserService.Username));

            try
            {
                if (File.Exists(attachment.FilePath))
                    File.Delete(attachment.FilePath);
            }
            catch
            {
                // Metadata already soft-deleted.
            }

            await LoadVoucherAttachmentsAsync(AttachmentsVoucher.Id);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"خطأ أثناء حذف المرفق: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CloseVoucherAttachments() => IsAttachmentsDialogOpen = false;
}

public sealed class PendingVoucherAttachment
{
    public string FileName { get; init; } = string.Empty;
    public string TempPath { get; init; } = string.Empty;
}
