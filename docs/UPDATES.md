# تحديثات AlMuhasib عبر الإنترنت

## نظرة عامة

1. تُنشر **`version.json`** وملف **`AlMuhasib-x.y.z.zip`** على [مستودع المشروع](https://github.com/mohsenkadm/AlMuhasib).
2. عند فتح البرنامج يفحص الرابط (إذا كان هناك إنترنت).
3. إذا وُجد إصدار أحدث، يعرض للمستخدم التثبيت.
4. يُغلق البرنامج ويُشغّل **`AlMuhasib.Updater.exe`** لتبديل الملفات.
5. عند إعادة التشغيل يُطبَّق **EF Core Migration** تلقائياً (موجود مسبقاً في بدء التشغيل).

## عناوين GitHub (للعملاء)

| الغرض | الرابط |
|--------|--------|
| ملف الإصدار (manifest) | `https://raw.githubusercontent.com/mohsenkadm/AlMuhasib/master/version.json` |
| تنزيل الحزمة | `https://github.com/mohsenkadm/AlMuhasib/releases/download/v{الإصدار}/AlMuhasib-{الإصدار}.zip` |

مثال للإصدار 1.1.0:  
`https://github.com/mohsenkadm/AlMuhasib/releases/download/v1.1.0/AlMuhasib-1.1.0.zip`

### ملف `appsettings.json` عند العميل

```json
"Updates": {
  "Enabled": true,
  "ManifestUrl": "https://raw.githubusercontent.com/mohsenkadm/AlMuhasib/master/version.json",
  "CheckOnStartup": true,
  "CheckIntervalHours": 6,
  "DownloadTimeoutMinutes": 30
}
```

## نشر تحديث جديد (PowerShell)

من جذر المستودع.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-update.ps1 -Version "1.2.0" -OutputDir ".\publish\release-1.2.0" -ReleaseNotes "وصف التحديث"
```

ثم ارفع **فقط** ملفي التحديث إلى GitHub:

```powershell
git add version.json
git commit -m "release: 1.2.0"
git push origin master

gh release create v1.2.0 "AlMuhasib 1.2.0" ".\publish\release-1.2.0\AlMuhasib-1.2.0.zip" --repo mohsenkadm/AlMuhasib
```

> يجب أن يطابق الوسم (tag) في الـ Release الاسم في `downloadUrl` داخل `version.json` (مثل `v1.2.0`).

## محتوى حزمة ZIP

يجب أن يحتوي الـ ZIP على **محتويات مجلد النشر** (وليس المجلد نفسه فقط)، مثلاً:

- `AlMuhasib.exe`
- `AlMuhasib.dll` + بقية الـ DLL
- `AlMuhasib.Updater.exe` (مهم)
- `appsettings.json` (اختياري — لن يُستبدل عند العميل إن وُجد)

**لا تُدرج** قاعدة بيانات العميل في الحزمة.

## فحص يدوي

من القائمة: **النسخ الاحتياطي والاستعادة** → **التحقق من التحديثات الآن**.
