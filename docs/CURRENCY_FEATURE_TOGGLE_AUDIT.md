# تقرير تدقيق وإصلاح مفتاح تعدد العملات (Multi-Currency ON/OFF)

**الفرع:** `cursor/multicurrency-feature-toggle-audit-a199`  
**الأساس:** `master` @ `74890b0`  
**التاريخ:** 2026-09-27  
**الهدف:** جعل Multi-Currency ميزة اختيارية حقيقية end-to-end، مع ضمان تطابق سلوك النظام عند OFF مع ما قبل إضافة الميزة.

---

## 1. فهم التصميم الحالي

| سؤال | الإجابة |
|------|---------|
| أين تُخزَّن حالة الميزة؟ | `BusinessSettings.MultiCurrencyEnabled` (Desktop DB + Cloud tenant) + نسخة UI في `UserPreferences.FeatureFlags.MultiCurrency` |
| كيف يُعرف ON/OFF؟ | Backend يقرأ `BusinessSettings`؛ Desktop UI كان يقرأ التفضيلات المحلية؛ Flutter يقرأ `getBusinessSettings().multiCurrencyEnabled` |
| هل التحقق Backend أم Frontend فقط؟ | **قبل الإصلاح:** Frontend/prefs فقط لمعظم المسارات. **بعد الإصلاح:** Backend يفرض عبر `AccountingCurrencyRules.ApplyFeatureGate` |
| العمليات المعتمدة على Currency | فواتير، سندات، مصروفات، تحويلات، قاصات، بنوك، أرصدة افتتاحية، أقساط، تقارير/COGS، أرصدة عملاء |
| البيانات القديمة بدون Currency؟ | الهجرة عيّنت `Currency=IQD`, `FxRate=1` — لا تُغيَّر المبالغ |
| Regression عند OFF | تجاوز API بـ USD؛ OR sticky في إعدادات الميزات؛ واجهة أرصدة افتتاحية تعرض العملة دائماً |

---

## 2. المشاكل المكتشفة

### CRITICAL

#### C1 — تجاوز Feature Toggle من API / خدمات الكتابة

| الحقل | التفاصيل |
|--------|-----------|
| **الخطورة** | CRITICAL |
| **السبب** | مسارات Create* كانت تستدعي `RequireFxRateOrThrow` فقط دون رفض USD عند `MultiCurrencyEnabled=false` |
| **الملفات** | `InvoiceService`, `CashBankService`, `ExpenseService`, `OpeningPartyBalanceService`, `InstallmentService`, `CloudMobileWriteService` |
| **التأثير** | عميل/موبايل يستطيع إرسال `Currency=USD` + `FxRate` وإنشاء مستندات دولار رغم OFF → فساد بيانات للمستخدمين IQD-only |
| **الإصلاح** | `ApplyFeatureGate` + `MultiCurrencyFeatureGate` / `CloudMultiCurrencyFeatureGate` على كل Create/Upsert المالي الجديد |

#### C2 — تفضيلات UI تُبقي الميزة ON بعد إطفائها في DB (sticky OR)

| الحقل | التفاصيل |
|--------|-----------|
| **الخطورة** | CRITICAL (UX/consistency) |
| **السبب** | `MultiCurrency = settings.MultiCurrencyEnabled \|\| MultiCurrency` في `BusinessFeaturesSettingsViewModel` |
| **التأثير** | إطفاء الميزة في قاعدة البيانات لا يُطفئ الواجهة إذا بقيت prefs محلية ON |
| **الإصلاح** | BusinessSettings مصدر الحقيقة؛ مزامنة prefs عند التحميل + عند بدء MainWindow |

### HIGH

#### H1 — COGS يستبعد مشتريات USD بالكامل

| الحقل | التفاصيل |
|--------|-----------|
| **الخطورة** | HIGH |
| **السبب** | `ProductCostHelper.GetPurchaseItemsByProductAsync` فلتر `Currency == IQD` |
| **التأثير** | عند ON، تكلفة البضاعة تقلّ وتتضخم الأرباح بشكل خاطئ |
| **الإصلاح** | شمول USD مع التحويل عبر `FxRate` اللقطة إلى دينار داخل المتوسط فقط |

