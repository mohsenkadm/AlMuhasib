# تحديثات AlMuhasib عبر الإنترنت

## نظرة عامة

1. تُنشر **`version.json`** وملف **`AlMuhasib-x.y.z.zip`** على [مستودع GitHub](https://github.com/mohsenkadm/almahasib).
2. عند فتح البرنامج يفحص الرابط (إذا كان هناك إنترنت).
3. إذا وُجد إصدار أحدث، يعرض للمستخدم التثبيت.
4. يُغلق البرنامج ويُشغّل **`AlMuhasib.Updater.exe`** لتبديل الملفات.
5. عند إعادة التشغيل يُطبَّق **EF Core Migration** تلقائياً (موجود مسبقاً في بدء التشغيل).

## عناوين GitHub (للعملاء)

| الغرض | الرابط |
|--------|--------|
| ملف الإصدار (manifest) | `https://raw.githubusercontent.com/mohsenkadm/almahasib/main/version.json` |
| تنزيل الحزمة | `https://github.com/mohsenkadm/almahasib/releases/download/v{الإصدار}/AlMuhasib-{الإصدار}.zip` |

مثال للإصدار 1.1.0:  
`https://github.com/mohsenkadm/almahasib/releases/download/v1.1.0/AlMuhasib-1.1.0.zip`

### ملف `appsettings.json` عند العميل

```json
"Updates": {
  "Enabled": true,
  "ManifestUrl": "https://raw.githubusercontent.com/mohsenkadm/almahasib/main/version.json",
  "CheckOnStartup": true,
  "CheckIntervalHours": 6,
  "DownloadTimeoutMinutes": 30
}
```

## نشر تحديث جديد (PowerShell)

من جذر المستودع.

إذا ظهر خطأ *running scripts is disabled*:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

أو:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-update.ps1 -Version "1.2.0" -OutputDir ".\publish\release-1.2.0" -ReleaseNotes "وصف التحديث"
```

السكربت يبني الـ ZIP و`version.json` ويحدّث **`version.json`** في جذر المشروع (لرفعه على GitHub).

### رفع الإصدار إلى GitHub

1. **Release** على GitHub بنفس الوسم `v1.2.0` وارفع ملف `AlMuhasib-1.2.0.zip` من مجلد `publish`.
2. **ادفع** `version.json` إلى الفرع `main`:

```powershell
git add version.json
git commit -m "release: 1.2.0"
git push origin main
```

أو باستخدام [GitHub CLI](https://cli.github.com/):

```powershell
gh release create v1.2.0 "AlMuhasib 1.2.0" ".\publish\release-1.2.0\AlMuhasib-1.2.0.zip" --repo mohsenkadm/almahasib
```

> يجب أن يطابق اسم الوسم (tag) في الـ Release الاسم في `downloadUrl` داخل `version.json` (مثل `v1.2.0`).

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
