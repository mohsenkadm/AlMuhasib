# مفتاح تفعيل سطح المكتب (Admin)

## ماذا يحدث عند العميل؟

| الحالة | النتيجة |
|--------|---------|
| تنصيب جديد | تجربة 30 يوماً بعد معالج الإعداد |
| انتهاء التجربة | شاشة تفعيل فقط (البيانات لا تُحذف) |
| مفتاح مدى الحياة صحيح | يعمل دائماً دون إنترنت |
| عميل قديم (`SelectedAt` قبل 2026-07-14) بدون ملف ترخيص | يُرخَّص تلقائياً مرة واحدة (Grandfathered) |
| حذف ملف الترخيص بعد التنصيب الجديد | يُقفل النظام (لا يحصل على تجديد تلقائي) |

ملف الحالة المحلي: `%LocalAppData%\AlMuhasib\desktop-license.json`

## إعداد المفتاح الخاص (Admin فقط)

المفتاح **العام** مضمّن في تطبيق العميل (`DesktopLicenseKeys.PublicKeySpkiBase64`).

المفتاح **الخاص** (PKCS#8 base64) يجب ألا يُرفع إلى Git:

```bash
cd src/AlMuhasib.Admin
dotnet user-secrets set "DesktopLicense:PrivateKeyPkcs8" "<PKCS8_BASE64>"
```

أو متغير بيئة:

```bash
export DesktopLicense__PrivateKeyPkcs8="<PKCS8_BASE64>"
```

توليد زوج مفاتيح جديد (عند التدوير — حدّث المفتاح العام في العميل أيضاً):

```csharp
using System.Security.Cryptography;
using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
Console.WriteLine(Convert.ToBase64String(ecdsa.ExportPkcs8PrivateKey()));
Console.WriteLine(Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo()));
```

> تنبيه: تدوير المفتاح العام بدون الاحتفاظ بالتحقق من المفاتيح القديمة يُبطل تراخيص Lifetime الصادرة سابقاً حتى يُعاد إدخال مفتاح جديد.
