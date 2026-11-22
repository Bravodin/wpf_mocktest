namespace ErpMock.Core.Models;

public class Product : Entity
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public int Stock { get; set; }
    public int MinStock { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsLowStock => Stock <= MinStock;
    public decimal Margin => Price == 0 ? 0 : (Price - Cost) / Price;
}
