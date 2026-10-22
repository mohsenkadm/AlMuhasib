# خطة تنفيذ نظام التخليص الكمركي والنقل — المرحلة 1 (تحليل)

> **حالة المرحلة:** تحليل فقط — لا تنفيذ Features ولا تعديل كود تطبيقي في هذه الوثيقة.  
> **القاعدة الحاكمة:** النظام جزء أصيل من منصة قيد، بنفس الأنماط الموجودة، دون كسر الأنظمة الحالية.  
> **تاريخ التحليل:** 2026-10-22  
> **المستودع:** AlMuhasib / قيد

---

## 1. Architecture الحالية

### 1.1 نوع المنتج

منصة قيد تطبيق سطح مكتب **WPF (.NET)** باسم تقني `AlMuhasib`، مع طبقات اختيارية للسحابة والجوال وTelegram.

| الطبقة | المسار | الدور |
|--------|--------|--------|
| UI | `src/AlMuhasib.UI` | WPF، ViewModels، Modules، Styles، Controls |
| Core | `src/AlMuhasib.Core` | Entities، Enums، Interfaces، Models |
| Infrastructure | `src/AlMuhasib.Infrastructure` | EF Core DbContexts، Services، Repositories، Migrations |
| Shared | `src/AlMuhasib.Shared` | طباعة، تصدير Excel/PDF، مساعدات مشتركة |
| API | `src/AlMuhasib.Api` | REST للموبايل/السحابة (حسب النظام) |
| Cloud | `src/AlMuhasib.Cloud.*` | مزامنة متعددة المستأجرين |
| Mobile | `mobile/almuhasib_mobile` | Flutter |
| Telegram | `src/Qaid.TelegramBot*` | بوت تقارير |

### 1.2 نمط الأنظمة (مهم جداً)

الأنظمة **ليست plugins تعمل معاً داخل جلسة واحدة**. كل نظام هو **وضع منتج متبادل** (`ApplicationSystemType`):

| القيمة | الاسم | قاعدة البيانات |
|-------:|------|----------------|
| 0 | `Accounting` | `AlMuhasibDb` |
| 1 | `CarContracts` | `AlMuhasibCarContractsDb` |
| 2 | `HotelManagement` | `AlMuhasibHotelsDb` |
| 3 | `CarTrading` | `AlMuhasibCarTradingDb` |
| 4 | `RealEstateContracts` | `AlMuhasibRealEstateDb` |
| 5 | `GoldShop` | `AlMuhasibGoldShopDb` |

- الاختيار يُحفظ في `%LocalAppData%\AlMuhasib\system-profile.json` عبر `ISystemProfileService` / `SystemProfileService`.
- عند التشغيل يُسجَّل **DbContext واحد فقط** عبر `AddInfrastructure(ActiveSystem)` في `DependencyInjection.cs`.
- تبديل النظام يتطلب **إعادة تشغيل التطبيق** (`DeveloperSystemSwitchView` → `ChangeSystem` → restart).

```
OnStartup
  ├─ First run? → SetupWizardHostWindow → SaveSelection → Trial
  ├─ EnsureServiceProvider
  │     ├─ SystemModuleRegistry
  │     ├─ ScreenPermissionRegistry.Initialize
  │     └─ AddInfrastructure(ActiveSystem) → DbContext + services
  ├─ Migrate active DB
  └─ Login → MainWindow (menu من ActiveModule)
```

### 1.3 تقنيات أساسية

| المجال | التقنية |
|--------|---------|
| UI | WPF + MaterialDesignThemes + MVVM |
| ORM | Entity Framework Core + SQL Server / LocalDB |
| DI | Microsoft.Extensions.DependencyInjection |
| Charts | LiveCharts2 |
| Auth محلي | مستخدمون + كلمات مرور في DB النظام النشط |
| State | ViewModels + خدمات جلسة (`ICurrentUserService`, `ISystemProfileService`) |

### 1.4 القرار المعماري لنظام التخليص (مثبّت)

إضافة نظام منتج جديد:

