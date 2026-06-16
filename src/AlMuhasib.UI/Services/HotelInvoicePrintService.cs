using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Shared.Services;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.Services;

public sealed class HotelInvoicePrintService : IHotelInvoicePrintService
{
    private static readonly CultureInfo ArabicCulture = CultureInfo.GetCultureInfo("ar-IQ");
    private static readonly Brush BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));
    private static readonly Brush HeaderBg = new SolidColorBrush(Color.FromRgb(0xD9, 0xD9, 0xD9));

    public void PrintReservationInvoice(Reservation reservation, int copies = 1)
    {
        var document = BuildFlowDocument(reservation);
        DocumentPrintHelper.PrintWithPreview(document, $"فاتورة {reservation.ReservationNumber}", defaultCopies: copies);
    }

    private static FlowDocument BuildFlowDocument(Reservation reservation)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 12,
            FlowDirection = FlowDirection.RightToLeft,
            PagePadding = new Thickness(36, 20, 36, 28)
        };

        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc);

        doc.Blocks.Add(new Paragraph(new Run("فاتورة حجز"))
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        });

        var info = new Table { CellSpacing = 0 };
        info.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        info.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var rowGroup = new TableRowGroup();
        var row = new TableRow();

        row.Cells.Add(CreateCell($"رقم الحجز: {reservation.ReservationNumber}"));
        row.Cells.Add(CreateCell($"التاريخ: {DateTime.Now.ToString("yyyy/MM/dd", ArabicCulture)}"));
        rowGroup.Rows.Add(row);

        var row2 = new TableRow();
        row2.Cells.Add(CreateCell($"النزيل: {reservation.Guest?.FullName ?? "—"}"));
        row2.Cells.Add(CreateCell($"الهاتف: {reservation.Guest?.Phone ?? "—"}"));
        rowGroup.Rows.Add(row2);

        var row3 = new TableRow();
        row3.Cells.Add(CreateCell($"الغرفة: {reservation.Room?.RoomNumber ?? "—"}"));
        row3.Cells.Add(CreateCell($"النوع: {reservation.Room?.RoomType?.Name ?? "—"}"));
        rowGroup.Rows.Add(row3);

        var nights = Math.Max(1, (reservation.CheckOutDate.Date - reservation.CheckInDate.Date).Days);
        var row4 = new TableRow();
        row4.Cells.Add(CreateCell($"الوصول: {reservation.CheckInDate:yyyy/MM/dd}"));
        row4.Cells.Add(CreateCell($"المغادرة: {reservation.CheckOutDate:yyyy/MM/dd} ({nights} ليلة)"));
        rowGroup.Rows.Add(row4);

        info.RowGroups.Add(rowGroup);
        doc.Blocks.Add(info);

        doc.Blocks.Add(new Paragraph(new Run($"الإجمالي: {reservation.TotalAmount:N0}"))
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 16, 0, 4)
        });
        doc.Blocks.Add(new Paragraph(new Run($"المدفوع: {reservation.AmountPaid:N0}")));
        doc.Blocks.Add(new Paragraph(new Run($"المتبقي: {reservation.RemainingAmount:N0}")
        {
            Foreground = reservation.RemainingAmount > 0 ? Brushes.DarkRed : Brushes.Black
        }));

        if (!string.IsNullOrWhiteSpace(reservation.Notes))
        {
            doc.Blocks.Add(new Paragraph(new Run($"ملاحظات: {reservation.Notes}"))
            {
                Margin = new Thickness(0, 12, 0, 0),
                FontStyle = FontStyles.Italic
            });
        }

        doc.Blocks.Add(new Paragraph(new Run($"الحالة: {HotelDisplayHelper.GetReservationStatusLabel(reservation.Status)}"))
        {
            Margin = new Thickness(0, 16, 0, 0)
        });

        return doc;
    }

    private static TableCell CreateCell(string text) => new(new Paragraph(new Run(text)))
    {
        BorderBrush = BorderBrush,
        BorderThickness = new Thickness(1),
        Padding = new Thickness(8, 6, 8, 6),
        Background = HeaderBg
    };
}
