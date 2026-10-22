# Receipt / Voucher / Payment → Customer & Invoice Linkage

> Analysis for Customs clearance receipts. Source of truth: existing entities/services in the repo (2026-10-22).  
> Companion to `CUSTOMS_IMPLEMENTATION_PLAN.md`.

---

## Verdict (Customs)

**Closest pattern for receipts that allocate to one or many invoices: Accounting `Voucher` (Receipt / DebtReceipt) + `CustomerBalanceHelper.AllocateToCreditInvoices`.**

| Need | Existing pattern | Fits Customs? |
|------|------------------|---------------|
| One invoice | `Voucher.InvoiceId` **or** `GoldPayment.InvoiceId` (required) | Yes |
| Many invoices | Accounting FIFO when only `CustomerId` (no allocation rows) | Partial — works, but allocation is not stored per invoice |
| Auditable split (id→amount) | `InvoiceReturnCreditHelper` notes `[RET-APPLIED:id=amt;…]` | Closest *explicit* multi-line; not a first-class table |
| Single “document” (contract/tx) | CarTrade / RealEstate / Car payments | No multi-invoice |

**Recommendation for Customs:** model a header receipt like Accounting voucher + **child allocation rows** (real FKs), combining:

1. **Header** ← `Voucher` / `SalesRepCollection` (party, amount, number, method, cash, soft delete)
2. **Lines** ← explicit `InvoiceId` + `Amount` (upgrade of return-notes / FIFO result), update invoice `PaidAmount`/`RemainingAmount` like Gold/Accounting
3. **Attachments** ← `CustomerAttachment` disk pattern under `Attachments/Customs/...`

Do **not** copy CarTrade/RealEstate for multi-invoice (they pay one parent only). Do **not** rely on GoldVoucher for invoice settlement (party credit only, no invoice FK).

---

## 1. Accounting `Voucher`

**Entity:** `src/AlMuhasib.Core/Entities/Voucher.cs`  
**Config:** `…/Configurations/VoucherConfiguration.cs`  
**Service:** `CashBankService` (create / soft-delete / apply / reverse)

### Fields

| Field | Type | Notes |
|-------|------|--------|
| `Id`, `SyncId`, audit, soft-delete | from `BranchScopedEntity` → `BaseEntity` | `BranchId` required |
| `VoucherNumber` | `string` | Auto: `{prefix}-{####}`; unique per branch when `IsDeleted = 0` |
| `VoucherType` | `VoucherType` | See enum below |
| `Currency` | `AccountingCurrency` | Must match cash box / bank |
| `FxRate` | `decimal` | For cross-currency settlement |
| `SettlementCurrency` | `AccountingCurrency?` | `[NotMapped]`; encoded in Notes as `[FX-SETTLE:…]` |
| `Amount` | `decimal` | |
| `BankFees` | `decimal` | BankReceipt |
| `CustomerId` | `int?` | |
| `SupplierId` | `int?` | Payments |
| `InvestorId` | `int?` | Investor deposit/withdrawal |
| `EmployeeId` | `int?` | Advances (no invoice apply) |
| `CashBoxId` | `int` | Required |
| `BankAccountId` | `int?` | BankReceipt |
| `Date` | `DateTime` | |
| `Notes` | `string?` | Markers: `[CR-APPLIED]`, `[FX-SETTLE:IQD]` |
| `InvoiceId` | `int?` | **Optional single credit invoice** |
| `InstallmentId` | `int?` | Optional; mutually exclusive with `InvoiceId` |
| `IsReconciled` / `ReconciledAt` / `ReconciledBy` | bank recon | |

### `VoucherType`

```
Receipt, Payment, BankReceipt, InvestorDeposit, InvestorWithdrawal, DebtReceipt
```

### Allocation to invoices

**No allocation child table.** Behavior in `CashBankService`:

