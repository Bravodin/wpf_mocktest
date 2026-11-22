namespace ErpMock.Core.Models;

public enum SaleStatus
{
    Completed = 0,  // Faturada
    Cancelled = 1   // Cancelada
}

public class Sale : Entity
{
    public int Number { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public decimal Discount { get; set; }
    public SaleStatus Status { get; set; } = SaleStatus.Completed;
    public string? Notes { get; set; }

    public List<SaleItem> Items { get; set; } = [];

    public decimal Subtotal => Items.Sum(i => i.Total);
    public decimal Total => Math.Max(0, Subtotal - Discount);
}

public class SaleItem : Entity
{
    public int SaleId { get; set; }
    public Sale? Sale { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal Total => Quantity * UnitPrice;
}
