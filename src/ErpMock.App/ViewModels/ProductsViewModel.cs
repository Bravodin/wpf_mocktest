using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using ErpMock.App.Services;
using ErpMock.Contracts.Models;
using ErpMock.Contracts.Services;

namespace ErpMock.App.ViewModels;

public class ProductsViewModel(IProductService service, IDialogService dialogs)
    : CrudPageViewModel<Product, ProductEditorViewModel>(dialogs)
{
    private List<string> _categories = [];

    public override string Title => "Produtos";

    protected override async Task<List<Product>> QueryAsync(string? search, bool onlyActive)
    {
        _categories = await service.GetCategoriesAsync();
        return await service.GetAllAsync(search, onlyActive);
    }

    protected override ProductEditorViewModel CreateEditor(Product? model) => new(model, _categories);
    protected override Task SaveModelAsync(Product model) => service.SaveAsync(model);
    protected override Task DeleteModelAsync(Product model) => service.DeleteAsync(model.Id);
    protected override string Describe(Product model) => $"o produto \"{model.Name}\"";
}

public partial class ProductEditorViewModel : EditorViewModel<Product>
{
    public ProductEditorViewModel(Product? model, IReadOnlyList<string> categories)
    {
        var p = model ?? new Product();
        Id = p.Id;
        Categories = categories;
        Sku = p.Sku;
        Name = p.Name;
        Category = p.Category;
        Price = p.Price;
        Cost = p.Cost;
        Stock = p.Stock;
        MinStock = p.MinStock;
        IsActive = p.IsActive;
    }

    public override string Title => IsNew ? "Novo produto" : "Editar produto";

    public IReadOnlyList<string> Categories { get; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Informe o SKU.")]
    [MaxLength(20, ErrorMessage = "Máximo de 20 caracteres.")]
    public partial string Sku { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Informe o nome.")]
    public partial string Name { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Informe a categoria.")]
    public partial string Category { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyPropertyChangedFor(nameof(MarginText))]
    [Range(typeof(decimal), "0.01", "9999999", ErrorMessage = "Preço deve ser maior que zero.")]
    public partial decimal Price { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyPropertyChangedFor(nameof(MarginText))]
    [Range(typeof(decimal), "0", "9999999", ErrorMessage = "Custo inválido.")]
    public partial decimal Cost { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, int.MaxValue, ErrorMessage = "Estoque não pode ser negativo.")]
    public partial int Stock { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, int.MaxValue, ErrorMessage = "Valor inválido.")]
    public partial int MinStock { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    public string MarginText => Price <= 0 ? "—" : $"Margem: {(Price - Cost) / Price:P1}";

    public override Product ToModel() => new()
    {
        Id = Id,
        Sku = Sku,
        Name = Name.Trim(),
        Category = Category.Trim(),
        Price = Price,
        Cost = Cost,
        Stock = Stock,
        MinStock = MinStock,
        IsActive = IsActive,
    };
}
