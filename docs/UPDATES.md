# تحديثات AlMuhasib عبر الإنترنت

## نظرة عامة

1. أنت تنشر ملف **`version.json`** وملف **`AlMuhasib-x.y.z.zip`** على خادمك (HTTPS).
2. عند فتح البرنامج يفحص الرابط (إذا كان هناك إنترنت).
3. إذا وُجد إصدار أحدث، يعرض للمستخدم التثبيت.
4. يُغلق البرنامج ويُشغّل **`AlMuhasib.Updater.exe`** لتبديل الملفات.
5. عند إعادة التشغيل يُطبَّق **EF Core Migration** تلقائياً (موجود مسبقاً في بدء التشغيل).

## إعداد الخادم

### 1) ملف `version.json` (مثال)

```json
{
  "version": "1.1.0",
  "releaseDate": "2026-05-28",
  "downloadUrl": "https://yourserver.com/almahasib/releases/AlMuhasib-1.1.0.zip",
  "sha256": "ضع_هنا_بصمة_SHA256_لملف_zip",
  "sizeBytes": 0,
  "releaseNotes": "إصلاحات وتحسينات الإصدار 1.1.0",
  "isMandatory": false,
  "minSupportedVersion": "1.0.0"
}
```

### 2) ملف `appsettings.json` عند العميل

```json
"Updates": {
  "Enabled": true,
  "ManifestUrl": "https://yourserver.com/almahasib/version.json",
  "CheckOnStartup": true,
  "CheckIntervalHours": 6,
  "DownloadTimeoutMinutes": 30
}
```

## نشر تحديث جديد (PowerShell)

من جذر المستودع:

```powershell
.\scripts\publish-update.ps1 -Version "1.1.0" -OutputDir ".\publish\release-1.1.0"
```

ثم ارفع إلى الخادم:

- `publish/release-1.1.0/AlMuhasib-1.1.0.zip`
- `publish/release-1.1.0/version.json`

## محتوى حزمة ZIP

يجب أن يحتوي الـ ZIP على **محتويات مجلد النشر** (وليس المجلد نفسه فقط)، مثلاً:

- `AlMuhasib.exe`
- `AlMuhasib.dll` + بقية الـ DLL
- `AlMuhasib.Updater.exe` (مهم)
- `appsettings.json` (اختياري — لن يُستبدل عند العميل إن وُجد)

**لا تُدرج** قاعدة بيانات العميل في الحزمة.

## قاعدة البيانات

عند إضافة Migration جديد في المشروع:

```bash
dotnet ef migrations add YourMigrationName --project src/AlMuhasib.Infrastructure --startup-project src/AlMuhasib.UI
```

بعد تحديث ملفات البرنامج عند العميل، عند أول تشغيل يطبّق النظام الـ migrations المعلقة تلقائياً.

## فحص يدوي

من القائمة: **النسخ الاحتياطي والاستعادة** → **التحقق من التحديثات الآن**.
