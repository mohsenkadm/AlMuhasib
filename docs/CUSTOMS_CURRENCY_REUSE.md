# Currency & Exchange Rates — Reuse Guide for CustomsClearance

**Scope:** How IQD/USD work in Accounting and Gold, what CarTrade/RealEstate do, and what CustomsClearance should reuse.  
**Date:** 2026-10-22  
**Constraint for Customs:** IQD + USD only (same pair as Accounting/Gold).

---

## Verdict for CustomsClearance

Reuse the **Accounting** multi-currency pattern (document `Currency` + snapshot `FxRate`, no mixing of balances), with a **module-local** FX table like Gold’s `GoldFxRates` (do not share `AppDbContext.ExchangeRates`). There is **no** `Money` type — amounts are plain `decimal` plus an enum.

---

## 1. Currency enums

### Accounting — `AccountingCurrency`

| Path | `/workspace/src/AlMuhasib.Core/Enums/AccountingCurrency.cs` |
|------|--------------------------------------------------------------|
| Values | `IQD = 0`, `USD = 1` |
| Default | IQD (base currency) |
| Storage | Usually `HasConversion<string>()`, max length 10 |

Helpers / rules:

| Path | Role |
|------|------|
| `/workspace/src/AlMuhasib.Core/Helpers/AccountingCurrencyHelper.cs` | Labels (`د.ع` / `$`), format, convert, round |
| `/workspace/src/AlMuhasib.Core/Helpers/AccountingCurrencyRules.cs` | `EnsureValidFxRate`, `RequireFxRateOrThrow`, `EnsureSameCurrency`, `ApplyFeatureGate`, `ToBaseIqdStrict` |
| `/workspace/src/AlMuhasib.Core/Enums/ReportCurrencyScope.cs` | `Iqd` / `Usd` / `All` (All = separate disclosure, never mixed sum) |
| `/workspace/src/AlMuhasib.Core/Helpers/ReportCurrencyScopeHelper.cs` | Report filters |
| `/workspace/src/AlMuhasib.Core/Helpers/FinancialReportIqdFoldIn.cs` | Optional fold of USD→IQD for P&L-style totals via document `FxRate` |

**Rounding (Accounting):**

- IQD: `Math.Round(..., 0, AwayFromZero)` → whole dinars  
- USD: `Math.Round(..., 2, AwayFromZero)` → 2 decimals  

### Gold — `GoldCurrency`

| Path | `/workspace/src/AlMuhasib.Core/Enums/Gold/GoldCurrency.cs` |
|------|------------------------------------------------------------|
| Values | `IQD = 0`, `USD = 1` (identical numeric meaning) |
| Helper | `/workspace/src/AlMuhasib.Infrastructure/Services/Gold/GoldCurrencyHelper.cs` (internal) |

Gold also supports **pricing currency ≠ payment currency** on one invoice (`PricingCurrency` / `PaymentCurrency`) and stores dual totals (`TotalAmountIqd`, `TotalAmountUsd`).

**Do not reuse `GoldCurrency` for Customs** unless Customs needs pricing≠payment; prefer `AccountingCurrency` or a thin `CustomsCurrency` mirror with the same `IQD=0, USD=1` values for DB/API clarity.

---

## 2. ExchangeRate entity & ExchangeRateService (Accounting)

### Entity

| Item | Detail |
|------|--------|
| Path | `/workspace/src/AlMuhasib.Core/Entities/ExchangeRate.cs` |
| Base | `BranchScopedEntity` (per-branch rates) |
| Fields | `RateDate` (`DateTime`), `UsdToIqd` (`decimal`), `Notes` (`string`) |
| Meaning | **How many IQD per 1 USD** |
| Config | `/workspace/src/AlMuhasib.Infrastructure/Data/Configurations/ExchangeRateConfiguration.cs` |
| Table | `ExchangeRates` |
| Precision | `UsdToIqd` → **`decimal(18, 4)`**; `Notes` max 500 |
| Index | `RateDate` (not unique per day — same-day upsert is service logic) |

### Service

