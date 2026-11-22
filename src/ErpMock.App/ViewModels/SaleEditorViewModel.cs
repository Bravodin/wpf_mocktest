using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErpMock.App.Services;
using ErpMock.Core.Models;
using ErpMock.Core.Services;

namespace ErpMock.App.ViewModels;

public partial class SaleItemRow(Product product, int quantity) : ObservableObject
{
    public Product Product { get; } = product;
    public decimal UnitPrice { get; } = product.Price;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    public partial int Quantity { get; set; } = quantity;

    public decimal Total => Quantity * UnitPrice;
}

/// <summary>Tela de "PDV": escolhe cliente, lança itens, aplica desconto e fatura.</summary>
public partial class SaleEditorViewModel : ViewModelBase
{
    private readonly ISaleService _sales;

    public SaleEditorViewModel(ISaleService sales, IDialogService dialogs) : base(dialogs)
    {
        _sales = sales;
        Items.CollectionChanged += OnItemsChanged;
    }

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<SaleItemRow> Items { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial Customer? SelectedCustomer { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddItemCommand))]
    public partial Product? SelectedProduct { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddItemCommand))]
    public partial int Quantity { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    public partial decimal Discount { get; set; }

    [ObservableProperty]
    public partial string? Notes { get; set; }

    public decimal Subtotal => Items.Sum(i => i.Total);
    public decimal Total => Math.Max(0, Subtotal - Discount);

    public async Task InitializeAsync(ICustomerService customers, IProductService products)
    {
        foreach (var c in await customers.GetAllAsync(onlyActive: true)) Customers.Add(c);
        foreach (var p in await products.GetAllAsync(onlyActive: true)) Products.Add(p);
    }

    private bool CanAddItem() => SelectedProduct is not null && Quantity > 0;

    [RelayCommand(CanExecute = nameof(CanAddItem))]
    private void AddItem()
    {
        var product = SelectedProduct!;
        var existing = Items.FirstOrDefault(i => i.Product.Id == product.Id);
        var newQty = (existing?.Quantity ?? 0) + Quantity;

        if (newQty > product.Stock)
        {
            Dialogs.ShowWarning($"Estoque insuficiente para {product.Name}. Disponível: {product.Stock}.");
            return;
        }

        if (existing is not null) existing.Quantity = newQty;
        else Items.Add(new SaleItemRow(product, Quantity));

        SelectedProduct = null;
        Quantity = 1;
    }

    [RelayCommand]
    private void RemoveItem(SaleItemRow row) => Items.Remove(row);

    private bool CanSave() => SelectedCustomer is not null && Items.Count > 0;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (Discount > Subtotal)
        {
            Dialogs.ShowWarning("O desconto não pode ser maior que o subtotal.");
            return;
        }

        var request = new NewSale(
            SelectedCustomer!.Id,
            Items.Select(i => new NewSaleItem(i.Product.Id, i.Quantity, i.UnitPrice)).ToList(),
            Discount,
            Notes);

        Sale? sale = null;
        if (await RunAsync(async () => sale = await _sales.CreateAsync(request)))
        {
            Dialogs.ShowInfo($"Venda nº {sale!.Number} faturada: {sale.Total:C}");
            Saved?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (SaleItemRow row in e.NewItems ?? Array.Empty<SaleItemRow>()) row.PropertyChanged += OnRowChanged;
        foreach (SaleItemRow row in e.OldItems ?? Array.Empty<SaleItemRow>()) row.PropertyChanged -= OnRowChanged;
        RecalculateTotals();
        SaveCommand.NotifyCanExecuteChanged();
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e) => RecalculateTotals();

    private void RecalculateTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
    }
}
