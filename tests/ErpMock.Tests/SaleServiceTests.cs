using ErpMock.Contracts.Models;
using ErpMock.Contracts.Services;
using ErpMock.Core.Services;

namespace ErpMock.Tests;

public class SaleServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly SaleService _service;

    public SaleServiceTests() => _service = new SaleService(_db);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Create_decrements_stock_and_numbers_sequentially()
    {
        var (customer, product) = await _db.SeedBasicAsync(stock: 10);

        var first = await _service.CreateAsync(new NewSale(customer.Id, [new NewSaleItem(product.Id, 3, 100m)], Discount: 20m));
        var second = await _service.CreateAsync(new NewSale(customer.Id, [new NewSaleItem(product.Id, 1, 100m)]));

        Assert.Equal(1, first.Number);
        Assert.Equal(2, second.Number);
        Assert.Equal(280m, first.Total);

        await using var ctx = _db.CreateDbContext();
        Assert.Equal(6, (await ctx.Products.FindAsync(product.Id))!.Stock);
    }

    [Fact]
    public async Task Create_merges_duplicate_product_lines()
    {
        var (customer, product) = await _db.SeedBasicAsync();

        var sale = await _service.CreateAsync(new NewSale(customer.Id,
            [new NewSaleItem(product.Id, 2, 100m), new NewSaleItem(product.Id, 3, 100m)]));

        var item = Assert.Single(sale.Items);
        Assert.Equal(5, item.Quantity);
    }

    [Fact]
    public async Task Create_with_insufficient_stock_throws_and_changes_nothing()
    {
        var (customer, product) = await _db.SeedBasicAsync(stock: 2);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.CreateAsync(new NewSale(customer.Id, [new NewSaleItem(product.Id, 5, 100m)])));

        Assert.Contains("Estoque insuficiente", ex.Message);
        await using var ctx = _db.CreateDbContext();
        Assert.Equal(2, (await ctx.Products.FindAsync(product.Id))!.Stock);
        Assert.Empty(ctx.Sales);
    }

    [Fact]
    public async Task Create_with_discount_greater_than_subtotal_throws()
    {
        var (customer, product) = await _db.SeedBasicAsync();

        await Assert.ThrowsAsync<BusinessException>(() =>
            _service.CreateAsync(new NewSale(customer.Id, [new NewSaleItem(product.Id, 1, 100m)], Discount: 150m)));
    }

    [Fact]
    public async Task Create_without_items_throws()
    {
        var (customer, _) = await _db.SeedBasicAsync();
        await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(new NewSale(customer.Id, [])));
    }

    [Fact]
    public async Task Cancel_returns_items_to_stock()
    {
        var (customer, product) = await _db.SeedBasicAsync(stock: 10);
        var sale = await _service.CreateAsync(new NewSale(customer.Id, [new NewSaleItem(product.Id, 4, 100m)]));

        await _service.CancelAsync(sale.Id);

        await using var ctx = _db.CreateDbContext();
        Assert.Equal(10, (await ctx.Products.FindAsync(product.Id))!.Stock);
        Assert.Equal(SaleStatus.Cancelled, (await ctx.Sales.FindAsync(sale.Id))!.Status);
    }

    [Fact]
    public async Task Cancel_twice_throws()
    {
        var (customer, product) = await _db.SeedBasicAsync();
        var sale = await _service.CreateAsync(new NewSale(customer.Id, [new NewSaleItem(product.Id, 1, 100m)]));
        await _service.CancelAsync(sale.Id);

        await Assert.ThrowsAsync<BusinessException>(() => _service.CancelAsync(sale.Id));
    }
}