| Item | Detail |
|------|--------|
| Interface | `/workspace/src/AlMuhasib.Core/Interfaces/Services/IExchangeRateService.cs` |
| Impl | `/workspace/src/AlMuhasib.Infrastructure/Services/ExchangeRateService.cs` |
| API | `/workspace/src/AlMuhasib.Api/Controllers/ExchangeRatesController.cs` |
| UI | `/workspace/src/AlMuhasib.UI/ViewModels/ExchangeRatesViewModel.cs`, `Views/ExchangeRatesView.xaml` |

Methods:

- `GetAllAsync` / `GetLatestAsync` / `GetForDateAsync` (≤ date, latest wins)
- `GetUsdToIqdForDateOrLatestAsync` → `0` if missing/invalid
- `SaveAsync` — rejects `UsdToIqd <= 0`; upserts same `BranchId` + calendar day
- `DeleteAsync` — soft delete

### Related: cash currency exchange (not the rate table)

| Item | Detail |
|------|--------|
| Entity | `/workspace/src/AlMuhasib.Core/Entities/CurrencyExchange.cs` |
| Service | `/workspace/src/AlMuhasib.Infrastructure/Services/CurrencyExchangeService.cs` |
| Purpose | Move money between IQD and USD cash boxes |
| Fields | `FromCashBoxId`, `ToCashBoxId`, `FromCurrency`, `ToCurrency`, `FromAmount`, `ToAmount`, `FxRate`, `Date`, `Notes` |
| Precision | amounts & `FxRate` → **`decimal(18, 4)`** |

### Gold FX (separate catalog)

| Item | Detail |
|------|--------|
| Entity | `/workspace/src/AlMuhasib.Core/Entities/Gold/GoldFxRate.cs` |
| Fields | `RateDate`, `UsdToIqd`, `Notes` |
| Table | `GoldFxRates` |
| Precision | `UsdToIqd` → **`decimal(18, 2)`** (coarser than Accounting) |
| Service | `/workspace/src/AlMuhasib.Infrastructure/Services/Gold/GoldPricingService.cs` (`GetLatestFxRateAsync`, `SaveFxRateAsync`, …) |
| UI | `/workspace/src/AlMuhasib.UI/ViewModels/Gold/GoldFxRatesViewModel.cs` |

Gold rates live in `GoldDbContext` and are **not** shared with Accounting `ExchangeRates`.

---

## 3. How invoices store Currency + FxRate

### Accounting `Invoice`

| Path | `/workspace/src/AlMuhasib.Core/Entities/Invoice.cs` |
|------|-----------------------------------------------------|
| `Currency` | `AccountingCurrency` — **single document currency** (default IQD) |
| `FxRate` | `decimal`, default `1m` — USD→IQD snapshot at save; used when `Currency == USD` |
| Amounts | `TotalAmount`, `DiscountAmount`, `NetAmount`, `PaidAmount`, `RemainingAmount`, fees — all in **document currency** |
| Config | `/workspace/src/AlMuhasib.Infrastructure/Data/Configurations/InvoiceConfiguration.cs` |
| Precision | `FxRate` **`(18,4)`**; money fields **`(18,2)`**; `Currency` string conversion |

UI fill path (example): `/workspace/src/AlMuhasib.UI/ViewModels/SalesInvoiceViewModel.Currency.cs`

- IQD → `FxRate = 1`
- USD → load via `IExchangeRateService.GetUsdToIqdForDateOrLatestAsync(InvoiceDate)`
- Cash boxes filtered to matching currency

Write-path enforcement: `AccountingCurrencyRules` / `MultiCurrencyFeatureGate` (`/workspace/src/AlMuhasib.Infrastructure/Services/MultiCurrencyFeatureGate.cs`) — when MultiCurrency feature is OFF, USD writes are rejected.

Other Accounting documents with the same pattern:

| Entity | Path | Currency field | FxRate |
|--------|------|----------------|--------|
| Voucher | `.../Entities/Voucher.cs` | `Currency` | yes `(18,4)` |
| Expense | `.../Entities/Expense.cs` | `Currency` | yes |
| Transfer | `.../Entities/Transfer.cs` | `Currency` | yes |
| CashBox | `.../Entities/CashBox.cs` | fixed `Currency` (single balance) | — |
| BankAccount | `.../Entities/BankAccount.cs` | fixed `Currency` | — |
| SalesRepCollection / CommissionEntry | under `Entities/` | `Currency` | — |

