# AlMuhasib Customs Clearance — Production Readiness Audit

**Scope:** Financial integrity, permissions, cancel/edit side effects  
**Mode:** READ ONLY (no invented issues; findings verified in source)  
**Date:** 2026-10-23  
**Codebase:** `/workspace` (master + Customs services under `src/AlMuhasib.*`)

---

## Executive summary

Customs financial core (settlement from allocations, dual-currency balances, cancel-blocks-when-paid, soft-delete drafts only, currency match on allocate) is largely sound and better-structured than FIFO voucher notes in Accounting. Production blockers center on **allocation validation that does not aggregate per invoice**, **no DB transactions around receipt save + recompute** (partial commit if recompute throws), and **draft invoices included in profit/revenue aggregates**. Permissions on GetById are present; a few preview/report print paths are UI-gated only.

---

## Verified issues

### 1. Receipt over-allocation via multiple lines on the same invoice — **High**

| | |
|--|--|
| **Where** | `CustomsReceiptService.ValidateRequestAsync` |
| **Evidence** | Per-line check `line.Amount > available` without summing lines per `InvoiceId` (`CustomsReceiptService.cs` ~451–467) |
| **Scenario** | Invoice remaining = 100. Request amount = 150 with two allocation lines to the same invoice: 80 + 70. Each line ≤ 100 passes; sum 150 exceeds remaining. `SaveChanges` persists allocations; `RecomputeInvoiceSettlementAsync` then throws `paid > TotalAmount` (or leaves inconsistent state — see #2). |
| **UI note** | Form builds one row per invoice (`CustomsReceiptFormViewModel.ReloadOpenInvoicesAsync`), so desktop UI is unlikely to send duplicates; **service API does not enforce**. |
| **Proposed fix** | Group `allocations` by `InvoiceId`, compare `Sum(Amount)` to `RemainingAmount + prior` once per invoice; reject duplicates or merge. |

### 2. Receipt create/update: save allocations then recompute outside a transaction — **High**

| | |
|--|--|
| **Where** | `CustomsReceiptService.CreateAsync` / `UpdateAsync` / `CancelAsync`; no `BeginTransaction` anywhere under `**/Customs/**/*.cs` |
| **Evidence** | `CreateAsync`: first `SaveChangesAsync` (receipt + allocations), then `RecomputeInvoiceSettlementAsync` (throws if over-allocated **before** assigning Paid), then second `SaveChangesAsync` (`CustomsReceiptService.cs` ~214–241). `RecomputeInvoiceSettlementAsync` throws prior to mutating Paid when `paid > TotalAmount` (`CustomsSettlementHelper.cs` ~35–44). |
| **Scenario** | Over-alloc (issue #1) or concurrent double-allocate: receipt rows committed; invoice `PaidAmount`/`RemainingAmount` unchanged; Cancel invoice only checks `PaidAmount > 0` and may succeed while active allocations exist. |
| **Compare** | Accounting `CashBankService.DeleteVoucherAsync` / `InvoiceService.DeleteInvoiceAsync` wrap reverse + soft-delete in `BeginTransactionAsync`. |
| **Proposed fix** | Single transaction for validate → write allocations → recompute invoice + customer balance → audit → commit. On failure, roll back. Optionally re-read Remaining with row version / serializable isolation. |

### 3. Invoice cancel checks `PaidAmount` only, not active allocations — **Medium** (defense-in-depth; amplifies #1–#2)

| | |
|--|--|
| **Where** | `CustomsInvoiceService.CancelAsync` |
| **Evidence** | `if (invoice.PaidAmount > 0) throw` only (`CustomsInvoiceService.cs` ~363–364). Architecture (`CUSTOMS_ARCHITECTURE.md` §4.2) documents this decision. |
| **Scenario** | If allocations exist but `PaidAmount` still 0 (failed recompute after commit), cancel succeeds; orphan allocations remain. |
| **Proposed fix** | Also reject when `CustomsReceiptAllocations.Any(a => a.InvoiceId == id && !a.IsDeleted)`; or always `Recompute` inside a transaction before cancel decision. |

### 4. Profit / revenue reports and dashboard profit include **Draft** invoices — **Medium**

| | |
|--|--|
| **Where** | `CustomsReportService.GetProfitReportAsync`, `GetMonthlyReportAsync`, `GetDailyReportAsync`, `GetCustomerReportAsync`; `CustomsDashboardService.GetDashboardStatsAsync` period profit |
| **Evidence** | Filters use `Status != Cancelled` (includes `Draft`), e.g. profit at `CustomsReportService.cs` ~595–597; dashboard period invoices ~40–55 + profit loop ~109–121. Debts correctly use `Approved` only (~57–60, debts report ~512–515). |
| **Scenario** | Unapproved drafts inflate revenue, expenses rollup, and InternalProfit KPIs. |
| **Proposed fix** | Restrict profit/revenue aggregates to `Status == Approved` (keep draft visibility only in invoice lists / optional filters). |

### 5. Soft-delete draft invoice does not cascade attachments (DB + disk orphans) — **Medium**

| | |
|--|--|
| **Where** | `CustomsInvoiceService.SoftDeleteAsync`; `CustomsAttachmentService` |
| **Evidence** | Soft-deletes vehicles + expenses + invoice only (`CustomsInvoiceService.cs` ~425–430). Attachments stay active with `OwnerType=Invoice`, `OwnerId=id`; files under `AppDomain.../Attachments/Customs/...` untouched. `GetByOwnerAsync` still returns them. |
| **Scenario** | Delete draft with attachments → hidden invoice, lingering attachment rows + files. |
| **Proposed fix** | On invoice soft-delete, soft-delete owner attachments and delete files (same as `CustomsAttachmentService.SoftDeleteAsync`), or block soft-delete while attachments exist. |

### 6. Attachment add does not verify owner exists — **Low**

| | |
|--|--|
| **Where** | `CustomsAttachmentService.AddFromFileAsync` |
| **Evidence** | Copies file then inserts row; no check that customer/invoice/receipt id exists (~46–78). |
| **Scenario** | Bad `ownerId` → orphan file + metadata. |
| **Proposed fix** | Validate owner row exists (and not deleted) before copy/insert. |

### 7. Report print/export often UI-gated only (bypasses `CustomsPrintService` Ensure*) — **Medium**

| | |
|--|--|
| **Where** | `CustomsReportViewModelBase.PrintTable` / `ExportPdfDocument`; `CustomsProfitReportViewModel`, daily/customer/debts/invoice reports |
| **Evidence** | `ListTableExportHelper.Print` → `IExportService.PrintTable` with UI `CanPrint` only. `PrintStaffReport` → `EnsurePrint(Reports)` is used for monthly/vehicles only. Profit loads `ProfitReport` permissions but never calls `EnsurePrint(ProfitReport)` / `EnsureExport(ProfitReport)` at service layer. |
| **Scenario** | Defense-in-depth gap if ExportService is invoked without UI flags (tests, future API). Data load still requires View. |
| **Proposed fix** | Route all staff print/export through `CustomsPrintService` with screen-specific EnsurePrint/Export (Reports vs ProfitReport). |

### 8. `PreviewNextInvoiceNumberAsync` / `PreviewNextReceiptNumberAsync` have no permission check — **Low**

| | |
|--|--|
| **Where** | `CustomsInvoiceService.PreviewNextInvoiceNumberAsync`; `CustomsReceiptService.PreviewNextReceiptNumberAsync` |
| **Evidence** | No `_permissions.Ensure*` (~127–130 invoice; ~120–123 receipt). |
| **Scenario** | Caller without View can learn next document number. |
| **Proposed fix** | `EnsureView` on Invoices / Receipts. |

### 9. Invoice soft-delete / customer CRUD lack semantic `CustomsAuditWriter` entity names — **Low**

| | |
|--|--|
| **Where** | `SoftDeleteAsync` invoice; customer create/update/delete; attachment add/delete |
| **Evidence** | Semantic writer used for invoice Create/Update/Approve/Cancel and receipt Create/Update/Cancel; permissions via `UserPermissions`. Soft-delete invoice relies on `CustomsDbContext.SaveChangesAsync` auto-audit (`EntityName = CLR type`, Delete when `IsDeleted` flips). |
| **Scenario** | Harder to filter “CustomsInvoice.SoftDelete” vs property diffs; trail still exists if `UserId` set. |
| **Proposed fix** | Add semantic logs for SoftDelete / attachment ops (optional). |

### 10. Discount can exceed vehicles sum (silent clamp to total 0) — **Low**

| | |
|--|--|
| **Where** | `CustomsInvoiceService.CreateAsync` / `UpdateAsync` |
| **Evidence** | `total = Math.Max(0, vehiclesSum - request.DiscountAmount)` (~151, ~236). Matches architecture §4.1. |
| **Scenario** | Discount 2_000_000 on vehicles 1_000_000 → Total/Remaining 0 without error. |
| **Proposed fix** | Reject `DiscountAmount > vehiclesSum` if business requires explicit validation. |

### 11. Customer list / file “open unpaid” counts include Draft remainings — **Low**

| | |
|--|--|
| **Where** | `CustomsCustomerService.GetPagedAsync` OpenInvoiceCount; `GetCustomerFileAsync` UnpaidInvoiceCount |
| **Evidence** | Non-cancelled invoices with `RemainingAmount > 0` (~65–71, ~215–241). Balance fields correctly use approved remainings via settlement. |
| **Scenario** | Drafts look like open debts in customer KPIs while `BalanceIqd/Usd` exclude them. |
| **Proposed fix** | Count only `Approved && RemainingAmount > 0` for unpaid/open. |

---

## Verified CORRECT behaviors

1. **Invoice totals:** `TotalAmount = Max(0, Σ VehiclePrice − Discount)`; create sets `PaidAmount=0`, `Remaining=ComputeRemaining(total,0)` (`CreateAsync`).
2. **Update + settlement:** After vehicle/expense upsert, `RecomputeInvoiceSettlementAsync`; approved customer (and previous customer) balances recomputed (`UpdateAsync`).
3. **Cannot lower approved total below PaidAmount** (`UpdateAsync` ~238–239).
4. **Cannot change currency when paid/allocated** (`UpdateAsync` ~228–231).
5. **Vehicle currency forced to invoice currency** (`ApplyVehicleFields`) — mix prevented without relying on callers.
6. **Approve:** Draft-only; requires customer, number, ≥1 vehicle; then `RecomputeCustomerBalanceAsync` (`ApproveAsync`).
7. **Cancel with payments blocked** when `PaidAmount > 0`; status → Cancelled (row retained); balance recomputed (`CancelAsync`). Tested in `Cannot_cancel_invoice_with_payments`.
8. **Soft-delete drafts only;** approved/cancelled must Cancel; blocks if `PaidAmount > 0` (`SoftDeleteAsync`).
9. **Receipt allocate:** sum must equal amount; approved only; customer match; `EnsureReceiptMatchesInvoiceCurrency`; remaining check (per line) (`ValidateRequestAsync`).
10. **Settlement helper:** Paid = Σ non-deleted allocations; Remaining = `ComputeRemaining`; customer `BalanceIqd`/`BalanceUsd` = Σ approved remainings by currency; uses **tracked** invoices to avoid stale AsNoTracking (`CustomsSettlementHelper`).
11. **Receipt cancel:** soft-deletes receipt + allocations, recomputes Paid/Remaining/balance; semantic `CustomsReceipt.Cancel` (`CancelAsync`). Tested reverse in `Allocate_receipt_updates_remaining_and_cancel_reverses`.
12. **CustomsFinancialRules:** FX normalize; same-currency ensures; InternalProfit = revenue − same-currency expenses only; chart mix assert (`CustomsFinancialRules.cs`). Cross-currency expense on IQD invoice does not reduce IQD profit (tests).
13. **Expenses permission:** `GetById` strips expenses without `CanViewExpenses`; create with expenses forces Expenses View; update without permission leaves expenses untouched (`CustomsInvoiceService`).
14. **Expense currency may differ from invoice** — intentional; profit excludes other currency (`ComputeInternalProfit`).
15. **GetById / GetPaged** invoices & receipts & customers: **`EnsureView` present** (no GetById-without-view gap on those services).
16. **Mutations:** Add/Edit/Delete/Approve(Edit)/Cancel(Edit invoice, Delete receipt) guarded; Print/Export on `CustomsPrintService` methods that go through it.
17. **Dashboard debts / debts report:** approved outstanding only; dual IQD/USD; profit gated by `CanViewProfit && CanViewExpenses`.
18. **Reports charts:** dual KPI columns; chart series single-currency (IQD or filter) — no IQD+USD sum (`GetMonthlyReportAsync` DailyChart, debts AgingChart, profit MonthlyChart, customer TopCustomersChart).
19. **Attachments path** under `AppDomain.CurrentDomain.BaseDirectory/Attachments/Customs/{OwnerType}/{ownerId}` (`GetOwnerFolder`); soft-delete metadata + best-effort `File.Delete`.
20. **Audit:** `CustomsDbContext.SaveChangesAsync` auto-audits BaseEntity Add/Edit/soft-Delete; skips `AuditLog`/`Permission`; semantic writer for invoice lifecycle, receipts, `UserPermissions`.
21. **Statement:** approved invoices debit + receipts credit; dual running balances; soft-deleted receipts excluded by query filter.
22. **vs Accounting cancel/receipt reverse:** Customs refuses invoice cancel while paid (must cancel receipts first) vs Accounting soft-delete invoice reversing linked vouchers/cash in one transaction. Customs receipt cancel recomputes from remaining allocation rows (explicit FKs) — clearer than Accounting FIFO note markers / LIFO deallocate. Gap: Customs lacks Accounting’s transactional wrapper (#2).

---

## Permission matrix (service layer)

| Service / method | Guard |
|------------------|--------|
| Invoice GetPaged/GetById | EnsureView(Invoices); expenses stripped if !CanViewExpenses |
| Invoice PreviewNext* | **none** (#8) |
| Invoice Create | EnsureAdd; expenses need CanViewExpenses or EnsureView(Expenses) |
| Invoice Update/Approve/Cancel | EnsureEdit |
| Invoice SoftDelete | EnsureDelete |
| Receipt Get*/GetOpen | EnsureView |
| Receipt PreviewNext* | **none** (#8) |
| Receipt Create/Update | EnsureAdd / EnsureEdit |
| Receipt Cancel | EnsureDelete |
| Customer CRUD/file | View/Add/Edit/Delete as expected |
| Attachment Get/Add/SoftDelete | View/Edit/Delete on ScreenForOwner |
| ExpenseType Get/Create | View(Expenses); Create also EnsureEdit(Invoices) |
| Statement | EnsureView(Statement) |
| Dashboard | EnsureView(Dashboard) |
| Reports (most) | EnsureView(Reports); Profit EnsureView(ProfitReport) |
| PrintService Print/Export* | EnsurePrint / EnsureExport / EnsurePrintOrExport |

---

## Audit log coverage

| Op | Auto SaveChanges | Semantic CustomsAuditWriter |
|----|------------------|------------------------------|
| Invoice create/update | Yes | `CustomsInvoice` Add/Edit |
| Invoice approve | Yes (status) | `CustomsInvoice.Approve` |
| Invoice cancel | Yes | `CustomsInvoice.Cancel` |
| Invoice soft-delete draft | Yes (Delete) | **No** dedicated name |
| Receipt create/update/cancel | Yes | `CustomsReceipt` / `.Cancel` |
| Customer / expense type / attachment | Yes (if UserId) | **No** (except N/A) |
| SaveUserPermissions | Skipped for Permission entity | `UserPermissions` |

`CustomsAuditWriter` no-ops if `CurrentUserService.UserId` is null.

---

## Needs Verification

- Concurrent multi-user receipt allocate under default SQL isolation (whether #2 manifests in production load).
- Whether any non-UI caller posts duplicate `InvoiceId` allocation lines (#1).
- UI warning when expense currency ≠ invoice currency (logic excludes from profit by design).
- Architecture note that other-currency expenses are “converted for display” — profit report shows same-currency expenses only; confirm product intent.
- Orphan files when `File.Delete` fails after attachment soft-delete (intentionally swallowed).
- Invoice/receipt number races under concurrent Create (helpers use max Id + IgnoreQueryFilters; unique filtered indexes may reject duplicates).

---

## Suggested fix priority

1. Aggregate per-invoice allocation validation (#1)  
2. Transactions around receipt/invoice settlement writes (#2)  
3. Cancel + soft-delete defenses (#3, #5)  
4. Approved-only profit/revenue (#4)  
5. Print/export Ensure* consistency (#7)  
6. Low items (#6, #8–#11)
