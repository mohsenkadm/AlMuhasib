# تثبيت وتشغيل نظام قيد (Qayd)

## متطلبات بناء المثبت

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Inno Setup 6](https://jrsoftware.org/isinfo.php)
- اتصال إنترنت (لتنزيل VC++ Redistributable و SQL LocalDB عند البناء لأول مرة)

## بناء المثبت

من جذر المستودع:

```powershell
.\scripts\build-installer.ps1
```

الخيارات:

| الخيار | الوصف |
|--------|--------|
| `-Version 1.14.6` | تحديد رقم الإصدار |
| `-SkipLogo` | تخطي تجهيز الشعار |
| `-SkipPrerequisites` | عدم تنزيل المتطلبات (يجب أن تكون موجودة مسبقاً في `installer\prerequisites`) |
| `-SkipCompile` | نشر التطبيق فقط دون تجميع المثبت |

المخرج: `dist\Qayd-Setup-{version}.exe`

## ماذا يفعل المثبت؟

1. يعرض معالج تثبيت باسم **قيد** مع شعار النظام
2. يطلب اختيار مجلد التثبيت ومجلد **البيانات** (قاعدة البيانات)
3. يثبّت **Visual C++ Redistributable** إن لزم (متطلب LocalDB)
4. يثبّت **SQL Server LocalDB** بصمت مع سجل أخطاء إن لزم
5. ينسخ التطبيق **مع .NET مضمّن (self-contained)** — لا يحتاج العميل تنصيب .NET يدوياً
6. يكتب `appsettings.json` مع:
   - `Installation:DataDirectory` = المجلد الذي اخترته
   - `ConnectionStrings:DefaultConnection` = LocalDB (قيمة أولية)
7. عند أول تشغيل ومعالج الإعداد: إن اخترت **حاسبة مستقلة** تظهر قائمة منسدلة بأسماء سيرفرات SQL المتوفرة على الجهاز
8. ينشئ اختصار **قيد** على سطح المكتب
9. يشغّل التطبيق (اختياري)

## لماذا كان التنصيب يفشل سابقاً؟

1. المثبت كان يبحث عن متطلبات .NET/LocalDB **قبل** استخراجها من ملف التنصيب
2. التطبيق كان يعتمد على .NET المنفصل (framework-dependent) فيفشل إن لم يكتمل تنصيب الـ Runtime
3. LocalDB يحتاج غالباً Visual C++ Redistributable ولم يكن يُثبَّت تلقائياً

## رفع المثبت للعملاء

ارفع الملف إلى GitHub Releases (وليس داخل git):

```powershell
gh release upload v1.14.6 dist\Qayd-Setup-1.14.6.exe --clobber
```

حد المستودع 100MB؛ Releases تقبل حتى 2GB.

## تجهيز الشعار فقط

```powershell
.\scripts\prepare-logo.ps1
```

## ملاحظات

- اسم الملف التنفيذي يبقى `AlMuhasib.exe` داخلياً؛ الاسم الظاهر للمستخدم هو **قيد**
- التحديثات عبر الإنترنت تُبنى أيضاً self-contained عبر `scripts\publish-update.ps1`
- عند إعادة التثبيت، يُحافظ على `appsettings.json` الموجود
- سجل فشل LocalDB: `%TEMP%\Qayd-SqlLocalDB-Install.log`
