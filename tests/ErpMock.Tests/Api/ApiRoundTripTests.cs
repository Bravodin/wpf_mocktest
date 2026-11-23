using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpMock.Client;
using ErpMock.Contracts.Models;
using ErpMock.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ErpMock.Tests.Api;

/// <summary>
/// API + cliente HTTP juntos: o que o WPF recebe pela rede tem que ser o mesmo que os serviços devolvem,
/// e os erros de regra têm que chegar como BusinessException com a mesma mensagem.
/// </summary>
public class ApiRoundTripTests(ErpApiFactory api) : IClassFixture<ErpApiFactory>
{
    [Fact]
    public void Api_UsesItsOwnTemporaryDatabase()
    {
        _ = api.Http; // sobe o servidor
        Assert.True(File.Exists(api.DatabasePath));
    }

    [Fact]
    public async Task Customers_ComeFromTheSeededDatabase()
    {
        var customers = await api.Customers.GetAllAsync();

        Assert.True(customers.Count >= 50);
        Assert.All(customers, c => Assert.True(DocumentValidator.IsValidCpf(c.Document) || DocumentValidator.IsValidCnpj(c.Document)));
        Assert.All(await api.Customers.GetAllAsync(onlyActive: true), c => Assert.True(c.IsActive));
    }

    [Fact]
    public async Task Customer_IsCreatedThenUpdated()
    {
        var created = await api.Customers.SaveAsync(new Customer { Name = "Cliente de Teste", Document = TestDocuments.NewCpf(), City = "Santos", State = "SP" });
        Assert.NotEqual(0, created.Id);
        Assert.Contains('.', created.Document); // formatado pela regra do servidor

        created.City = "Campinas";
        var updated = await api.Customers.SaveAsync(created);

        Assert.Equal(created.Id, updated.Id);
        var found = Assert.Single(await api.Customers.GetAllAsync(search: created.Document));
        Assert.Equal("Campinas", found.City);
    }

