# تقرير تحديث واجهة وتجربة المستخدم — التخليص الكمركي والنقل

**الفرع:** `cursor/customs-ui-ux-matching-d57e`  
**النطاق:** UI/UX لنظام Customs فقط — بدون Migrations أو تغيير حسابات مالية أو تأثير على قيد المحاسبي.

---

## 1. الملفات المعدّلة / المضافة

| ملف | نوع التغيير |
|-----|-------------|
| `Views/Customs/CustomsDashboardView.xaml` | Card رئيسي بأسلوب قيد + إجراءات سريعة |
| `ViewModels/Customs/CustomsDashboardViewModel.cs` | أوامر تنقل + ترحيب |
| `MainWindow.xaml` | أزرار شريط سريع لـ Customs |
| `ViewModels/MainWindowViewModel.Workspace.cs` | فرع `IsCustomsClearance` لعزل الإجراءات |
| `ViewModels/MainWindowViewModel.cs` | استهلاك `PendingOpenAdd` للعملاء |
| `Services/CustomsInvoiceNavigationBridge.cs` | `PendingServiceType` / `PendingFocusSection` |
| `Services/CustomsCustomersNavigationBridge.cs` | **جديد** — فتح حوار إضافة عميل |
| `Views/Customs/CustomsInvoiceListView.xaml` | أزرار نصية: تفاصيل، اعتماد، طباعة، مصاريف، مرفقات… |
| `ViewModels/Customs/CustomsInvoiceListViewModel.cs` | أوامر التفاصيل/الحالة/الطباعة/المتعلقات |
| `Views/Customs/CustomsCustomerFileView.xaml` | «عرض التفاصيل» + مرفقات موسومة |
| `ViewModels/Customs/CustomsCustomersViewModel.cs` | استهلاك جسر الإضافة |
| `Views/Customs/CustomsInvoiceFormView.xaml` | جدول مركبات، نوع مصروف، تلميح مرفق، حفظ/إلغاء |
| `ViewModels/Customs/CustomsInvoiceFormViewModel.cs` | تطبيق النوع المسبق + تلميح القسم |
| `Controls/AttachmentsPanel.xaml(.cs)` | خاصية اختيارية `UseLabeledActions` (افتراضي = سلوك قديم) |
| `Core/Models/Customs/CustomsInvoiceModels.cs` | `CanApprove` على عنصر القائمة (خاصية محسوبة فقط) |
| `Core.Tests/Customs/CustomsUiUxNavigationTests.cs` | **جديد** — عقود الحالة/الأنواع |
| `CUSTOMS_UI_UX_UPDATE_REPORT.md` | هذا التقرير |

---

## 2. المكونات التي أُعيد استخدامها

- `DashboardHeroBrush` / `DashboardGlassChipBrush` / `DashboardQuickActionBrush*`
- نمط `DashboardQuickAction` (نسخة محلية في Customs Dashboard — لا تُغيّر `DashboardView`)
- `DashboardKpiCard`, `ChartCard`, `AnimatedStatCard`, `ElevatedCard`
- `ModernDataGrid` / `InvoiceItemsDataGrid` (ارتفاع الصف محلياً على جدول مركبات Customs فقط)
- `OutlinedButton` / `MaterialDesignRaisedButton` / `PageStyle`
- `AttachmentsPanel` مع امتداد اختياري لا يكسر Accounting
- `ICustomsPrintService` / `ICustomsInvoiceService.ApproveAsync` / `CancelAsync` كما هي
- جسور تنقل بنفس نمط المشروع (`PartnerListNavigationBridge`, Hotel, …)

---

## 3. التحسينات المنفّذة حسب البنود

### 1) لوحة تحكم Customs
- Card رئيسي بتدرج البطل وchips للفواتير/غير المسددة والمقبوضات (بيانات موجودة فقط، فصل IQD/USD كما في الخدمة).
- إجراءات سريعة: عميل، فاتورة تخليص، نقل، تخليص+نقل، إيصال، قائمة فواتير، مركبات/شحنات.

### 2) شريط الإجراءات تحت التبويبات
- فرع `IsCustomsClearance` في `ApplyWorkspaceProfile` يخفي إجراءات المحاسبة ويعرض إجراءات Customs فقط.
- قيد المحاسبي وHotel/Gold/Cars دون تغيير في مسارهم.

