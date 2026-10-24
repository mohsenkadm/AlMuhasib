# تقرير فني — معالج النقل (Transfer Wizard)

**نوع الوثيقة:** تحليل وضع حالي فقط (بدون تعديلات وبدون اقتراحات حلول).  
**الأساس الموثَّق:** `origin/master` @ `3554ff6` — قبل تعديلات الفرع `cursor/migration-wizard-branch-cashier-71a5`.  
**الغرض:** إرسال لمطور أو نموذج تحليل لاحق دون إعادة فحص المشروع من الصفر.

> ملاحظة: حقل `Product.ShowInCashier` موجود في الكيان والمزامنة وشاشة الكاشير، لكن **معالج النقل على master لا يعرضه ولا يكتبه**. المنتجات الجديدة تأخذ الافتراضي `true` من الكيان.

---

## 0) الملفات والمكونات الأساسية

| الدور | المسار / الاسم |
|--------|----------------|
| الواجهة | `src/AlMuhasib.UI/Views/MigrationWizardView.xaml` + `.xaml.cs` |
| منطق الخطوات والحفظ | `src/AlMuhasib.UI/ViewModels/MigrationWizardViewModel.cs` |
| نماذج الصفوف والخطوات | `src/AlMuhasib.UI/Models/MigrationWizardModels.cs` (`MigrationStepKind`, `MigrationNamedBalanceRow`, `MigrationBranchCapitalRow`, …) |
| Excel منتجات | `src/AlMuhasib.UI/Helpers/MigrationProductExcelHelper.cs` |
| Excel عام | `src/AlMuhasib.UI/Helpers/MigrationExcelHelper.cs` |
| فتح المعالج | `SetupWizardViewModel` / `SetupWizardView` — صلاحية الشاشة `DataImport` — `AccountingMenuBuilder` |

---

## 1) مراحل المعالج بالترتيب

تُبنى عبر `BuildSteps()` داخل `MigrationWizardViewModel`.  
الأعضاء `Capital` و `ProductPricing` موجودان في الـ enum لكن **غير مضافين** لخطوات العرض الحالية.

| # | `MigrationStepKind` | العنوان | الوظيفة | إلزامي؟ |
|---|---------------------|---------|---------|---------|
| 1 | `Branches` | الفروع ورأس المال | إضافة فروع + رأس مال/أرباح افتتاحية لكل فرع + خيار تعدد العملات وسعر الصرف | نعم |
| 2 | `Categories` | التصنيفات | إدخال أصناف المنتجات (`Categories`) | اختياري (يمكن تخطي) |
| 3 | `Warehouses` | المخازن | إنشاء مخازن مربوطة بفرع — مطلوب قبل المنتجات | نعم |
| 4 | `PricingTypes` | أنواع التسعير | أنواع أسعار البيع (سعر مفرد/جملة/…) | إلزامي إن لم يوجد نوع تسعير |
| 5 | `Products` | المنتجات | منتج + تكلفة + كميات مخازن + أسعار أنواع التسعير + ربط فروع اختياري | اختياري |
| 6 | `CashAndBank` | القاصات والمصرف | قاصات وحسابات مصرف بأرصدة افتتاحية | اختياري |
| 7 | `Investors` | المستثمرون | مستثمرون + نسبة ربح + رصيد افتتاحي اختياري | اختياري |
| 8 | `Customers` | العملاء | عملاء + رصيد آجل افتتاحي اختياري | اختياري |
| 9 | `Suppliers` | الموردون | موردون + رصيد آجل افتتاحي اختياري | اختياري |
| 10 | `ExpenseTypes` | أنواع المصاريف | أنواع مصروف افتتاحية | اختياري |
| 11 | `Installments` | الأقساط | أرصدة أقساط افتتاحية عبر `IInstallmentService` | اختياري |

**التنقل:** التنقل الأمامي يتطلب حفظ الخطوات السابقة في `_savedSteps`.  
`CanSkip` يمنع تخطي `Branches` / `Warehouses`، ويمنع تخطي `PricingTypes` عندما `_needsPricingTypes = true`.

---

## 2) الشاشات والمكونات لكل مرحلة

### هيكل الواجهة العام

- **عمود يمين:** شريط خطوات (`ItemsControl` على `Steps`).
- **يسار:** `HeroHeader` (`StepTitle` / `StepDescription` / `EntryHint`) ثم محتوى الخطوة ثم تذييل التنقل/الحفظ.
- `ShowManualEntryForm = false` دائماً — الإدخال اليدوي عبر جداول شبيهة بـ Excel فقط.