    [Fact]
    public async Task InvalidCpf_ArrivesAsBusinessException_WithTheServersMessage()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            api.Customers.SaveAsync(new Customer { Name = "X", Document = "111.111.111-11" }));

        Assert.Equal("CPF inválido.", ex.Message);
    }

    [Fact]
    public async Task BusinessRule_IsA422ProblemDetails_OnTheWire()
    {
        var response = await api.Http.PostAsJsonAsync("api/products", new { sku = "X-1", name = "Caro", category = "Teste", price = 10m, cost = 20m });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Preço de venda não pode ser menor que o custo.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Sale_LowersStock_AndCancellingRestoresIt()
    {
        var customer = (await api.Customers.GetAllAsync(onlyActive: true))[0];
        var product = await api.Products.SaveAsync(new Product
        {
            Sku = $"T-{Guid.NewGuid():N}"[..12], Name = "Produto de teste", Category = "Teste", Cost = 10m, Price = 15m, Stock = 10, MinStock = 1,
        });

        var sale = await api.Sales.CreateAsync(new NewSale(customer.Id, [new NewSaleItem(product.Id, 3, product.Price)]));
        Assert.Equal(7, await StockOf(product.Id));

        var loaded = await api.Sales.GetByIdAsync(sale.Id);
        Assert.NotNull(loaded);
        Assert.Equal(customer.Name, loaded.Customer?.Name);
        var item = Assert.Single(loaded.Items);
        Assert.Equal("Produto de teste", item.Product?.Name);
        Assert.Equal(45m, loaded.Total);
        Assert.Equal(SaleStatus.Completed, loaded.Status);

        await api.Sales.CancelAsync(sale.Id);
        Assert.Equal(10, await StockOf(product.Id));

        var again = await Assert.ThrowsAsync<BusinessException>(() => api.Sales.CancelAsync(sale.Id));
        Assert.Equal("Venda já está cancelada.", again.Message);
    }

    [Fact]
    public async Task Sale_AboveStock_IsRefusedWithTheStockLeft()
    {
        var customer = (await api.Customers.GetAllAsync(onlyActive: true))[0];
        var product = await api.Products.SaveAsync(new Product
        {
            Sku = $"T-{Guid.NewGuid():N}"[..12], Name = "Pouco estoque", Category = "Teste", Cost = 1m, Price = 2m, Stock = 2, MinStock = 1,
        });

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            api.Sales.CreateAsync(new NewSale(customer.Id, [new NewSaleItem(product.Id, 5, 2m)])));

        Assert.Equal("Estoque insuficiente para Pouco estoque (disponível: 2).", ex.Message);
        Assert.Equal(2, await StockOf(product.Id));
    }

    [Fact]
    public async Task Sale_ThatDoesNotExist_IsNull() =>
        Assert.Null(await api.Sales.GetByIdAsync(999_999));

    [Fact]
    public async Task Sales_FilterByStatusAndPeriod()
    {
        var cancelled = await api.Sales.GetAsync(new SaleFilter(Status: SaleStatus.Cancelled));
        Assert.NotEmpty(cancelled);
        Assert.All(cancelled, s => Assert.Equal(SaleStatus.Cancelled, s.Status));

        var from = DateTime.Today.AddDays(-30);
        var recent = await api.Sales.GetAsync(new SaleFilter(From: from, To: DateTime.Today));
        Assert.All(recent, s => Assert.InRange(s.Date, from, DateTime.Today.AddDays(1)));
    }

    [Fact]
    public async Task Enums_TravelAsText()
    {
        var json = await api.Http.GetStringAsync("api/sales?status=Cancelled");
        Assert.Contains("\"status\":\"Cancelled\"", json);
    }

    [Theory]
    [InlineData(ReportType.SalesByPeriod)]
    [InlineData(ReportType.SalesByCustomer)]
    [InlineData(ReportType.SalesByProduct)]
    [InlineData(ReportType.Inventory)]
    public async Task Report_ThroughTheApi_IsTheSameTableAsTheService(ReportType type)
    {
        var request = new ReportRequest(type, DateTime.Today.AddMonths(-3), DateTime.Today);
        var server = await api.Services.GetRequiredService<IReportService>().GenerateAsync(request);

        var client = await api.Reports.GenerateAsync(request);

        Assert.Equal(server.TableName, client.TableName);
        Assert.Equal(
            server.Columns.Cast<System.Data.DataColumn>().Select(c => (c.ColumnName, c.DataType)),
            client.Columns.Cast<System.Data.DataColumn>().Select(c => (c.ColumnName, c.DataType)));
        Assert.True(client.Rows.Count > 0);
        Assert.Equal(server.Rows.Count, client.Rows.Count);
        for (var r = 0; r < server.Rows.Count; r++)
            Assert.Equal(server.Rows[r].ItemArray, client.Rows[r].ItemArray);
        Assert.Equal(api.Reports.ToCsv(server), api.Reports.ToCsv(client));
    }

    [Fact]
    public async Task Dashboard_ThroughTheApi_MatchesTheService()
    {
        var today = DateTime.Today;
        var server = await api.Services.GetRequiredService<IDashboardService>().GetSummaryAsync(today);

        var client = await api.Dashboard.GetSummaryAsync(today);

        Assert.Equal(server.RevenueThisMonth, client.RevenueThisMonth);
        Assert.Equal(server.RevenueGrowth, client.RevenueGrowth);
        Assert.Equal(server.MonthlyRevenue, client.MonthlyRevenue);
        Assert.Equal(server.TopProducts, client.TopProducts);
        Assert.Equal(server.RecentSales.Select(s => (s.Number, s.Total, s.Customer?.Name)),
            client.RecentSales.Select(s => (s.Number, s.Total, s.Customer?.Name)));
        Assert.Equal(server.LowStock.Select(p => p.Sku), client.LowStock.Select(p => p.Sku));
    }

    [Fact]
    public async Task ApiOffline_GivesAFriendlyMessage()
    {
        // Porta 9 (discard) num endereço local: a conexão é recusada na hora.
        var offline = new CustomerApiClient(new HttpClient { BaseAddress = new Uri("http://127.0.0.1:9/") });

        var ex = await Assert.ThrowsAsync<ApiUnavailableException>(() => offline.GetAllAsync());

        Assert.Contains("Não foi possível conectar à API em http://127.0.0.1:9/", ex.Message);
    }

    private async Task<int> StockOf(int productId) =>
        (await api.Products.GetAllAsync()).Single(p => p.Id == productId).Stock;
}
