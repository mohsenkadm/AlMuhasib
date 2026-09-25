# Gold multi-currency pattern → main accounting reuse

Brief map of how Gold Shop already does IQD/USD, plus where main accounting still hardcodes `د.ع` / `FormatCurrency`, and a recommended dual-currency architecture behind a feature flag.

---

## 1. Gold pattern (reuse checklist)

### 1.1 `GoldCurrency` enum

**Path:** `src/AlMuhasib.Core/Enums/Gold/GoldCurrency.cs`

```csharp
public enum GoldCurrency { IQD = 0, USD = 1 }
```

UI label helper (local to VMs): `GoldCurrencyOption` in  
`src/AlMuhasib.UI/ViewModels/Gold/GoldSaleLineDraft.cs`  
(`IQD` → «دينار عراقي», `USD` → «دولار أمريكي»).

Stored on: invoices (`PricingCurrency` / `PaymentCurrency`), vouchers, expenses, payments, cash boxes, mithqal prices, making charges.

### 1.2 `GoldFxRate` entity + rates list UI

**Entity:** `src/AlMuhasib.Core/Entities/Gold/GoldFxRate.cs`

| Field | Role |
|-------|------|
| `RateDate` | Calendar day of the rate |
| `UsdToIqd` | 1 USD → IQD |
| `Notes` | Optional |

**Service:** `IGoldPricingService` / `GoldPricingService` — `GetLatestFxRateAsync`, `GetFxRatesAsync`, `SaveFxRateAsync`.

**UI:**

| Piece | Path |
|-------|------|
| View | `src/AlMuhasib.UI/Views/Gold/GoldFxRatesView.xaml` |
| VM | `src/AlMuhasib.UI/ViewModels/Gold/GoldFxRatesViewModel.cs` |
| Menu | `GoldShopMenuBuilder` → «أسعار الصرف» |
| Live refresh | `GoldFxRateRefreshHelper` + `GoldFxRateChangedMessage` (open invoices pick up new rate) |

**Filters:** list uses `MasterDataColumnFilterHelper` + `DataGridColumnFilterBehavior` on `RateDate`, `UsdToIqd`, `Notes`, `CreatedAt` (same list-filter pattern as other master grids).

Latest rate banner: `1 USD = {N0} IQD — yyyy/MM/dd`.

### 1.3 Currency picker on invoices (when context allows)

Gold dual currency is **not** a separate toggle inside Gold; it is part of the **Gold Shop system profile** (`ApplicationSystemType.GoldShop` / `IsGoldShop`). In that context, sale / purchase / exchange / return invoice screens always expose:

1. **عملة التسعير** → `PricingCurrency`
2. **عملة الدفع** → `PaymentCurrency`
3. **سعر الصرف** → `FxRate` (+ refresh from latest `GoldFxRate`)
4. Cash box list reloads so default box matches `PaymentCurrency`

**Canonical UI:** `src/AlMuhasib.UI/Views/Gold/GoldSaleInvoiceView.xaml` (pricing/payment combos + FX field).  
**Canonical VM:** `src/AlMuhasib.UI/ViewModels/Gold/GoldSaleInvoiceViewModel.cs`  
Same pattern: purchase, exchange, sale-return VMs.

**Invoice persistence** (`GoldInvoice`):

- `PricingCurrency`, `PaymentCurrency`, `FxRate`
- Dual totals: `TotalAmount` (pricing currency) + `TotalAmountIqd` + `TotalAmountUsd`
- `PaidAmount` / `RemainingAmount` in payment currency
- `CashBoxId` must match payment currency box

Party credit is dual: `GoldCustomer` / `GoldSupplier` → `CreditBalanceIqd` + `CreditBalanceUsd`.

### 1.4 `GoldCashBox` per currency

**Entity:** `src/AlMuhasib.Core/Entities/Gold/GoldCashBox.cs`

- `Currency` (`GoldCurrency`)
- `Balance`, `IsDefault`, `IsActive`
- Defaults ensured per currency in `GoldSettingsService` (one default IQD box + one default USD box)

**UI:** `GoldCashBoxesView` / `GoldCashBoxesViewModel` — currency combo on create/edit; setup wizard seeds IQD + USD boxes.

Contrast main accounting: `CashBox` / `BankAccount` have **no** currency field (single IQD ledger).

