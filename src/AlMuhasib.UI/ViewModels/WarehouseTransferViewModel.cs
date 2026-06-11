using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class WarehouseTransferViewModel : ViewModelBase
{
    private readonly IWarehouseTransferService _transferService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserPreferencesService _preferences;

    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<TransferLineItem> Lines { get; } = [];

    [ObservableProperty] private Warehouse? _fromWarehouse;
    [ObservableProperty] private Warehouse? _toWarehouse;
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private decimal _lineQuantity = 1;
    [ObservableProperty] private string? _notes;

    public WarehouseTransferViewModel(
        IWarehouseTransferService transferService,
        IUnitOfWork unitOfWork,
        IUserPreferencesService preferences,
        ICurrentUserService currentUserService)
    {
        _transferService = transferService;
        _unitOfWork = unitOfWork;
        _preferences = preferences;
        PageTitle = "نقل بين مخازن";
        LoadPermissions(currentUserService, "Warehouses");
    }

    public override async Task InitializeAsync()
    {
        if (!_preferences.Current.FeatureFlags.WarehouseTransfers)
        {
            BeautifulMessageDialog.ShowWarning("فعّل «نقل بين مخازن» من إعدادات الميزات");
            return;
        }
        foreach (var w in await _unitOfWork.Warehouses.GetAllAsync()) Warehouses.Add(w);
        foreach (var p in await _unitOfWork.Products.GetAllAsync()) Products.Add(p);
    }

    [RelayCommand]
    private void AddLine()
    {
        if (SelectedProduct is null || LineQuantity <= 0) return;
        Lines.Add(new TransferLineItem { ProductId = SelectedProduct.Id, ProductName = SelectedProduct.Name, Quantity = LineQuantity });
    }

    [RelayCommand]
    private async Task SaveTransferAsync()
    {
        if (FromWarehouse is null || ToWarehouse is null || FromWarehouse.Id == ToWarehouse.Id)
        {
            BeautifulMessageDialog.ShowWarning("اختر مخزنين مختلفين");
            return;
        }
        if (Lines.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("أضف بنود النقل");
            return;
        }
        try
        {
            IsBusy = true;
            var transfer = new WarehouseTransfer
            {
                FromWarehouseId = FromWarehouse.Id,
                ToWarehouseId = ToWarehouse.Id,
                Date = DateTime.Now,
                Notes = Notes
            };
            var items = Lines.Select(l => new WarehouseTransferItem { ProductId = l.ProductId, Quantity = l.Quantity });
            await _transferService.CreateTransferAsync(transfer, items);
            Lines.Clear();
            BeautifulMessageDialog.ShowSuccess($"تم النقل — {transfer.TransferNumber}");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class TransferLineItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}
