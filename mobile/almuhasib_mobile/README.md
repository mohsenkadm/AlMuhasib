# المحاسب Mobile

تطبيق Flutter (Android + iOS) للتقارير ولوحة التحكم و**إنشاء البيانات** — متصل بـ `AlMuhasib.Api`.

## المتطلبات

- Flutter SDK 3.2+
- Git في PATH
- Android Studio (للمحاكي Android) أو Xcode (لـ iOS)
- تشغيل `AlMuhasib.Api` محلياً
- **IsMobileEnabled** مفعّل للمستأجر (Cloud Admin)

## تشغيل الـ API

```powershell
cd src/AlMuhasib.Api
dotnet run --launch-profile https
```

- Swagger: https://localhost:7031/swagger (أو http://localhost:5265/swagger)
- بيانات تجريبية: `demo` / `demo123`

## إعداد التطبيق

```powershell
cd mobile/almuhasib_mobile
copy .env.example .env
flutter pub get
```

عدّل `.env` وأضف `ONESIGNAL_APP_ID` من [OneSignal Dashboard](https://onesignal.com) لتفعيل الإشعارات.

## عناوين API حسب البيئة

| البيئة | العنوان الافتراضي |
|--------|-------------------|
| Android Emulator | `https://10.0.2.2:7031` |
| iOS Simulator | `https://127.0.0.1:7031` |
| جهاز حقيقي | IP جهازك + `:7031` |

يمكن تغيير الرابط من **الإعدادات** داخل التطبيق.

## تشغيل التطبيق

```powershell
flutter run
```

## الميزات

### قراءة
- Splash + Onboarding + تسجيل دخول JWT
- لوحة تحكم (KPI + رسم بياني)
- تقارير: مبيعات، مشتريات، أرباح، متأخرات، كشف عميل، **كشف مستثمر**، مخزون، أفضل منتجات
- بيانات مع **بحث وفلاتر**: عملاء، منتجات، فواتير، موردون، مستثمرون، مخازن

### إنشاء (REST `/api/mobile/*`)
- عملاء، موردون، منتجات، مستثمرون
- **فاتورة كاملة** (بيع، شراء، أقساط، مرتجع شراء) عبر معالج 5 خطوات
- ترقيم تلقائي + مخزون + صندوق على الخادم

### واجهة
- مكونات: SearchFilterBar، FormSectionCard، LookupPickerSheet، EntityListTile
- عربي RTL + إنجليزي + وضع داكن/فاتح
- OneSignal + تسجيل الجهاز

## API الجوال (ملخص)

| Method | Path |
|--------|------|
| POST | `/api/mobile/customers` |
| POST | `/api/mobile/suppliers` |
| POST | `/api/mobile/products` |
| POST | `/api/mobile/investors` |
| POST | `/api/mobile/invoices` |
| GET | `/api/reports/statements/investor?investorSyncId=&from=&to=` |

قوائم GET تدعم `search`, `page`, `pageSize` (وفلاتر إضافية للفواتير والمنتجات).

## هيكل المشروع

```
lib/
├── core/          # شبكة، تخزين، ثيم، router
├── features/
│   ├── operations/   # forms + invoice wizard + mobile repository
│   ├── data_tab/     # lists + search
│   └── reports/      # including investor statement
└── shared/        # models, widgets, utils
```
