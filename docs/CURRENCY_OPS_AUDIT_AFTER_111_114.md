# Multi-currency ops audit — AFTER PRs #111–#114

**Scope:** Integrity of `Currency` + `FxRate` through create/update/delete in services/ops (not UI polish).  
**Base:** `master` after #111 (audit), #112/#113 (integrity fixes), #114 (disclosure).  
**Question set:** Does CUD preserve Currency+FxRate? Can IQD/USD mix silently? FIFO same-currency? Delete reverses correctly?

---

## Executive verdict

The four CRITICAL integrity bugs from #111 are **fixed**. Create paths for invoices, vouchers, expenses, transfers, opening balances, and sync now enforce same-currency + valid FxRate. FIFO allocate/deallocate filters by currency. Delete of FIFO vouchers and invoice-linked independent vouchers reverse cash (and AR for FIFO).

**Still broken / incomplete** items are mostly: (1) ProductCost/COGS silently ignores USD purchases, (2) Installment list/totals hard-filter IQD and hide USD, (3) FIFO reverse without per-invoice allocation trail, (4) sync upsert can still rewrite invoice/expense/transfer currency, (5) customer-balance validation remains tautological, (6) secondary reports still IQD-primary by design.

No remaining `?? 1m` FX fallback in Infrastructure services. Entity defaults `Currency=IQD` / `FxRate=1` are safe for legacy IQD only; USD create paths call `RequireFxRateOrThrow`.

---

## FIXED (were broken in #111; closed by #112–#114)