- `ApplicationSystemType.CustomsClearance = 6`
- قاعدة بيانات: `AlMuhasibCustomsDb`
- `CustomsDbContext` + **EF Migrations من اليوم الأول** (تجنّب نمط Gold القائم على `EnsureCreated`)
- `CustomsSystemModule` + `CustomsPermissionRegistry` + `CustomsMenuBuilder`
- **عدم** إدراج جداول التخليص داخل `AppDbContext` المحاسبي

**مرجع البنية:** Gold (slice غني) + CarTrade (migrations أنظف).  
**مرجع الواجهة:** قيد المحاسبي + `docs/GOLD_PHASE2_UI_PATTERNS.md`.

**تصميم المجال (المرحلة 2):** انظر [`CUSTOMS_ARCHITECTURE.md`](CUSTOMS_ARCHITECTURE.md) — الكيانات، العلاقات، العملات، FX، المقبوضات متعددة الفواتير، والمرفقات.  
مراجع داعمة: [`docs/CUSTOMS_CURRENCY_REUSE.md`](docs/CUSTOMS_CURRENCY_REUSE.md)، [`docs/CUSTOMS_RECEIPT_PAYMENT_PATTERNS.md`](docs/CUSTOMS_RECEIPT_PAYMENT_PATTERNS.md).

---

## 2. أهم Modules الموجودة

| Module | الملف | القائمة | Dashboard VM |
|--------|-------|---------|--------------|
| محاسبي | `Modules/AccountingSystemModule.cs` | `AccountingMenuBuilder` | `DashboardViewModel` |
| عقود سيارات | `Modules/CarContractsSystemModule.cs` | inline | `CarDashboardViewModel` |
| فنادق | `Modules/HotelSystemModule.cs` | inline + flyouts | `HotelDashboardViewModel` |
| بيع/شراء سيارات | `Modules/CarTradingSystemModule.cs` | inline | `CarTradeDashboardViewModel` |
| عقارات | `Modules/RealEstateContractsSystemModule.cs` | inline + flyouts | `RealEstateDashboardViewModel` |
| ذهب | `Modules/GoldShopSystemModule.cs` | `GoldShopMenuBuilder` | `GoldDashboardViewModel` |

### عقد الوحدة `ISystemModule`

المسار: `src/AlMuhasib.UI/Modules/ISystemModule.cs`

- `SystemType`, `DisplayName`
- `PermissionScreens`
- `BuildMenuItems()`
- `DashboardViewModelType`
- `SetupWizardViewModelType` (اختياري)
- `GetScreenName` / `GetLabel` / `GetDefaultViewModelType`

التسجيل المركزي: `SystemModuleRegistry` (Singleton في `App.xaml.cs`).

### ما يعنيه ذلك للتخليص

يجب إنشاء `CustomsClearanceSystemModule` بنفس العقد، وتسجيله في `SystemModuleRegistry` مع فلاج `IsCustomsClearance`، دون تعديل منطق الأنظمة الأخرى إلا بإضافة فرع `switch` آمن.

---

## 3. أهم Components التي سنعيد استخدامها

### 3.1 مبدأ العمل

**Reuse > Extend > Create** — لا إنشاء بدائل مكررة لـ Theme أو الجداول أو الـ KPI.

### 3.2 Controls مشتركة (أولوية إعادة الاستخدام)

| Control | المسار | الاستخدام |
|---------|--------|-----------|
| `DashboardKpiCard` | `Controls/DashboardKpiCard.xaml` | KPI لوحة التحكم |
| `AnimatedStatCard` | `Controls/AnimatedStatCard.xaml` | إحصائيات التقارير |
| `ChartCard` | `Controls/ChartCard.xaml` | غلاف الرسوم |
| `ListTableHeaderBar` | `Controls/ListTableHeaderBar.xaml` | فلترة/تصدير/طباعة |
| `PaginationBar` | `Controls/PaginationBar.xaml` | ترقيم الصفحات |
| `ColumnFilterToggle` | `Controls/ColumnFilterToggle.xaml` | فلاتر الأعمدة |
| `BeautifulMessageDialog` | `Controls/BeautifulMessageDialog.xaml` | رسائل تأكيد |
| `ToastHost` | `Controls/ToastHost.xaml` | إشعارات خفيفة |
| `PartyQuickDetailOverlay` | `Controls/PartyQuickDetailOverlay.xaml` | تفاصيل طرف |
| `InvoiceDetailOverlay` | `Controls/InvoiceDetailOverlay.xaml` | تفاصيل فاتورة (نمط) |
| `CustomFieldsEditor` | `Controls/CustomFieldsEditor.xaml` | حقول مخصصة إن لزم |

