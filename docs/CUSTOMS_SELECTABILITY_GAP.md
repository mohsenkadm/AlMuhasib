# CustomsClearance — Selectability & Dashboard Gap Checklist

**Branch context:** built on top of DB schema work (`ApplicationSystemType.CustomsClearance = 6` + `CustomsDbContext`).  
**Goal:** make التخليص الكمركي selectable and show a Customs Dashboard in the MainWindow sidebar — same mutually exclusive system pattern as GoldShop / CarTrading.

---

## Mutual exclusivity (do not mis-wire)

Systems are **mutually exclusive**. One profile → one `ActiveSystem` → one `ISystemModule` → one sidebar menu.

| Correct | Wrong |
|---------|--------|
| التخليص الكمركي appears as a **system card** in Setup Wizard and as a **developer switch** target | Adding “التخليص الكمركي” as a peer menu item inside `AccountingMenuBuilder` |
| After selection, sidebar home is the **Customs** module’s `لوحة التحكم` (`CustomsDashboardViewModel`) | Mixing Customs screens into Accounting / Gold menus |
| Login opens `ActiveModule.DashboardViewModelType` via `MainWindowViewModel.ActiveDashboardType` | Hard-coding Accounting `DashboardViewModel` when Customs is active |

**Login path (already works once module is registered):**

1. `SystemModuleRegistry.ActiveModule` ← `_profile.ActiveSystem`
2. `MainWindowViewModel.InitializeMenu()` ← `ActiveModule.BuildMenuItems()`
3. `OpenInitialSessionTabsAsync()` ← `OpenTabAsync(ActiveDashboardType, "لوحة التحكم", …)`
4. `ActiveDashboardType` → `_moduleRegistry.ActiveModule.DashboardViewModelType`

---

## Status of the 10 checklist items

| # | Item | Status |
|---|------|--------|
| 1 | `ApplicationSystemType.CustomsClearance` | **EXISTS** (`= 6`) |
| 2 | `SystemModuleRegistry` Customs registration | **MISSING** |
| 3 | `CustomsClearanceSystemModule` / MenuBuilder / PermissionRegistry | **MISSING** (all three) |
| 4 | `SetupWizardHostWindow` customs card | **MISSING** |
| 5 | `DeveloperSystemSwitchView(Model)` customs command | **MISSING** |
| 6 | `MainWindow.xaml` DataTemplates for Customs dashboard | **MISSING** |
| 7 | `CustomsDashboardView` / `ViewModel` | **MISSING** |
| 8 | `ScreenPermissionRegistry` Customs branches | **MISSING** |
| 9 | `App.xaml.cs` DI for Customs VMs | **MISSING** |
| 10 | `ThemeService` gold-style accent for customs | **MISSING** |

**Already present (infra / profile — not enough for UI selectability):**

- `SystemConnectionStrings.CustomsDatabase` → `AlMuhasibCustomsDb`
- `SystemProfileService` DB name map for `CustomsClearance`
- `DependencyInjection.RegisterCustomsInfrastructure`
- `MainServerHostingService` / `NetworkConnectionService` Customs branches
- Entities, enums, `CustomsDbContext`, initial migration, UoW, auth/search/audit/sync stubs

---

## File-by-file: what to ADD

Do **not** reorder existing Gold/CarTrade/Hotel/etc. menu items. Append Customs as a new system only.

### A. Module shell (required for sidebar home)

| Action | Path | Notes (mirror) |
|--------|------|----------------|
| **ADD** | `src/AlMuhasib.UI/Modules/CustomsClearanceSystemModule.cs` | Like `GoldShopSystemModule.cs` / `CarTradingSystemModule.cs`: `SystemType = CustomsClearance`, `DisplayName = "التخليص الكمركي"`, `DashboardViewModelType = typeof(CustomsDashboardViewModel)`, `SetupWizardViewModelType = null` (CarTrading pattern) unless a customs setup wizard is added later |
| **ADD** | `src/AlMuhasib.UI/Modules/CustomsMenuBuilder.cs` | Minimal: first item `لوحة التحكم` → `CustomsDashboardViewModel` + shared Users/Permissions/Backup/CloudSync/Network/SystemUpdate/Developer switch (same shared screens as other non-accounting modules). Expand later; do not touch Accounting menu order |
| **ADD** | `src/AlMuhasib.UI/Services/CustomsPermissionRegistry.cs` | Like `GoldShopPermissionRegistry` / `CarTradePermissionRegistry`: `Dashboard = "CustomsDashboard"`, screen↔VM maps, labels |
| **EDIT (append only)** | `src/AlMuhasib.UI/Modules/SystemModuleRegistry.cs` | Field `_customs`, ctor `new CustomsClearanceSystemModule()`, `ActiveModule` case `CustomsClearance => _customs`, flag `IsCustomsClearance` |

### B. System selection surfaces

| Action | Path | Notes |
|--------|------|-------|
| **EDIT (append card)** | `src/AlMuhasib.UI/Windows/SetupWizardHostWindow.xaml` | Add column + `CustomsCard` after `GoldCard` (today columns 0…10 for 6 systems). Label: **التخليص الكمركي**. Pick a distinct accent (not Gold `#B8860B`; e.g. teal/slate). Do not reshuffle existing cards’ order |
| **EDIT** | `src/AlMuhasib.UI/Windows/SetupWizardHostWindow.xaml.cs` | `OnCustomsSelected`, include card in `ResetCard` list, map `CustomsClearance => SystemConnectionStrings.CustomsDatabase` in **all** local DB-name switches (today Gold is last; Customs is absent → would fall through incorrectly) |
| **EDIT** | `src/AlMuhasib.UI/ViewModels/DeveloperSystemSwitchViewModel.cs` | Display name branch; `SwitchToCustomsClearance` command; `GetTargetDatabaseName` → `AlMuhasibCustomsDb` |
| **EDIT (append button)** | `src/AlMuhasib.UI/Views/DeveloperSystemSwitchView.xaml` | Button after Gold, bound to new command; do not reorder existing buttons |

