using System.Text.Json;
using Microsoft.Extensions.Logging;
using Qaid.TelegramBot.Application.Abstractions;
using Qaid.TelegramBot.Application.Models;
using Qaid.TelegramBot.Application.Reports;
using Qaid.TelegramBot.Application.Security;

namespace Qaid.TelegramBot.Infrastructure.Reports;

public sealed class ReportPdfService : IReportPdfService
{
    private readonly IQaidApiClient _api;
    private readonly IReportPdfGenerator _pdf;
    private readonly ILogger<ReportPdfService> _logger;

    public ReportPdfService(IQaidApiClient api, IReportPdfGenerator pdf, ILogger<ReportPdfService> logger)
    {
        _api = api;
        _pdf = pdf;
        _logger = logger;
    }

    public async Task<ReportPdfFile> BuildReportPdfAsync(
        ReportKind kind,
        string accessToken,
        string companyName,
        DateRange range,
        CancellationToken ct)
    {
        if (!ReportCatalog.TryGet(kind, out var def))
            throw new InvalidOperationException($"Unknown report kind: {kind}");

        var query = def.NeedsPeriod ? DateQuery(range.From, range.To) : null;
        var json = await _api.GetJsonAsync(def.Endpoint, accessToken, query, ct);
        var title = $"{def.Icon} {def.Title}";
        var period = def.NeedsPeriod ? range.Label : "حتى تاريخ اليوم";
        var model = JsonReportDocumentMapper.Map(title, companyName, period, json);
        var bytes = _pdf.Generate(model);
        var safeName = SanitizeFileName(def.Title);
        return new ReportPdfFile
        {
            Content = bytes,
            FileName = $"Qaid_{safeName}_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
            Caption = $"{title}\nالفترة: {period}\nالشركة: {companyName}"
        };
    }

    public async Task<ReportPdfFile> BuildStatementPdfAsync(
        bool customers,
        Guid syncId,
        string accessToken,
        string companyName,
        CancellationToken ct)
    {
        var path = customers ? AllowedEndpoints.CustomerStatement : AllowedEndpoints.SupplierStatement;
        var query = customers
            ? $"customerSyncId={syncId:D}"
            : $"supplierSyncId={syncId:D}";
        var json = await _api.GetJsonAsync(path, accessToken, query, ct);
        var title = customers ? "👤 كشف حساب زبون" : "🏭 كشف حساب مورد";
        var model = JsonReportDocumentMapper.Map(title, companyName, "كامل الفترة المتاحة", json);
        var bytes = _pdf.Generate(model);
        return new ReportPdfFile
        {
            Content = bytes,
            FileName = $"Qaid_Statement_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
            Caption = $"{title}\nالشركة: {companyName}"
        };
    }

    public async Task<ReportPdfFile> BuildDashboardPdfAsync(
        int applicationSystemType,
        string accessToken,
        string companyName,
        CancellationToken ct)
    {
        var (path, title) = applicationSystemType switch
        {
            1 => (AllowedEndpoints.CarDashboard, "🚗 لوحة نظام السيارات"),
            2 => (AllowedEndpoints.HotelRestaurantSummary, "🏨 ملخص المطعم/الفندق"),
            3 => (AllowedEndpoints.CarTradeDashboard, "🚙 لوحة تجارة السيارات"),
            4 => (AllowedEndpoints.RealEstateDashboard, "🏠 لوحة العقارات"),
            5 => (AllowedEndpoints.GoldStock, "🥇 مخزون الذهب"),
            _ => (AllowedEndpoints.FinancialPositionSummary, "📐 الوضع المالي")
        };

        try
        {
            var json = await _api.GetJsonAsync(path, accessToken, query: null, ct);
            var model = JsonReportDocumentMapper.Map(title, companyName, "لوحة النظام", json);
            var bytes = _pdf.Generate(model);
            return new ReportPdfFile
            {
                Content = bytes,
                FileName = $"Qaid_Dashboard_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                Caption = $"{title}\nالشركة: {companyName}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dashboard PDF failed for system type {Type}", applicationSystemType);
            throw;
        }
    }

    private static string DateQuery(DateTime? from, DateTime? to)
    {
        var parts = new List<string>();
        if (from is not null)
            parts.Add("from=" + Uri.EscapeDataString(from.Value.ToString("o")));
        if (to is not null)
            parts.Add("to=" + Uri.EscapeDataString(to.Value.ToString("o")));
        return string.Join("&", parts);
    }

    private static string SanitizeFileName(string title)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(title.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        cleaned = cleaned.Replace(' ', '_');
        return cleaned.Length > 40 ? cleaned[..40] : cleaned;
    }
}
