# تدقيق نظام التخليص الكمركي والنقل — CUSTOMS_SYSTEM_AUDIT

| | |
|--|--|
| **المنتج** | منصة قيد (AlMuhasib) — نظام `CustomsClearance` |
| **نمط المراجعة** | Audit & Gap Analysis فقط — **بدون تعديل كود** |
| **المرجع** | Qaid Accounting + وثائق `CUSTOMS_ARCHITECTURE.md` / `CUSTOMS_IMPLEMENTATION_PLAN.md` |
| **تاريخ التدقيق** | 2026-10-23 |
| **نطاق الكود** | `src/AlMuhasib.Core` · `Infrastructure/Services/Customs` · `UI/Views|ViewModels|Controls` · اختبارات `Core.Tests/Customs` |

> **قاعدة:** لا تُذكر مشكلة إلا بعد تحقق من الكود (Backend + بيانات + صلاحيات). النقاط غير القابلة للتحقق وُضعت تحت **Needs Verification**.

---

## ملخص تنفيذي

النواة المالية الأساسية للتخليص **سليمة في المسار السعيد**: التسوية من التوزيعات، أرصدة مزدوجة IQD/USD، منع خلط العملات عند الربط، إلغاء الفاتورة عند وجود مدفوع، حذف ناعم للمسودات فقط، وطباعة نسخة العميل بلا مصاريف/ربح. الحماية على مستوى الخدمات عبر `CustomsPermissionGuard` موجودة لمعظم عمليات CRUD.

**قبل الإنتاج** يجب معالجة: (1) تحقق التوزيع لا يجمع أسطر نفس الفاتورة، (2) حفظ الوصل ثم إعادة الحساب خارج Transaction — مع رمي عند تجاوز الإجمالي يترك بيانات جزئية، (3) تضمين **المسودات** في Aggregates الربح/الإيراد في التقارير والداشبورد.

**درجة الجاهزية للإنتاج:** **68 / 100**  
(وظيفية جيدة · مخاطر مالية على الحواف · تقارير غير دقيقة للربح · أداء الداشبورد/ملف العميل غير محدود · المزامنة السحابية غير مفعّلة)

---

## ما تم التحقق أنه صحيح (لا يُعد مشكلة)

| المجال | الدليل |
|--------|--------|
| فصل النظام عن المحاسبة | `DependencyInjection`: تسجيل `RegisterCustomsInfrastructure` فقط عند `ApplicationSystemType.CustomsClearance` — DB منفصلة `AlMuhasibCustomsDb` |
| إجمالي الفاتورة | `Total = max(0, Σ VehiclePrice − Discount)` في `CustomsInvoiceService.CreateAsync/UpdateAsync` |
| المتبقي | `CustomsFinancialRules.ComputeRemaining`؛ إعادة الحساب من مجموع التوزيعات غير المحذوفة في `CustomsSettlementHelper` |
| الرصيد ≠ الربح | `RecomputeCustomerBalanceAsync` يجمع `Remaining` للفواتير **المعتمدة** فقط؛ الربح الداخلي = إيراد − مصروف **نفس العملة** |
| منع خلط عملة الوصل/الفاتورة | `EnsureReceiptMatchesInvoiceCurrency` في التحقق |
| إلغاء فاتورة عليها مدفوع | `CancelAsync` يرفض إن `PaidAmount > 0` |
| حذف معتمد | `SoftDeleteAsync` للمسودات فقط |
| طباعة عميل | `BuildCustomerInvoiceCore` سيارات فقط — بلا مصاريف/ربح/`InternalNotes` (مغطى باختبار) |
| صلاحيات GetById | `EnsureView` على فواتير/وصولات/عملاء |
| UI المشتركة | `PageStyle`, `ModernDataGrid`, `FilterBar`, `ChartCard`, `ListTableExportHelper`, `AttachmentsPanel`, `CustomerSearchableComboBox` |
| أدوار المنصة | نفس `User` / `Permission` / `UserRole` (Admin\|User) + شاشات `CustomsPermissionRegistry` |

