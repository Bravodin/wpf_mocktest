using Bogus;
using Bogus.Extensions.Brazil;
using ErpMock.Contracts.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpMock.Core.Data;

/// <summary>Popula o banco com dados fictícios (pt-BR) na primeira execução.</summary>
public static class DataSeeder
{
    private static readonly string[] States = ["SP", "RJ", "MG", "PR", "SC", "RS", "BA", "PE", "GO", "DF"];

    // (categoria, nome, custo aproximado)
    private static readonly (string Category, string Name, decimal Cost)[] Catalog =
    [
        ("Informática", "Notebook 14\"", 3200m), ("Informática", "Mouse sem fio", 45m),
        ("Informática", "Teclado mecânico", 220m), ("Informática", "Monitor 24\"", 750m),
        ("Informática", "SSD 1TB", 380m), ("Informática", "Webcam Full HD", 160m),
        ("Informática", "Headset USB", 130m), ("Informática", "Hub USB-C", 90m),
        ("Escritório", "Cadeira ergonômica", 850m), ("Escritório", "Mesa de escritório", 620m),
        ("Escritório", "Luminária LED", 75m), ("Escritório", "Organizador de mesa", 35m),
        ("Escritório", "Quadro branco", 140m),
        ("Papelaria", "Resma papel A4", 22m), ("Papelaria", "Caneta esferográfica (cx)", 28m),
        ("Papelaria", "Caderno universitário", 18m), ("Papelaria", "Grampeador", 32m),
        ("Papelaria", "Post-it (pct)", 15m),
        ("Rede", "Roteador Wi-Fi 6", 420m), ("Rede", "Switch 8 portas", 180m),
        ("Rede", "Cabo de rede 5m", 12m), ("Rede", "Access Point", 560m),
    ];

    public static async Task SeedAsync(AppDbContext db, int seed = 42, CancellationToken ct = default)
    {
        if (await db.Customers.AnyAsync(ct)) return;

        Randomizer.Seed = new Random(seed);
        var f = new Faker("pt_BR");

        var customers = new List<Customer>();
        for (var i = 0; i < 60; i++)
        {
            var isCompany = f.Random.Bool(0.3f);
            var person = new Person("pt_BR");
            var companyName = f.Company.CompanyName();
            customers.Add(new Customer
            {
                Type = isCompany ? PersonType.Company : PersonType.Individual,
                Name = isCompany ? companyName : person.FullName,
                Document = isCompany ? f.Company.Cnpj() : person.Cpf(),
                Email = isCompany ? f.Internet.Email(companyName).ToLowerInvariant() : person.Email.ToLowerInvariant(),
                Phone = f.Phone.PhoneNumber("(##) 9####-####"),
                City = f.Address.City(),
                State = f.PickRandom(States),
                IsActive = f.Random.Bool(0.9f),
                CreatedAt = f.Date.Past(2),
            });
        }
        // CPFs/CNPJs gerados podem colidir em teoria; garante unicidade.
        customers = customers.DistinctBy(c => c.Document).ToList();

        var products = new List<Product>();
        var sku = 1000;
        foreach (var (category, name, baseCost) in Catalog)
        {
            var cost = Math.Round(baseCost * f.Random.Decimal(0.9m, 1.1m), 2);
            products.Add(new Product
            {
                Sku = $"PRD-{sku++}",
                Name = name,
                Category = category,
                Cost = cost,
                Price = Math.Round(cost * f.Random.Decimal(1.3m, 1.8m), 2),
                Stock = f.Random.Int(0, 150),
                MinStock = f.Random.Int(5, 20),
            });
        }

        db.Customers.AddRange(customers);
        db.Products.AddRange(products);
        await db.SaveChangesAsync(ct);

        var sales = new List<Sale>();
        var start = DateTime.Today.AddMonths(-11);
        start = new DateTime(start.Year, start.Month, 1);
        var days = (DateTime.Today - start).Days;
        var activeCustomers = customers.Where(c => c.IsActive).ToList();

        for (var n = 1; n <= 450; n++)
        {
            var sale = new Sale
            {
                Number = n,
                Date = start.AddDays(f.Random.Int(0, days)).AddHours(f.Random.Int(8, 18)).AddMinutes(f.Random.Int(0, 59)),
                Customer = f.PickRandom(activeCustomers),
                Status = f.Random.Bool(0.06f) ? SaleStatus.Cancelled : SaleStatus.Completed,
            };
            foreach (var p in f.PickRandom(products, f.Random.Int(1, 4)))
                sale.Items.Add(new SaleItem { Product = p, Quantity = f.Random.Int(1, 5), UnitPrice = p.Price });

            if (f.Random.Bool(0.2f))
                sale.Discount = Math.Round(sale.Subtotal * f.Random.Decimal(0.02m, 0.1m), 2);
            sales.Add(sale);
        }

        // Numeração em ordem cronológica, como num sistema real.
        var ordered = sales.OrderBy(s => s.Date).ToList();
        for (var i = 0; i < ordered.Count; i++) ordered[i].Number = i + 1;

        db.Sales.AddRange(ordered);
        await db.SaveChangesAsync(ct);
    }
}