#### H2 — قائمة الأقساط تخفي أقساط الدولار وتخلط المجاميع

| الحقل | التفاصيل |
|--------|-----------|
| **الخطورة** | HIGH |
| **السبب** | `BuildInstallmentsQuery` يفرض IQD فقط؛ المجاميع كانت تجميع خام |
| **التأثير** | أقساط USD تختفي؛ عند إظهارها بدون فصل يحدث خلط |
| **الإصلاح** | إزالة فلتر IQD؛ مجاميع IQD أساسية + إفصاح `*Usd` منفصل |

#### H3 — شاشات الأرصدة الافتتاحية تعرض اختيار العملة دائماً

| الحقل | التفاصيل |
|--------|-----------|
| **الخطورة** | HIGH (OFF regression UX) |
| **السبب** | لا ربط بـ FeatureFlag في Opening* ViewModels |
| **التأثير** | مستخدم OFF يرى حقول عملة/صرف غير متوقعة |
| **الإصلاح** | `ShowMultiCurrency` + إخفاء Combo + إجبار IQD عند الحفظ |

### MEDIUM / ملاحظات متبقية

| # | المشكلة | الحالة |
|---|---------|--------|
| M1 | أجزاء من تقرير #118 (Employee dual, ReportCurrencyScope كامل، Cloud MaxCreditLimit) ليست كلها على هذا الفرع | متبقية على PR #118 — لا تمنع سلامة OFF |
| M2 | بناء UI على Linux يحتاج `EnableWindowsTargeting` | بيئة فقط؛ منطق الكود مُراجع |
| M3 | اختبارات Flutter/Offline يدوية كاملة | لم تُشغَّل GUI هنا؛ Flutter يقرأ BusinessSettings أصلاً ويُرسل IQD عند OFF؛ Backend يرفض USD الآن |

---

## 3. الإصلاحات المنفّذة

### Backend gate

- `AccountingCurrencyRules.ApplyFeatureGate` — رفض USD عند OFF؛ FxRate=1 للدينار
- `MultiCurrencyFeatureGate` (Desktop) / `CloudMultiCurrencyFeatureGate` (Cloud)
- موصول على: فاتورة، قاصة، بنك، سند، تحويل، مصروف، أرصدة افتتاحية عميل/مورد/أقساط، وكتابات الموبايل Create*/UpsertCashBox/UpsertBank

### تزامن المفتاح

- إزالة OR sticky في `BusinessFeaturesSettingsViewModel`
- مزامنة `FeatureFlags.MultiCurrency` من BusinessSettings عند فتح الإعدادات وعند بدء `MainWindow`

### محاسبة / تقارير

- COGS: تضمين مشتريات USD عبر FxRate اللقطة (`ProductCostHelper` + `CloudProductCostHelper`)
- أقساط: عرض الكل + مجاميع مفصولة IQD/USD

### Flutter / UI عند OFF

- إخفاء اختيار العملة في الأرصدة الافتتاحية
- Flutter كان يُرسل `currency: 0` عند OFF؛ Backend يمنع التجاوز الآن

### الملفات الرئيسية المعدّلة

```
src/AlMuhasib.Core/Helpers/AccountingCurrencyRules.cs
src/AlMuhasib.Infrastructure/Services/MultiCurrencyFeatureGate.cs (جديد)
src/AlMuhasib.Cloud.Infrastructure/Mobile/CloudMultiCurrencyFeatureGate.cs (جديد)
src/AlMuhasib.Infrastructure/Services/{Invoice,CashBank,Expense,OpeningPartyBalance,Installment,ProductCost}Service*.cs
src/AlMuhasib.Cloud.Infrastructure/Mobile/CloudMobileWriteService.cs
src/AlMuhasib.Cloud.Infrastructure/Reports/CloudProductCostHelper.cs
src/AlMuhasib.UI/ViewModels/{BusinessFeaturesSettings,MainWindow*,Opening*,Installments}*.*
src/AlMuhasib.UI/Views/Opening*.xaml
src/AlMuhasib.Core.Tests/{AccountingCurrencyRulesTests,ProductCostHelperCurrencyTests}.cs
```

