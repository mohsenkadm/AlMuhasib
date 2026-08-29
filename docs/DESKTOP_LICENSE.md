# مفتاح تفعيل سطح المكتب (Admin)

المفتاح **العام** مضمّن في تطبيق العميل (`DesktopLicenseKeys.PublicKeySpkiBase64`).

المفتاح **الخاص** (PKCS#8 base64) يجب ألا يُرفع إلى Git. عيّنه محلياً على سيرفر/جهاز المطوّر بإحدى الطرق:

## 1) ملف محلي (موصى به)

أنشئ `src/AlMuhasib.Admin/appsettings.DesktopLicense.json` (مستثنى من Git):

```json
{
  "DesktopLicense": {
    "PrivateKeyPkcs8": "<PKCS8_BASE64>"
  }
}
```

عند النشر على الاستضافة انسخ نفس الملف بجانب ملفات Admin المنشورة، أو عيّن متغير البيئة أدناه.

## 2) User Secrets (تطوير محلي)

```bash
# من مجلد src/AlMuhasib.Admin
dotnet user-secrets init
dotnet user-secrets set "DesktopLicense:PrivateKeyPkcs8" "<PKCS8_BASE64>"
```

## 3) متغير بيئة

```bash
export DesktopLicense__PrivateKeyPkcs8="<PKCS8_BASE64>"
```

توليد زوج مفاتيح جديد (عند الحاجة للتدوير) — احتفظ بالخاص خارج المستودع وحدّث المفتاح العام في `DesktopLicenseKeys.cs` عند التبديل:

```csharp
using System.Security.Cryptography;
using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
Console.WriteLine(Convert.ToBase64String(ecdsa.ExportPkcs8PrivateKey()));
Console.WriteLine(Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo()));
```