---

## المشاكل المثبتة

### C-01 — 🔴 Critical: حفظ وصل ثم فشل إعادة التسوية يترك بيانات مالية غير متسقة

| | |
|--|--|
| **المشكلة** | إنشاء/تعديل الوصل يستدعي `SaveChanges` للوصل والتوزيعات **قبل** `RecomputeInvoiceSettlementAsync`. إن رُمي استثناء (مثلاً مجموع التوزيعات > إجمالي الفاتورة) يبقى الوصل والتوزيعات مُلتزمين بينما `PaidAmount`/`RemainingAmount` قد لا يُحدَّثان. |
| **المكان** | [`CustomsReceiptService.CreateAsync`](src/AlMuhasib.Infrastructure/Services/Customs/CustomsReceiptService.cs) (~214–220)، `UpdateAsync`، `CancelAsync`؛ [`CustomsSettlementHelper.RecomputeInvoiceSettlementAsync`](src/AlMuhasib.Infrastructure/Services/Customs/CustomsSettlementHelper.cs) (~35–44) يرمي قبل تعيين Paid عند `paid > TotalAmount`. لا يوجد `BeginTransaction` تحت `**/Customs/**`. |
| **سبب الخطورة** | خسارة اتساق مالي: وصل ظاهر مع توزيعات، فاتورة تظهر غير مسددة، وإمكانية إلغاء فاتورة لاحقاً لأن `CancelAsync` يفحص `PaidAmount` فقط. |
| **السيناريو** | طلب توزيع يتجاوز المتبقي (انظر C-02) أو سباق متزامن → أول `SaveChanges` ينجح → `Recompute` يرمي → المستخدم يرى خطأ لكن الصفوف موجودة في DB. |
| **الحل المقترح** | معاملة واحدة: تحقق → كتابة → إعادة تسوية الفواتير + رصيد العميل → Audit → Commit؛ عند الفشل Rollback كامل. |
| **الأولوية** | P0 — قبل أي إنتاج مالي حقيقي |

---

### C-02 — 🔴 Critical: تحقق التوزيع لا يجمع أسطر نفس الفاتورة (تجاوز المتبقي)

| | |
|--|--|
| **المشكلة** | `ValidateRequestAsync` يقارن **كل سطر** مع `RemainingAmount` دون تجميع حسب `InvoiceId`. سطران 80+70 على متبقي 100 يمرّان إن كان مبلغ الوصل = 150. |
| **المكان** | [`CustomsReceiptService.ValidateRequestAsync`](src/AlMuhasib.Infrastructure/Services/Customs/CustomsReceiptService.cs) (~451–467) |
| **سبب الخطورة** | يسمح بتجاوز المتبقي على مستوى الخدمة؛ يتفاعل مع C-01 لإحداث عدم اتساق. |
| **السيناريو** | API/عميل غير الواجهة يرسل سطري توزيع لنفس `InvoiceId`. واجهة سطح المكتب الحالية تبني صفاً واحداً لكل فاتورة (`CustomsReceiptFormViewModel`) فتُقلل الاحتمال لكنها **لا تُغني عن حماية Backend**. |
| **الحل المقترح** | `GroupBy(InvoiceId)` ومقارنة `Sum(Amount)` مع `Remaining + prior` مرة واحدة؛ رفض التكرار أو دمجه. |
| **الأولوية** | P0 |

---

### H-01 — 🟠 High: تقارير الربح/الإيراد والداشبورد تضمّن المسودات