| المرحلة | مكوّن العرض | ملاحظات |
|---------|-------------|---------|
| `Branches` | نموذج مخصص (`IsCustomFormStep`) + `DataGrid` على `BranchRows` | تفعيل الدولار، سعر الصرف، تاريخ/ملاحظات رأس المال، إضافة فرع |
| كل ما عدا `Branches` و `Products` | `PreviewGrid` + شريط أدوات + وضع Excel اختياري | إظهار الأعمدة عبر `ShowCol*` و `ApplyColumnVisibility()` |
| `Warehouses` | `PreviewGrid` + Combo `SelectedBranchForWarehouse` | عمود الفرع لكل صف + فرع افتراضي للصفوف الجديدة |
| `Products` | `ProductsGrid` منفصل (`RebuildProductsGridColumns` في code-behind) | Checkbox «أعمدة الفروع» عند تعدد الفروع (`ShowProductBranchColumns`) |

### أعمدة `ProductsGrid` (ديناميكية)

الاسم، الباركود، التصنيف (Combo)، الوصف، اختياري: ظهور فرع لكل فرع، حقول ميزات (صيدلية/سيارات/وزن/خصم حسب `FeatureFlags`)، تكلفة دينار (+ دولار)، كمية لكل مخزن، سعر بيع لكل نوع تسعير (+ دولار وشراء عند تفعيل التسعير).

---

## 3) طريقة عمل المنتجات داخل المعالج

### إدخال يدوي

1. `EnsureProductRowsReady` يجهّز ~8 صفوف فارغة من نوع `MigrationNamedBalanceRow`.
2. `SyncProductRowStructure` يملأ `ProductPrices` و `WarehouseQtys` و `BranchVisibility` حسب القوائم الحالية.
3. `AddBlankRows` يضيف 5 صفوف إضافية.

### Excel

1. `DownloadTemplateAsync` → `MigrationProductExcelHelper.BuildTemplate`
2. `BrowseFile` / `LoadPreviewAsync` → `Parse` ثم `SyncProductRowStructure` وإضافة للجدول
3. الحفظ الفعلي عند «حفظ الخطوة» فقط (`SaveProductsAsync`)

### اختيار المنتج

لا توجد شاشة اختيار منتج من قائمة منفصلة داخل المعالج؛ الإدخال بالاسم في الصف. عند الحفظ يُبحث عن منتج موجود بنفس الاسم (بدون تمييز حالة الأحرف). الباركود على التحديث يُملأ فقط إذا كان المنتج الحالي بلا باركود.

---

## 4) الفروع وعلاقة المنتجات / الكميات / الأسعار

| المفهوم | الواقع الحالي على master |
|---------|---------------------------|
| الكتالوج `Product` | مشترك على مستوى الشركة — لا `BranchId` على `Product` (`SharedCatalogAcrossBranches`) |
| الظهور في الفروع | جدول `ProductBranches` (`ProductId`, `BranchId`) — علاقة M:N |
| أعمدة الفروع في المعالج | اختيارية عبر `ShowProductBranchColumns`؛ تظهر عند `AvailableBranches.Count > 1` |
| `QueueProductBranches` | يدمج الفروع المختارة من الأعمدة + **دائماً** يضيف كل `GetAllowedBranchIds()` (+ الفرع الحالي) |
| تطبيق الربط | `IProductService.AddProductBranchesAsync` — دمج (merge) وليس استبدالاً |
| الكميات | `WarehouseStock` لكل `Warehouse`؛ المخزن له `BranchId`؛ الحفظ عبر `PersistAllowedWarehouseStocksAsync` بعد فلترة الفروع المسموحة |
| الأسعار | `ProductPrice` على مستوى المنتج + `PricingType` — **ليست لكل فرع** |
| مخازن الخطوة | كل مخزن يرتبط بفرع عبر `SelectedBranchForWarehouse` / عمود الفرع في الصف |

**سلوك ربط الفروع الفعلي:** حتى مع إلغاء تحديد أعمدة الفروع، `QueueProductBranches` يضيف كل الفروع المسموحة للمستخدم (`allowedBranchIds`)، فيظهر المنتج في كل تلك الفروع.

---

## 5) قاعدة البيانات — الجداول والحقول والعلاقات

