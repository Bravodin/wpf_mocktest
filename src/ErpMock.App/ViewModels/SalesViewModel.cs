using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErpMock.App.Services;
using ErpMock.Contracts.Models;
using ErpMock.Contracts.Services;

namespace ErpMock.App.ViewModels;

public record StatusOption(string Label, SaleStatus? Value);

public partial class SalesViewModel(
    ISaleService sales,
    ICustomerService customers,
    IProductService products,
    IDialogService dialogs) : PageViewModel(dialogs)
{
    public override string Title => "Vendas";

    public ObservableCollection<Sale> Items { get; } = [];

    public static IReadOnlyList<StatusOption> StatusOptions { get; } =
    [
        new("Todas", null),
        new("Faturadas", SaleStatus.Completed),
        new("Canceladas", SaleStatus.Cancelled),
    ];

    [ObservableProperty]
    public partial DateTime? From { get; set; } = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    public partial DateTime? To { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial StatusOption Status { get; set; } = StatusOptions[0];

    [ObservableProperty]
    public partial string? SearchText { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelSaleCommand))]
    public partial Sale? Selected { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    public partial SaleEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial decimal TotalAmount { get; set; }

    [ObservableProperty]
    public partial int CompletedCount { get; set; }

    public bool IsEditing => Editor is not null;

    [RelayCommand]
    public override Task LoadAsync() => RunAsync(async () =>
    {
        var list = await sales.GetAsync(new SaleFilter(From, To, Status.Value, SearchText));
        Items.Clear();
        foreach (var s in list) Items.Add(s);

        var completed = list.Where(s => s.Status == SaleStatus.Completed).ToList();
        CompletedCount = completed.Count;
        TotalAmount = completed.Sum(s => s.Total);
    });

    [RelayCommand]
    private async Task NewSaleAsync()
    {
        var editor = new SaleEditorViewModel(sales, Dialogs);
        if (await RunAsync(() => editor.InitializeAsync(customers, products)))
        {
            editor.Saved += async (_, _) => { Editor = null; await LoadAsync(); };
            editor.Cancelled += (_, _) => Editor = null;
            Editor = editor;
        }
    }

    private bool CanCancelSale() => Selected?.Status == SaleStatus.Completed;

    [RelayCommand(CanExecute = nameof(CanCancelSale))]
    private async Task CancelSaleAsync()
    {
        if (Selected is not { } sale) return;
        if (!Dialogs.Confirm($"Cancelar a venda nº {sale.Number}? Os itens voltarão para o estoque.")) return;

        if (await RunAsync(() => sales.CancelAsync(sale.Id)))
            await LoadAsync();
    }
}