### 3.3 Styles من Theme (إلزامي)

المسار: `src/AlMuhasib.UI/Styles/Theme.xaml`

- تخطيط الصفحة: `PageStyle`, `ElevatedCard`, `FilterBar`, `SectionHeader`
- جداول: `ModernDataGrid` + أنماط خلايا الإجراءات
- حقول: `FilledTextBox`, `FilledComboBox`, `FilledDatePicker`, `SearchBox`
- أزرار: `PrimaryButton`, `AccentButton`, `SuccessButton`, `DangerButton`, `FlatButton`, `IconButton`
- أزرار جدول: `TableEditIconButton`, `TableDeleteIconButton`, `TableViewIconButton`, `TablePrintIconButton`, `TableAttachIconButton`
- حالات: `PageLoading`, `PaidBadge`, `OverdueBadge`, `PendingBadge`
- حوارات: `ModernDialogHost`, `DialogContentWrapper`
- ألوان Dashboard: `DashboardKpi*Brush`, `DashboardChart*Brush`

### 3.4 ما لا يُعاد استخدامه كما هو

- Controls خاصة بالذهب (`Gold*`) أو الفندق (`Hotel*`) — تُنسخ **النمط** فقط عند الحاجة، ولا تُربط كاعتمادية.

---

## 4. نظام الصلاحيات

### 4.1 النموذج الحالي (لا مفاتيح `CUSTOMS_VIEW`)

الكيان: `src/AlMuhasib.Core/Entities/Permission.cs`

| حقل | المعنى |
|-----|--------|
| `ScreenName` | مفتاح الشاشة PascalCase |
| `CanView` / `CanAdd` / `CanEdit` / `CanDelete` / `CanPrint` / `CanExport` | أفعال |
| `CanEditPrice` | تعديل الأسعار (عند الحاجة) |
| `IsViewOnly` | يعطّل Add/Edit/Delete |

فحص الخدمة: `IAuthService.HasPermissionAsync(userId, screenName, action)` حيث `action` ∈ `View|Add|Edit|Delete|Print|Export`.

في الواجهة: `ViewModelBase.LoadPermissions(_currentUserService, screenName)` ثم ربط `CanAdd` / `CanEdit` … في XAML.

### 4.2 سجلات الشاشات لكل نظام

| النظام | Registry |
|--------|----------|
| محاسبي | `ScreenPermissionRegistry` (AccountingScreens) |
| ذهب | `GoldShopPermissionRegistry` |
| سيارات بيع | `CarTradePermissionRegistry` |
| عقود سيارات | `CarPermissionRegistry` |
| فنادق | `HotelPermissionRegistry` |
| عقارات | `RealEstatePermissionRegistry` |

واجهة الواجهة المشتركة: `ScreenPermissionRegistry` تفوّض حسب `ActiveSystem`.  
شاشة التعيين: `PermissionsView` / `PermissionsViewModel` تقرأ `AllScreens`.

### 4.3 ترجمة المتطلبات المقترحة → Convention قيد

