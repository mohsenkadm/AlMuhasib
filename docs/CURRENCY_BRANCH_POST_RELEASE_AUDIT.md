# تدقيق ما بعد الدولار والفروع — API / Sync / التقارير

**التاريخ:** 2026-10-02  
**النطاق:** محاسبة Desktop ↔ Cloud API ↔ Sync ↔ Reports عند `MultiCurrencyEnabled`

## بيئة الاختبار (المرحلة 0)

| عنصر | القيمة |
|------|--------|
| Tenant | تجريبي بفرعين `MAIN` و `BRANCH-B` |
| OFF | `MultiCurrencyEnabled=false` — سلوك عملاء قدماء |
| ON | ميزة مفعّلة + سعر صرف + قاصة IQD + قاصة USD |
| بذرة | فواتير/سندات/مصاريف بالعملتين، صيرفة، أسعار منتج USD، ذمم دولار |

سكربت مساعدة: `scripts/seed-multicurrency-branch-audit.sql` (اختياري على DB محلي).

## مصفوفة الإصلاحات

| ID | الحالة | الوصف |
|----|--------|-------|
| H1 | Fixed | بوابة MultiCurrency على SyncEngine للكتابات المالية + replay تاريخي USD موجود |
| H2 | Fixed | تغيير MultiCurrencyEnabled عبر API مقتصر على حساب المالك (أقدم حساب نشط) → 403 |
| H3 | Fixed | heal الفرع عند Login للحسابات الجديدة فقط (≤24 ساعة) |
| H4 | Fixed | تقرير فواتير الذهب المحذوفة مقيّد بـ AllowedBranchIds / BranchId |
| G1 | Fixed | SalePriceUsd / PurchasePriceUsd في CloudProductPrice + Sync push/pull + migration |
| G2 | Fixed | مزامنة CurrencyExchanges كاملة (DTO + SyncEngine + SyncMapper + جدول Cloud) |
| G3 | Fixed | SettlementCurrency على VoucherSyncDto مع ترميز Notes `[FX-SETTLE:…]` |
| R1 | Fixed | `currencyScope` في ReportsController لـ DailySales / Collections / COGS / P&L |
| R2 | Fixed | Aging: صفوف IQD+USD منفصلة عند ON؛ المجاميع لا تُخلط |
| R3 | Fixed | Expenses: Fold USD→IQD عند ON (Desktop + Cloud) |
| R4 | Fixed | DailySales: صف يوم واحد مطوي عند ON (Desktop + Cloud) |
| R5 | Fixed | Bank statement: يشمل كل المصارف عند ON بدون bankId |
| Tests | Fixed | `PostReleaseCurrencyBranchAuditTests` (6 passed) |

## قرارات منتج

- ذهب: نظام عملة مستقل عن `MultiCurrencyEnabled` المحاسبي.
- مستثمرون / رأس المال: IQD فقط بالتصميم.
- صيرفة: إلزامية في Sync بعد هذا الإصلاح.
- تفعيل تنوع العملات عبر API: مالك المستأجر فقط؛ Desktop sync يبقى مصدر الحقيقة لإعدادات BusinessSettings.

## Smoke أمني (يدوي قبل الإنتاج)

1. OFF + mobile USD invoice → 400
2. OFF + sync push USD جديد → conflict/reject
3. غير Owner يفعّل MultiCurrency عبر API → 403
4. Token فرع A + `X-Branch-Id: B` → 403
5. SyncId من فرع B ضمن سياق A → 404
6. حذف تعيين فرع لحساب قديم ثم login → بدون صلاحية
7. ذهب فرع A لا يرى محذوفات فرع B

## شرط الرفع

H1 و G1 و G2 مغلقة. يُنصح بإكمال Smoke اليدوي على Tenant بفرعين قبل نشر الإنتاج.