| Case | Behavior |
|------|----------|
| `InvoiceId` set (Receipt/DebtReceipt) | Apply full settleable amount to **that one** credit invoice (`ApplyAmountToCreditInvoiceAsync`) |
| `InstallmentId` set | Apply to installment; may backfill `InvoiceId` from plan |
| Only `CustomerId` (no invoice/installment) | **FIFO** across open credit sale/installment invoices same settlement currency (`ApplyDebtReceiptToCreditInvoicesAsync` → `CustomerBalanceHelper.AllocateToCreditInvoices`) |
| Payment + `InvoiceId` | One purchase credit invoice |
| Payment + only `SupplierId` | FIFO across supplier purchase credit invoices |
| Employee party | Cash only; no invoice apply |

After apply: Notes get `[CR-APPLIED]` (boolean marker only — **does not list which invoice ids/amounts**). Soft-delete reverses: single-invoice exact reverse, or FIFO reverse via **LIFO** (`DeallocateFromCreditInvoices`) when multi was applied.

Invoice side updated: `PaidAmount`, `RemainingAmount`, `IsCreditPaid`.

### Numbering

Prefixes: `RCV`, `PAY`, `BRV`, `IDP`, `IWD`, `DRC` → e.g. `RCV-0001`.  
Soft-deleted numbers included when computing next (avoid reuse under filtered unique index).

---

## 2. Gold: `GoldPayment` vs `GoldVoucher`

### `GoldPayment` — invoice-tied installment ledger

**Entity:** `Entities/Gold/GoldPayment.cs`  
**Created by:** `GoldSaleService` / `GoldPurchaseService.RecordPaymentAsync`

| Field | Type | Notes |
|-------|------|--------|
| BaseEntity | soft delete, SyncId, … | |
| `InvoiceId` | `int` | **Required** — one payment → one invoice |
| `PaymentDate` | `DateTime` | |
| `Amount` | `decimal` | In payment currency |
| `Currency` | `GoldCurrency` | |
| `FxRate` | `decimal` | Converted into invoice pricing currency |
| `CashBoxId` | `int?` | |
| `Notes` | `string` | |

- Collection on `GoldInvoice.Payments`
- Updates invoice `PaidAmount` / `RemainingAmount` / status; adjusts customer/supplier credit and cash box
- **No multi-invoice allocation**

### `GoldVoucher` — party / cash movement (not invoice allocation)

**Entity:** `Entities/Gold/GoldVoucher.cs`  
**Service:** `GoldCashService.CreateVoucherAsync`

| Field | Type | Notes |
|-------|------|--------|
| `VoucherNumber` | `string` | `GRC-####` / `GPY-####` |
| `VoucherDate` | `DateTime` | |
| `VoucherType` | `GoldVoucherType` | `Receipt`, `Payment` |
| `Currency`, `Amount` | | |
| `CashBoxId` | `int?` | |
| `CustomerId` / `SupplierId` | `int?` | Adjusts party credit |
| `IsOpeningBalance` | `bool` | Skip credit adjust when opening |
| `AffectsCashBox` | `bool` | default true |
| `Notes` | `string` | |

**No `InvoiceId`.** Used for cash/party books and opening balances, not settling specific invoices.

### `GoldPaymentMethod` (on invoice, not on payment row)

`Cash = 0`, `Credit = 1`

---

## 3. `CarTradePayment` — link to transaction

**Entity:** `Entities/CarTrade/CarTradePayment.cs`  
**Service:** `CarTradeService.RecordPurchasePaymentAsync` / `RecordSalePaymentAsync`

| Field | Type | Notes |
|-------|------|--------|
| BaseEntity | | |
| `TransactionId` | `int` | FK → `CarTradeTransaction` |
| `PaymentKind` | `CarTradePaymentKind` | `Purchase` or `Sale` |
| `PaymentDate` | `DateTime` | |
| `Amount` | `decimal` | |
| `Notes` | `string` | |
| `RemainingBefore` / `RemainingAfter` | `decimal` | Snapshot of the relevant remaining |

