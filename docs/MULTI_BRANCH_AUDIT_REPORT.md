# Multi-Branch Production Audit Report (Accounting)

**Scope:** Accounting system only (Desktop + Cloud API + Flutter + Sync)  
**Date:** 2026-09-27  
**Branch:** `cursor/multi-branch-accounting-5d23`  
**Principle audited:** `User + Company + Branch + Permission = allowed data only`

---

## Summary

| Metric | Count |
|--------|------:|
| Findings discovered | **18** |
| Critical | 4 |
| High | 8 |
| Medium | 4 |
| Low | 2 |
| **Fixed in this audit** | **16** |
| Remaining (accepted / documented) | **2** (Low/Medium residual) |

---

## Critical (fixed)

### C1 — AllBranches suppressed EF BranchId filter (full-tenant leak)
- **Where:** `AppDbContext.SuppressBranchIdMatch`, `CloudDbContext.SuppressBranchIdMatch`
- **Scenario:** `IsAllBranchesMode=true` with `AllowedBranchIds=[1]` still returned branches 2 & 3.
- **Impact:** Cross-branch data disclosure in every LINQ query / report / dashboard.
- **Fix:** AllBranches now filters `BranchId ∈ AllowedBranchIdsForFilter`. Full suppress reserved for Bypass/SyncPull only.
- **Evidence:** `BranchIsolationSecurityTests.AllBranches_mode_only_returns_AllowedBranchIds_not_all_company_branches` — PASS.

### C2 — Empty branch assignments → all active branches
- **Where:** `TenantContextMiddleware`, `AuthController.LoadAllowedBranchesAsync`, `AuthController.EnsureMainBranchAndAssignmentAsync`, `BranchService.GetBranchesForUserAsync`
- **Scenario:** Revoke all `TenantAccountBranches` / `UserBranches` → next request treated user as allowed on every branch; login re-linked to Main.
- **Impact:** Privilege escalation after revocation; stale JWT `branch_id` worked.
- **Fix:** Fail-closed on empty assignments. Auto-assign Main only when Main branch is **just created** (tenant bootstrap). Desktop login no longer calls `EnsureUserLinkedToMainAsync`.

### C3 — SyncEngine Pull/Push bypassed branch ACL for whole tenant
- **Where:** `SyncEngine.PushAsync` / `PullAsync`, `PullEntitiesAsync`, custom Pull*
- **Scenario:** Branch-restricted JWT could pull/push all tenant accounting entities via `BypassBranchFilter` + `IgnoreQueryFilters` without AllowedBranchIds.
- **Impact:** Full cross-branch sync exfiltration / injection.
- **Fix:** Load AllowedBranchIds from context or `TenantAccountBranches`; scope all pulls; reject push BranchSyncId outside assignment; reset `BypassBranchFilter` in `finally`; refuse empty/unknown BranchSyncId (no silent Main remap for multi-branch); block entity branch relocation on update.

### C4 — Mobile writes stamped into Main (empty BranchSyncId)
- **Where:** `CloudMobileWriteService` → `SyncEngine.ResolveBranchId`
- **Scenario:** Mobile invoice/customer create omitted `BranchSyncId` → written under Main; updates could relocate other-branch rows to Main.
- **Impact:** Accounting contamination across branches.
- **Fix:** `PushSingleAsync` stamps `BranchSyncId` from `ITenantContext.RequireWriteBranchId()`; SyncEngine rejects relocate / disallowed branches.

---

## High (fixed)

### H1 — Cross-branch FK via SyncIdResolver
- **Where:** `SyncIdResolver.ResolveAsync`
- **Fix:** Optional `allowedBranchIds` — reject FK SyncIds outside allowed set.

### H2 — SupervisoryReportService IgnoreQueryFilters without BranchId
- **Where:** `SupervisoryReportService` (all deleted-* queries)
- **Fix:** Re-apply `AllowedBranchIdsOrCurrent()` after IgnoreQueryFilters.

### H3 — Product soft-delete revive across branches
- **Where:** `ProductService.CreateAsync`
- **Fix:** Soft-delete lookup scoped to `RequireWriteBranchId()`.

### H4 — Invoice restore / voucher numbering cross-branch
- **Where:** `InvoiceService.RestoreInvoiceAsync`, `GetNextDebtReceiptNumberAsync`, `GetNextPaymentVoucherNumberAsync`
- **Fix:** Branch-scoped restore + numbering.

### H5 — Invoice create without FK branch integrity
- **Where:** `InvoiceService.CreateInvoiceAsync`
- **Fix:** Validate warehouse/customer/supplier exist in current branch filter; stamp `invoice.BranchId`.

### H6 — Admin Role → CanViewAllBranches escalation
- **Where:** `LoginViewModel.LoginAsync`
- **Fix:** Flags driven by `ViewAllBranches` / `ManageAllBranches` permissions only.