| | |
|--|--|
| **المشكلة** | استعلامات الربح والإيراد تستخدم `Status != Cancelled` فتدخل **Draft** في الإيراد والربح الداخلي. تقرير الديون يستخدم `Approved` بشكل صحيح — عدم اتساق. |
| **المكان** | [`CustomsReportService.GetProfitReportAsync`](src/AlMuhasib.Infrastructure/Services/Customs/CustomsReportService.cs) (~595–597)؛ Daily/Monthly/Customer/Invoice reports بنفس الفلتر؛ [`CustomsDashboardService`](src/AlMuhasib.Infrastructure/Services/Customs/CustomsDashboardService.cs) (~40–51, 109–122) للربح الدوري. |
| **سبب الخطورة** | تضخيم الربح والإيراد في قرارات الإدارة؛ رقم «الربح» لم يعد يمثل عملاً معتمداً. |
| **السيناريو** | إنشاء مسودات كبيرة دون اعتماد → فتح تقرير الربح / لوحة التحكم → يظهر إيراد وربح غير محقَّقين. |
| **الحل المقترح** | Aggregates الربح والإيراد والرسوم البيانية الشهرية: `Status == Approved` فقط. الإبقاء على تقارير تشغيلية للمسودات إن لزم بفلتر حالة صريح. |
| **الأولوية** | P1 |

---

### H-02 — 🟠 High: إلغاء الفاتورة لا يفحص التوزيعات النشطة (دفاع متعمق)

| | |
|--|--|
| **المشكلة** | `CancelAsync` يرفض فقط إن `PaidAmount > 0`. بعد C-01 قد توجد توزيعات نشطة مع `PaidAmount == 0`. |
| **المكان** | [`CustomsInvoiceService.CancelAsync`](src/AlMuhasib.Infrastructure/Services/Customs/CustomsInvoiceService.cs) (~363–364) |
| **سبب الخطورة** | إلغاء فاتورة ما زال عليها توزيعات وصل → تشويه كشف الحساب والتقارير. |
| **السيناريو** | وقوع C-01 ثم إلغاء الفاتورة من الواجهة بنجاح. |
| **الحل المقترح** | رفض الإلغاء إن وُجدت `CustomsReceiptAllocations` غير محذوفة؛ أو إجبار إلغاء الوصولات أولاً. |
| **الأولوية** | P1 (مع C-01/C-02) |

---

### H-03 — 🟠 High: ملف العميل والداشبورد يحمّلان بيانات غير محدودة الحجم

| | |
|--|--|
| **المشكلة** | `GetCustomerFileAsync` يجلب كل الفواتير/السيارات/الوصولات/المرفقات دفعة واحدة. الداشبورد يحمّل **كل** الفواتير غير الملغاة ثم يفلتر الفترة في الذاكرة. |
| **المكان** | [`CustomsCustomerService.GetCustomerFileAsync`](src/AlMuhasib.Infrastructure/Services/Customs/CustomsCustomerService.cs) (~189–208)؛ [`CustomsDashboardService.GetDashboardStatsAsync`](src/AlMuhasib.Infrastructure/Services/Customs/CustomsDashboardService.cs) (~40–51) |
| **سبب الخطورة** | بطء شديد / استهلاك ذاكرة مع نمو البيانات؛ قد يجمّد الواجهة. |
| **السيناريو** | عميل بآلاف الفواتير، أو قاعدة فيها عشرات الآلاف من الفواتير → فتح الملف أو اللوحة. |
| **الحل المقترح** | تجميعات SQL للفترة؛ صفحات/حدود لشبكات ملف العميل؛ عدم تحميل كل التاريخ للداشبورد. |
| **الأولوية** | P1 قبل إنتاج بحجم بيانات حقيقي |

---

### H-04 — 🟠 High: مسارات طباعة/تصدير الجداول تتجاوز بوابات `CustomsPrintService`

