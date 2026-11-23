using ErpMock.Contracts.Models;
using ErpMock.Contracts.Services;
using ErpMock.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace ErpMock.Core.Services;

public class ProductService(IDbContextFactory<AppDbContext> factory) : IProductService
{
    public async Task<List<Product>> GetAllAsync(string? search = null, bool onlyActive = false)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Products.AsNoTracking();

        if (onlyActive) query = query.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s)
                                  || p.Sku.ToLower().Contains(s)
                                  || p.Category.ToLower().Contains(s));
        }
        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Products.Select(p => p.Category).Distinct().OrderBy(c => c).ToListAsync();
    }

    public async Task<Product> SaveAsync(Product product)
    {
        if (product.Price < product.Cost)
            throw new BusinessException("Preço de venda não pode ser menor que o custo.");

        product.Sku = product.Sku.Trim().ToUpperInvariant();

        await using var db = await factory.CreateDbContextAsync();
        if (await db.Products.AnyAsync(p => p.Sku == product.Sku && p.Id != product.Id))
            throw new BusinessException($"Já existe um produto com o SKU {product.Sku}.");

        if (product.Id == 0) db.Products.Add(product);
        else db.Products.Update(product);

        await db.SaveChangesAsync();
        return product;
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        if (await db.SaleItems.AnyAsync(i => i.ProductId == id))
            throw new BusinessException("Produto já foi vendido e não pode ser excluído. Desative-o.");
        await db.Products.Where(p => p.Id == id).ExecuteDeleteAsync();
    }
}
