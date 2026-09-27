# Multi-Branch Architecture — نظام قيد (Accounting)

## Scope (Phase 1)

- **Accounting only** (Desktop `AppDbContext` + Cloud API + Sync + Flutter).
- Other verticals (Gold / Hotel / Car / …) keep working on the **Main Branch** until a later phase.
- Isolation model: **one database**, **separate business data per branch** via `BranchId`.

## Hierarchy

```
Company / Tenant
  └── Branch (Main + optional branches)
        └── Branch-scoped business data
  └── Users / TenantAccounts (company-level)
        └── UserBranches / TenantAccountBranches (assignment)
```

Do **not** confuse organizational `Branch` with LAN `DeploymentMode.BranchClient`
(network client sharing the main SQL server).

## Entities

### Company-level (no BranchId filter)

| Entity | Notes |
|--------|--------|
| `Branch` / `CloudBranch` | Branch master |
| `UserBranch` / `TenantAccountBranch` | User ↔ Branch assignment |
| `User`, `Permission` | Auth / screen ACL |
| `UserTask`, `UserNote`, `UserLoginLog` | User personal |
| `SyncState`, `CloudSyncSettings` | Sync infrastructure |
| `Tenant`, `TenantAccount` | Cloud platform |

### Branch-scoped (required BranchId)

All accounting business data: products, customers, suppliers, warehouses, cash boxes,
banks, invoices, vouchers, expenses, stock, installments, investors, settings, etc.
Child rows (`InvoiceItem`, …) also carry `BranchId` for defense-in-depth.

Cloud: `BranchId` lives on `CloudBaseEntity` so non-accounting modules also land on
Main Branch after migration (backward compatible).

## Branch Context

| Layer | Mechanism |
|-------|-----------|
| Desktop | `IBranchContext` session after login / switcher |
| API | JWT `branch_id` + `X-Branch-Id` validated against assignments |
| EF | Global query filter on `IBranchEntity` / `CloudBaseEntity.BranchId` |
| Writes | Stamp `BranchId` from context; reject cross-branch mutations |
| Reports | Single branch by default; `AllBranches` only with `ViewAllBranches` |

**Never trust client-supplied BranchId alone.** Backend resolves and authorizes.

### Rules

1. One allowed branch → auto-select, no picker.
2. Multiple branches → must pick before any business screen.
3. Writes / numbering **never** use “All Branches”.
4. Admin with `ManageAllBranches` / `ViewAllBranches` may report across allowed branches.
5. Unauthorized branch → **403 Forbidden**.

## Migration (safe order)

1. Create `Branches` (+ cloud equivalent).
2. Insert **Main** branch (`Code = MAIN`) per company/tenant.
3. Add nullable `BranchId` columns.
4. Backfill all rows → Main.
5. Assign all users / tenant accounts → Main (`IsDefault = true`).
6. Verify no NULL `BranchId` on branch-scoped tables.
7. Make `BranchId` required + FK + indexes `(BranchId)`, `(BranchId, Date)`, unique `(BranchId, Number)`.

Existing users continue on Main with no setup change.

## Document numbering

Invoice / voucher (and similar) sequences are **per branch**.
Unique indexes: `(BranchId, InvoiceNumber)` where not deleted.

## Sync

- Branches sync by `SyncId`.
- Every sync DTO carries `BranchSyncId`.
- Push/pull scoped to tenant; branch identity remapped like other FKs.
- Offline Flutter queue stamps `branchId` / `branchSyncId` on enqueue; flush rejects mismatch.

## Flutter

- Login returns `allowedBranches` + `defaultBranchId` + flags.
- Single branch → skip picker.
- `ApiClient` sends `X-Branch-Id`; server validates.
- On switch: clear controllers / queue isolation by `tenantId+branchId`.

## Adding a new branch-scoped entity

1. Inherit `BranchScopedEntity` (desktop) or `CloudBaseEntity` (cloud).
2. Rely on query filter + write guards — do not hand-filter only in UI.
3. Add `BranchSyncId` on sync DTO; map in `SyncMapper` / `SyncEngine`.
4. Include in migration backfill to Main for existing rows.
5. Add indexes for common filters.

## Permissions (screen names)

`Branches`, `AssignUserBranches`, plus flags:
`ViewAllBranches`, `ManageAllBranches`, `SwitchBranch`
(integrated with existing `Permission.ScreenName` model).

## Security checklist

- Tampered `BranchId` / `X-Branch-Id` → 403
- Entity ID from another branch → not found / 403
- Cross-tenant branch id → 403
- All-branches report without permission → 403
- Offline queue cannot flush into another branch context