Product list prices (dual columns, not document FX): `/workspace/src/AlMuhasib.Core/Entities/ProductPrice.cs` — `SalePrice` / `SalePriceUsd`, `PurchasePrice` / `PurchasePriceUsd`.

### Gold `GoldInvoice`

| Path | `/workspace/src/AlMuhasib.Core/Entities/Gold/GoldInvoice.cs` |
|------|-------------------------------------------------------------|
| `PricingCurrency` | `GoldCurrency` |
| `PaymentCurrency` | `GoldCurrency` |
| `FxRate` | snapshot; precision **`(18,2)`** |
| `TotalAmount` | in pricing currency |
| `TotalAmountIqd` / `TotalAmountUsd` | dual stored totals via `GoldCurrencyHelper.ApplyDualTotals` |
| Payments | `/workspace/src/AlMuhasib.Core/Entities/Gold/GoldPayment.cs` — own `Currency` + `FxRate` |

Party balances stored dual: `GoldCustomer.CreditBalanceIqd` / `CreditBalanceUsd` (same on suppliers).

---

## 4. Are amounts in different currencies kept separate? (no mixing)

**Yes — by design in Accounting and Gold.**

Evidence:

1. **Document rule:** one operational currency per Accounting invoice; vouchers must match cash/bank currency (`EnsureSameCurrency`).
2. **AR/AP:** `/workspace/src/AlMuhasib.Core/CustomerBalanceHelper.cs` — “التسوية والحسابات تتم لكل عملة على حدة — لا يُخلط دينار بدولار.” Returns `DualCurrencyBalance(Iqd, Usd)` from `AccountingCurrencyRules.cs`.
3. **Reports:** `ReportCurrencyScope.All` means **separate** IQD and USD fields — never one mixed total (unless an explicit fold via `FinancialReportIqdFoldIn` for financial summaries, using stored document `FxRate`).
4. **Cash:** each `CashBox` / `BankAccount` has one fixed currency and one balance.
5. **FX settlement exception:** cross-currency voucher settlement is encoded in notes (`[FX-SETTLE:...]` on `Voucher`), not by adding IQD+USD into one number.
6. **Gold:** dual credit columns and dual invoice totals; conversion only through positive `FxRate`.

There is **no FX gain/loss** entity in the Accounting design (`docs/CURRENCY_MULTI_CURRENCY_AUDIT.md`).

---

## 5. CarTrade / RealEstate currency handling

**Neither module has currency or FX fields.**

| Module | Key entities | Amounts |
|--------|--------------|---------|
| CarTrade | `/workspace/src/AlMuhasib.Core/Entities/CarTrade/CarTradeTransaction.cs`, `CarTradePayment.cs` | `PurchasePrice`, `SalePrice`, `TotalAmount`, `AmountPaid`, … — plain `decimal`, implicit single currency |
| RealEstate | `/workspace/src/AlMuhasib.Core/Entities/RealEstate/RealEstateContract.cs`, payments/expenses | `TotalPrice`, `DownPayment`, `AmountPaid`, `Amount` — plain `decimal` |

Confirmed: no `Currency` / `FxRate` / `AccountingCurrency` / `GoldCurrency` under CarTrade or RealEstate entity trees. Migrations use `decimal(18,2)` for money. Treat as **single-currency (implicit IQD) legacy modules** — not a reuse target for Customs multi-currency.

Hotel note (out of scope): `HotelSettings.Currency` is a **string** default `"IQD"`, not the Accounting enum.

---

## 6. Money abstraction

**None.** Grep finds no `Money` / `MoneyAmount` class, record, or struct.

Pattern everywhere:

```text
decimal amount + AccountingCurrency|GoldCurrency + optional FxRate snapshot
```

Optional aggregate: `DualCurrencyBalance` (`readonly record struct` in `AccountingCurrencyRules.cs`) for reporting/balances only.

---

## 7. Precision cheat sheet