| # | Area | File:method | What was wrong | What fixed | Severity was |
|---|------|-------------|----------------|------------|--------------|
| F1 | Sync outbound | `SyncMapper.MapInvoice` / `MapExpense` / `MapTransfer`(+Safe) | Dropped Currency/FxRate → cloud IQD | Maps `d.Currency` / `d.FxRate` | CRITICAL |
| F2 | Sync inbound | `SyncMapper` upsert expense/voucher/transfer; Cloud `SyncEngine` | Wrote defaults | RequireFxRate + cashbox/bank currency match | CRITICAL |
| F3 | FIFO delete | `CashBankService.DeleteVoucherAsync` → `ReverseFifoCustomerApplicationAsync` / `ReverseFifoSupplierApplicationAsync` | Soft-delete cash only; AR stayed paid | Deallocate same-currency LIFO reverse + Unmark `[CR-APPLIED]` | CRITICAL |
| F4 | Invoice delete | `InvoiceService.DeleteInvoiceAsync` → `ReverseLinkedVoucherCashAsync` | Soft-deleted independent vouchers without cash reverse | Reverses cash for non–report-only linked vouchers; installment direct pays reversed | CRITICAL |
| F5 | Investor profit | `InvestorService.GetDistributableProfitsAsync` | `Expenses.Sum` no currency | `Where(e => e.Currency == IQD)` | CRITICAL |
| F6 | Investor vouchers | `CashBankService.CreateVoucherAsync` InvestorDeposit/Withdrawal | Could take USD cashbox | Rejects non-IQD | HIGH |
| F7 | Cloud EF | `CloudDbContext` Currency props | No `HasConversion<string>()` | Conversion + default IQD on cash/bank/invoice/voucher/expense/transfer | HIGH |
| F8 | Installment oldest-first | `InstallmentService.PayCustomerAmountOldestFirstAsync` | Mixed currencies | Filters installments by cashbox currency | HIGH |
| F9 | Opening balances | `OpeningPartyBalanceService.CreateCustomer/SupplierCoreAsync` | Always IQD/Fx=1 | `request.Currency` + `RequireFxRateOrThrow` | HIGH |
| F10 | Opening installments | `InstallmentService` opening plan create | IQD only | Currency/FxRate on request | HIGH |
| F11 | Credit limit | `CustomerCreditService.DebtToIqd` | `fxRate<=0 → 0` silent | `ToBaseIqdStrict` throws | HIGH |
| F12 | Edit FxRate | `SalesInvoiceViewModel` / `PurchaseInvoiceViewModel` `.Currency.cs` `ApplyCurrencyFromDocument` | Load refreshed Fx to today | `_suppressFxRateRefresh` keeps stored rate | HIGH |
| F13 | Cash adjust | `CashBankService.AdjustCashBoxBalanceAsync` | Voucher default IQD | Copies cashbox Currency + Fx | HIGH |
| F14 | Transfer create | `CashBankService.CreateTransferAsync` | FxRate always 1; no currency check | Same-currency required; USD gets rate | HIGH→fixed |
| F15 | Party totals | `PartyQuickDetailService` | `Sum(NetAmount)` mixed | Separate IQD / USD totals | HIGH |
| F16 | Statement / aging / sales / profit disclosure | Report + quick statement + validation message | USD hidden | Dual totals / columns (#114) | HIGH |
| F17 | Cashbox validation rebuild | `AccountingValidationService.ValidateCashBoxBalanceAsync` | Incomplete + no currency | Currency-scoped; includes expenses/transfers/returns/installments (non-voucher) | HIGH |
| F18 | Returns apply | `InvoiceService.ApplyReturnCreditImpactAsync` | — | Filters `i.Currency == returnInvoice.Currency`; marks allocations | (was OK / hardened) |
| F19 | Returns reverse | `InvoiceService.ReverseReturnCreditImpactAsync` | — | Uses `[RET-APPLIED:id=amt]` trail | OK |
| F20 | Balance helper | `CustomerBalanceHelper.Allocate/Deallocate/ComputeOutstandingBalances` | — | Same-currency FIFO + dual balance | OK |
| F21 | Voucher/Expense create | `CashBankService.CreateVoucherAsync`, `ExpenseService` create | — | EnsureSameCurrency + RequireFxRate | OK |
| F22 | Invoice create | `InvoiceService.CreateInvoiceAsync` | — | RequireFxRate + cashbox match | OK |
| F23 | Invoice update | `InvoiceService` update-via-delete+recreate | — | Re-create enforces rules; credit preserve requires same Currency | OK |
| F24 | Gold `?? 1m` | Gold helpers (#113) | Silent Fx=1 | `RequirePositiveFxRate` | MEDIUM |

---

## STILL BROKEN / GAPS

| # | Severity | File:method | Issue | Accounting impact | Mix IQD+USD? |
|---|----------|-------------|-------|-------------------|--------------|
| B1 | **HIGH** | `ProductCostHelper.GetPurchaseItemsByProductAsync` (+ `CloudProductCostHelper`) | Purchase cost query **hard-filters `Currency == IQD`**. USD purchases increase stock qty but are omitted from average unit cost / inventory value / COGS inputs. | Stock bought in USD can carry understated (or opening-only) IQD unit cost → **understated COGS / overstated IQD profit** when sold in IQD. Not a unit mix in one Sum, but silent omission. | No direct add of $ into د.ع; **silent wrong cost basis** |
| B2 | **HIGH** | `InstallmentService.BuildInstallmentsQuery` → `GetInstallmentTotalsAsync` / list queries | Always `Invoice.Currency == IQD`. Main Installments UI totals/lists **hide all USD plans**. | Ops see incomplete debt; USD installment AR invisible in primary screen (aging report was disclosed in #114, list was not). | No mix; **silent exclusion** |
| B3 | **MEDIUM** | `CustomerBalanceHelper.DeallocateFromCreditInvoices` used by `CashBankService.ReverseFifo*` | Delete reverses by **LIFO on PaidAmount**, without per-invoice allocation ids (unlike returns). Deleting a non-latest FIFO voucher can reassign which invoice looks paid. | **Customer total AR usually OK**; **invoice-level Paid/Remaining can be wrong** after selective FIFO deletes. | Same-currency only |
| B4 | **MEDIUM** | `CashBankService.ApplyDebtReceiptToCreditInvoicesAsync` / supplier twin | Always marks `[CR-APPLIED]` even if voucher amount **exceeds** open same-currency invoices. Excess disappears from unapplied-receipt side of `CustomerBalanceHelper`. | Overpayment via FIFO debt receipt can **vanish from outstanding formula** (neither AR nor advance). Currency OK. | No |
| B5 | **MEDIUM** | `SyncMapper.UpsertInvoicesAsync` (also expense/transfer upsert) | Unlike cashbox/bank, **no guard** against changing `Currency` on existing entity. | Corrupt/stale sync DTO could rewrite USD→IQD (or reverse) on update. Outbound map is fixed; inbound still permissive. | Possible via sync |
| B6 | **MEDIUM** | `AccountingValidationService.ValidateCustomerBalanceAsync` / supplier twin | Expected vs actual built from the **same** Sum components → always “valid”. Does not compare to a stored party balance or ledger. | **False confidence**; will not catch AR corruption. Dual IQD/USD disclosure exists but check is tautological. | No |
| B7 | **MEDIUM** | `ReportService` installment detail reports (~lines 457–639) + `ReportService.NewReports` | Many installment/collection reports still **IQD-only** (by design / #114 out of scope). | USD activity missing from those screens (not mixed into IQD totals). | No mix; exclusion |
| B8 | **MEDIUM** | `InvoiceService.DeleteInvoiceAsync` cash reverse on `invoice.CashBoxId` | Adjusts cashbox balance **without** re-checking `invoice.Currency == cashBox.Currency` (create enforced it). | Corrupt historical mismatch could move $ amount on IQD box (or reverse). | Only if data already corrupt |
| B9 | **LOW** | `CashBankService` — **no `DeleteTransferAsync`** | Transfers are create-only; no service delete/reverse. | Cannot reverse a transfer in-app; not a currency mix bug. Manual DB soft-delete would desync balances. | N/A |
| B10 | **LOW** | `ExpenseService` — no amount/currency Update | Create/Delete only; delete restores cashbox. | Cannot silently change currency post-create via service. | N/A |
| B11 | **LOW** | `InstallmentService.PayInstallmentAsync` audit text | Hardcodes `د.ع` in audit string even for USD pays. | Display/audit only; cash path currency-checked. | No |
| B12 | **LOW** | Entity defaults `FxRate = 1m` on Invoice/Voucher/Expense/Transfer | Safe for IQD; USD must hit RequireFxRate on write paths. | Bypass of service layer (raw EF) could store USD+FxRate=1. | Risk if API/raw write |
| B13 | **INFO** | `InvestorService` / capital | IQD-only by policy (blocked on USD cashbox). | USD profit not distributable — intentional. | Blocked |

---

## Per-component CUD matrix

| Component | Create preserves C+Fx? | Update preserves C+Fx? | Delete reverses correctly? | Silent IQD/USD mix? | FIFO same-currency? |
|-----------|------------------------|------------------------|----------------------------|---------------------|---------------------|
| **InvoiceService** | Yes (`RequireFxRate` + cashbox match) | Via delete+recreate; same rules; credit preserve needs same Currency; UI keeps historical Fx | Cash for invoice box + independent vouchers; return allocations reversed; installment direct pays reversed. Report-only DP vouchers soft-deleted without double cash reverse | Blocked on create | N/A (returns FIFO filtered by currency) |
| **CashBankService vouchers** | Yes | No update API | FIFO + invoice-linked + installment reverse; cash reverse | Blocked | **Yes** allocate; reverse LIFO approx (**B3**) |
| **CashBankService transfer** | Yes; rejects cross-currency | No update | **No delete API (B9)** | Blocked on create | N/A |
| **CashBankService adjust box** | Yes (from box) | N/A | Via voucher delete path | Blocked | N/A |
| **InstallmentService** | Plan inherits invoice currency; pay checks cashbox vs invoice | Pay updates amounts only | Invoice delete handles; no standalone plan-currency rewrite | Pay blocked on mismatch; **list hides USD (B2)** | Oldest-first same cashbox currency |
| **InvestorService** | IQD-only enforced | N/A | Via voucher delete | Expenses filtered IQD | N/A |
| **ExpenseService** | Yes | No update | Restores cashbox by expense.Currency | Blocked on create; report/get totals IQD-filtered | N/A |
| **OpeningPartyBalance** | Yes | N/A | Soft-delete if `PaidAmount==0`; no cash | Blocked | N/A |
| **CustomerCredit** | Uses stored invoice Fx via `ToBaseIqdStrict` | N/A | N/A | No silent zero | N/A |
| **CustomerBalanceHelper** | Dual / filtered | Deallocate same currency | Used by voucher delete | No | FIFO yes |
| **SyncMapper** | Outbound OK | Inbound can overwrite Currency (**B5**) | N/A | Risk on bad inbound | N/A |
| **AccountingValidation** | Cashbox rebuild currency-scoped | Customer check tautological (**B6**) | N/A | Cashbox OK | N/A |
| **ProductCost / COGS** | IQD purchases only (**B1**) | N/A | N/A | Omission, not Sum-mix | N/A |
| **Returns** | Same currency as return doc | Allocations recorded | Reverse by marks | Blocked | Same-currency FIFO |

---

## Dangerous-pattern scan (current tree)

| Pattern | Result after #112–#114 |
|---------|-------------------------|
| `Sum(...)` without Currency filter | Still present in places that **pre-filter IQD** (installment list, secondary reports, expense IQD queries). Primary sales/purchases/profit/dashboard disclose USD separately. **ProductCost** filters IQD only → omission (**B1**). |
| `Amount +=` / `PaidAmount +=` without currency | Present in apply helpers **after** `EnsureSameCurrency` or currency-filtered queries — OK. |
| `FxRate = 1` silent for USD | Service writes use `RequireFxRateOrThrow` (forces 1 only for IQD). UI sets `FxRate=1m` when selecting IQD — OK. |
| `?? 1m` for FX | **None** left under `src/` Infrastructure/Core helpers (Gold fixed in #113). |
| Hardcoded IQD assumptions | Intentional IQD-only: Investor, `InvoiceFilters` default, Installment list (**B2**), ProductCost (**B1**), several secondary reports (**B7**). |

---

## Scenario answers (post-fix)

| Scenario | Verdict |
|----------|---------|
| Legacy IQD-only | Safe; migration defaults unchanged |
| IQD + USD mixed ops | Create/pay paths refuse cross-currency; AR/cash helpers split. Gaps: installment UI hide USD, COGS ignore USD buys, FIFO invoice-level reverse |
| USD create + sync round-trip | Outbound+inbound map Currency/FxRate — **fixed** vs #111 |
| FIFO receipt then delete | Cash + same-currency AR reverse — **fixed**; invoice assignment may shuffle (**B3**) |
| Credit invoice paid by independent voucher then invoice delete | Independent voucher cash reversed — **fixed** |
| Change daily FX then edit old USD invoice | Historical Fx preserved on load — **fixed** |
| Investor distribution with USD expense | USD expense excluded — **fixed** |

---

## Suggested next fix priority

1. **B1** — ProductCost/COGS: either maintain separate USD cost layers or convert USD purchases with **document FxRate** into IQD cost basis explicitly (never raw Sum mix).  
2. **B2** — Installment queries: dual-currency list/totals or currency parameter (stop hard IQD filter).  
3. **B3/B4** — Store FIFO allocation marks on vouchers (like returns) and reverse exactly; don’t mark applied when leftover unallocated.  
4. **B5** — Reject currency change on existing invoice/expense/transfer in SyncMapper upsert (mirror cashbox).  
5. **B6** — Customer validation: compare helper balance to statement ledger / running balance, not to itself.

---

*Generated by ops re-audit after PRs #111–#114. Analysis only — no runtime logic changes in this document PR.*