| | |
|--|--|
| **المشكلة** | Excel/Print للجداول يمر عبر `ListTableExportHelper` → `IExportService` مباشرة بعد فحص VM (`CanPrint`/`CanExport`). لا يستدعي `CustomsPrintService.EnsurePrint/Export` لشاشات التقارير/الربح. |
| **المكان** | [`CustomsReportViewModelBase`](src/AlMuhasib.UI/ViewModels/Customs/CustomsReportViewModelBase.cs)؛ قوائم العملاء/الفواتير/الوصولات |
| **سبب الخطورة** | دفاع متعمق أضعف من مسار سند/فاتورة العميل؛ أي مسار غير UI يستدعي التصدير لاحقاً قد يفلت (حالياً سطح مكتب فقط). |
| **السيناريو** | مستخدم بلا Print على التقارير لكن تم تسريب استدعاء؛ أو توسيع لاحق لـ API. |
| **الحل المقترح** | توجيه Print/Export التقارير عبر `ICustomsPrintService` مع `Ensure*` حسب الشاشة (خصوصاً ProfitReport). |
| **الأولوية** | P1 أمني |

---

### M-01 — 🟡 Medium: حذف مسودة فاتورة لا ينظّف المرفقات

| | |
|--|--|
| **المشكلة** | `SoftDeleteAsync` يحذف ناعمًا السيارات والمصاريف والفاتورة فقط — دون `CustomsAttachment` أو ملفات القرص. |
| **المكان** | [`CustomsInvoiceService.SoftDeleteAsync`](src/AlMuhasib.Infrastructure/Services/Customs/CustomsInvoiceService.cs) (~425–431)؛ مسارات الملفات تحت `Attachments/Customs/...` |
| **سبب الخطورة** | بقايا metadata وملفات على القرص؛ تضليل الأرشفة. |
| **السيناريو** | إضافة مرفقات لمسودة ثم حذف المسودة. |
| **الحل المقترح** | Cascade soft-delete للمرفقات + حذف ملف بأفضل جهد (مثل `CustomsAttachmentService.SoftDeleteAsync`). |
| **الأولوية** | P2 |

---

### M-02 — 🟡 Medium: قوائم الفواتير/الوصولات/العملاء تضبط `IsBusy` دون Overlay في XAML

| | |
|--|--|
| **المشكلة** | الـ VM يضبط `IsBusy` لكن الشاشة لا تعرض شريط/غطاء تحميل (بعكس كشف الحساب والتقارير). النماذج بلا `PageLoading` بخلاف `SalesInvoiceView`. |
| **المكان** | `CustomsInvoiceListView`، `CustomsReceiptListView`، `CustomsCustomersView`؛ نماذج الفاتورة/الوصل |
| **سبب الخطورة** | نقر مزدوج / شعور بأن التطبيق متجمد؛ UX أضعف من المحاسبة في النماذج. |
| **السيناريو** | شبكة بطيئة أثناء التحميل الأول للقائمة أو الحفظ. |
| **الحل المقترح** | ربط Overlay مثل التقارير؛ `PageLoading` للنماذج الثقيلة. |
| **الأولوية** | P2 |

---

### M-03 — 🟡 Medium: عدّادات العميل (فواتير مفتوحة) قد تشمل مسودات

| | |
|--|--|
| **المشكلة** | أرصدة العميل من المعتمد فقط (صحيح)، لكن بعض عدّادات المفتوح/`UnpaidInvoiceCount` في ملف العميل تعتمد على `RemainingAmount > 0` ضمن غير الملغى — وقد تشمل مسودات. |
| **المكان** | [`CustomsCustomerService.GetCustomerFileAsync`](src/AlMuhasib.Infrastructure/Services/Customs/CustomsCustomerService.cs) (~241) |
| **سبب الخطورة** | تضليل تشغيلي (عدد غير المسدد). |
| **السيناريو** | مسودات بمتبقي = الإجمالي → تظهر ضمن غير المسدد. |
| **الحل المقترح** | العدّ على `Approved && Remaining > 0` فقط. |
| **الأولوية** | P2 |

---

### M-04 — 🟡 Medium: لا يوجد فرز من الخادم للقوائم الرئيسية