### 1.5 `GoldCurrencyHelper`

**Path:** `src/AlMuhasib.Infrastructure/Services/Gold/GoldCurrencyHelper.cs`

| Method | Purpose |
|--------|---------|
| `ConvertAmount(amount, from, to, fxRate)` | USD↔IQD; IQD rounds 0 dp, USD 2 dp |
| `ApplyDualTotals(invoice)` | Fills `TotalAmountIqd` / `TotalAmountUsd` from pricing currency + FX |
| `ResolveStatus(...)` | Paid vs total → invoice status |
| `ToListItem(invoice)` | List DTO including both currencies |
| `Round` / stock helpers | Shared rounding |

Domain services (`GoldSaleService`, vouchers, expenses, cash) call these instead of inlining FX math.

---

## 2. Top 20 main-accounting UI entry points that hardcode `د.ع` / `FormatCurrency`

Focus: invoices, vouchers, expenses, transfers, cashboxes/banks, customer balances, reports.  
Central helpers amplify impact: `ReportViewModelBase.FormatCurrency` → `$"{value:N0} د.ع"` and `CurrencyFormatConverter`.

| # | Path | Why it matters |
|---|------|----------------|
| 1 | `src/AlMuhasib.UI/ViewModels/ReportViewModelBase.cs` | Shared `FormatCurrency` for nearly all reports |
| 2 | `src/AlMuhasib.UI/Converters/CurrencyFormatConverter.cs` | Global XAML converter appends `د.ع` |
| 3 | `src/AlMuhasib.UI/Views/SalesInvoiceView.xaml` | Sale totals / footer hardcode `د.ع` |
| 4 | `src/AlMuhasib.UI/ViewModels/SalesInvoiceViewModel.cs` | Sale messaging / display amounts |
| 5 | `src/AlMuhasib.UI/Views/PurchaseInvoiceView.xaml` | Purchase totals hardcode `د.ع` |
| 6 | `src/AlMuhasib.UI/ViewModels/PurchaseInvoiceViewModel.cs` | Purchase display |
| 7 | `src/AlMuhasib.UI/Views/PosQuickSaleView.xaml` | POS line/totals `د.ع` |
| 8 | `src/AlMuhasib.UI/ViewModels/PosQuickSaleViewModel.cs` | POS amount strings |
| 9 | `src/AlMuhasib.UI/Views/InstallmentInvoiceView.xaml` | Installment invoice totals |
| 10 | `src/AlMuhasib.UI/ViewModels/InstallmentInvoiceViewModel.cs` | Installment create flow |
| 11 | `src/AlMuhasib.UI/Views/InstallmentsView.xaml` | Highest density of `د.ع` in UI (~19) |
| 12 | `src/AlMuhasib.UI/ViewModels/VouchersViewModel.cs` | Receipt/payment balance text (`د.ع`) |
| 13 | `src/AlMuhasib.UI/Views/ExpenseView.xaml` | Expense amounts (IQD-only model; `N0` UI) |
| 14 | `src/AlMuhasib.UI/Views/CashBankView.xaml` | Cash/bank balances (no currency column) |
| 15 | `src/AlMuhasib.UI/Controls/InvoiceDetailMapper.cs` | Invoice detail overlay amount labels |
| 16 | `src/AlMuhasib.UI/ViewModels/CustomerStatementViewModel.cs` | Customer statement totals via `FormatCurrency` |
| 17 | `src/AlMuhasib.UI/ViewModels/SupplierStatementViewModel.cs` | Supplier statement totals |
| 18 | `src/AlMuhasib.UI/Views/OpeningCustomerBalanceView.xaml` (+ `OpeningCustomerBalanceViewModel.cs`) | Opening A/R hardcode |
| 19 | `src/AlMuhasib.UI/ViewModels/CashBoxMovementReportViewModel.cs` | Cashbox movement report |
| 20 | `src/AlMuhasib.UI/ViewModels/BankAccountStatementReportViewModel.cs` | Bank statement report |

**Also high priority (just outside top 20):**  
`CashBalancesSummaryReportViewModel.cs`, `SalesReportViewModel.cs` / `SalesReportView.xaml`, `ExpensesReportViewModel.cs`, `TransfersReportViewModel.cs` / `TransfersReportView.xaml`, `OpeningSupplierBalanceView.xaml`, `CustomersView.xaml` (credit limit hint), `IraqiCurrencyChangeDialog.xaml` / `IraqiCurrencyChangeViewModel.cs`, Shared print/export (`ExcelExportService`, `InvoicePdfGenerator`, `PosReceiptDocumentBuilder`).