| المطلوب في الطلب | المكافئ في قيد |
|------------------|----------------|
| `CUSTOMS_VIEW` | `CanView` على شاشة مثل `CustomsDashboard` / `CustomsInvoices` |
| `CUSTOMS_CREATE` | `CanAdd` |
| `CUSTOMS_EDIT` | `CanEdit` |
| `CUSTOMS_DELETE` | `CanDelete` |
| `CUSTOMS_PRINT` | `CanPrint` |
| `CUSTOMS_EXPORT` | `CanExport` |
| `CUSTOMS_VIEW_PROFIT` | شاشة منفصلة أو نفس شاشة التقارير مع `CanView` فقط (مثال: `CustomsProfitReport`) |
| `CUSTOMS_VIEW_EXPENSES` | شاشة `CustomsExpenses` + `CanView` |
| `CUSTOMS_VIEW_REPORTS` | شاشات `Customs*Report` + `CanView` |
| `CUSTOMS_VIEW_ATTACHMENTS` | جزء من شاشة الكيان أو شاشة مخصصة + `CanView` |
| `CUSTOMS_APPROVE` / `CUSTOMS_CANCEL` | **لا يوجد عمود Approve/Cancel** اليوم — يُنفَّذ عبر `CanEdit` على شاشة الحالة، أو شاشة منفصلة بصلاحية View/Edit حسب تصميم سير العمل لاحقاً |

**أمثلة أسماء شاشات مقترحة (بادئة `Customs`):**

- `CustomsDashboard`
- `CustomsCustomers`
- `CustomsVehicles`
- `CustomsInvoices`
- `CustomsExpenses`
- `CustomsCollections`
- `CustomsReports`
- `CustomsProfitReport`
- `Users`, `Permissions`, `PrintSettings`, `Backup`, `CloudSync` (مشتركة بلا بادئة)

---

## 5. نظام المستخدمين

| عنصر | المسار / السلوك |
|------|------------------|
| كيان | `Entities/User.cs` — `UserRole`, علاقة `Permissions` |
| أدوار | `Enums/UserRole.cs` (Admin / User …) |
| Auth | `IAuthService` — تنفيذ لكل نظام (`AuthService`, `GoldAuthService`, `CarTradeAuthService` …) |
| جلسة | `ICurrentUserService` / `CurrentUserService` |
| شاشات | `UsersView` + `PermissionsView` |
| Admin | يحصل على كل الأعلام عند `LoadPermissions` |

لكل نظام غير محاسبي تُنسخ جداول `Users` / `Permissions` داخل قاعدة ذلك النظام (عبر `NonAccountingEntityModel`). نظام التخليص يحتاج `CustomsAuthService : IAuthService` مربوطاً بـ `CustomsDbContext` فقط.

---

## 6. نظام الملفات (Attachments)

**النمط المرجعي الوحيد اليوم:** مرفقات العملاء في المحاسبة.

| الطبقة | التفاصيل |
|--------|----------|
| كيان | `CustomerAttachment` — `FileName`, `FilePath`, `Description`, FK |
| تخزين | قرص: `{AppBase}/Attachments/Customers/{customerId}/` (ليست BLOB) |
| UI | `CustomersView` + `CustomersViewModel` — اختيار ملف / ماسح / فتح / حذف |
| زر الجدول | `TableAttachIconButton` |

**للتخليص:** كيان مثل `CustomsAttachment` (أو مرفقات لكل كيان: فاتورة/سيارة) + مجلد `Attachments/Customs/...` — **دون** إعادة استخدام جدول `CustomerAttachments` المحاسبي.

---

## 7. نظام التقارير

| قطعة | المسار |
|------|--------|
| أساس VM | `ViewModels/ReportViewModelBase.cs` |
| هيكل الصفحة | عنوان → `AnimatedStatCard` → `FilterBar` → `ListTableHeaderBar` → Grid → `PaginationBar` |
| مراجع | `CustomerStatementView`, `ReceivablesAgingReportView`, تقارير الذهب تحت `Views/Gold/` |
| تصدير/طباعة جداول | `IExportService` + `ListTableExportHelper` |

تقارير التخليص يجب أن تتبع نفس الهيكل البصري (لا لوحة تقارير مستقلة بتصميم جديد).

---

## 8. نظام الطباعة

