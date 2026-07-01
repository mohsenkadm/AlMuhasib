# تثبيت وتشغيل نظام قيد (Qayd)

## متطلبات بناء المثبت

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Inno Setup 6](https://jrsoftware.org/isinfo.php)
- اتصال إنترنت (لتنزيل .NET Desktop Runtime و SQL LocalDB عند البناء لأول مرة)

## بناء المثبت

من جذر المستودع:

```powershell
.\scripts\build-installer.ps1
```

الخيارات:

| الخيار | الوصف |
|--------|--------|
| `-Version 1.13.0` | تحديد رقم الإصدار |
| `-SkipLogo` | تخطي تجهيز الشعار |
| `-SkipPrerequisites` | عدم تنزيل متطلبات .NET/LocalDB |
| `-SkipCompile` | نشر التطبيق فقط دون تجميع المثبت |

المخرج: `dist\Qayd-Setup-{version}.exe`

## ماذا يفعل المثبت؟

1. يعرض معالج تثبيت باسم **قيد** مع شعار النظام
2. يطلب اختيار مجلد التثبيت ومجلد **البيانات** (قاعدة البيانات)
3. يتحقق من **.NET 10 Desktop Runtime** ويثبته إن لزم
4. يتحقق من **SQL Server LocalDB** ويثبته إن لزم
5. ينسخ ملفات التطبيق (`AlMuhasib.exe` داخلياً)
6. يكتب `appsettings.json` مع:
   - `Installation:DataDirectory` = المجلد الذي اخترته
   - `ConnectionStrings:DefaultConnection` = LocalDB
7. ينشئ اختصار **قيد** على سطح المكتب
8. يشغّل التطبيق (اختياري)

## تجهيز الشعار فقط

```powershell
.\scripts\prepare-logo.ps1
```

ينشئ الملفات في `src\AlMuhasib.UI\Assets\Brand\` و `installer\assets\`.

## ملاحظات

- اسم الملف التنفيذي يبقى `AlMuhasib.exe` داخلياً؛ الاسم الظاهر للمستخدم هو **قيد**
- التحديثات عبر الإنترنت تبقى عبر `AlMuhasib.Updater` و GitHub Releases
- عند إعادة التثبيت، يُحافظ على `appsettings.json` الموجود