| الخطوة / المسار | الجداول (EF) | حقول أساسية تُكتب |
|-----------------|--------------|-------------------|
| `AddBranchAsync` | `Branches` | `Name`, `Code` عبر `IBranchService.CreateAsync` |
| `PersistCurrencySettingsAsync` | `BusinessSettings`, `ExchangeRates` | MultiCurrency؛ `UsdToIqd`, `RateDate` |
| `SaveBranchesAndCapitalAsync` | `CapitalEntries` | `BranchId`, `Amount`, `Date`, `Type` (`Initial` / `ProfitOpeningBalance`), `Notes` |
| `SaveWarehousesAsync` | `Warehouses` | `Name`, `Location`, `BranchId` |
| `SaveCategoriesAsync` | `Categories` | `Name` (تخطي إن وُجد نفس الاسم) |
| `SavePricingTypesAsync` | `PricingTypes` | `Name`, `IsActive`, `IsDefault` |
| `SaveProductsAsync` | `Categories`, `Products`, `ProductPrices`, `WarehouseStocks`, `ProductBranches` | انظر التفصيل أدناه |
| `SaveCashAndBankAsync` | `CashBoxes`, `BankAccounts` | `Name`, `Balance`, `Currency`؛ `AccountNumber` للمصرف |
| `SaveInvestorsAsync` | `Investors` (+ أرصدة عبر الخدمة) | `Name`, `Phone`, `ProfitPercentage`, `OpeningBalance` |
| `SaveCustomersAsync` / `SaveSuppliersAsync` | `Customers` / `Suppliers` + OpeningPartyBalance | `Name`, `Phone`, `FileNumber`, `Notes`, `BranchId` |
| `SaveExpenseTypesAsync` | `ExpenseTypes` | `Name` |
| `SaveInstallmentsAsync` | خطط أقساط عبر `CreateOpeningBalancePlansBatchAsync` | زبون، مبلغ، عدد أقساط، مسدَّد، تاريخ بداية |

### كيان `Product` والعلاقات

- الملف: `src/AlMuhasib.Core/Entities/Product.cs`
- حقول أساسية: `Name`, `Barcode`, `Description`, `CategoryId`, `ShowInCashier` (افتراضي `true`)، حقول ميزات (`ScientificName`, `Weight`, `Discount*`, حقول سيارات…)
- `ProductBranch`: `ProductId` + `BranchId` (فريد)
- `WarehouseStock`: `ProductId` + `WarehouseId` + `BranchId` + `Quantity` + `OpeningQuantity` + `UnitCost`
- `ProductPrice`: `ProductId` + `PricingTypeId` + `SalePrice` / `SalePriceUsd` / `PurchasePrice` / `PurchasePriceUsd`

---

## 6) استيراد وتصدير Excel

### منتجات — `MigrationProductExcelHelper`

| العمود | الاستخدام |
|--------|-----------|
| `اسم_المنتج` | مطلوب |
| `الباركود` | اختياري |
| `الصنف` | فارغ → «عام» |
| `تكلفة_دينار` / `تكلفة_دولار` | تكلفة وحدة افتتاحية؛ توافق قديم: `تكلفة_الوحدة` + `عملة_التكلفة` + `سعر_الصرف` |
| `كمية_{اسم_المخزن}` | كمية افتتاحية لكل مخزن؛ توافق: `الكمية` + `المخزن` |
| `سعر_بيع_{نوع}_دينار` / `_دولار` | أسعار البيع؛ توافق قديم: `سعر_{نوع}` |
| `ظهور_فرع_{اسم}` | نعم/1/yes… عند تعدد الفروع وتفعيل أعمدة الفروع |

- ورقة «البيانات» + ورقة «تعليمات».
- قيم ظهور الفرع عبر `ProductImportSchema.IsBranchVisibleCell`.
- إن وُجدت أعمدة فروع وكلها غير ظاهرة → يُفعَّل الفرع الرئيسي (أو الأول).

### خطوات أخرى — `MigrationExcelHelper` / خدمات متخصصة

| الخطوة | أعمدة القالب (ملخص) |
|--------|---------------------|
| `CashAndBank` | `النوع`, `الاسم`, `الرصيد`, `رقم_الحساب`, `العملة` |
| `Investors` | `الاسم`, `الهاتف`, `نسبة_الربح`, `الرصيد_الافتتاحي` |
| `Warehouses` | `الاسم`, `الفرع`, `الموقع` |
| `Categories` | `اسم_الصنف` |
| `PricingTypes` / `ExpenseTypes` | `الاسم` |
| `Customers` / `Suppliers` / `Installments` | خدمات Excel مخصصة (`_customerBalanceExcel` …) |

**ملاحظة واقعية:** قالب منتجات المعالج على master لا يتضمن أعمدة ميزات الصيدلية/السيارات/الخصم رغم ظهورها في الشبكة عند تفعيل `FeatureFlags`.

