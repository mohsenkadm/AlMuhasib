# تدقيق واجهات API لبوت Telegram (قيد / AlMuhasib)

تاريخ التدقيق: مستخرج من Controllers والكود المصدري في المستودع.  
**لا يوجد ملف swagger.json أو OpenAPI ثابت في Git.** المصدر التشغيلي هو Swashbuckle في `src/AlMuhasib.Api/Program.cs` (`/swagger/v1/swagger.json` وقت التشغيل). لم يُعثر على أي كود Telegram سابق.

## 1. المصادقة

| المسار | الطريقة | الجسم | الاستجابة | ملاحظات |
|--------|---------|-------|-----------|---------|
| `api/auth/login` | POST | `TenantLoginRequest`: `Username`, `Password` | `TenantLoginResponse` | AllowAnonymous |
| `api/auth/refresh` | POST | `RefreshTokenRequest`: `RefreshToken` | `TenantLoginResponse` | AllowAnonymous |
| `api/auth/select-branch` | POST | `SelectBranchRequest`: `BranchId`, `AllBranches` | `SelectBranchResponse` | Policy `Tenant` |
| `api/auth/license-status` | GET | — | `LicenseStatusResponse` | Policy `Tenant` |

### خصائص `TenantLoginResponse` (مثبتة)

`AccessToken`, `RefreshToken`, `AccessTokenExpiresAt`, `TenantId`, `CompanyName`, `TenantName`, `ApplicationSystemType`, `IsMobileEnabled`, `LicenseExpiresAt`, `AccountExpiresAt`, `AllowedBranches`, `DefaultBranchId`, `RequiresBranchSelection`, `CanViewAllBranches`, `CanManageAllBranches`, `CurrentBranchId`

### JWT Claims ذات الصلة بالعزل

| Claim | المعنى |
|-------|--------|
| `tenant_id` | معرّف المستأجر (مصدر الحقيقة للعزل) |
| `NameIdentifier` | `TenantAccount.Id` |
| `Role` | `Tenant` |
| `branch_id` | الفرع المحدد (إن وُجد) |
| `all_branches` | وضع كل الفروع |

عزل البيانات: قاعدة سحابية مشتركة + فلتر EF على `TenantId` عبر `TenantContextMiddleware` — **ليس** قاعدة بيانات منفصلة لكل شركة.

### قيود مثبتة

- مدة access token الافتراضية: 15 دقيقة (`Jwt:AccessTokenMinutes`).
- Refresh token يُخزَّن على `TenantAccount` ويُستبدل عند كل login → جلسة refresh واحدة فعّالة لكل حساب.
- Swagger الحالي بلا `AddSecurityDefinition` لـ Bearer.

## 2. قائمة السماح لبوت الاستعلام (قراءة فقط)

| غرض البوت | Endpoint | معاملات الفترة |
|-----------|----------|----------------|
| المبيعات اليومية | `GET api/reports/daily-sales` | `from`, `to` (+ `currencyScope` اختياري) |
| المصروفات | `GET api/reports/expenses` | `from`, `to` |
| الأرباح والخسائر | `GET api/reports/profit-and-loss` | `from`, `to`, `currencyScope` |
| الأرصدة | `GET api/reports/cash-balances-summary` | لا فلتر تاريخ |
| التدفق النقدي | `GET api/reports/cash-flow` | `from`, `to` |
| ملخص الزبائن | `GET api/reports/customers/overview` | `from`, `to` |
| آخر مزامنة | `GET api/sync/status` → `LastSyncAt` | — |
| تجديد الجلسة | `POST api/auth/refresh` | — |
| اختيار الفرع (صفحة الربط فقط) | `POST api/auth/select-branch` | — |
| تسجيل الدخول (صفحة الويب فقط) | `POST api/auth/login` | — |

فلتر التقارير الشائع: `ReportFilterRequest` (`From`, `To`, …) في `AlMuhasib.Cloud.Application/Models/MasterDataModels.cs`.

### نماذج الاستجابة المستخدمة (خصائص أساسية)

- `DailySalesReportResult`: `TotalSales`, `TotalSalesUsd`, `DayCount`, `InvoiceCount`, `AverageDaily`, `Rows`
- `ExpensesReportResult`: `TotalExpenses`, `TodayExpenses`, `MonthExpenses`, `TopExpenseType`, `Rows`
- `ProfitAndLossReportResult`: `TotalSales`, `CostOfGoodsSold`, `GrossProfit`, `TotalExpenses`, `OperatingProfit`, `NetProfit`, `GrossMarginPercent`, `NetMarginPercent`, `Lines`
- `CashBalancesSummaryReportResult`: `CashBoxesTotal`, `BanksTotal`, `TotalLiquid`, `*Usd`, `AccountCount`, `Rows`
- `CashFlowResult`: `TotalIncoming`, `TotalOutgoing`, `NetFlow`, `CurrentBalance`, `Rows`
- `CustomersOverviewReportResult`: `TotalSales`, `TotalCollected`, `TotalOutstanding`, `CustomerCount`, `Rows`
- `SyncStatusResponse`: `LastSyncAt`, `PendingPushCount`, `IsLicensed`, `LicenseMessage`

## 3. قائمة المنع (لا يستدعيها البوت)

- `POST api/sync/push`, `POST api/sync/pull`
- كل عمليات الكتابة في `api/mobile` (إنشاء/تعديل فواتير، سندات، مصروفات، …)
- أي `POST`/`PUT`/`DELETE` على الفواتير أو العقود أو العمليات الرأسية (فندق/سيارات/ذهب/عقارات)
- واجهات المطوّر `api/admin/*` و`api/auth/developer/login`

تُفرض القائمة داخل طبقة عميل البوت (`QaidApiClient` / `AllowedEndpoints`).

## 4. ما لا يمكن إثباته من الكود/Swagger الثابت

- لا يوجد ملف OpenAPI مفحوص في Git؛ الاعتماد على Controllers + نماذج Sync/Report.
- لا يوجد endpoint مخصص لربط Telegram في API الحالية؛ الربط يُنفَّذ في خدمة البوت المستقلة.
- سلوك تعدد الجلسات المتزامن غير مدعوم صراحةً بسبب استبدال refresh token عند login.

## 5. قرار التنفيذ

خدمة مستقلة `Qaid.TelegramBot` تستدعي فقط قائمة السماح أعلاه عبر HTTPS + Bearer، دون الاتصال بقواعد البيانات المحاسبية ودون تعديل `AlMuhasib.Api` في هذه المرحلة.
