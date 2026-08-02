# Gold Phase 2 — Accounting UI pattern checklist

Mirror Accounting (and existing Gold list screens) for Phase 2. Prefer shared controls/styles over one-off layout.

## Global conventions

| Rule | Mirror |
|------|--------|
| Root | `Style="{StaticResource PageStyle}"` + `FlowDirection="RightToLeft"` |
| Styles | `/workspace/src/AlMuhasib.UI/Styles/Theme.xaml` (`PageStyle`, `FilterBar`, `ElevatedCard`, `ModernDataGrid`, `FilledTextBox` / `FilledComboBox` / `FilledDatePicker`, `PrimaryButton`, `FlatButton`) |
| Page margin | Outer `Grid`/`StackPanel` ~`24` (lists often `20–24`) |
| Data tables | `ModernDataGrid` + column filter behavior when filterable |
| Feedback | `BeautifulMessageDialog`; busy = semi-transparent `#80FFFFFF` overlay + circular progress |

---

## 1. List / CRUD screens (Suppliers pattern)

**Canonical:** `/workspace/src/AlMuhasib.UI/Views/SuppliersView.xaml`  
**Gold already close:** `/workspace/src/AlMuhasib.UI/Views/Gold/GoldCustomersView.xaml`

Layout (top → bottom):

1. **Action bar** (`ElevatedCard`): title + Add / Refresh + search (`FilledTextBox`)
2. **Table card** (`ElevatedCard` Padding=`0`):
   - `ListTableHeaderBar` — Title, `TargetDataGrid`, column-filter bindings, Export/Print commands, optional `ShowCardToggle`
   - `DataGrid` (`ModernDataGrid`) with `DataGridColumnFilterBehavior.*` and `FilterPropertyPath` on columns
   - Row actions: `TableEditIconButton` / `TableDeleteIconButton` + `TableActionsCellStyle`
   - `PaginationBar` (binds VM `CurrentPage` / `TotalPages` / page commands)
3. **Dialogs:** `md:DialogHost` overlays (`IsDialogOpen`, `IsDeleteDialogOpen`), `CloseOnClickAway="False"`, card `MinWidth≈420`

**Controls:**  
`/workspace/src/AlMuhasib.UI/Controls/ListTableHeaderBar.xaml`  
`/workspace/src/AlMuhasib.UI/Controls/PaginationBar.xaml`  
`/workspace/src/AlMuhasib.UI/Controls/ColumnFilterToggle.xaml`  
`/workspace/src/AlMuhasib.UI/Controls/ListViewModeToggle.xaml`

**Phase 2 gap on Gold lists:** wire `ShowFilter`/`ShowExport`/`ShowPrint`, `TargetDataGrid`, and column-filter behavior (today often forced `False`).

---

## 2. Expenses-style operational list

**Canonical:** `/workspace/src/AlMuhasib.UI/Views/ExpenseView.xaml` (class `ExpenseView`, not `ExpensesView`)

1. Top: form / side panel cards (`ElevatedCard`)
2. Mid: **filter row** in `ElevatedCard` — type/cashbox/date range/search + Search/Clear icon buttons
3. Bottom table: `ListTableHeaderBar` → grid → optional totals strip → `PaginationBar`

Use for Gold operational registers (vouchers, collections, adjustments) that need filters + totals, not only CRUD.

---

## 3. Statement / aging reports

**Canonical statement:** `/workspace/src/AlMuhasib.UI/Views/CustomerStatementView.xaml`  
**Canonical aging:** `/workspace/src/AlMuhasib.UI/Views/ReceivablesAgingReportView.xaml` (also `PayablesAgingReportView`, `InstallmentAgingReportView`)  
**VM base:** `/workspace/src/AlMuhasib.UI/ViewModels/ReportViewModelBase.cs`  
**Gold stub to upgrade:** `/workspace/src/AlMuhasib.UI/Views/Gold/GoldCustomerStatementView.xaml`

Report skeleton:

1. Title (`FontSize="24"` Bold)
2. `UniformGrid` of `AnimatedStatCard`
3. Optional `ChartCard` (aging)
4. `Border Style="{StaticResource FilterBar}"` — party combo + dates + `LoadDataCommand` / بحث
5. Card → `ListTableHeaderBar` (Export/Print, usually `ShowCardToggle="False"`) → grid + filters → `PaginationBar`
6. Busy overlay spanning page

---

## 4. Print invoice (sales)

**Runtime API is `IExportService`, not `IPrintService`.**  
`IPrintService` (`/workspace/src/AlMuhasib.Core/Interfaces/Services/IPrintService.cs`) is a thin contract; UI print path uses export.