### C. Dashboard + MainWindow wiring

| Action | Path | Notes |
|--------|------|-------|
| **ADD** | `src/AlMuhasib.UI/ViewModels/Customs/CustomsDashboardViewModel.cs` | Start stub/KPI shell like a thin `CarTradeDashboardViewModel`; full Gold-level KPIs later |
| **ADD** | `src/AlMuhasib.UI/Views/Customs/CustomsDashboardView.xaml` (+ `.xaml.cs`) | Own view; **do not** reuse Accounting `DashboardView` |
| **EDIT** | `src/AlMuhasib.UI/MainWindow.xaml` | `xmlns:customsViews` / `customsVm`; `DataTemplate` for `CustomsDashboardViewModel` → `CustomsDashboardView` (near Gold templates). No need to change Accounting templates |

`MainWindowViewModel` itself needs **no** Customs-specific menu logic if `ActiveModule` is correct — `ActiveDashboardType` already delegates.

### D. Permissions facade

| Action | Path | Notes |
|--------|------|-------|
| **EDIT** | `src/AlMuhasib.UI/Services/ScreenPermissionRegistry.cs` | `IsCustomsClearance`; prepend Customs branches in `AllScreens`, `GetScreenName`, `GetDefaultViewModelType`, `GetLabel` (same cascade pattern as Gold first) |

### E. DI

| Action | Path | Notes |
|--------|------|-------|
| **EDIT** | `src/AlMuhasib.UI/App.xaml.cs` | `services.AddTransient<CustomsDashboardViewModel>();` (and later Customs screens). Optional: setup-wizard / `EnsureDefaults` branches only if a customs settings service exists — CarTrading skips this |

Infra DI for Customs **already exists** (`RegisterCustomsInfrastructure`) — no change required for selectability alone.

### F. Theme (gold-style accent for customs)

| Action | Path | Notes |
|--------|------|-------|
| **EDIT** | `src/AlMuhasib.UI/Services/ThemeService.cs` | Today only `isGold` special-cases Material primary + sidebar selected brushes. Add `isCustoms` (or `isGold \|\| isCustoms` with **customs palette**) so Customs gets a dedicated accent (primary hue + sidebar selection), analogous to Gold’s `#B8860B` treatment — use a customs color, not copy Gold gold |

---

## Already exists — Entities / Infrastructure (this branch)

### Core entities — `src/AlMuhasib.Core/Entities/Customs/`

- `CustomsCustomer.cs`
- `CustomsInvoice.cs`
- `CustomsVehicle.cs`
- `CustomsExpenseType.cs`
- `CustomsExpense.cs`
- `CustomsReceipt.cs`
- `CustomsReceiptAllocation.cs`
- `CustomsFxRate.cs`
- `CustomsAttachment.cs`

### Core enums — `src/AlMuhasib.Core/Enums/Customs/`

- `CustomsCustomerType`, `CustomsServiceType`, `CustomsVehicleStatus`
- `CustomsInvoiceStatus`, `CustomsPaymentMethod`, `CustomsAttachmentOwnerType`

### Core interfaces

- **No** `ICustoms*` service interfaces yet (unlike `Interfaces/Services/Gold/`). Dashboard can stub until domain services land.

### Infrastructure

| Area | Paths |
|------|--------|
| DbContext | `Data/Customs/CustomsDbContext.cs`, `CustomsDbContextFactory.cs` |
| EF configs | `Data/Customs/Configurations/CustomsEntityConfigurations.cs` |
| Migration | `Data/Customs/Migrations/20261022233522_CustomsInitial.cs` (+ Designer, Snapshot) |
| UoW / repo | `Repositories/CustomsUnitOfWork.cs`, `Repositories/Customs/CustomsRepository.cs` |
| Host services | `CustomsAuthService`, `CustomsAuditLogService`, `CustomsDatabaseMigrationService`, `CustomsGlobalSearchService`, `Customs/CustomsSyncService` |
| DI switch | `DependencyInjection.cs` → `case CustomsClearance: RegisterCustomsInfrastructure(...)` |
| Connection | `SystemConnectionStrings.CustomsDatabase` |

Shared DbSets on Customs DB (users/permissions/audit/print/cloud sync) are already on `CustomsDbContext`.

---

## Minimal MVP order (selectability only)

1. `CustomsPermissionRegistry` + `CustomsDashboardView(Model)` stub  
2. `CustomsMenuBuilder` (dashboard + shared admin items) + `CustomsClearanceSystemModule`  
3. Register in `SystemModuleRegistry` + `ScreenPermissionRegistry`  
4. DI + `MainWindow.xaml` DataTemplate  
5. Setup Wizard card + Developer switch  
6. `ThemeService` customs accent  

After that: choosing Customs in wizard / developer switch → restart → Login → sidebar shows Customs home → dashboard tab opens. Domain screens (vehicles, invoices, receipts) are a later phase and must stay under the Customs module menu only.

---

## Explicit non-goals for this gap

- Do **not** add Customs entries to `AccountingMenuBuilder`
- Do **not** reorder Gold/CarTrade/Hotel/RealEstate/Accounting sidebar items
- Do **not** require a Customs setup wizard for first selectability (CarTrading has none)
- Do **not** block on cloud SyncEngine Customs push/pull (infra stub exists; cloud parity is separate)