| ملاحظة | التفاصيل |
|--------|----------|
| المسار الفعلي | `IExportService` (وليس الاعتماد على `IPrintService` كمسار UI رئيسي) |
| العقد | `Core/Interfaces/Services/IExportService.cs` |
| التنفيذ | `Shared/Services/ExcelExportService.cs` (+ مساعدات FlowDocument) |
| غلاف UI | `TrackingExportService` |
| هوية الطباعة | `IPrintBrandingService`, `PrintBrandingFlowDocumentHelper`, `DocumentPrintHelper` |
| فواتير | بناء `InvoicePrintModel` → `PrintInvoice` / `ExportInvoiceToPdf` |
| جداول | `PrintTable` / `ExportToExcel` |

مستندات التخليص: إعادة استخدام نفس مسار الطباعة والهوية البصرية؛ تخصيص نموذج الطباعة عند الحاجة دون تغيير مسار المحاسبة الحالي.

---

## 9. نظام Charts

| قطعة | المسار |
|------|--------|
| مكتبة | LiveCharts2 |
| تهيئة الثيم | `Charts/ChartThemeConfig.cs`, `ChartThemeHooks.cs` |
| غلاف | `ChartCard` |
| KPI مع Sparkline | `DashboardKpiCard.ChartValues` |
| مرجع Dashboard | `DashboardView.xaml`, `GoldDashboardView.xaml` |

لوحة تحكم التخليص: نفس المكوّنات والألوان العامة للمنصة (يمكن لاحقاً لمسة لونية خفيفة مثل الذهب، دون Theme جديد كامل).

---

## 10. نظام الجداول

الهيكل القياسي للقوائم (من `GOLD_PHASE2_UI_PATTERNS.md`):

1. `PageStyle` + `FlowDirection="RightToLeft"`
2. شريط إجراءات داخل `ElevatedCard` (عنوان + إضافة/تحديث + بحث)
3. بطاقة جدول: `ListTableHeaderBar` → `DataGrid` بـ `ModernDataGrid` → `PaginationBar`
4. فلاتر أعمدة عبر `DataGridColumnFilterBehavior`
5. حوارات CRUD عبر `DialogHost` (`BeautifulMessageDialog` للتأكيد)
6. طبقة تحميل شبه شفافة عند Busy

مراجع: `SuppliersView`, `CustomersView`, `ExpenseView`, `GoldCustomersView`.

---

## 11. Database architecture

### 11.1 الجذور المشتركة

`BaseEntity`:

- `Id` (int PK)
- `SyncId` (Guid)
- تدقيق: `CreatedAt/By`, `UpdatedAt/By`
- حذف ناعم: `IsDeleted`, `DeletedAt/By`
- `RowVersion` (concurrency)

`BranchScopedEntity` + فلاتر الفروع: **محاسبة فقط**. الأنظمة الأخرى ترث `BaseEntity` فقط.

### 11.2 فصل قواعد البيانات

كل نظام = كتالوج SQL منفصل عبر `SystemConnectionStrings`. العزل **بالـ Database وليس بالـ Schema**.

سياقات موجودة:

- `AppDbContext`, `CarDbContext`, `CarTradeDbContext`, `HotelDbContext`, `RealEstateDbContext`, `GoldDbContext`

### 11.3 Conventions للجداول

- `ToTable("PascalCasePlural")`
- بادئة المجال: `Gold*`, `Car*`, `RealEstate*`, `Hotel*`
- مبالغ: `decimal(18,2)`؛ أسعار صرف غالباً `(18,4)`
- عملة: غالباً enum كنص (`HasConversion<string>`)

### 11.4 Migrations

| النظام | الأسلوب |
|--------|---------|
| محاسبي / Car / CarTrade / RealEstate / Hotel | EF Migrations |
| ذهب | `EnsureCreated` + ترقية SQL يدوية (دين تقني — **لا نقلّده**) |

**التخليص:** `CustomsDbContext` + مجلد `Data/Customs/Migrations` + `CustomsDatabaseMigrationService`، مع كيانات تحت `Core/Entities/Customs/`.

### 11.5 جداول مشتركة داخل كل DB غير محاسبية

