using Qaid.TelegramBot.Application.Models;
using Qaid.TelegramBot.Application.Reports;

namespace Qaid.TelegramBot.Application.Abstractions;

public interface IReportPdfGenerator
{
    byte[] Generate(ReportDocumentModel model);
}

public interface IReportPdfService
{
    Task<ReportPdfFile> BuildReportPdfAsync(
        ReportKind kind,
        string accessToken,
        string companyName,
        DateRange range,
        CancellationToken ct);

    Task<ReportPdfFile> BuildStatementPdfAsync(
        bool customers,
        Guid syncId,
        string accessToken,
        string companyName,
        CancellationToken ct);

    Task<ReportPdfFile> BuildDashboardPdfAsync(
        int applicationSystemType,
        string accessToken,
        string companyName,
        CancellationToken ct);
}