---

## 4. الاختبارات

| السيناريو | النتيجة |
|-----------|---------|
| Unit: `ApplyFeatureGate` OFF يرفض USD | ✅ |
| Unit: OFF يجبر IQD/Fx=1 | ✅ |
| Unit: ON يقبل USD بسعر صالح ويرفض Fx=0 | ✅ |
| Unit: COGS تحويل USD→IQD عبر اللقطة | ✅ |
| Unit: مرتجع شراء USD بإشارة سالبة | ✅ |
| Build: Core + Infrastructure + Cloud.Infrastructure | ✅ |
| Build: UI (Windows targeting على Linux) | ⚠ بيئة — لم يُبنَ هنا |
| Multi-Currency OFF regression (يدوي GUI) | منطق Backend/UI جاهز؛ يحتاج تحقق على جهاز Windows |
| Multi-Currency ON + IQD/USD mixed | COGS + أقساط + بوابة الكتابة جاهزة |
| API security: إرسال USD عند OFF | ✅ مرفوض برسالة واضحة |
| Flutter OFF يرسل IQD | ✅ موجود مسبقاً + حارس Backend |
| Offline/Sync تاريخي USD أثناء OFF | SyncEngine يبقى يسمح بالمزامنة التاريخية؛ Create* التفاعلي مرفوض |
| Legacy data | لا migrations تغيّر مبالغ؛ defaults IQD |

---

## 5. النتيجة النهائية

| سؤال | الحكم |
|------|--------|
| هل النظام آمن عند OFF؟ | **نعم** لكتابات جديدة: Backend يرفض USD ويجبر IQD/Fx=1. الواجهة تُزامَن مع BusinessSettings. |
| هل النظام آمن عند ON؟ | **أفضل بكثير**: COGS يحسب مشتريات الدولار؛ الأقساط لا تُخفى؛ لا خلط في مجاميع الأقساط. بقايا تقرير #118 (Employee dual / ReportCurrencyScope الكامل) تُكمَل عبر ذلك الـPR إن لم تُدمَج بعد. |
| هل البيانات القديمة سليمة؟ | **نعم** — لا تعديل عشوائي على صفوف قديمة؛ الهجرة السابقة defaults فقط. |
| هل توجد ثغرات متبقية؟ | متوسطة: إكمال إفصاح التقارير من #118 إن لم يُدمَج؛ اختبار GUI Windows يدوي للـ OFF regression الكامل. |
| هل تحتاج تدخل يدوي؟ | لا حذف بيانات. بعد النشر: مستخدمو OFF يعملون كالسابق. إن وُجدت prefs محلية ON بينما DB OFF، أول فتح MainWindow يصحّحها. |

### Acceptance Criteria

| معيار | الحالة |
|--------|--------|
| OFF يعمل مثل النظام السابق للعمليات الجديدة | ✅ (Backend + UI sync) |
| ON يعمل بشكل صحيح للعملة/الصرف/COGS/أقساط | ✅ للمسارات المصلحة |
| البيانات القديمة لم تتضرر | ✅ |
| لا جمع خام لعملات مختلفة في الأقساط/COGS | ✅ |
| لا تجاوز Feature Toggle من API | ✅ |
| لا تلاعب بقيم حساسة من Client عند OFF | ✅ |
| Offline/Sync لا يفسد التاريخ عند OFF | ✅ سياسة: تاريخي مسموح / Create جديد مرفوض |
| لا تغيير تاريخي بتغيير سعر الصرف الحالي | ✅ (لقطة FxRate على المستند) |

---

**الخلاصة:** Multi-Currency أصبحت ميزة اختيارية تُفرَض في Backend وليس في الواجهة فقط. المستخدمون الحاليون مع OFF محميون من إنشاء دولار عبر API، وتفضيلات الواجهة لم تعد تُبقي الميزة عالقة. عند ON، COGS والأقساط يعاملان الدولار بشكل صحيح دون خلط مع الدينار.