`Users`, `Permissions`, `AuditLogs`, `PrintBrandingSettings`, `CloudSyncSettings`, `SyncState` عبر `NonAccountingEntityModel`.

---

## 12. Routes architecture

لا يوجد Router ويب. التنقل:

1. `ActiveModule.BuildMenuItems()` → `MainWindowViewModel.MenuItems`
2. كل عنصر: `ViewModelType` + `ScreenName`
3. الرؤية: `CanView(screenName)` (+ feature flags للمحاسبة فقط)
4. الربط البصري: `DataTemplate` لكل VM→View داخل `MainWindow.xaml`

إضافة نظام التخليص تتطلب تسجيل DataTemplates جديدة **بالإضافة** للقائمة الحالية، دون حذف قوالب الأنظمة الأخرى.

---

## 13. API architecture

### 13.1 سطح المكتب أولاً

نظام التخليص في المرحلة التنفيذية الأولى يعمل محلياً عبر Services + DbContext (مثل باقي الأنظمة عند الإطلاق).

### 13.2 السحابة / الموبايل (مرحلة لاحقة اختيارية)

النمط الحالي:

- `*ApiControllerBase` يتحقق أن `tenant.ApplicationSystemType` يطابق النظام
- أمثلة: `GoldShopApiControllerBase`, `CarTradeApiControllerBase`
- `SyncEngine` يفرّع حسب نوع النظام

عند الحاجة لاحقاً: `CustomsApiControllerBase` + Controllers تحت `Api/Controllers/Customs/` + فرع في Sync — دون تعديل عقود API للأنظمة الأخرى.

---

## 14. أفضل صفحات Reference من قيد المحاسبي (وما يجاورها)

### 14.1 مرجع UI أساسي (محاسبة)

| الغرض | View | ViewModel |
|-------|------|-----------|
| Dashboard + KPI + Charts | `Views/DashboardView.xaml` | `DashboardViewModel` |
| عملاء + مرفقات | `Views/CustomersView.xaml` | `CustomersViewModel` |
| قائمة CRUD نظيفة | `Views/SuppliersView.xaml` | `SuppliersViewModel` |
| سجل تشغيلي + فلاتر + مجاميع | `Views/ExpenseView.xaml` | `ExpenseViewModel` |
| مستند مالي معقّد + طباعة | `Views/SalesInvoiceView.xaml` | `SalesInvoiceViewModel` |
| كشف حساب / تقرير | `Views/CustomerStatementView.xaml` | + `ReportViewModelBase` |

### 14.2 مرجع إضافة Module حديث

| الغرض | المسار |
|-------|--------|
| أنماط UI موثّقة | `docs/GOLD_PHASE2_UI_PATTERNS.md` |
| Module + Menu | `GoldShopSystemModule`, `GoldShopMenuBuilder` |
| Permissions | `GoldShopPermissionRegistry` |
| Dashboard نظام | `Views/Gold/GoldDashboardView.xaml` |
| قائمة عملاء نظام | `Views/Gold/GoldCustomersView.xaml` |
| Module أصغر + EF Migrations | مجلد `CarTrade` (Core/Infrastructure/UI/Api) |

### 14.3 Checklist إعادة الاستخدام لشاشات التخليص

- [ ] `PageStyle` + RTL
- [ ] قوائم: Suppliers/Customers skeleton
- [ ] سجلات تشغيل: Expense filter + totals
- [ ] مستندات: SalesInvoice overlays/print عبر `IExportService`
- [ ] تقارير: `AnimatedStatCard` → `ChartCard` → Filter → Grid
- [ ] Dashboard خاص بالنظام: `DashboardKpiCard` + `ChartThemeConfig` (لا تعديل `DashboardView` المحاسبي)

---

## 15. مخاطر قد تؤثر على الأنظمة الحالية