Parent `CarTradeTransaction` holds dual remainings:

- Purchase: `AmountPaid`, `RemainingAmount`, `PaymentMode` (`FullCash` / `Partial`)
- Sale: `SaleAmountPaid`, `SaleRemainingAmount`, `SalePaymentMode`

**No customer entity FK; no invoices.** One payment updates one side of one transaction.

---

## 4. RealEstate — payment allocation

**Entity:** `RealEstateContractPayment`  
**Service:** `RealEstateContractService.RecordPaymentAsync`

| Field | Type |
|-------|------|
| `ContractId` | `int` |
| `PaymentDate` | `DateTime` |
| `Amount` | `decimal` |
| `Notes` | `string` |
| `RemainingBefore` / `RemainingAfter` | `decimal` |

Parent `RealEstateContract`: `TotalPrice`, `AmountPaid`, `RemainingAmount`, `PaymentMode` (`Cash` / `Credit`).

**There is no multi-invoice (or multi-contract) allocation.** Same 1-parent pattern as Car / CarTrade. Expenses (`RealEstateExpense`) may optionally set `RelatedContractId` but are not payment allocations.

Same shape: `CarContractPayment` → `CarSaleContract`.

---

## 5. Soft delete, numbering, payment methods

### Soft delete (`BaseEntity`)

```
IsDeleted, DeletedAt, DeletedBy
MarkSoftDeleted(deletedBy) / RestoreFromSoftDelete(restoredBy)
```

Vouchers: `DeleteVoucherAsync` reverses cash + invoice/installment/FIFO application, then soft-deletes. Unique `VoucherNumber` index filtered to `[IsDeleted] = 0`.

### Numbering

| Entity | Field | Generation |
|--------|-------|------------|
| `Voucher` | `VoucherNumber` | Auto prefix-seq per branch/type |
| `GoldVoucher` | `VoucherNumber` | Auto `GRC`/`GPY` |
| `SalesRepCollection` | `ReceiptNumber` | **Manual optional** string (indexed, not unique/auto) |
| CarTrade / RealEstate payments | *(none)* | Rely on parent document number |

`ReceiptNumber` in this codebase lives on **`SalesRepCollection`**, not on `Voucher`.

### Payment method enums

| Enum | Values | Used on |
|------|--------|---------|
| `PaymentMethod` | `Cash`, `Credit`, `Installment` | Accounting `Invoice`, `SalesRepCollection` |
| `GoldPaymentMethod` | `Cash`, `Credit` | `GoldInvoice` |
| `CarTradePaymentMode` | `FullCash`, `Partial` | Transaction purchase/sale mode |
| `CarTradePaymentKind` | `Purchase`, `Sale` | Payment row direction |
| `RealEstatePaymentMode` | `Cash`, `Credit` | Contract |
| `RestaurantPaymentMethod` | `Cash`, `Card`, `RoomCharge`, `Mixed` | Hotel restaurant (out of scope) |

Voucher itself has **no** payment-method enum — type + cash/bank implies channel.

### Related: `SalesRepCollection`

| Field | Notes |
|-------|--------|
| `SalesRepresentativeId`, `CustomerId` | Required |
| `Amount`, `Currency`, `CollectionDate` | |
| `ReceiptNumber` | Optional free-text |
| `PaymentMethod` | Default Cash |
| `HandedOverAmount` / `HandedOverAt` | Handover tracking |
| `InvoiceId` | Optional **single** invoice link |
| `Notes` | |

CRUD via UI/UoW only — **does not** run FIFO or update invoice paid amounts on save.

---

## 6. Attachments (payments / expenses)

**Payments and expenses have no attachment entities today.**

Only reference pattern: **`CustomerAttachment`**

| Field | Type |
|-------|------|
| `CustomerId` | `int` |
| `FileName` | `string` |
| `FilePath` | `string` (absolute on disk) |
| `Description` | `string?` |
| Branch + BaseEntity | soft delete |

