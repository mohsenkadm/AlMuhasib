# تقرير تدقيق تعدد العملات (دينار / دولار) — محدّث

**النطاق:** صحة البيانات والمنطق المحاسبي والأرصدة وتأثير العملة.  
**الأساس:** النظام كان IQD فقط ثم أُضيف USD (`AccountingCurrency`: IQD=0, USD=1).  
**الحالة:** محدّث بعد دمج #111–#115 (تدقيق + إصلاحات سلامة + إفصاح جزئي).

---

## النموذج المحاسبي

- لا يوجد `JournalEntry`؛ محاسبة وثائقية (فاتورة / سند / مصروف / تحويل).
- العملة: enum نصي على المستند + `FxRate` لقطة وقت التسجيل (لا `CurrencyId` / لا `BaseAmount` مخزّن).
- القاصة/البنك: عملة ثابتة لكل حساب (رصيد واحد).
- ذمم العميل/المورد: محسوبة مزدوجة عبر `CustomerBalanceHelper` / `SupplierBalanceHelper`.
- مستثمر / رأس مال / توزيع أرباح: IQD فقط ومحصّن.
- فروق صرف: غير موجودة في التصميم.

الهجرة `20260925210000_AddAccountingMultiCurrency` آمنة للقديم (defaults IQD / FxRate=1).

---

## ما أُغلق (CRITICAL السابقة)

| # | المشكلة | الإصلاح |
|---|---------|---------|
| 1 | SyncMapper يسقط Currency/FxRate | #112 — Map + RequireFxRate |
| 2 | حذف سند FIFO بلا عكس تخصيص | #112 — Deallocate |
| 3 | حذف فاتورة بلا عكس نقد السندات | #112/#113 |
| 4 | مصروفات USD في أرباح المستثمر | #112 — فلتر IQD + قفل قاصة |
| 5 | Cloud Currency بلا HasConversion | #112 |
| 6 | ذهب FxRate=1 صامت | #113 |
| 7 | افتتاح بلا Currency في API/UI | #113/#114 |
| 8 | إفصاح مبيعات/مشتريات/أرباح/لوحة/تقادم أقساط | #114 |

---

## ما أُغلق في تنفيذ التدقيق الكامل (هذا الفرع)

| # | المشكلة | الإصلاح |
|---|---------|---------|
| 1 | سلف موظفين مختلطة | `OpeningBalanceCurrency` + `EmployeeBalanceHelper` → `DualCurrencyBalance` |
| 2 | COGS يتجاهل مشتريات USD | `ProductCostHelper` / Cloud twin يحوّلان عبر FxRate اللقطة |
| 3 | تقارير بلا نطاق عملة | `ReportCurrencyScope` + فلتر UI + *Usd على يومية/تحصيلات/COGS/P&L |
| 4 | مندوب مبيعات بلا عملة | `Currency` على Collection/CommissionEntry |
| 5 | افتتاح Excel/Update بلا Currency | ImportRow + UpdateRequest + قالب Excel |
| 6 | Flutter/API رصيد واحد | `balanceIqd`/`balanceUsd` + عرض مزدوج |
| 7 | Cloud MaxCreditLimit | عمود + فحص ائتمان بالموبايل |

### متبقٍ / خارج المرحلة (متعمد أو منتج)
- مستثمر / رأس مال / توزيع أرباح: IQD فقط ومحصّن
- فروق صرف: غير موجودة في التصميم
- بعض التقارير الفرعية ما زالت IQD-default مع إفصاح جزئي — وسّع `currencyScope` تدريجياً
- `ExchangeRate.RateDate` بلا unique
- وحدات فندق/سيارات/عقارات خارج مخطط المحاسبة

---

## سياسة التكلفة المعتمدة (مرحلة التنفيذ)

عند حساب متوسط التكلفة / COGS: بند الشراء USD يُحوَّل إلى IQD عبر `FxRate` المخزّن على فاتورة الشراء فقط؛ مبلغ الفاتورة يبقى USD. تقارير المخزون تبقى تكلفة أساسية بالدينار مع إفصاح مشتريات USD منفصل.

---

## مسار العملة المطلوب

`Input → Document(Currency,FxRate) → EnsureSameCurrency → Cash/AR-AP → Reports(dual) → Sync → Cloud → Flutter`
