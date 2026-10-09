# Qaid.TelegramBot

خدمة ASP.NET Core مستقلة لاستعلام تقارير نظام قيد عبر Telegram (قراءة فقط).

## المتطلبات

- .NET 10
- توكن بوت Telegram
- عنوان HTTPS عام للـ Webhook
- عنوان واجهة `AlMuhasib.Api` السحابية

## التشغيل

> **مهم:** لا تضع `QaidApi:BaseUrl` في `appsettings.Development.json` يشير إلى `localhost` إلا إذا كان الـ API يعمل محلياً فعلاً. وإلا صفحة `/link` تظهر «تعذّر الاتصال بخدمة قيد» رغم أن Swagger يعمل على السيرفر.

1. انسخ `appsettings.Example.json` وعدّل القيم، أو استخدم User Secrets / متغيرات البيئة:

```bash
dotnet user-secrets set "Telegram:BotToken" "<token>"
dotnet user-secrets set "Telegram:WebhookSecret" "<random-secret>"
dotnet user-secrets set "QaidApi:BaseUrl" "https://your-api-host"
dotnet user-secrets set "Telegram:PublicBaseUrl" "https://your-bot-host"
dotnet user-secrets set "Telegram:WebhookUrl" "https://your-bot-host/telegram/webhook"
```

2. شغّل الخدمة:

```bash
dotnet run --project src/Qaid.TelegramBot
```

3. افتح `/link` وسجّل الدخول بحساب قيد، ثم أرسل الرمز إلى البوت.

## الأمان

- لا تُطلب كلمة المرور داخل Telegram.
- رموز الربط أحادية الاستخدام لمدة 5 دقائق وتُخزَّن كـ hash.
- access/refresh tokens تُشفَّر بـ ASP.NET Data Protection.
- قائمة سماح صارمة لـ endpoints القراءة فقط — انظر `docs/QAID_TELEGRAM_API_AUDIT.md`.

## الاختبارات

```bash
dotnet test src/Qaid.TelegramBot.Tests
```