---

## 7) آلية الحفظ والتحديث والمنتج الموجود مسبقاً

### `SaveProductsAsync` — تسلسل التنفيذ

1. صفوف ذات اسم غير فارغ → `GroupBy` الاسم (`IgnoreCase`) → يؤخذ **آخر صف** لكل اسم.
2. رفض إن وُجد صف `!IsValid`.
3. تحميل مخازن مع `BypassBranchFilter` ثم فلترة بـ `GetAllowedBranchIds()`.
4. لكل صف: إنشاء/جلب `Category` بالاسم → إنشاء `Product` أو تحديث الموجود بنفس الاسم → `ApplyProductFeatureFields` → تجميع `pendingBranches` و `pendingStocks` → Upsert `ProductPrice` حسب `PricingType`.
5. Commit المعاملة → `PersistAllowedWarehouseStocksAsync` → `AddProductBranchesAsync` لكل pending (دمج).

| الحالة | السلوك |
|--------|--------|
| منتج جديد (لا يوجد اسم مطابق) | `Products.Add`؛ `ShowInCashier` يبقى افتراضي الكيان `true`؛ لا يُعيَّن من المعالج |
| منتج موجود بنفس الاسم | `Update`؛ باركود فقط إن كان فارغاً؛ وصف إن وُجد؛ ميزات؛ أسعار؛ كميات؛ دمج فروع |
| تكرار الاسم داخل الجدول | آخر صف يفوز قبل الحفظ |
| تحديث المخزون الموجود | `OpeningQuantity = الكمية المدخلة`؛ `Quantity = Max(الحالي, المدخل)` |
| أسعار موجودة | تُحدَّث الحقول التي أدخلها المستخدم (`> 0`) فقط |

---

## 8) التحقق والأخطاء التي يمنعها النظام حالياً

التحذيرات عبر `Warn` → `BeautifulMessageDialog.ShowWarning`.

| السياق | أمثلة رسائل / شروط |
|--------|---------------------|
| تنقل | «احفظ الخطوات السابقة بالترتيب قبل الانتقال لهذه الخطوة» |
| فروع / رأس مال | سعر صرف مطلوب مع مبالغ دولار؛ رأس مال لكل فرع بلا رأس مال موجود؛ لا فروع |
| مخازن | مخزن واحد على الأقل؛ اختيار فرع لكل صف؛ صلاحية الفرع |
| منتجات | أضف منتجات أو تخطي؛ صحّح الصفوف غير الصالحة؛ لا مخزن ضمن الفروع المسموحة؛ كميات لمخازن خارج الصلاحية؛ المخزن الافتراضي خارج الصلاحية |
| أنواع تسعير | نوع واحد على الأقل إن `_needsPricingTypes` |
| عملاء / موردون / أقساط | صفوف غير صالحة؛ أرصدة دولار بلا سعر صرف؛ فشل دُفعة الأرصدة |
| خطوات اختيارية فارغة | غالباً رسالة «أضف … أو اضغط تخطي» |

---

## 9) الأجزاء المرتبطة التي قد تتأثر بأي تعديل

| المكوّن | العلاقة |
|---------|---------|
| `IProductService` / `ProductService` | `AddProductBranchesAsync` (دمج) — مستخدم من المعالج؛ `SetProductBranchesAsync` (استبدال) — ProductForm و BulkProducts |
| `ProductFormViewModel` + `ProductFormView` | إنشاء/تعديل منتج + `ShowInCashier` + اختيار فروع (استبدال) |
| `BulkProductsEntryViewModel` | إدخال جماعي بنفس نمط أعمدة الفروع؛ `SetProductBranchesAsync` |
| `DataImportService` + `ProductImportSchema` | استيراد منتجات من شاشة المنتجات — قوالب مختلفة (`الاسم`/`التصنيف`…)؛ صلاحية مشتركة `DataImport` |
| `PosQuickSaleViewModel` | يحمّل `GetVisibleInCurrentBranchAsync` ثم يفلتر `ShowInCashier` في الشبكة |
| `IBranchContext` / `IBranchService` | `AllowedBranchIds`, `CurrentBranchId`, إنشاء فروع |
| Sync: `ProductSyncDto` / `SyncMapper` / `SyncEngine` | `ShowInCashier` + `VisibleBranchSyncIds` |
| Cloud: `CloudProduct` / `CloudProductBranch` | نفس مفاهيم الظهور والمزامنة سحابياً |
| `Warehouse` / `WarehouseStock` / `ProductPrice` / `Category` / `PricingType` | كيانات الحفظ المباشر للمنتجات |
| `IFeatureFlagService` | أعمدة ميزات المنتج في الشبكة |
| `ScreenPermissionRegistry` + `AccountingMenuBuilder` | صلاحية `DataImport` وقائمة «معالج النقل» |

