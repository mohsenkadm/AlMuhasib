using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using AlMuhasib.Shared.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductsViewModel : ViewModelBase
{
    private readonly IProductService _productService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthService _authService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBarcodeLabelService _barcodeLabelService;
    private readonly IUserPreferencesService _userPreferences;

    // ── Collections ────────────────────────────────────────
    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];

    // ── Filter / Search ────────────────────────────────────
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private Category? _selectedCategory;

    // ── Pagination ─────────────────────────────────────────
    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 20;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private string _paginationText = string.Empty;

    // ── Selected item ──────────────────────────────────────
    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private bool _isCardView;

    // ── Dialog state ───────────────────────────────────────
    [ObservableProperty]
    private bool _isDialogOpen;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _dialogTitle = string.Empty;

    // ── Dialog form fields ─────────────────────────────────
    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    [ObservableProperty]
    private string _editBarcode = string.Empty;

    [ObservableProperty]
    private Category? _editCategory;

    [ObservableProperty]
    private string _dialogError = string.Empty;

    // ── Delete confirmation ────────────────────────────────
    [ObservableProperty]
    private bool _isDeleteDialogOpen;

    [ObservableProperty]
    private Product? _productToDelete;

    private int? _editingProductId;
    private System.Timers.Timer? _debounceTimer;

    public ProductsViewModel(
        IProductService productService,
        IUnitOfWork unitOfWork,
        IAuthService authService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IBarcodeLabelService barcodeLabelService,
        IUserPreferencesService userPreferences)
    {
        _productService = productService;
        _unitOfWork = unitOfWork;
        _authService = authService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _barcodeLabelService = barcodeLabelService;
        _userPreferences = userPreferences;
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.Products);

        PageTitle = "المنتجات";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            LoadPermissions(_currentUserService, "Products");
            await LoadCategoriesAsync();
            await LoadProductsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Category loading ───────────────────────────────────
    private async Task LoadCategoriesAsync()
    {
        var categories = await _unitOfWork.Categories.GetAllAsync();
        Categories.Clear();
        foreach (var c in categories)
            Categories.Add(c);
    }

    // ── Product loading ────────────────────────────────────
    private async Task LoadProductsAsync()
    {
        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
        {
            var (allItems, _) = await _productService.GetPagedAsync(
                1, int.MaxValue,
                SelectedCategory?.Id,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim());

            var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
            MasterDataColumnFilterHelper.ApplyClientPagination(
                filtered, Products, CurrentPage, PageSize,
                out var filteredTotal, out var filteredPages, out var filteredText);
            TotalCount = filteredTotal;
            TotalPages = filteredPages;
            PaginationText = filteredText;
            return;
        }

        var (items, totalCount) = await _productService.GetPagedAsync(
            CurrentPage,
            PageSize,
            SelectedCategory?.Id,
            string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim());

        TotalCount = totalCount;
        TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
        PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

        Products.Clear();
        foreach (var p in items)
            Products.Add(p);
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        _ = ReloadAsync();
    }

    // ── Search with debounce ───────────────────────────────
    partial void OnSearchTextChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(400);
        _debounceTimer.Elapsed += async (_, _) =>
        {
            _debounceTimer?.Stop();
            CurrentPage = 1;
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadProductsAsync();
            });
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        CurrentPage = 1;
        _ = ReloadAsync();
    }

    // ── Pagination commands ────────────────────────────────
    [RelayCommand]
    private async Task FirstPage()
    {
        CurrentPage = 1;
        await LoadProductsAsync();
    }

    [RelayCommand]
    private async Task PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadProductsAsync();
        }
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadProductsAsync();
        }
    }

    [RelayCommand]
    private async Task LastPage()
    {
        CurrentPage = TotalPages;
        await LoadProductsAsync();
    }

    // ── Refresh ────────────────────────────────────────────
    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        SearchText = string.Empty;
        SelectedCategory = null;
        await LoadProductsAsync();
    }

    private async Task ReloadAsync()
    {
        await LoadProductsAsync();
    }

    // ── Add / Edit Dialog ──────────────────────────────────
    [RelayCommand]
    private void OpenAddDialog()
    {
        _editingProductId = null;
        IsEditMode = false;
        DialogTitle = "إضافة منتج جديد";
        EditName = string.Empty;
        EditDescription = string.Empty;
        EditBarcode = string.Empty;
        EditCategory = null;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Product product)
    {
        if (product is null) return;

        _editingProductId = product.Id;
        IsEditMode = true;
        DialogTitle = "تعديل المنتج";
        EditName = product.Name;
        EditDescription = product.Description ?? string.Empty;
        EditBarcode = product.Barcode ?? string.Empty;
        EditCategory = Categories.FirstOrDefault(c => c.Id == product.CategoryId);
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveProduct()
    {
        // Validation
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم المنتج مطلوب";
            return;
        }
        if (EditCategory is null)
        {
            DialogError = "يرجى اختيار الصنف";
            return;
        }

        DialogError = string.Empty;

        try
        {
            if (IsEditMode && _editingProductId.HasValue)
            {
                var product = await _productService.GetByIdAsync(_editingProductId.Value);
                if (product is null) return;

                product.Name = EditName.Trim();
                product.Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim();
                product.Barcode = string.IsNullOrWhiteSpace(EditBarcode) ? null : EditBarcode.Trim();
                product.CategoryId = EditCategory.Id;

                await _productService.UpdateAsync(product);
            }
            else
            {
                var product = new Product
                {
                    Name = EditName.Trim(),
                    Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim(),
                    Barcode = string.IsNullOrWhiteSpace(EditBarcode) ? null : EditBarcode.Trim(),
                    CategoryId = EditCategory.Id
                };

                await _productService.CreateAsync(product);
            }

            IsDialogOpen = false;
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            DialogError = $"حدث خطأ: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CancelDialog()
    {
        IsDialogOpen = false;
    }

    // ── Delete ─────────────────────────────────────────────
    [RelayCommand]
    private void ConfirmDelete(Product product)
    {
        if (product is null) return;
        ProductToDelete = product;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private async Task ExecuteDelete()
    {
        if (ProductToDelete is null) return;

        try
        {
            await _productService.DeleteAsync(ProductToDelete.Id);
            IsDeleteDialogOpen = false;
            ProductToDelete = null;
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الحذف: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        ProductToDelete = null;
    }

    // ── Export to Excel ────────────────────────────────────
    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            // Load all matching products (not just current page)
            var (allItems, _) = await _productService.GetPagedAsync(
                1, int.MaxValue,
                SelectedCategory?.Id,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim());

            var exportData = allItems.Select(p => new
            {
                الاسم = p.Name,
                الصنف = p.Category?.Name ?? "",
                الباركود = p.Barcode ?? "",
                الوصف = p.Description ?? "",
                تاريخ_الإنشاء = p.CreatedAt.ToString("yyyy/MM/dd")
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"المنتجات_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "المنتجات");
                BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء التصدير: {ex.Message}");
        }
    }

    // ── Print ──────────────────────────────────────────────
    [RelayCommand]
    private async Task Print()
    {
        try
        {
            var (allItems, _) = await _productService.GetPagedAsync(
                1, int.MaxValue,
                SelectedCategory?.Id,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim());

            var printData = allItems.ToList();

            // Build FlowDocument
            var document = new System.Windows.Documents.FlowDocument
            {
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FlowDirection = FlowDirection.RightToLeft,
                PageWidth = 793,
                PagePadding = new Thickness(50),
                ColumnWidth = double.MaxValue
            };

            PrintBrandingFlowDocumentHelper.PrependBrandingHeader(document);

            // Title
            var title = new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run("قائمة المنتجات"))
            {
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            document.Blocks.Add(title);

            // Date
            var dateParagraph = new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run($"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd hh:mm tt}"))
            {
                FontSize = 11,
                TextAlignment = TextAlignment.Left,
                Margin = new Thickness(0, 0, 0, 15)
            };
            document.Blocks.Add(dateParagraph);

            // Table
            var table = new System.Windows.Documents.Table();
            table.CellSpacing = 0;

            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(50) });   // #
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(200) });  // Name
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(120) });  // Category
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(120) });  // Barcode
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(200) });  // Description

            // Header row
            var headerGroup = new System.Windows.Documents.TableRowGroup();
            var headerRow = new System.Windows.Documents.TableRow();
            headerRow.Background = System.Windows.Media.Brushes.DarkSlateBlue;
            headerRow.Foreground = System.Windows.Media.Brushes.White;

            foreach (var h in new[] { "#", "الاسم", "الصنف", "الباركود", "الوصف" })
            {
                var cell = new System.Windows.Documents.TableCell(
                    new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(h))
                    { FontWeight = FontWeights.Bold, FontSize = 12 });
                cell.Padding = new Thickness(6, 4, 6, 4);
                cell.BorderThickness = new Thickness(0.5);
                cell.BorderBrush = System.Windows.Media.Brushes.Gray;
                headerRow.Cells.Add(cell);
            }
            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            // Data rows
            var dataGroup = new System.Windows.Documents.TableRowGroup();
            int index = 1;
            foreach (var p in printData)
            {
                var row = new System.Windows.Documents.TableRow();
                if (index % 2 == 0)
                    row.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(245, 245, 245));

                var values = new[] { index.ToString(), p.Name, p.Category?.Name ?? "", p.Barcode ?? "", p.Description ?? "" };
                foreach (var v in values)
                {
                    var cell = new System.Windows.Documents.TableCell(
                        new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(v))
                        { FontSize = 11 });
                    cell.Padding = new Thickness(6, 3, 6, 3);
                    cell.BorderThickness = new Thickness(0.5);
                    cell.BorderBrush = System.Windows.Media.Brushes.LightGray;
                    row.Cells.Add(cell);
                }
                dataGroup.Rows.Add(row);
                index++;
            }
            table.RowGroups.Add(dataGroup);
            document.Blocks.Add(table);

            // Total count
            var totalParagraph = new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run($"إجمالي المنتجات: {printData.Count}"))
            {
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 15, 0, 0)
            };
            document.Blocks.Add(totalParagraph);

            PrintBrandingFlowDocumentHelper.AppendBrandingFooter(document, systemLine: $"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}");

            DocumentPrintHelper.PrintWithPreview(document, "طباعة المنتجات");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.Products, value);
}
