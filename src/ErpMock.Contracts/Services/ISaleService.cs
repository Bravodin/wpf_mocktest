using ErpMock.Contracts.Models;

namespace ErpMock.Contracts.Services;

public record SaleFilter(DateTime? From = null, DateTime? To = null, SaleStatus? Status = null, string? Search = null);

public record NewSaleItem(int ProductId, int Quantity, decimal UnitPrice);

public record NewSale(int CustomerId, IReadOnlyList<NewSaleItem> Items, decimal Discount = 0, string? Notes = null);

public interface ISaleService
{
    Task<List<Sale>> GetAsync(SaleFilter filter);
    Task<Sale?> GetByIdAsync(int id);
    Task<Sale> CreateAsync(NewSale request);
    Task CancelAsync(int id);
}
