using ErpMock.Contracts.Models;
using ErpMock.Contracts.Services;
using ErpMock.Core.Services;

namespace ErpMock.Tests;

public class CustomerServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly CustomerService _service;

    public CustomerServiceTests() => _service = new CustomerService(_db);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Save_formats_document()
    {
        var saved = await _service.SaveAsync(new Customer { Name = "Maria", Document = "52998224725" });
        Assert.Equal("529.982.247-25", saved.Document);
    }

    [Fact]
    public async Task Save_rejects_invalid_cpf()
    {
        await Assert.ThrowsAsync<BusinessException>(() =>
            _service.SaveAsync(new Customer { Name = "Maria", Document = "123.456.789-00" }));
    }

    [Fact]
    public async Task Save_rejects_duplicate_document()
    {
        await _db.SeedBasicAsync(); // já existe 529.982.247-25
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.SaveAsync(new Customer { Name = "Outro", Document = "52998224725" }));
        Assert.Contains("Já existe", ex.Message);
    }

    [Fact]
    public async Task Delete_customer_with_sales_is_blocked()
    {
        var (customer, product) = await _db.SeedBasicAsync();
        await new SaleService(_db).CreateAsync(new NewSale(customer.Id, [new NewSaleItem(product.Id, 1, 100m)]));

        await Assert.ThrowsAsync<BusinessException>(() => _service.DeleteAsync(customer.Id));
    }

    [Fact]
    public async Task Search_matches_name_case_insensitive()
    {
        await _db.SeedBasicAsync();
        var result = await _service.GetAllAsync("fulano");
        Assert.Single(result);
    }
}
