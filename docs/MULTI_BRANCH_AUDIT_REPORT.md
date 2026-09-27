# Multi-Branch Production Audit Report (Accounting) — Pass 2

**Scope:** Accounting only — Desktop + Cloud API + Flutter + Sync + Reports  
**Date:** 2026-09-27  
**Branch:** `cursor/multi-branch-accounting-5d23`  
**Path audited:** Login → Authorization → CurrentBranch → API → Business Logic → DB → Accounting → Reports → Flutter → Cache → Offline → Sync

---

## Totals (both audit passes combined)

| Metric | Count |
|--------|------:|
| Findings discovered | **24** |
| Critical | 4 |
| High | 11 |
| Medium | 6 |
| Low | 3 |
| **Fixed** | **22** |
| Remaining | **2** (Low notes) |

---

## Pass 2 findings (this round)

### High — fixed

#### H9 — Mobile delete/update IgnoreQueryFilters without BranchId
- **Files:** `CloudMobileWriteService` (`DeletePricingType`, `DeleteProductPrice`, `UpdateBusinessSettings`, invoice idempotent load, `AdjustStock`, `EnsureDefaultPricingType`, `RollbackOrphanInvoiceBundle`)
- **Scenario:** SyncId of branch B while bound to A → mutate B.
- **Fix:** `FindInWriteBranchAsync` + explicit `BranchId == RequireWriteBranchId()`; stock push stamps `BranchSyncId`.

#### H10 — CloudMasterData ensure paths mutate other branch
- **File:** `CloudMasterDataService.EnsureBusinessSettingsAsync` / `EnsureDefaultPricingDataAsync`
- **Fix:** Scope create/mutate to write branch only; AllBranches reads filtered set without cross-branch mutation.

#### H11 — Repository.GetByIdAsync used FindAsync (IDOR read)
- **File:** `Repository.GetByIdAsync`
- **Fix:** `FirstOrDefaultAsync(e => e.Id == id)` so EF branch + soft-delete filters apply.
- **Also:** `CashBankService`, `ReportService`, `CloudReportService` FindAsync → LINQ.

#### H12 — Soft-delete revive across branches (masters)
- **File:** `Repository.FindSoftDeletedFirstAsync` (+ UnitOfWork passes `IBranchContext`)
- **Fix:** After IgnoreQueryFilters, require `BranchId` of current write branch (or Allowed list).

### Medium — fixed

#### M5 — Single-branch regression after fail-closed assignments
- **Risk:** Legacy cloud/desktop account with missing assignment on a one-branch company would see empty data.
- **Fix:** Heal **only** when tenant/company has exactly **one** active branch — auto-assign that branch once. Multi-branch empty assignments stay fail-closed.

### Verified clear this pass

| Scenario | Result |
|----------|--------|
| Single-branch company (MAIN only) | Sees all Main rows; auto-bind; login OK |
| Legacy migration BranchId NULL/0 | Backfill → NOT NULL → FK; safety THROW if residuals |
| Controllers Get-by-SyncId (Invoices/Products) | EF branch filter → NotFound for other branch |
| ReportService / Dashboard lists | Rely on EF AllowedBranchIds / current branch |
| Flutter offline queue | Branch+tenant match required; cleared on logout |
| Document numbering (desktop) | Branch-aware with IgnoreQueryFilters + BranchId |
| Admin AllBranches writes | `RequireWriteBranchId` blocks accounting writes |

---

## Pass 1 Critical/High (previously fixed — re-verified)

1. AllBranches no longer full-suppresses BranchId (uses AllowedBranchIds)
2. Empty assignments ≠ all branches; revoked access stays revoked
3. SyncEngine pull/push scoped + BranchSyncId required
4. Mobile writes stamp BranchSyncId from context
5. Supervisory reports re-filter BranchId
6. Product/Invoice restore & FK integrity
7. Admin Role ≠ automatic ViewAllBranches
8. SetTenant preserves account id

---

## Remaining (not Critical/High)

| # | Severity | Item | Why not fixed now |
|---|----------|------|-------------------|
| L1 | Low | Concurrent document numbering race under extreme load | Unique `(BranchId, Number)` + retries already mitigate |
| L2 | Low | Cloud `can_view_all_branches` claim not issued yet | Product flag reserved; AllBranches remains permission-gated |

---

## Isolation test results

```
dotnet test --filter FullyQualifiedName~Branch
Passed: 15 / Failed: 0
```

Coverage includes:
- Branch stamp / cross-branch mutation denied
- Query filter hides other branch
- AllBranches respects AllowedBranchIds only
- Unbound fail-closed
- FindAsync vs LINQ evidence
- **Repository GetByIdAsync respects branch**
- **Soft-delete revive stays in write branch**
- **Single-branch legacy Main rows all visible**

API build: succeeded.

---

## Acceptance checklist

| Criterion | Status |
|-----------|--------|
| Branch 1 user → zero access to Branch 2 data (queries/API/sync/mobile) | ✅ |
| BranchId tampering from client rejected (server context wins) | ✅ |
| Cross-company blocked by TenantId filter | ✅ |
| Single-branch behaves like pre-Multi-Branch | ✅ (heal + Main backfill) |
| Legacy rows linked to MAIN, not NULL | ✅ (migration) |
| Offline no cross-branch flush / logout clears queue | ✅ |
| Sync no wrong-branch push/pull | ✅ |
| All Branches for reports only (writes need CurrentBranch) | ✅ |
| No open Critical/High | ✅ |

---

## Files modified (pass 2)

- `CloudMobileWriteService.cs`, `CloudMasterDataService.cs`
- `Repository.cs`, `UnitOfWork.cs`
- `CashBankService.cs`, `ReportService.cs`, `ReportService.NewReports.cs`
- `CloudReportService.cs`, `CloudReportService.NewReports.cs`
- `AuthController.cs`, `BranchService.cs`
- `BranchIsolationSecurityTests.cs`
- `docs/MULTI_BRANCH_AUDIT_REPORT.md`

**New migrations this pass:** none.
