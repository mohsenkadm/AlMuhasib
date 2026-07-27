# موقع العرض التسويقي — قيد (Qayd)

المجلد: [`website/`](../website/)

## المعاينة المحلية

**لا تفتح `index.html` بالنقر المزدوج مباشرة** — استخدم خادماً محلياً:

```powershell
cd website
npx --yes serve .
```

ثم افتح `http://localhost:3000`

> النصوص مضمّنة في `js/locales-data.js` وتعمل حتى مع `file://`، لكن الخادم المحلي أفضل للخطوط والتنزيل.

## النشر على GitHub Pages

1. من إعدادات المستودع → **Pages** → Source: فرع `master` / مجلد `/website`
2. أو انسخ محتوى `website/` إلى فرع `gh-pages`

## رابط التنزيل

الموقع يقرأ [`version.json`](../version.json) تلقائياً من:

`https://raw.githubusercontent.com/mohsenkadm/AlMuhasib/master/version.json`

عند إصدار جديد، حدّث `version.json` و GitHub Release فقط — لا حاجة لتعديل HTML.

## المحتوى

- **العلامة التجارية:** قيد / Qayd
- **قسم الأنظمة** (`#systems`): تبويبات تفاعلية — محاسبة، فندق (+ مطعم)، عقود سيارات، تجارة سيارات، جوال
- **المنصة:** مساعد صوتي قيد، ربط فروع، طباعة احترافية، مزامنة، تحديثات تلقائية
- **Hero:** منصة متعددة الأنظمة + كلمات متحركة
- النصوص في `js/locales-data.js` (المصدر الرئيسي) و `locales/*.json` (اختياري / قديم)

## اللغات

- العربية (افتراضي، RTL)
- الإنجليزية (LTR)