| المخاطرة | الأثر | التخفيف |
|----------|-------|---------|
| تعديل `ApplicationSystemType` / switches | نسيان فرع → سقوط لـ Accounting DB بالخطأ | مراجعة كل `switch` على النوع؛ اختبارات دخان لكل نظام قديم |
| تعديل `SystemModuleRegistry` / DI | كسر تسجيل نظام آخر | إضافة فقط؛ عدم إعادة هيكلة التسجيل الحالي |
| `MainWindow.xaml` DataTemplates | تعارض مفاتيح القوالب | قوالب جديدة بأسماء VM فريدة تحت `Customs/` |
| تعديل Theme / Controls المشتركة | تغيّر شكل كل الأنظمة | ممنوع في التنفيذ إلا بامتداد خلفي متوافق |
| تعديل `Permission` schema | كسر كل قواعد الصلاحيات | ممنوع؛ استخدم الأعمدة الحالية فقط |
| تعديل Auth العام | كسر تسجيل الدخول | `CustomsAuthService` منفصل مثل Gold |
| دمج جداول في `AppDbContext` | تلوث مخطط المحاسبة | DB منفصل إلزامي |
| تقليد Gold `EnsureCreated` | صعوبة الترقيات | Migrations من اليوم الأول |
| Cloud Sync / API بدون حراسة النوع | بيانات خاطئة بين المستأجرين | بوابة `CustomsApiControllerBase` لاحقاً |
| Wizard البطاقات الستة | ازدحام UI أول تشغيل | بطاقة سابعة بنفس أسلوب البطاقات الحالية فقط |

**قاعدة الاختبار قبل الاعتماد:** بعد أي لمسة على Core مشترك، التحقق يدوياً أو باختبار دخان أن المحاسبة / الذهب / السيارات تفتح Login + Menu + Dashboard كما قبل.

---

## 16. خطة تنفيذ النظام الجديد (مراحل لاحقة — بعد هذه الوثيقة)

> هذه خارطة طريق تنفيذية. **لا تُنفَّذ في المرحلة 1.**

### المرحلة A — هيكل النظام (Skeleton)

1. `ApplicationSystemType.CustomsClearance = 6`
2. `SystemConnectionStrings.CustomsDatabase = "AlMuhasibCustomsDb"`
3. فروع آمنة في: `SystemProfileService`, `DependencyInjection`, `SetupWizardHostWindow`, `DeveloperSystemSwitchView(Model)`, `ScreenPermissionRegistry`
4. `CustomsPermissionRegistry` + `CustomsSystemModule` + `CustomsMenuBuilder`
5. `CustomsDbContext` فارغ نسبياً (Users/Permissions/Audit…) + migration أولية
6. `CustomsDashboardView(Model)` بسيط + DataTemplate
7. تسجيل VMs في DI

**معيار نجاح:** اختيار النظام من المعالج/تبديل المطوّر → تشغيل → لوحة تحكم فارغة بأسلوب قيد.

### المرحلة B — المجال والبيانات

1. كيانات تحت `Core/Entities/Customs/` (عملاء، سيارات/شحنات، فواتير، مدفوعات، مصاريف، مرفقات…) حسب متطلبات المنتج التفصيلية
2. Configurations + Migrations
3. UnitOfWork / Repositories / Domain Services
4. `CustomsAuthService`, Audit عبر SaveChanges

### المرحلة C — واجهات CRUD والتشغيل

1. قوائم العملاء/السيارات وفق `SuppliersView` / `CustomersView`
2. فواتير/عمليات وفق `ExpenseView` أو `SalesInvoiceView` حسب التعقيد
3. صلاحيات `LoadPermissions` على كل شاشة
4. مرفقات بنمط العملاء

### المرحلة D — Dashboard والتقارير والطباعة

1. KPIs بنفس `DashboardKpiCard` (عدد العملاء، الفواتير، غير المسدد، قيد الإجراء، وصلت، خرجت…)
2. تقارير بـ `AnimatedStatCard` + جداول + Export/Print
3. طباعة المستندات عبر `IExportService`

### المرحلة E — تكاملات اختيارية

1. Cloud API + Sync
2. موبايل / Telegram إن لزم
3. تنبيهات ذكية بنمط Dashboard المحاسبي/الذهب