| | |
|--|--|
| **المشكلة** | القوائم تعتمد `OrderBy` ثابت في الخدمة؛ لا API للفرز. أعمدة الفرز في شبكات النماذج فقط محلياً. |
| **المكان** | `GetPagedAsync` للفواتير/الوصولات/العملاء |
| **سبب الخطورة** | ضعف قابلية الاستخدام مع جداول كبيرة (أقل من Critical). |
| **السيناريو** | المستخدم يريد الفرز حسب المتبقي/التاريخ من الخادم. |
| **الحل المقترح** | معاملات `sortBy`/`sortDir` اختيارية — نفس نمط تحسينات المحاسبة عند الحاجة. |
| **الأولوية** | P3 |

---

### M-05 — 🟡 Medium: حوارات مرفقات مزدوجة داخل نموذج الفاتورة

| | |
|--|--|
| **المشكلة** | بعد توحيد `AttachmentsPanel`، ما زال هناك تضمين للوحة داخل حوار تفاصيل السيارة/المصروف **وحوارات منفصلة** لنفس المرفقات. |
| **المكان** | [`CustomsInvoiceFormView.xaml`](src/AlMuhasib.UI/Views/Customs/CustomsInvoiceFormView.xaml) |
| **سبب الخطورة** | ارتباك UX وصيانة مضاعفة — ليست ثغرة مالية. |
| **السيناريو** | مستخدم يفتح المرفقات من الشبكة ومن داخل حوار التفاصيل. |
| **الحل المقترح** | مسار واحد لإدارة مرفقات السطر. |
| **الأولوية** | P3 |

---

### M-06 — 🟡 Medium: الخصم الأكبر من مجموع السيارات يُثبَّت الإجمالي إلى 0 بصمت

| | |
|--|--|
| **المشكلة** | `Math.Max(0, vehiclesSum - discount)` لا يرفض خصماً > المجموع. |
| **المكان** | `CustomsInvoiceService.CreateAsync/UpdateAsync` |
| **سبب الخطورة** | فاتورة معتمدة بإجمالي 0 قد تُقبل دون تنبيه واضح. |
| **السيناريو** | إدخال خصم أكبر من الأسعار بالخطأ. |
| **الحل المقترح** | Validation: `DiscountAmount <= vehiclesSum` مع رسالة عربية. |
| **الأولوية** | P2 |

---

### L-01 — 🔵 Low: `PreviewNextInvoiceNumberAsync` / `PreviewNextReceiptNumberAsync` بلا `EnsureView`

| | |
|--|--|
| **المشكلة** | معاينة الرقم التالي لا تمر عبر Guard. |
| **المكان** | `CustomsInvoiceService` / `CustomsReceiptService` |
| **الحل المقترح** | `EnsureView` على الشاشة المعنية. |
| **الأولوية** | P3 |

---

### L-02 — 🔵 Low: عمليات حذف ناعم/مرفقات بلا أسماء Audit دلالية موحّدة

| | |
|--|--|
| **المشكلة** | الاعتماد على auto-audit في `SaveChanges`؛ أسماء مثل `CustomsInvoice.Approve` موجودة للاعتماد/الإلغاء لكن ليس لكل المسارات. |
| **المكان** | `CustomsDbContext.SaveChangesAsync` + `CustomsAuditWriter` |
| **الحل المقترح** | إدخالات دلالية للـ SoftDelete/Attachment عند الحاجة للتصفية. |
| **الأولوية** | P3 |

---

### L-03 — 🔵 Low: `ChartCard.IsEmpty` غير مربوط في داشبورد/تقارير التخليص

| | |
|--|--|
| **المشكلة** | الكنترول يدعم الحالة الفارغة؛ بعض شاشات المحاسبة تربطه. |
| **الحل المقترح** | ربط `IsEmpty` عند عدم وجود نقاط. |
| **الأولوية** | P4 |

---

### L-04 — 🔵 Low: المزامنة السحابية Stub

