using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class BulkProductsEntryView
{
    private readonly List<DataGridColumn> _dynamicColumns = [];
    private int _lastGridColumnsVersion = -1;

    public BulkProductsEntryView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) =>
        {
            SyncColumns();
            RebuildDynamicColumns();
        };
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is BulkProductsEntryViewModel oldVm)
            oldVm.PropertyChanged -= OnVmPropertyChanged;
        if (e.NewValue is BulkProductsEntryViewModel newVm)
            newVm.PropertyChanged += OnVmPropertyChanged;
        SyncColumns();
        RebuildDynamicColumns(force: true);
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BulkProductsEntryViewModel.GridColumnsVersion)
            or nameof(BulkProductsEntryViewModel.ShowPricingFields)
            or nameof(BulkProductsEntryViewModel.ShowMultiCurrency))
        {
            RebuildDynamicColumns(force: true);
        }

        if (e.PropertyName is null
            || e.PropertyName.StartsWith("Show", StringComparison.Ordinal)
            || e.PropertyName.Contains("CustomField", StringComparison.Ordinal))
        {
            SyncColumns();
        }
    }

    private void SyncColumns()
    {
        if (DataContext is not BulkProductsEntryViewModel vm)
            return;

        SetColumn("ColPharmacyScientific", vm.ShowPharmacyFields);
        SetColumn("ColPharmacyUsage", vm.ShowPharmacyFields);
        SetColumn("ColVehicleType", vm.ShowCarShowroomFields);
        SetColumn("ColChassisNumber", vm.ShowCarShowroomFields);
        SetColumn("ColCarModel", vm.ShowCarShowroomFields);
        SetColumn("ColVehicleColor", vm.ShowCarShowroomFields);
        SetColumn("ColPassengerCount", vm.ShowCarShowroomFields);
        SetColumn("ColPlateNumber", vm.ShowCarShowroomFields);
        SetColumn("ColPlateType", vm.ShowCarShowroomFields);
        SetColumn("ColWeight", vm.ShowWeightFields);
        SetColumn("ColWeightUnit", vm.ShowWeightFields);
        SetColumn("ColDiscountType", vm.ShowDiscountFields);
        SetColumn("ColDiscountValue", vm.ShowDiscountFields);
        SetColumn("ColDiscountExpires", vm.ShowDiscountFields);

        SetNamedColumn("ColCf1", vm.ShowCustomField1, vm.CustomField1Header);
        SetNamedColumn("ColCf2", vm.ShowCustomField2, vm.CustomField2Header);
        SetNamedColumn("ColCf3", vm.ShowCustomField3, vm.CustomField3Header);
        SetNamedColumn("ColCf4", vm.ShowCustomField4, vm.CustomField4Header);
        SetNamedColumn("ColCf5", vm.ShowCustomField5, vm.CustomField5Header);
        SetNamedColumn("ColCf6", vm.ShowCustomField6, vm.CustomField6Header);
        SetNamedColumn("ColCf7", vm.ShowCustomField7, vm.CustomField7Header);
        SetNamedColumn("ColCf8", vm.ShowCustomField8, vm.CustomField8Header);
    }

    private void RebuildDynamicColumns(bool force = false)
    {
        if (DataContext is not BulkProductsEntryViewModel vm || BulkGrid is null)
            return;

        if (!force && vm.GridColumnsVersion == _lastGridColumnsVersion && _dynamicColumns.Count > 0)
            return;

        _lastGridColumnsVersion = vm.GridColumnsVersion;

        foreach (var col in _dynamicColumns)
            BulkGrid.Columns.Remove(col);
        _dynamicColumns.Clear();

        // إدراج بعد عمود الوصف (الفهرس 3) وقبل أعمدة الميزات
        var insertAt = Math.Min(4, BulkGrid.Columns.Count);

        var warehouses = vm.GetWarehouseNames();
        for (var i = 0; i < warehouses.Count; i++)
        {
            var name = warehouses[i];
            InsertDynamic(CreateTextColumn(
                header: null,
                $"WarehouseQtys[{i}].Quantity",
                120,
                minWidth: 100,
                stringFormat: "N0",
                toolTip: $"كمية الرصيد الافتتاحي لمخزن «{name}»",
                headerElement: CreateWarehouseQtyHeader(name)), insertAt++);
        }

        var pricing = vm.GetPricingTypeNames();
        for (var i = 0; i < pricing.Count; i++)
        {
            var typeName = pricing[i];

            InsertDynamic(CreateTextColumn(
                $"بيع {typeName}\n(دينار)",
                $"ProductPrices[{i}].SalePrice",
                140,
                minWidth: 120,
                stringFormat: "N0",
                toolTip: $"سعر البيع «{typeName}» بالدينار",
                isUsd: false), insertAt++);

            if (vm.ShowMultiCurrency)
            {
                InsertDynamic(CreateTextColumn(
                    $"بيع {typeName}\n(دولار)",
                    $"ProductPrices[{i}].SalePriceUsd",
                    140,
                    minWidth: 120,
                    stringFormat: "N2",
                    toolTip: $"سعر البيع «{typeName}» بالدولار",
                    isUsd: true), insertAt++);
            }

            InsertDynamic(CreateTextColumn(
                $"شراء {typeName}\n(دينار)",
                $"ProductPrices[{i}].PurchasePrice",
                140,
                minWidth: 120,
                stringFormat: "N0",
                toolTip: $"سعر الشراء «{typeName}» بالدينار",
                isUsd: false), insertAt++);

            if (vm.ShowMultiCurrency)
            {
                InsertDynamic(CreateTextColumn(
                    $"شراء {typeName}\n(دولار)",
                    $"ProductPrices[{i}].PurchasePriceUsd",
                    140,
                    minWidth: 120,
                    stringFormat: "N2",
                    toolTip: $"سعر الشراء «{typeName}» بالدولار",
                    isUsd: true), insertAt++);
            }
        }
    }

    private void InsertDynamic(DataGridColumn column, int index)
    {
        BulkGrid.Columns.Insert(Math.Min(index, BulkGrid.Columns.Count), column);
        _dynamicColumns.Add(column);
    }

    private static DataGridTextColumn CreateTextColumn(
        string? header,
        string bindingPath,
        double width,
        double minWidth = 80,
        string? stringFormat = null,
        string? toolTip = null,
        bool? isUsd = null,
        FrameworkElement? headerElement = null)
    {
        var binding = new Binding(bindingPath) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
        if (!string.IsNullOrEmpty(stringFormat))
            binding.StringFormat = stringFormat;

        var col = new DataGridTextColumn
        {
            Header = headerElement ?? CreateHeader(header ?? string.Empty, isUsd),
            Binding = binding,
            Width = new DataGridLength(width),
            MinWidth = minWidth
        };
        if (!string.IsNullOrWhiteSpace(toolTip))
            col.HeaderStyle = CreateHeaderStyle(toolTip);
        return col;
    }

    /// <summary>رأس كمية+مخزن بألوان الثيم — واضح في الوضع الداكن والفاتح.</summary>
    private static FrameworkElement CreateWarehouseQtyHeader(string warehouseName)
    {
        var qtyLabel = new TextBlock
        {
            Text = "كمية",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            Opacity = 0.9
        };
        qtyLabel.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryHueLightForegroundBrush");

        var nameLabel = new TextBlock
        {
            Text = warehouseName,
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            ToolTip = warehouseName,
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
            ToolTip = $"الكمية الافتتاحية — {warehouseName}"
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

    private static FrameworkElement CreateHeader(string text, bool? isUsd = null)
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

        // خلفية+نص من الثيم حتى يبقى واضحاً في الوضع الداكن
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

    private void SetColumn(string name, bool visible)
    {
        if (FindName(name) is DataGridColumn col)
            col.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetNamedColumn(string name, bool visible, string header)
    {
        if (FindName(name) is DataGridColumn col)
        {
            col.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            col.Header = header;
        }
    }
}
