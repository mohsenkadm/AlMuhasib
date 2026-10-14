using System.Globalization;
using System.Text.Json;

namespace Qaid.TelegramBot.Application.Reports;

/// <summary>
/// Converts arbitrary Qaid report JSON into a printable document model (summary + tables).
/// </summary>
public static class JsonReportDocumentMapper
{
    private const int MaxTableRows = 250;
    private static readonly CultureInfo Ar = CultureInfo.GetCultureInfo("ar-IQ");

    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TotalSales"] = "إجمالي المبيعات",
        ["TotalSalesUsd"] = "مبيعات دولار",
        ["TotalPurchases"] = "إجمالي المشتريات",
        ["TotalPurchasesUsd"] = "مشتريات دولار",
        ["TotalExpenses"] = "إجمالي المصروفات",
        ["TodayExpenses"] = "مصروفات اليوم",
        ["MonthExpenses"] = "مصروفات الشهر",
        ["TopExpenseType"] = "أعلى نوع مصروف",
        ["InvoiceCount"] = "عدد الفواتير",
        ["DayCount"] = "عدد الأيام",
        ["AverageDaily"] = "متوسط يومي",
        ["AverageInvoice"] = "متوسط الفاتورة",
        ["CostOfGoodsSold"] = "تكلفة البضاعة",
        ["GrossProfit"] = "مجمل الربح",
        ["OperatingProfit"] = "ربح التشغيل",
        ["NetProfit"] = "صافي الربح",
        ["GrossMarginPercent"] = "هامش مجمل %",
        ["NetMarginPercent"] = "هامش صافي %",
        ["CashBoxesTotal"] = "الصناديق",
        ["BanksTotal"] = "البنوك",
        ["TotalLiquid"] = "الإجمالي السائل",
        ["TotalLiquidUsd"] = "إجمالي دولار",
        ["AccountCount"] = "عدد الحسابات",
        ["TotalIncoming"] = "الوارد",
        ["TotalOutgoing"] = "الصادر",
        ["NetFlow"] = "صافي التدفق",
        ["CurrentBalance"] = "الرصيد الحالي",
        ["CustomerCount"] = "عدد الزبائن",
        ["SupplierCount"] = "عدد الموردين",
        ["TotalCollected"] = "المحصّل",
        ["TotalOutstanding"] = "المتبقي",
        ["TotalOutstandingUsd"] = "متبقي دولار",
        ["CashSales"] = "مبيعات نقدية",
        ["CreditSales"] = "مبيعات آجل",
        ["InstallmentSales"] = "مبيعات أقساط",
        ["SalesReturns"] = "مردودات",
        ["SupplierPayments"] = "مدفوعات الموردين",
        ["Receipts"] = "المقبوضات",
        ["NetAmount"] = "الصافي",
        ["CashSalesInvoiceCount"] = "فواتير نقدية",
        ["ExpenseCount"] = "عدد المصروفات",
        ["OverdueCustomerCount"] = "زبائن متأخرون",
        ["TotalOverdueAmount"] = "إجمالي المتأخر",
        ["TotalOverdue"] = "إجمالي المتأخر",
        ["TopOverdueCustomer"] = "أعلى متأخر",
        ["AverageOverdueDays"] = "متوسط أيام التأخير",
        ["AverageDaysOverdue"] = "متوسط أيام التأخير",
        ["TotalAmount"] = "الإجمالي",
        ["PaidAmount"] = "المدفوع",
        ["UnpaidAmount"] = "غير المدفوع",
        ["OverdueAmount"] = "المتأخر",
        ["TotalCount"] = "العدد الكلي",
        ["PaidCount"] = "عدد المدفوع",
        ["UnpaidCount"] = "عدد غير المدفوع",
        ["OverdueCount"] = "عدد المتأخر",
        ["ItemCount"] = "عدد البنود",
        ["RowCount"] = "عدد الصفوف",
        ["TotalItems"] = "إجمالي الأصناف",
        ["BelowMinimumCount"] = "تحت الحد",
        ["AtMinimumCount"] = "عند الحد",
        ["AboveMinimumCount"] = "فوق الحد",
        ["TotalShortage"] = "إجمالي النقص",
        ["LowStockCount"] = "مخزون منخفض",
        ["DeadStockCount"] = "مخزون راكد",
        ["TotalDeadStockValue"] = "قيمة الراكد",
        ["TotalRevenue"] = "إجمالي الإيراد",
        ["TotalRevenueUsd"] = "إيراد دولار",
        ["TotalQuantity"] = "إجمالي الكمية",
        ["ProductCount"] = "عدد المنتجات",
        ["WarehouseCount"] = "عدد المخازن",
        ["TotalPaid"] = "إجمالي المدفوع",
        ["VoucherCollections"] = "تحصيل سندات",
        ["InstallmentCollections"] = "تحصيل أقساط",
        ["VoucherPayments"] = "مدفوعات سندات",
        ["CashPurchases"] = "مشتريات نقدية",
        ["TotalAssets"] = "الأصول",
        ["TotalLiabilities"] = "الالتزامات",
        ["TotalEquity"] = "حقوق الملكية",
        ["NetWorkingCapital"] = "رأس المال العامل",
        ["Difference"] = "الفرق",
        ["IsBalanced"] = "متوازن",
        ["TotalCogs"] = "إجمالي التكلفة",
        ["TotalValue"] = "إجمالي القيمة",
        ["TransferCount"] = "عدد التحويلات",
        ["AverageAmount"] = "متوسط المبلغ",
        ["LastSyncAt"] = "آخر مزامنة",
        ["IsOnline"] = "متصل",
        ["PendingPushCount"] = "بانتظار الإرسال",
        ["PendingPullCount"] = "بانتظار السحب",
        ["CustomerName"] = "الزبون",
        ["SupplierName"] = "المورد",
        ["TotalDebit"] = "مدين",
        ["TotalCredit"] = "دائن",
        ["Balance"] = "الرصيد",
        ["BalanceUsd"] = "رصيد دولار",
        ["TransactionCount"] = "عدد الحركات"
    };

    public static ReportDocumentModel Map(
        string title,
        string companyName,
        string periodLabel,
        JsonElement root)
    {
        var summary = new List<ReportSummaryItem>();
        var tables = new List<ReportTableModel>();

        if (root.ValueKind == JsonValueKind.Array)
        {
            tables.Add(MapArrayTable("التفاصيل", root));
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind is JsonValueKind.Array)
                {
                    var tableTitle = LabelOf(prop.Name);
                    if (string.Equals(prop.Name, "Rows", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(prop.Name, "Items", StringComparison.OrdinalIgnoreCase))
                        tableTitle = "التفاصيل";
                    else if (string.Equals(prop.Name, "Buckets", StringComparison.OrdinalIgnoreCase))
                        tableTitle = "الفئات";
                    tables.Add(MapArrayTable(tableTitle, prop.Value));
                }
                else if (prop.Value.ValueKind is JsonValueKind.Object)
                {
                    foreach (var nested in prop.Value.EnumerateObject())
                    {
                        if (IsScalar(nested.Value))
                            summary.Add(new ReportSummaryItem
                            {
                                Label = $"{LabelOf(prop.Name)} / {LabelOf(nested.Name)}",
                                Value = FormatValue(nested.Value)
                            });
                    }
                }
                else if (IsScalar(prop.Value))
                {
                    summary.Add(new ReportSummaryItem
                    {
                        Label = LabelOf(prop.Name),
                        Value = FormatValue(prop.Value)
                    });
                }
            }
        }
        else
        {
            summary.Add(new ReportSummaryItem { Label = "القيمة", Value = FormatValue(root) });
        }

        if (summary.Count == 0 && tables.Count == 0)
            summary.Add(new ReportSummaryItem { Label = "الحالة", Value = "لا توجد بيانات للعرض" });

        return new ReportDocumentModel
        {
            Title = title,
            CompanyName = companyName,
            PeriodLabel = periodLabel,
            GeneratedAtLabel = DateTime.Now.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture),
            Summary = summary,
            Tables = tables.Where(t => t.Rows.Count > 0 || t.Headers.Count > 0).ToList()
        };
    }

    private static ReportTableModel MapArrayTable(string title, JsonElement array)
    {
        if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() == 0)
            return new ReportTableModel { Title = title, Headers = [], Rows = [] };

        var first = array[0];
        if (first.ValueKind != JsonValueKind.Object)
        {
            var simpleRows = array.EnumerateArray()
                .Take(MaxTableRows)
                .Select(x => (IReadOnlyList<string>)[FormatValue(x)])
                .ToList();
            return new ReportTableModel
            {
                Title = title,
                Headers = ["القيمة"],
                Rows = simpleRows
            };
        }

        var headers = first.EnumerateObject()
            .Where(p => IsScalar(p.Value))
            .Select(p => p.Name)
            .Take(8)
            .ToList();

        if (headers.Count == 0)
            return new ReportTableModel { Title = title, Headers = [], Rows = [] };

        var rows = new List<IReadOnlyList<string>>();
        foreach (var item in array.EnumerateArray().Take(MaxTableRows))
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;
            var cells = new List<string>(headers.Count);
            foreach (var h in headers)
            {
                cells.Add(item.TryGetProperty(h, out var v) ? FormatValue(v) : "");
            }
            rows.Add(cells);
        }

        return new ReportTableModel
        {
            Title = title,
            Headers = headers.Select(LabelOf).ToList(),
            Rows = rows
        };
    }

    private static bool IsScalar(JsonElement el)
        => el.ValueKind is JsonValueKind.String or JsonValueKind.Number
            or JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null;

    private static string LabelOf(string name)
        => Labels.TryGetValue(name, out var ar) ? ar : SplitPascal(name);

    private static string SplitPascal(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;
        var chars = new List<char>(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                chars.Add(' ');
            chars.Add(c);
        }
        return new string(chars.ToArray());
    }

    private static string FormatValue(JsonElement el)
        => el.ValueKind switch
        {
            JsonValueKind.Null => "—",
            JsonValueKind.True => "نعم",
            JsonValueKind.False => "لا",
            JsonValueKind.Number => FormatNumber(el),
            JsonValueKind.String => FormatString(el.GetString() ?? ""),
            _ => el.ToString()
        };

    private static string FormatNumber(JsonElement el)
    {
        if (el.TryGetInt64(out var l) && Math.Abs(l) < 1_000_000_000)
            return l.ToString("#,##0", Ar);
        if (el.TryGetDecimal(out var d))
            return d.ToString("#,##0.##", Ar);
        return el.GetRawText();
    }

    private static string FormatString(string value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt)
            || DateTime.TryParse(value, out dt))
            return dt.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
        return value.Length > 48 ? value[..45] + "…" : value;
    }
}