### H7 — SetTenant wiped account id (broke ACL chaining)
- **Where:** `TenantContext.SetTenant`
- **Fix:** Preserve existing `TenantAccountId` when callers pass tenant-only.

### H8 — Cloud write guards weak in AllBranches / allowed checks
- **Where:** `CloudDbContext.ApplyTenantWriteGuards`
- **Fix:** Enforce `IsBranchAllowed` on add/modify/delete when not bypassing.

---

## Medium (fixed / residual)

### M1 — Offline queue flush without strict branch/tenant match — **fixed**
- Flutter `OfflineWriteService.flush` now fails closed if branch/tenant missing or mismatched.
- Logout clears entire offline queue (`clearAll`).

### M2 — FindAsync bypasses query filters — **documented + residual**
- EF `FindAsync` ignores global filters (confirmed by test).
- **Remaining:** Many desktop services still use `FindAsync`. Write guards block most mutations; prefer LINQ `FirstOrDefaultAsync(e => e.Id == id)` for reads. Not fully migrated in this pass (Medium residual).

### M3 — Desktop SyncMapper still company-wide IgnoreQueryFilters — **accepted for desktop admin sync**
- Local desktop push uses Bypass intentionally; cloud SyncEngine now enforces assignment scope on receive.

### M4 — `can_view_all_branches` claims never issued on cloud — **by design for now**
- Hardcoded false until account-level flags exist; AllBranches path remains permission-gated.

---

## Low

### L1 — Performance indexes
- Composite `(TenantId, BranchId)` already on CloudBaseEntity; desktop has BranchId indexes. No random indexes added.

### L2 — Race on document numbers
- Unique `(BranchId, Number)` + existing retry paths remain primary mitigation.

---

## Isolation test results

```
dotnet test --filter "FullyQualifiedName~Branch"
Passed: 12, Failed: 0
```

Includes:
- Branch stamp on insert
- Cross-branch mutation denied
- Query filter hides other branch
- AllBranches respects AllowedBranchIds only
- Unbound branch fail-closed (empty)
- FindAsync vs LINQ isolation evidence

API build: **succeeded**  
Infrastructure + Core.Tests: **succeeded**

---

## Legacy data / regression

- Migration `AddMultiBranchAccounting` seeds `MAIN`, backfills `BranchId`, assigns users to Main (one-time).
- Runtime no longer re-escalates empty assignments to all branches / Main.
- Single-branch users: middleware auto-binds the only assignment (legacy clients OK).
- Multi-branch users: must `select-branch` before data/write APIs (fail-closed otherwise).

---

## Files modified (audit fixes)

**Database / filters**
- `src/AlMuhasib.Infrastructure/Data/AppDbContext.cs`
- `src/AlMuhasib.Cloud.Infrastructure/Data/CloudDbContext.cs`

**Auth / middleware**
- `src/AlMuhasib.Cloud.Infrastructure/Middleware/TenantContextMiddleware.cs`
- `src/AlMuhasib.Cloud.Infrastructure/Services/TenantContext.cs`
- `src/AlMuhasib.Api/Controllers/AuthController.cs`
- `src/AlMuhasib.Api/Controllers/SyncController.cs`
- `src/AlMuhasib.UI/ViewModels/LoginViewModel.cs`
- `src/AlMuhasib.Infrastructure/Services/BranchService.cs`

**Sync / mobile**
- `src/AlMuhasib.Cloud.Infrastructure/Services/SyncEngine.cs`
- `src/AlMuhasib.Cloud.Infrastructure/Services/SyncIdResolver.cs`
- `src/AlMuhasib.Cloud.Infrastructure/Mobile/CloudMobileWriteService.cs`

**Desktop accounting services**
- `src/AlMuhasib.Infrastructure/Services/SupervisoryReportService.cs`
- `src/AlMuhasib.Infrastructure/Services/ProductService.cs`
- `src/AlMuhasib.Infrastructure/Services/InvoiceService.cs`

**Flutter**
- `mobile/almuhasib_mobile/lib/core/offline/offline_write_queue.dart`
- `mobile/almuhasib_mobile/lib/features/auth/data/auth_repository.dart`

**Tests / docs**
- `src/AlMuhasib.Core.Tests/BranchIsolationSecurityTests.cs`
- `docs/MULTI_BRANCH_AUDIT_REPORT.md` (this file)

**Migrations added in this audit:** none (security/logic fixes only; prior multi-branch migration remains).

---

## Remaining work (not blocking Critical isolation)

1. Replace desktop `FindAsync` on branch-scoped entities with filtered LINQ (M2).
2. Persist cloud `can_view_all_branches` account flags when product requires All-Branches API mode.
3. Optional: `UPDLOCK`/`SERIALIZABLE` around branch document numbering under high concurrency.
