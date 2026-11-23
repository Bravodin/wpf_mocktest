using ErpMock.Contracts.Models;
using ErpMock.Contracts.Services;

namespace ErpMock.Api;

/// <summary>
/// Endpoints REST. Cada um só traduz HTTP ↔ serviço: as regras ficam em ErpMock.Core,
/// e os erros de regra (BusinessException) viram 422 no <see cref="ErrorHandler"/>.
/// </summary>
public static class ErpEndpoints
{
    public static void MapErpEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        var customers = api.MapGroup("/customers").WithTags("Clientes");
        customers.MapGet("/", (ICustomerService s, string? search, bool? onlyActive) =>
            s.GetAllAsync(search, onlyActive ?? false));
        customers.MapPost("/", async (ICustomerService s, Customer customer) =>
        {
            customer.Id = 0;
            var saved = await s.SaveAsync(customer);
            return Results.Created($"/api/customers/{saved.Id}", saved);
        });
        customers.MapPut("/{id:int}", async (ICustomerService s, int id, Customer customer) =>
        {
            customer.Id = id;
            return Results.Ok(await s.SaveAsync(customer));
        });
        customers.MapDelete("/{id:int}", async (ICustomerService s, int id) =>
        {
            await s.DeleteAsync(id);
            return Results.NoContent();
        });

        var products = api.MapGroup("/products").WithTags("Produtos");
        products.MapGet("/", (IProductService s, string? search, bool? onlyActive) =>
            s.GetAllAsync(search, onlyActive ?? false));
        products.MapGet("/categories", (IProductService s) => s.GetCategoriesAsync());
        products.MapPost("/", async (IProductService s, Product product) =>
        {
            product.Id = 0;
            var saved = await s.SaveAsync(product);
            return Results.Created($"/api/products/{saved.Id}", saved);
        });
        products.MapPut("/{id:int}", async (IProductService s, int id, Product product) =>
        {
            product.Id = id;
            return Results.Ok(await s.SaveAsync(product));
        });
        products.MapDelete("/{id:int}", async (IProductService s, int id) =>
        {
            await s.DeleteAsync(id);
            return Results.NoContent();
        });

        var sales = api.MapGroup("/sales").WithTags("Vendas");
        sales.MapGet("/", (ISaleService s, DateTime? from, DateTime? to, SaleStatus? status, string? search) =>
            s.GetAsync(new SaleFilter(from, to, status, search)));
        sales.MapGet("/{id:int}", async (ISaleService s, int id) =>
            await s.GetByIdAsync(id) is { } sale ? Results.Ok(sale) : Results.NotFound());
        sales.MapPost("/", async (ISaleService s, NewSale request) =>
        {
            var sale = await s.CreateAsync(request);
            return Results.Created($"/api/sales/{sale.Id}", sale);
        });
        sales.MapPost("/{id:int}/cancel", async (ISaleService s, int id) =>
        {
            await s.CancelAsync(id);
            return Results.NoContent();
        });

        api.MapGet("/dashboard", (IDashboardService s, DateTime? today) => s.GetSummaryAsync(today))
            .WithTags("Dashboard");

        api.MapGet("/reports/{type}", async (IReportService s, ReportType type, DateTime from, DateTime to) =>
                ReportData.FromTable(await s.GenerateAsync(new ReportRequest(type, from, to))))
            .WithTags("Relatórios");
    }
}
