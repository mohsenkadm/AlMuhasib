/*
  بذرة تدقيق (اختياري) — لا تُشغَّل على إنتاج العملاء.
  افترض وجود Tenant/Branch MAIN بعد migrate السحابة أو Desktop.
  الهدف: سيناريوهات ON/OFF + فرعين للتحقق اليدوي من API/Sync/التقارير.
*/
-- راجع docs/CURRENCY_BRANCH_POST_RELEASE_AUDIT.md
PRINT N'استخدم بيانات تجريبية يدوياً أو seed-accounting-demo-data.sql على Desktop فقط.';
