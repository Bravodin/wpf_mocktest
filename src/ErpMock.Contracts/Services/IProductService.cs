using ErpMock.Contracts.Models;

namespace ErpMock.Contracts.Services;

public interface IProductService
{
    Task<List<Product>> GetAllAsync(string? search = null, bool onlyActive = false);
    Task<List<string>> GetCategoriesAsync();
    Task<Product> SaveAsync(Product product);
    Task DeleteAsync(int id);
}