**Entity gap (not UI, but blocks dual currency):**  
`src/AlMuhasib.Core/Entities/CashBox.cs`, `BankAccount.cs` — single `Balance`, no `Currency`.

---

## 3. Recommended architecture (mirror Gold, behind a feature flag)

### 3.1 Feature flag

Add to `BusinessFeatureFlags` (same pattern as Loyalty / SalesRepresentatives):

```csharp
/// <summary>محاسبة متعددة العملات (دينار/دولار) — معطّل افتراضياً.</summary>
public bool DualCurrency { get; set; }
```

Wire through `IFeatureFlagService` / `FeatureFlagService`, settings card, and `IsFeatureFlagVisible` so FX menu + currency pickers hide when off.

When **off**: behavior identical to today (implicit IQD, no pickers, existing `FormatCurrency`).

When **on**: Gold-like dual currency on main accounting documents.

### 3.2 Layered mirror of Gold

| Layer | Gold today | Main accounting (proposed) |
|-------|------------|----------------------------|
| Enum | `GoldCurrency` | Shared `AppCurrency` (or reuse enum moved to `Core/Enums`) — avoid two parallel enums long-term |
| FX entity | `GoldFxRate` | `FxRate` (or `AccountingFxRate`) in main DbContext |
| FX UI | `GoldFxRatesView` | `FxRatesView` gated by `DualCurrency` |
| Helper | `GoldCurrencyHelper` | `CurrencyHelper` / `AccountingCurrencyHelper` (same convert + dual totals) |
| Cash | `GoldCashBox.Currency` | Add `Currency` (+ default-per-currency) to `CashBox`; optionally `BankAccount.Currency` |
| Documents | Invoice pricing/payment FX | `Invoice` / vouchers / expenses / transfers: `Currency` or pricing+payment + `FxRate` + dual totals |
| Party balances | `CreditBalanceIqd/Usd` | Customer/Supplier dual balances (or balance table by currency) |
| Formatting | Gold often shows both | Replace `FormatCurrency` + `CurrencyFormatConverter` with currency-aware formatter; XAML binds through converter that respects flag + document currency |

### 3.3 UI gating rule

```
if (!flags.DualCurrency)
  → hide currency/FX controls; IQD-only paths; FormatCurrency as today
else
  → show pricing/payment (or single Currency) + FX on invoices/vouchers/expenses
  → filter cashboxes/banks by selected currency
  → statements/reports show dual columns or currency filter
```

Reuse Gold UX: combos bound to `Currencies` options, cash box reload on currency change, FX broadcast message for open screens.

### 3.4 Suggested rollout order

1. **Foundation:** flag + shared enum + `FxRate` entity/service + rates UI + `CurrencyHelper` + currency-aware `FormatCurrency`.
2. **Cash layer:** `CashBox.Currency` (and banks) + migration/seed default IQD (+ optional USD when flag first enabled).
3. **Documents:** vouchers → expenses → sales/purchase invoices → POS/installments (highest UI density last or behind same flag).
4. **Balances & reports:** opening balances, statements, cash/bank reports, then `ReportViewModelBase` consumers.
5. **Print/export:** Shared PDF/Excel builders last so printed docs match on-screen currency.

### 3.5 Do / don’t

- **Do** keep Gold Shop’s implementation as the reference; extract shared primitives rather than coupling main accounting to `Gold*` types.
- **Do** store FX on each document at save time (Gold’s `invoice.FxRate`) so historical reports stay correct.
- **Don’t** flip the flag on by default; IQD-only tenants must remain unchanged.
- **Don’t** leave `ReportViewModelBase.FormatCurrency` and `CurrencyFormatConverter` as IQD-only after the flag ships — they are the highest leverage choke points.

---

## Related docs

- `docs/GOLD_USER_GUIDE.md` — operator FX / dual cashbox flow  
- `docs/GOLD_PHASE2_UI_PATTERNS.md` — UI layout mirror checklist  
- `docs/LOYALTY_SYSTEM_PLAN.md` — feature-flag wiring pattern to copy  
