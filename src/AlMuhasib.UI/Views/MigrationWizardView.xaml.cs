using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using AlMuhasib.UI.Converters;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.ViewModels;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Views;

public partial class MigrationWizardView
{
    private MigrationWizardViewModel? _vm;
    private int _lastTransitionToken;
    private int _lastProductsGridVersion = -1;

    public MigrationWizardView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MigrationWizardViewModel vm && !ReferenceEquals(_vm, vm))
        {
            if (_vm is not null)
                _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm = vm;
            _vm.PropertyChanged += OnVmPropertyChanged;
            _vm.RequestCommitGridEdits = CommitPendingGridEdits;
            ApplyColumnVisibility();
            RebuildProductsGridColumns();
        }

        PageEntranceAnimator.AnimateFadeSlide(HeroHeader, 0, axisY: true, from: 12);
        PageEntranceAnimator.AnimateFadeSlide(StepContentScroller, 120, axisY: false, from: -16);
        TryAnimateStepEntrance(force: true);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
            _vm.PropertyChanged -= OnVmPropertyChanged;

        _vm = e.NewValue as MigrationWizardViewModel;
        if (_vm is not null)
        {
            _vm.PropertyChanged += OnVmPropertyChanged;
            _vm.RequestCommitGridEdits = CommitPendingGridEdits;
        }

        ApplyColumnVisibility();
        RebuildProductsGridColumns();
    }

    private void CommitPendingGridEdits()
    {
        CommitGrid(PreviewGrid);
        CommitGrid(ProductsGrid);
    }

    private static void CommitGrid(System.Windows.Controls.DataGrid? grid)
    {
        if (grid is null) return;
        try
        {
            grid.CommitEdit(DataGridEditingUnit.Cell, true);
            grid.CommitEdit(DataGridEditingUnit.Row, true);
        }
        catch
        {
            // أفضل جهد — لا تمنع الحفظ
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MigrationWizardViewModel.StepTransitionToken))
            TryAnimateStepEntrance(force: false);
        else if (e.PropertyName is nameof(MigrationWizardViewModel.ProductsGridVersion)
                 or nameof(MigrationWizardViewModel.ShowProductsGrid)
                 or nameof(MigrationWizardViewModel.EnableMultiCurrency)
                 or nameof(MigrationWizardViewModel.ShowProductPharmacyFields)
                 or nameof(MigrationWizardViewModel.ShowProductCarFields)
                 or nameof(MigrationWizardViewModel.ShowProductWeightFields)
                 or nameof(MigrationWizardViewModel.ShowProductDiscountFields)
                 or nameof(MigrationWizardViewModel.ShowProductPurchasePriceColumns))
            RebuildProductsGridColumns();
        else if (e.PropertyName is null
                 || e.PropertyName.StartsWith("ShowCol", StringComparison.Ordinal)
                 || e.PropertyName is nameof(MigrationWizardViewModel.ShowUsdFields)
                     or nameof(MigrationWizardViewModel.HasRowErrors)
                     or nameof(MigrationWizardViewModel.ColNameHeader)
                     or nameof(MigrationWizardViewModel.IsInstallmentsStep)
                     or nameof(MigrationWizardViewModel.IsCustomersStep)
                     or nameof(MigrationWizardViewModel.IsSuppliersStep)
                     or nameof(MigrationWizardViewModel.IsWarehousesStep)
                     or nameof(MigrationWizardViewModel.IsCashStep))
            ApplyColumnVisibility();
    }

    private void TryAnimateStepEntrance(bool force)
    {
        if (_vm is null || StepContentPanel is null)
            return;

        if (!force && _vm.StepTransitionToken == _lastTransitionToken)
            return;

        _lastTransitionToken = _vm.StepTransitionToken;
        PageEntranceAnimator.AnimateStepTransition(StepContentPanel, slideFromRight: true);
    }

    private void ApplyColumnVisibility()
    {
        if (_vm is null)
            return;

        SetCol(ColCapitalUsd, _vm.ShowUsdFields);
        SetCol(ColProfitUsd, _vm.ShowUsdFields);

        if (ColName is null)
            return;

        SetCol(ColName, _vm.ShowColName);
        SetCol(ColBranch, _vm.ShowColBranch);
        SetCol(ColKind, _vm.ShowColKind);
        SetCol(ColCategory, _vm.ShowColCategory);
        SetCol(ColWarehouse, _vm.ShowColWarehouse);
        SetCol(ColPhone, _vm.ShowColPhone);
        SetCol(ColFileNumber, _vm.ShowColFileNumber);
        SetCol(ColBarcode, _vm.ShowColBarcode);
        SetCol(ColAccountNumber, _vm.ShowColAccountNumber);
        SetCol(ColAmount, _vm.ShowColAmount);
        SetCol(ColAmountUsd, _vm.ShowColAmountUsd);
        SetCol(ColProfitPercent, _vm.ShowColProfitPercent);
        SetCol(ColQuantity, _vm.ShowColQuantity);
        SetCol(ColCost, _vm.ShowColCost);
        SetCol(ColCurrency, _vm.ShowColCurrency);
        SetCol(ColFxRate, _vm.ShowColFxRate);
        SetCol(ColPrices, _vm.ShowColPrices);
        SetCol(ColInstallments, _vm.ShowColInstallments);
        SetCol(ColPaidInstallments, _vm.ShowColPaidInstallments);
        SetCol(ColDate, _vm.ShowColDate);
        SetCol(ColNotes, _vm.ShowColNotes);

        var hasErrors = _vm.HasRowErrors;
        SetCol(ColError, hasErrors);

        if (ColNameHeaderText is not null)
            ColNameHeaderText.Text = _vm.ColNameHeader;

        if (ColAmount is not null)
            ColAmount.Header = _vm.IsInstallmentsStep ? "المبلغ الكلي د.ع" : "رصيد د.ع";
        if (ColNotes is not null)
            ColNotes.Header = _vm.IsWarehousesStep ? "الموقع" : "ملاحظة";
        if (ColDateHeaderText is not null)
        {
            ColDateHeaderText.Text = _vm.IsInstallmentsStep
                ? "تاريخ بداية الأقساط"
                : _vm.IsCustomersStep || _vm.IsSuppliersStep
                    ? "تاريخ الرصيد الافتتاحي"
                    : "التاريخ";
        }
    }

    private void RebuildProductsGridColumns()
    {
        if (_vm is null || ProductsGrid is null)
            return;

        if (!_vm.ShowProductsGrid)
            return;

        if (_vm.ProductsGridVersion == _lastProductsGridVersion && ProductsGrid.Columns.Count > 0)
            return;

        _lastProductsGridVersion = _vm.ProductsGridVersion;
        ProductsGrid.Columns.Clear();

        ProductsGrid.Columns.Add(CreateTextColumn("اسم المنتج", "Name", 200, minWidth: 160));
        ProductsGrid.Columns.Add(CreateTextColumn("الباركود", "Barcode", 140, minWidth: 110));
        ProductsGrid.Columns.Add(CreateCategoryComboColumn());
        ProductsGrid.Columns.Add(CreateTextColumn("الوصف", "Description", 160, minWidth: 120));

        if (_vm.ShowProductPharmacyFields)
        {
            ProductsGrid.Columns.Add(CreateTextColumn("الاسم العلمي", "ScientificName", 160, minWidth: 130));
            ProductsGrid.Columns.Add(CreateTextColumn("طريقة الاستخدام", "UsageInstructions", 170, minWidth: 140));
        }

        if (_vm.ShowProductCarFields)
        {
            ProductsGrid.Columns.Add(CreateTextColumn("نوع السيارة", "VehicleType", 130, minWidth: 110));
            ProductsGrid.Columns.Add(CreateTextColumn("الصنف / الشاصي", "ChassisNumber", 140, minWidth: 120));
            ProductsGrid.Columns.Add(CreateTextColumn("الموديل", "CarModel", 120, minWidth: 100));
            ProductsGrid.Columns.Add(CreateTextColumn("اللون", "VehicleColor", 110, minWidth: 90));
            ProductsGrid.Columns.Add(CreateTextColumn("الركاب", "PassengerCountText", 90, minWidth: 70));
            ProductsGrid.Columns.Add(CreateTextColumn("رقم اللوحة", "PlateNumber", 120, minWidth: 100));
            ProductsGrid.Columns.Add(CreateComboColumn(
                "نوع اللوحة", "PlateTypeText", "PlateTypeOptions", 130, minWidth: 110));
        }

        if (_vm.ShowProductWeightFields)
        {
            ProductsGrid.Columns.Add(CreateTextColumn("الوزن", "Weight", 100, minWidth: 80, stringFormat: "N2"));
            ProductsGrid.Columns.Add(CreateComboColumn(
                "وحدة الوزن", "WeightUnit", "WeightUnitOptions", 110, minWidth: 90));
        }

        if (_vm.ShowProductDiscountFields)
        {
            ProductsGrid.Columns.Add(CreateComboColumn(
                "نوع الخصم", "DiscountTypeText", "DiscountTypeOptions", 130, minWidth: 110));
            ProductsGrid.Columns.Add(CreateTextColumn("قيمة الخصم", "DiscountValue", 110, minWidth: 90, stringFormat: "N0"));
            ProductsGrid.Columns.Add(CreateTextColumn("انتهاء الخصم", "DiscountExpiresText", 130, minWidth: 110));
        }

        ProductsGrid.Columns.Add(CreateTextColumn(
            "تكلفة\n(دينار)",
            "UnitCost",
            130,
            minWidth: 110,
            stringFormat: "N0",
            toolTip: "تكلفة الوحدة بالدينار العراقي",
            isUsd: false));

        if (_vm.EnableMultiCurrency)
        {
            ProductsGrid.Columns.Add(CreateTextColumn(
                "تكلفة\n(دولار)",
                "UnitCostUsd",
                130,
                minWidth: 110,
                stringFormat: "N2",
                toolTip: "تكلفة الوحدة بالدولار الأمريكي",
                isUsd: true));
        }

        var warehouses = _vm.GetProductWarehouseNames();
        if (warehouses.Count == 0)
        {
            ProductsGrid.Columns.Add(CreateTextColumn(
                "كمية\n(أضف مخازن أولاً)",
                "Quantity",
                150,
                minWidth: 130,
                stringFormat: "N0",
                toolTip: "لا توجد مخازن بعد — ارجع لخطوة المخازن وأضف مخزناً واحداً على الأقل"));
        }
        else
        {
            for (var i = 0; i < warehouses.Count; i++)
            {
                var warehouseDisplay = warehouses[i];
                var shortName = ShortWarehouseHeader(warehouseDisplay);
                if (string.IsNullOrWhiteSpace(shortName))
                    shortName = $"مخزن {i + 1}";

                ProductsGrid.Columns.Add(CreateTextColumn(
                    header: null,
                    bindingPath: $"WarehouseQtys[{i}].Quantity",
                    width: 150,
                    minWidth: 120,
                    stringFormat: "N0",
                    toolTip: $"الكمية الافتتاحية في مخزن: {warehouseDisplay}",
                    headerElement: CreateWarehouseQtyHeader(shortName, warehouseDisplay)));
            }
        }

        var pricing = _vm.GetProductPricingTypeNames();
        for (var i = 0; i < pricing.Count; i++)
        {
            var typeName = pricing[i];
            ProductsGrid.Columns.Add(CreateTextColumn(
                $"بيع {typeName}\n(دينار)",
                $"ProductPrices[{i}].SalePrice",
                150,
                minWidth: 130,
                stringFormat: "N0",
                toolTip: $"سعر البيع «{typeName}» بالدينار",
                isUsd: false));

            if (_vm.EnableMultiCurrency)
            {
                ProductsGrid.Columns.Add(CreateTextColumn(
                    $"بيع {typeName}\n(دولار)",
                    $"ProductPrices[{i}].SalePriceUsd",
                    150,
                    minWidth: 130,
                    stringFormat: "N2",
                    toolTip: $"سعر البيع «{typeName}» بالدولار",
                    isUsd: true));
            }

            if (_vm.ShowProductPurchasePriceColumns)
            {
                ProductsGrid.Columns.Add(CreateTextColumn(
                    $"شراء {typeName}\n(دينار)",
                    $"ProductPrices[{i}].PurchasePrice",
                    150,
                    minWidth: 130,
                    stringFormat: "N0",
                    toolTip: $"سعر الشراء «{typeName}» بالدينار",
                    isUsd: false));

                if (_vm.EnableMultiCurrency)
                {
                    ProductsGrid.Columns.Add(CreateTextColumn(
                        $"شراء {typeName}\n(دولار)",
                        $"ProductPrices[{i}].PurchasePriceUsd",
                        150,
                        minWidth: 130,
                        stringFormat: "N2",
                        toolTip: $"سعر الشراء «{typeName}» بالدولار",
                        isUsd: true));
                }
            }
        }
    }

    private System.Windows.Controls.DataGridTextColumn CreateTextColumn(
        string? header,
        string bindingPath,
        double width,
        double minWidth = 80,
        string? stringFormat = null,
        string? toolTip = null,
        bool? isUsd = null,
        FrameworkElement? headerElement = null)
    {
        var binding = new Binding(bindingPath)
        {
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
            Mode = BindingMode.TwoWay
        };
        if (!string.IsNullOrEmpty(stringFormat))
        {
            binding.Converter = new DecimalThousandsConverter();
            binding.ConverterParameter = stringFormat;
        }

        var col = new System.Windows.Controls.DataGridTextColumn
        {
            Header = headerElement ?? CreateHeader(header ?? string.Empty, isUsd),
            Binding = binding,
            Width = new DataGridLength(width),
            MinWidth = minWidth
        };

        if (!string.IsNullOrEmpty(stringFormat))
        {
            var styleKey = stringFormat.StartsWith("N2", StringComparison.OrdinalIgnoreCase)
                ? "WizardAmountEditN2"
                : "WizardAmountEditN0";
            if (TryFindResource(styleKey) is Style editStyle)
                col.EditingElementStyle = editStyle;
        }

        if (!string.IsNullOrWhiteSpace(toolTip))
            col.HeaderStyle = CreateHeaderStyle(toolTip);
        return col;
    }

    /// <summary>رأس عمود كمية يظهر اسم المخزن بوضوح (سطرين) — ألوان متوافقة مع الوضع الداكن/الفاتح.</summary>
    private FrameworkElement CreateWarehouseQtyHeader(string warehouseName, string fullDisplayName)
    {
        var qtyLabel = new TextBlock
        {
            Text = "كمية",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            Opacity = 0.85
        };
        qtyLabel.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryHueLightForegroundBrush");

        var nameLabel = new TextBlock
        {
            Text = warehouseName,
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            ToolTip = fullDisplayName,
            Margin = new Thickness(0, 2, 0, 0)
        };
        nameLabel.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryHueLightForegroundBrush");

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(2)
        };
        stack.Children.Add(qtyLabel);
        stack.Children.Add(nameLabel);

        var border = new Border
        {
            Child = stack,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(1),
            ToolTip = $"الكمية الافتتاحية — {fullDisplayName}"
        };
        border.SetResourceReference(Border.BackgroundProperty, "PrimaryHueLightBrush");
        return border;
    }

    private static Style CreateHeaderStyle(string toolTip)
    {
        var style = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
        style.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, toolTip));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(4, 6, 4, 6)));
        return style;
    }

    private System.Windows.Controls.DataGridTemplateColumn CreateCategoryComboColumn()
    {
        var col = new System.Windows.Controls.DataGridTemplateColumn
        {
            Header = CreateHeader("التصنيف"),
            Width = new DataGridLength(220),
            MinWidth = 180
        };

        var factory = new FrameworkElementFactory(typeof(ComboBox));
        factory.SetValue(ComboBox.IsEditableProperty, true);
        factory.SetValue(ComboBox.IsTextSearchEnabledProperty, true);
        factory.SetValue(ComboBox.StaysOpenOnEditProperty, true);
        factory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        factory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        factory.SetValue(FrameworkElement.MarginProperty, new Thickness(2));
        factory.SetValue(Control.PaddingProperty, new Thickness(10, 6, 10, 6));
        factory.SetValue(FrameworkElement.MinHeightProperty, 36.0);
        factory.SetValue(Control.FontSizeProperty, 13.5);
        factory.SetValue(HintAssist.HintProperty, "اختر تصنيفاً");
        factory.SetValue(FrameworkElement.ToolTipProperty, "اختر تصنيفاً من القائمة أو اكتب اسماً جديداً");
        factory.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("DataContext.AvailableCategories")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1)
        });
        factory.SetBinding(ComboBox.TextProperty, new Binding("CategoryName")
        {
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Mode = BindingMode.TwoWay
        });

        col.CellTemplate = new DataTemplate { VisualTree = factory };
        return col;
    }

    private System.Windows.Controls.DataGridTemplateColumn CreateComboColumn(
        string header,
        string textBindingPath,
        string optionsPropertyName,
        double width,
        double minWidth = 100)
    {
        var col = new System.Windows.Controls.DataGridTemplateColumn
        {
            Header = CreateHeader(header),
            Width = new DataGridLength(width),
            MinWidth = minWidth
        };

        var factory = new FrameworkElementFactory(typeof(ComboBox));
        factory.SetValue(ComboBox.IsEditableProperty, true);
        factory.SetValue(ComboBox.IsTextSearchEnabledProperty, true);
        factory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        factory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        factory.SetValue(FrameworkElement.MarginProperty, new Thickness(2));
        factory.SetValue(Control.PaddingProperty, new Thickness(8, 4, 8, 4));
        factory.SetValue(FrameworkElement.MinHeightProperty, 34.0);
        factory.SetBinding(ItemsControl.ItemsSourceProperty, new Binding($"DataContext.{optionsPropertyName}")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1)
        });
        factory.SetBinding(ComboBox.TextProperty, new Binding(textBindingPath)
        {
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Mode = BindingMode.TwoWay
        });

        col.CellTemplate = new DataTemplate { VisualTree = factory };
        return col;
    }

    private FrameworkElement CreateHeader(string text, bool? isUsd = null)
    {
        var block = new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(4, 2, 4, 2),
            VerticalAlignment = VerticalAlignment.Center
        };

        if (isUsd is null)
            return block;

        // ألوان الثيم بدل الثوابت الفاتحة حتى يبقى النص واضحاً في الوضع الداكن
        block.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryHueLightForegroundBrush");

        var border = new Border
        {
            Child = block,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(4, 2, 4, 2),
            Margin = new Thickness(1)
        };
        border.SetResourceReference(Border.BackgroundProperty, "PrimaryHueLightBrush");
        if (isUsd.Value)
            border.Opacity = 0.95;
        return border;
    }

    private static string ShortWarehouseHeader(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return string.Empty;
        var paren = displayName.LastIndexOf(" (", StringComparison.Ordinal);
        return paren > 0 ? displayName[..paren].Trim() : displayName.Trim();
    }

    private static void SetCol(System.Windows.Controls.DataGridColumn? column, bool visible)
    {
        if (column is null) return;
        column.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }
}
