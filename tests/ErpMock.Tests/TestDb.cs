using ErpMock.Core.Data;
using ErpMock.Contracts.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ErpMock.Tests;

/// <summary>SQLite em memória: banco real (constraints, transações) e descartável por teste.</summary>
public sealed class TestDb : IDbContextFactory<AppDbContext>, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DbContextOptions<AppDbContext> _options;

    public TestDb()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    public AppDbContext CreateDbContext() => new(_options);

    public async Task<(Customer Customer, Product Product)> SeedBasicAsync(int stock = 10)
    {
        await using var db = CreateDbContext();
        var customer = new Customer { Name = "Fulano de Tal", Document = "529.982.247-25" };
        var product = new Product { Sku = "PRD-1", Name = "Mouse", Category = "Informática", Price = 100m, Cost = 60m, Stock = stock, MinStock = 2 };
        db.AddRange(customer, product);
        await db.SaveChangesAsync();
        return (customer, product);
    }

    public void Dispose() => _connection.Dispose();
}
