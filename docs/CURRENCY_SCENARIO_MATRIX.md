# مصفوفة اختبار تعدد العملات (IQD / USD)

| # | سيناريو | تحقق متوقع | حالة التنفيذ |
|---|---------|------------|--------------|
| 1 | IQD فقط (بيانات قديمة) | defaults IQD/FxRate=1؛ أرصدة وتقارير بلا تغيّر | مدعوم (هجرة + فلاتر افتراضية) |
| 2 | USD فقط | فاتورة→سند→قاصة بنفس العملة→كشف→تقرير Usd | مدعوم (EnsureSameCurrency + ReportCurrencyScope.Usd) |
| 3 | IQD+USD لنفس العميل | رصيدان منفصلان؛ لا مجموع مختلط | مدعوم (CustomerBalanceHelper + Flutter/API dual) |
| 4 | شراء USD + بيع IQD | تكلفة عبر FxRate اللقطة؛ ربح متسق بالدينار | مدعوم (ProductCostHelper / Cloud twin) |
| 5 | سلفة موظف بعملتين | رصيدان (OpeningBalanceCurrency + Dual) | مدعوم (EmployeeBalanceHelper) |
| 6 | Offline→Sync USD | Currency/FxRate لا يُفقدان | مدعوم (SyncMapper + invoice wizard) |
| 7 | حذف سند FIFO / فاتورة | عكس نقد + تخصيص | مدعوم (#112/#113) |
| 8 | مندوب مبيعات | عمولة/تحصيل موسومان بعملة | مدعوم (SalesRep Currency) |
| 9 | افتتاح Excel | Currency/FxRate في القالب والاستيراد | مدعوم |
| 10 | حد ائتمان | الحد بالدينار + ToBaseIqdStrict / Cloud MaxCreditLimit | مدعوم |

## اختبارات آلية مضافة

- `EmployeeBalanceHelperTests` — فصل العملات وعدم الخلط
- `ReportCurrencyScopeHelperTests` — فلتر النطاق
- `InvoiceFiltersCurrencyScopeTests` — استعلام المبيعات حسب النطاق
- `AccountingCurrencyRulesTests` — FxRate صارم (موجود)

## تشغيل

```bash
dotnet test src/AlMuhasib.Core.Tests/AlMuhasib.Core.Tests.csproj --filter "FullyQualifiedName~EmployeeBalance|FullyQualifiedName~ReportCurrency|FullyQualifiedName~InvoiceFiltersCurrency|FullyQualifiedName~AccountingCurrency"
```