### ترتيب الأولوية المقترح للتنفيذ

```
Skeleton → Entities/Db → Customers/Vehicles lists → Invoices/Payments
  → Expenses → Dashboard KPIs → Reports → Print/Export → Attachments
  → (Optional) Cloud/Sync
```

### ملفات يُتوقَّع لمسها لاحقاً (إضافة فقط حيث أمكن)

- `ApplicationSystemType.cs`
- `SystemConnectionStrings.cs`
- `SystemProfileService.cs`
- `DependencyInjection.cs`
- `SystemModuleRegistry.cs`
- `ScreenPermissionRegistry.cs`
- `SetupWizardHostWindow.xaml(.cs)`
- `DeveloperSystemSwitchView.xaml` + ViewModel
- `MainWindow.xaml` (DataTemplates جديدة)
- `App.xaml.cs` (تسجيل DI)
- مجلدات جديدة: `Entities/Customs`, `Data/Customs`, `Views/Customs`, `ViewModels/Customs`, `Modules/Customs*`

### ملفات يُفضَّل عدم تعديل سلوكها

- منطق `AppDbContext` المحاسبي وmigrationsه
- Theme العام وألوان الأنظمة الأخرى
- عقود `IAuthService` / شكل جدول `Permission`
- شاشات Dashboard/الفواتير الخاصة بالأنظمة الحالية

---

## ملحق أ — قائمة تحقق قبل أي PR تنفيذي

- [ ] لا تغيير على routes/DataTemplates للأنظمة القديمة
- [ ] لا migration على `AlMuhasibDb` من أجل التخليص
- [ ] صلاحيات بأسماء `Customs*` + أعلام Can*
- [ ] UI يستخدم Theme/Controls الحالية فقط
- [ ] اختبار دخان: Accounting وGold وCarTrade ما زالت تعمل بعد الإضافة
- [ ] توثيق أي لمسة Core مشتركة وسببها وتوافقها الخلفي

---

## ملحق ب — مراجع سريعة للمسارات

| الموضوع | مسار |
|---------|------|
| Enum الأنظمة | `src/AlMuhasib.Core/Enums/ApplicationSystemType.cs` |
| عقد Module | `src/AlMuhasib.UI/Modules/ISystemModule.cs` |
| Registry | `src/AlMuhasib.UI/Modules/SystemModuleRegistry.cs` |
| أسماء DB | `src/AlMuhasib.Infrastructure/SystemConnectionStrings.cs` |
| DI | `src/AlMuhasib.Infrastructure/DependencyInjection.cs` |
| أنماط UI ذهب/محاسبة | `docs/GOLD_PHASE2_UI_PATTERNS.md` |
| Permission entity | `src/AlMuhasib.Core/Entities/Permission.cs` |
| BaseEntity | `src/AlMuhasib.Core/Entities/BaseEntity.cs` |
| معالج الإعداد | `src/AlMuhasib.UI/Windows/SetupWizardHostWindow.xaml` |
| تصدير/طباعة | `src/AlMuhasib.Core/Interfaces/Services/IExportService.cs` |
| تصميم Architecture (مرحلة 2) | [`CUSTOMS_ARCHITECTURE.md`](CUSTOMS_ARCHITECTURE.md) |
| عملات / FX للتخليص | [`docs/CUSTOMS_CURRENCY_REUSE.md`](docs/CUSTOMS_CURRENCY_REUSE.md) |
| أنماط المقبوضات | [`docs/CUSTOMS_RECEIPT_PAYMENT_PATTERNS.md`](docs/CUSTOMS_RECEIPT_PAYMENT_PATTERNS.md) |

---

**نهاية مرحلة التحليل.** تصميم المجال موثّق في [`CUSTOMS_ARCHITECTURE.md`](CUSTOMS_ARCHITECTURE.md). الخطوة التالية بعد اعتماده: تنفيذ المرحلة A (Skeleton) ثم B (Entities) وفق ذلك التصميم، مع الالتزام بقاعدة عدم التأثير على الأنظمة الحالية.