Disk: `{AppBase}/Attachments/Customers/{customerId}/`  
UI: `CustomersViewModel` (file picker + WIA scanner), `TableAttachIconButton`.

Accounting `Expense`, `GoldExpense`, `RealEstateExpense`, vouchers, and payment rows: **notes only, no files**.

Customs plan already points at `CustomsAttachment` + `Attachments/Customs/...` without reusing accounting `CustomerAttachments`.

---

## 7. Explicit multi-invoice notes (returns only)

`InvoiceReturnCreditHelper` stores allocations in invoice/return **Notes**:

```
[RET-APPLIED:12=100.00;15=50.00] [RET-ADVANCE:RCV-0042]
```

FIFO prefers `RelatedInvoiceId`, then date. This is the only place that **persists** which invoices received how much — still not a relational allocation table.

Accounting debt-receipt multi-apply only stamps `[CR-APPLIED]` and relies on LIFO reverse.

---

## 8. Pattern comparison (quick)

| Pattern | Party link | Invoice link | Multi-invoice | Persisted split | Soft delete | Number |
|---------|------------|--------------|---------------|-----------------|-------------|--------|
| `Voucher` | Customer/Supplier/… | Optional 1 | FIFO if none | No (marker only) | Yes + reverse | Auto |
| `GoldPayment` | via invoice | Required 1 | No | N/A (1:1) | Yes | None |
| `GoldVoucher` | Customer/Supplier | None | No | N/A | Yes | Auto |
| `CarTradePayment` | names on tx | N/A | No | N/A | Yes | None |
| `RealEstateContractPayment` | names on contract | N/A | No | N/A | Yes | None |
| `SalesRepCollection` | Customer | Optional 1 | No | No | Yes | Manual `ReceiptNumber` |
| Return notes | via invoices | Many | Yes | In Notes | — | — |

---

## 9. Suggested Customs shape

```
CustomsReceipt (header)
  ReceiptNumber (auto, e.g. CRC-0001) | Date | CustomerId
  Amount | Currency | PaymentMethod (or cash/bank refs)
  Notes | soft delete (BaseEntity)
  optional: single InvoiceId shortcut for UI “pay this invoice”

CustomsReceiptAllocation (lines)   // prefer over Notes / FIFO-only
  ReceiptId | InvoiceId | Amount
  // on save: update each invoice Paid/Remaining like GoldPayment / Voucher apply
  // on soft-delete: reverse each line exactly (avoid LIFO heuristic)

CustomsAttachment
  OwnerType + OwnerId (Receipt / Invoice / Vehicle / Expense)
  FileName | FilePath | Description
  disk: Attachments/Customs/{ownerType}/{ownerId}/
```

**Why this hybrid:** Accounting voucher for party+number+cash+soft-delete lifecycle; GoldPayment for durable per-invoice payment rows; return-helper intent for multi-invoice splits — but as real FKs instead of Notes.

---

## Source map

| Topic | Path |
|-------|------|
| Voucher entity | `src/AlMuhasib.Core/Entities/Voucher.cs` |
| Apply / FIFO / reverse | `src/AlMuhasib.Infrastructure/Services/CashBankService.cs` |
| FIFO helper | `src/AlMuhasib.Core/CustomerBalanceHelper.cs` |
| Return allocations | `src/AlMuhasib.Core/InvoiceReturnCreditHelper.cs` |
| Gold payment / voucher | `Entities/Gold/GoldPayment.cs`, `GoldVoucher.cs`; `GoldSaleService`, `GoldCashService` |
| CarTrade | `Entities/CarTrade/CarTradePayment.cs`; `CarTradeService` |
| RealEstate | `Entities/RealEstate/RealEstateContractPayment.cs`; `RealEstateContractService` |
| ReceiptNumber | `Entities/SalesRepCollection.cs` |
| Attachments | `Entities/CustomerAttachment.cs`; `CustomersViewModel` |
| Soft delete base | `Entities/BaseEntity.cs` |