---

## 10) نقاط حساسة في الوضع الحالي (وقائع فقط)

1. ربط الفروع في المعالج **دمج** (`AddProductBranchesAsync`) بينما نموذج المنتج/الإدخال الجماعي **يستبدل** (`SetProductBranchesAsync`).
2. `QueueProductBranches` يضيف دائماً كل `allowedBranchIds` حتى لو أُلغيت أعمدة ظهور الفروع.
3. مطابقة المنتج عند الحفظ **بالاسم فقط** (`IgnoreCase`) — ليس بالباركود؛ تكرار الاسم في الجدول = آخر صف يفوز.
4. تحديث كمية المخزون: `Quantity = Max(الحالي, المدخل)` وليس استبدالاً كاملاً للكمية الحالية.
5. الأسعار عامة على المنتج (`ProductPrice`) وليست لكل فرع؛ الكميات فقط مرتبطة بالمخزن/الفرع.
6. `ShowInCashier` موجود على `Product` والمزامنة والكاشير، لكن معالج النقل على master لا يقرأه ولا يكتبه (يبقى الافتراضي `true` للمنتجات الجديدة).
7. Hydrate للمنتجات الموجودة لا يعيد بناء مصفوفة كميات كل المخازن ولا `BranchVisibility` كاملة.
8. قالب Excel للمنتجات لا يغطي أعمدة الميزات الظاهرة في الشبكة عند تفعيل `FeatureFlags`.
9. التنقل للأمام مشروط بـ `_savedSteps`؛ الحفظ التلقائي قد يحدث عند Next إن كانت الخطوة متسخة.
10. `MigrationStepKind.Capital` و `ProductPricing` أعضاء ميتة في مسار العرض الحالي (`IsProductPricingStep => false`).
11. عرض المخزن في القوائم بصيغة «الاسم (الفرع)»؛ `ResolveWarehouse` يعتمد على تحليل هذه الصيغة.
12. صلاحيات الفروع عبر `GetAllowedBranchIds` من `IBranchContext` — أي حفظ كميات/فروع خارجها يُرفض.

---

## ملخص بنية معالج النقل

### تدفق العملية من البداية إلى النهاية

```
فتح الشاشة (صلاحية DataImport)
  → InitializeAsync يحمّل الفروع/رأس المال/أنواع التسعير ويبني Steps
  → المستخدم يمرّ بالمراحل بالترتيب مع حفظ كل خطوة في _savedSteps
  → خطوة الفروع تضبط العملات ورأس المال
  → التصنيفات والمخازن وأنواع التسعير تُجهّز الكتالوج
  → خطوة المنتجات:
        جدول أو Excel → MigrationNamedBalanceRow
        → SaveProductsAsync
           · إنشاء/تحديث Product بالاسم
           · كتابة ProductPrice
           · ترحيل WarehouseStock للفروع المسموحة
           · دمج ProductBranches
  → باقي الخطوات: قاصات، مستثمرون، عملاء، موردون، مصاريف، أقساط
  → اكتمال المعالج (IsCompleted)
```

### سلسلة الاعتماد للمنتجات

```
Branches (+ CapitalEntries)
  → Warehouses(BranchId) + Categories + PricingTypes
    → Products(CategoryId)
      → ProductPrices(PricingTypeId)
      → WarehouseStocks(WarehouseId, BranchId)
      → ProductBranches(BranchId)
        → ظهور لاحق عبر فلتر ProductBranches
        → وفي الكاشير عبر ShowInCashier (خارج كتابة المعالج على master)
```

### نقاط الدخول البرمجية المختصرة

| الطبقة | الرمز |
|--------|------|
| UI | `MigrationWizardView` ← VM: `MigrationWizardViewModel` |
| حفظ المنتجات | `SaveProductsAsync` → `QueueProductBranches` → `AddProductBranchesAsync` |
| Excel منتجات | `MigrationProductExcelHelper.BuildTemplate` / `Parse` |
| نماذج الصف | `MigrationNamedBalanceRow` في `MigrationWizardModels.cs` |

---

*نهاية التقرير — وثيقة وصفية فقط للحالة على master، بدون توصيات تنفيذ.*