| Field | Module | Precision |
|-------|--------|-----------|
| `ExchangeRate.UsdToIqd` | Accounting | `(18, 4)` |
| `Invoice.FxRate` / Voucher / Expense / Transfer | Accounting | `(18, 4)` |
| Invoice/voucher/expense amounts | Accounting | `(18, 2)` |
| `CurrencyExchange` amounts + FxRate | Accounting | `(18, 4)` |
| `GoldFxRate.UsdToIqd` | Gold | `(18, 2)` |
| `GoldInvoice.FxRate` / payment FxRate | Gold | `(18, 2)` |
| Gold money amounts | Gold | `(18, 2)` |
| Display/calc IQD | both | 0 decimal places |
| Display/calc USD | both | 2 decimal places |

---

## 8. Recommended reuse for CustomsClearance (IQD + USD only)

Customs uses its own DB (`AlMuhasibCustomsDb` / `CustomsDbContext`) — see `CUSTOMS_IMPLEMENTATION_PLAN.md`. Do **not** open `AppDbContext.ExchangeRates` from Customs.

### Reuse as-is (prefer Accounting over Gold)

| Asset | Reuse how |
|-------|-----------|
| `AccountingCurrency` (`IQD=0`, `USD=1`) | Reference directly, **or** define `CustomsCurrency` with identical values if you want domain isolation |
| `AccountingCurrencyHelper` | Shared convert/format/round |
| `AccountingCurrencyRules` (`EnsureSameCurrency`, `RequireFxRateOrThrow`, `ToBaseIqdStrict`) | Shared write guards |
| `DualCurrencyBalance` | Party/cash reporting |
| Document shape | `Currency` + `FxRate` snapshot on every monetary document |
| Cash boxes | Fixed currency per box; one balance |
| Report scope idea | Separate IQD/USD columns; fold only when explicitly required |

### Copy / adapt into Customs DB (do not share tables)

| Pattern | Suggested Customs names |
|---------|-------------------------|
| Daily USD→IQD table | `CustomsFxRate` / `CustomsExchangeRates` with `RateDate`, `UsdToIqd`, `Notes` |
| Precision | Prefer Accounting: **`UsdToIqd` & document `FxRate` = `(18,4)`**; amounts `(18,2)` |
| Service | `ICustomsExchangeRateService` mirroring `IExchangeRateService` (latest / for-date / upsert same day) |
| UI | Clone `ExchangeRatesView` patterns; optional later cash-exchange like `CurrencyExchange` |

### Avoid

| Avoid | Why |
|-------|-----|
| `GoldCurrency` + pricing≠payment unless needed | Extra complexity Customs does not require for IQD+USD docs |
| Sharing Accounting `ExchangeRates` across DBs | Systems are mutually exclusive product modes with separate catalogs |
| CarTrade/RealEstate amount model | No currency dimension |
| Introducing a `Money` type in phase 1 | Would diverge from platform conventions |
| Silent `FxRate = 1` on USD | Forbidden by `AccountingCurrencyRules` / Gold helper lessons |
| Mixing IQD+USD into one balance column | Violates platform invariant |

### Minimal Customs document contract

```csharp
// Conceptual — mirror Invoice, not a pasted type
AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
decimal FxRate { get; set; } = 1m;          // USD→IQD snapshot; 1 when IQD
decimal Amount { get; set; }               // always in Currency
```

On save: `RequireFxRateOrThrow(Currency, FxRate)`; when linking payment to cash box / receivable, `EnsureSameCurrency(...)`.

### Feature gate

Accounting’s `MultiCurrencyEnabled` lives in Accounting `BusinessSettings`. Customs should either:

- always allow IQD+USD (module always multi-currency), or  
- own a Customs settings flag — **do not** read Accounting `BusinessSettings` from another DB.

---

## Related existing audits

- `/workspace/docs/CURRENCY_MULTI_CURRENCY_AUDIT.md`
- `/workspace/docs/CURRENCY_SCENARIO_MATRIX.md`
- `/workspace/docs/CURRENCY_FEATURE_TOGGLE_AUDIT.md`
- `/workspace/docs/CURRENCY_BRANCH_POST_RELEASE_AUDIT.md`
- `/workspace/CUSTOMS_IMPLEMENTATION_PLAN.md` (architecture; links here for currency)