| | |
|--|--|
| **المشكلة** | `CustomsSyncService` يُرجع أن الميزة غير مفعّلة؛ لا `CustomsApi` controllers. متوافق مع خطة المرحلة E الاختيارية. |
| **المكان** | `CustomsSyncService`؛ `CUSTOMS_IMPLEMENTATION_PLAN` Phase E |
| **الحل المقترح** | إبقاؤه خارج نطاق الإنتاج المحلي؛ أو تنفيذه قبل نشر متعدد الأجهزة. |
| **الأولوية** | حسب احتياج النشر (ليس حاجزاً للتشغيل المحلي) |

---

## Needs Verification (لم تُثبت كثغرة)

| النقطة | لماذا لم تُحسم |
|--------|----------------|
| سباق تخصيص متزامن تحت عزل SQL الافتراضي | يحتاج اختبار تحميل/عزل؛ يُخفَّف بـ Transaction + إعادة قراءة Remaining |
| سباق توليد أرقام الفاتورة/الوصل | `GetNextAsync` بدون قفل صريح — شائع في الأنظمة الأخرى أيضاً |
| نية المنتج لعرض مصروف بعملة مختلفة ضمن الربح | التصميم الحالي يستبعدها عمداً من InternalProfit — صحيح حسب القواعد الموثّقة |
| سلوك طابعة Windows / QuestPDF على بيئة الإنتاج | غير قابل للتشغيل هنا؛ مسار البناء موجود عبر `IExportService` |
| مراجعة يدوية كاملة لكل شاشات Accounting/Gold/Cars بعد دمج التخليص | DI معزول؛ يُنصح بـ smoke على Windows قبل الإصدار |

---

# ملخصات مجمّعة

## 1. Critical Issues

1. **C-01** — Transaction مفقودة حول حفظ الوصل + إعادة التسوية → بيانات جزئية.  
2. **C-02** — تحقق التوزيع لا يجمع حسب الفاتورة → تجاوز المتبقي على مستوى الخدمة.

## 2. High Priority Issues

1. **H-01** — المسودات داخل Aggregates الربح/الإيراد (تقارير + داشبورد).  
2. **H-02** — إلغاء الفاتورة لا يفحص التوزيعات النشطة.  
3. **H-03** — استعلامات غير محدودة لملف العميل والداشبورد.  
4. **H-04** — تصدير/طباعة الجداول بلا بوابات `CustomsPrintService` للتقارير الحساسة.

## 3. Financial Integrity Issues

| ID | الموضوع |
|----|---------|
| C-01 / C-02 | اتساق Paid/Remaining مع التوزيعات |
| H-01 | دقة الربح والإيراد |
| H-02 | إلغاء مع توزيعات يتيمة |
| M-03 / M-06 | عدّادات وعرض خصم |
| — | **سليم:** منع خلط IQD+USD عند الربط؛ الرصيد من المتبقي المعتمد؛ الربح ≠ الرصيد؛ طباعة العميل بلا ربح |

## 4. Security & Permissions Issues

| ID | الموضوع |
|----|---------|
| H-04 | دفاع متعمق للتصدير/الطباعة |
| L-01 | معاينة الأرقام بلا View |
| — | **سليم:** `CustomsPermissionGuard` على CRUD الرئيسي؛ شاشة Expenses وProfitReport منفصلتان؛ Admin bypass كالمحاسبة؛ لا نظام صلاحيات موازٍ |

**ملاحظة أدوار:** المنصة تدعم `Admin` و`User` فقط مع صلاحيات شاشة (View/Add/Edit/Delete/Print/Export) — نفس نموذج المحاسبة، وليس Roles مسمّاة إضافية للتخليص.

## 5. UX/UI Issues

| ID | الموضوع |
|----|---------|
| M-02 | Loading overlays ناقصة في القوائم/النماذج |
| M-05 | حوارات مرفقات مزدوجة في نموذج الفاتورة |
| L-03 | Empty chart state |
| — | **سليم بعد المراحل 18–20:** ألوان الثيم، `ChartCard`، `AttachmentsPanel`، `ListTableExportHelper`، جداول/فلاتر مشتركة |