### 3–4) قائمة الفواتير + ملف العميل
- أزرار نص+أيقونة: عرض التفاصيل، اعتماد، إلغاء، معاينة، طباعة، PDF، مصاريف، مرفقات، مركبات، حذف مسودة.
- الطباعة/PDF عبر `ICustomsPrintService` مع فحص `CanPrint` / `CanExport`.
- الاعتماد/الإلغاء عبر طبقة الخدمة فقط.
- ملف العميل: «عرض التفاصيل» بدل أيقونة فقط؛ مرفقات بـ `UseLabeledActions`.

### 5–8) شاشة الفاتورة
- صفوف مركبات أطول (`RowHeight=64`)، أعمدة أوسع، أزرار تفاصيل/مرفقات/حذف بنص.
- تسمية واضحة «نوع المصروف» + «إضافة نوع جديد».
- تلميح: «يجب حفظ الفاتورة أولاً لإضافة مرفق للمصروف.» مع تعطيل عبر `HasSavedOwner`.
- شريط علوي وسفلي: «حفظ الفاتورة» / «إلغاء» بأحجام ومسافات أوضح؛ `IsBusy`/`IsSaving` كما هما.

### 9) متعلقات من قائمة الفواتير
- فتح النموذج مع `PendingFocusSection` (`expenses` / `attachments` / `vehicles`) وبانر توجيهي — بلا Backend جديد.

---

## 4. الاختبارات

| المجموعة | النتيجة |
|----------|---------|
| `dotnet test … --filter FullyQualifiedName~Customs` | **Passed: 91 / Failed: 0** |
| منها `CustomsUiUxNavigationTests` | عقود `CanApprove` / `CanCancel` / أنواع الخدمة |
| بناء `AlMuhasib.UI` على Linux | **غير ممكن** (WPF) — متوقع |

لم يُغيَّر أي منطق مالي في الخدمات؛ اختبارات الفواتير/الأرصدة/الصلاحيات/الفرز السابقة ضمن الـ 91 ناجحة.

---

## 5. ما يحتاج اختبار Windows يدوياً

1. ظهور Card البطل والإجراءات على لوحة Customs.
2. شريط الإجراءات السريع في Customs مقابل غيابه/اختلافه في قيد.
3. أزرار قائمة الفواتير (تفاصيل، اعتماد، طباعة، مصاريف، مرفقات) على شاشة حقيقية.
4. ملف العميل: وضوح «عرض التفاصيل» وأزرار المرفقات.
5. جدول السيارات داخل الفاتورة + تلميح مرفق المصروف قبل/بعد الحفظ.
6. أحجام أزرار الحفظ/الإلغاء وعدم كسر تخطيط النافذة.

---

## 6. تأكيدات العزل والمنطق

| القاعدة | الحالة |
|---------|--------|
| لا تغيير حسابات الإجمالي/الخصم/العملات/التسويات/الأرباح/الأرصدة | مؤكد — UI فقط + `CanApprove` محسوبة |
| Approved فقط في مؤشرات الداشبورد (من الخدمة الحالية) | لم تُمس الخدمة |
| صلاحيات الطباعة/التصدير/الاعتماد في الطبقة الخدمية | مؤكد |
| M-01 مرفقات المسودة | لم يُمس منطق الحذف |
| H-03 Pagination/Sort | لم يُمس |
| Accounting / Gold / Cars / Real Estate | شريط سريع معزول؛ `AttachmentsPanel` افتراضي كما كان |
| لا Migration / لا مزامنة سحابية | مؤكد |

---

## 7. قرارات UX قد تحتاج موافقة

1. **المصروفات من الشريط السريع** تفتح قائمة الفواتير (لا توجد شاشة مصروفات مستقلة).
2. **الأرشيف/المرفقات** من الشريط تفتح قائمة العملاء (المرفقات داخل ملف العميل).
3. عمود الإجراءات في قائمة الفواتير أصبح أعرض (~420) لاستيعاب الأزرار النصية — قد يفضّل لاحقاً قائمة سياقية إذا ازدحمت الشاشات الصغيرة.
4. زر «معاينة» في شريط الفاتورة يستدعي نفس مسار PDF الحالي (فتح مؤقت) لغياب معاينة منفصلة في الخدمة.