| Role | Path |
|------|------|
| Contract + `InvoicePrintModel` | `/workspace/src/AlMuhasib.Core/Interfaces/Services/IExportService.cs` |
| Implementation | `/workspace/src/AlMuhasib.Shared/Services/ExcelExportService.cs` (`PrintInvoice`) |
| Preview helper | `/workspace/src/AlMuhasib.Shared/Services/DocumentPrintHelper.cs` |
| Branding | `/workspace/src/AlMuhasib.Shared/Services/PrintBrandingFlowDocumentHelper.cs`, `IPrintBrandingService` |
| Sales VM | `/workspace/src/AlMuhasib.UI/ViewModels/SalesInvoiceViewModel.cs` — build `InvoicePrintModel` → `_exportService.PrintInvoice(model)` (+ warehouse copy when needed) |
| Sales UI button | `/workspace/src/AlMuhasib.UI/Views/SalesInvoiceView.xaml` |
| Thermal POS | `PrintThermalReceipt` + `/workspace/src/AlMuhasib.Shared/Services/PosReceiptDocumentBuilder.cs` |

Gold sale/purchase invoices should follow the same: map saved invoice → `InvoicePrintModel` → `IExportService.PrintInvoice` (RTL FlowDocument + branding).

---

## 5. Installment flow (key screens only)

Only if Gold Phase 2 adds installment-like credit/plan UX:

| Screen | Path |
|--------|------|
| Create installment invoice | `/workspace/src/AlMuhasib.UI/Views/InstallmentInvoiceView.xaml` (+ VM + `.Queue` / `.Drafts` / `.Contract` partials) |
| Plans / pay / print plan | `/workspace/src/AlMuhasib.UI/Views/InstallmentsView.xaml` |
| Aging | `/workspace/src/AlMuhasib.UI/Views/InstallmentAgingReportView.xaml` |

Invoice overlays to reuse:  
`ProductPickerOverlay`, `InvoiceSearchSidePanel` (same ZIndex pattern as sales/installment invoices).  
Print extras on `IExportService`: schedule, contract PDF, plan detail, payment receipt.

---

## 6. Dashboard smart alerts / daily tasks

**Accounting pattern:**

| Piece | Path |
|-------|------|
| UI panels | `/workspace/src/AlMuhasib.UI/Views/DashboardView.xaml` — `DailyTasks` + `SmartAlerts` side-by-side |
| VM | `/workspace/src/AlMuhasib.UI/ViewModels/DashboardViewModel.cs` — load `ISmartAlertService.GetSummaryAsync()`, `ExecuteDailyTaskCommand` → MainWindow navigation |
| Models | `/workspace/src/AlMuhasib.Core/Models/Ux/SmartAlertModels.cs` (`SmartAlert`, `DailyTaskItem`, `SmartAlertAction`) |
| Service | `/workspace/src/AlMuhasib.Infrastructure/Services/SmartAlertService.cs` |
| Notification bridge | `/workspace/src/AlMuhasib.UI/Services/NotificationCenterService.cs` |

**Gold today:** alerts-only card on `/workspace/src/AlMuhasib.UI/Views/Gold/GoldDashboardView.xaml` via `IGoldSmartAlertService` / `GoldSmartAlertService` + `GoldNotificationsView`.

**Phase 2 expansion:** add a **Daily tasks** column (title/description/priority → navigate), keep clickable alerts with actions (mirror `SmartAlertAction` → screen open), reuse `DashboardKpiCard` / existing gold accent — do not invent a second alert pipeline.

---

## Phase 2 UI checklist (copy/paste)

- [ ] `PageStyle` + explicit `FlowDirection="RightToLeft"`
- [ ] List: action bar → `ListTableHeaderBar` → `ModernDataGrid` → `PaginationBar`
- [ ] Enable header Filter/Export/Print + `DataGridColumnFilterBehavior` when data supports it
- [ ] CRUD: `DialogHost` add/edit + delete confirm (Suppliers / GoldCustomers)
- [ ] Filters: `FilterBar` or Expense-style filter card; Search + Clear
- [ ] Reports: stats (`AnimatedStatCard`) → filters → header bar → grid → pagination → busy overlay
- [ ] Print: `InvoicePrintModel` + `IExportService.PrintInvoice` (+ branding helpers)
- [ ] Invoice chrome: `ProductPickerOverlay` / `InvoiceSearchSidePanel` if picker/search needed
- [ ] Dashboard: actionable alerts + daily tasks (Accounting `DashboardView` layout), fed by Gold smart-alert service
- [ ] Prefer Theme styles / shared controls; keep Gold accent only as brand color, not new layout system