## 6. Performance Issues

| ID | الموضوع |
|----|---------|
| H-03 | Dashboard + Customer File unbounded |
| M-04 | لا server-side sort |
| — | قوائم العملاء/الفواتير/الوصولات: paging من الخادم (جيد)؛ التقارير/الكشف: تحميل النطاق ثم paging عميل (نفس نمط المحاسبة) |

## 7. Missing Requirements

| المتطلب | الحالة |
|---------|--------|
| CRUD عملاء/فواتير/سيارات/مصاريف/وصولات/كشف/تقارير/طباعة | منفّذ |
| قواعد العملات والربح الداخلي | منفّذ في المنطق الأساسي |
| Audit للعمليات الحساسة (اعتماد/إلغاء/وصل) | منفّذ دلالياً + auto-audit |
| مزامنة سحابية / API مخصص | **غير منفّذ** (Phase E / Stub) — L-04 |
| معالج إعداد خاص بالتخليص | غير موجود (`SetupWizardViewModelType => null`) — مقبول إن كان الإعداد العام يكفي |
| شاشة مستقلة لأنواع المصاريف | غير مطلوبة معمارياً — تُدار من نموذج الفاتورة |

## 8. Regression Risks

| المخاطر | التقييم |
|---------|---------|
| كسر Accounting/Gold/Cars/RealEstate عبر DI | **منخفض** — تسجيل متبادل حسب `ActiveSystem` |
| تغيير Theme العام | **منخفض** — لا مسار خاص يغيّر المحاسبة |
| تعديل `ListTableExportHelper` / `AttachmentsPanel` / `ChartCard` المشتركة | **متوسط خفيف** — لمسّت Hotel عبر `ToRows`؛ يلزم smoke للفندق عند الدمج |
| Migrations على DB المحاسبة | **لا** — سياق التخليص منفصل |

## 9. Recommended Fix Order

1. **P0:** C-02 (تجميع التوزيع) ثم C-01 (Transaction) — معاً.  
2. **P1:** H-02 (إلغاء + توزيعات) فوراً بعدهما.  
3. **P1:** H-01 (Approved-only للربح/الإيراد).  
4. **P1:** H-04 (بوابات طباعة/تصدير التقارير).  
5. **P1:** H-03 (أداء داشبورد + ملف عميل).  
6. **P2:** M-01، M-03، M-06، M-02.  
7. **P3+:** M-04، M-05، L-*.  
8. قبل الإصدار: اختبارات آلية لـ C-01/C-02 + smoke Windows للأنظمة الأخرى.

## 10. Production Readiness Score / 100

| المحور | الدرجة (من) | تعليق |
|--------|-------------|--------|
| الوظائف الأساسية | 18/20 | مسار العمل مكتمل |
| السلامة المالية | 10/25 | حواف التوزيع/المعاملات + تقارير المسودات |
| الصلاحيات والأمان | 14/15 | Guard جيد؛ دفاع التصدير أضعف |
| UI/UX والتوافق | 12/15 | قريب من المحاسبة بعد 18–20 |
| الأداء | 6/10 | قوائم جيدة؛ داشبورد/ملف ثقيلان |
| التكامل/الانحدار | 8/10 | عزل ممتاز؛ مشتركات تحتاج smoke |
| اكتمال المتطلبات | 7/10 | محلي جاهز؛ سحابة غير مفعّلة |
| **المجموع** | **68/100** | **غير جاهز للإنتاج المالي** حتى إغلاق P0–P1 |

---

## خاتمة

التدقيق **توقف هنا**. لم يُنفَّذ أي إصلاح أو Migration أو تعديل على قواعد البيانات.

للمتابعة: راجع هذا التقرير، ثم أعطِ موافقة صريحة على نطاق الإصلاح (مثلاً: «نفّذ P0+P1 فقط») قبل أي تغيير على الكود.
