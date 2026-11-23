using System.Data;
using ErpMock.Contracts.Models;
using ErpMock.Contracts.Services;

namespace ErpMock.Client;

public sealed class CustomerApiClient(HttpClient http) : ApiClient(http), ICustomerService
{
    public Task<List<Customer>> GetAllAsync(string? search = null, bool onlyActive = false) =>
        GetAsync<List<Customer>>("api/customers" + Query(("search", search), ("onlyActive", onlyActive)));

    public Task<Customer> SaveAsync(Customer customer) => customer.Id == 0
        ? SendAsync<Customer>(HttpMethod.Post, "api/customers", customer)
        : SendAsync<Customer>(HttpMethod.Put, $"api/customers/{customer.Id}", customer);

    public Task DeleteAsync(int id) => SendAsync(HttpMethod.Delete, $"api/customers/{id}");
}

public sealed class ProductApiClient(HttpClient http) : ApiClient(http), IProductService
{
    public Task<List<Product>> GetAllAsync(string? search = null, bool onlyActive = false) =>
        GetAsync<List<Product>>("api/products" + Query(("search", search), ("onlyActive", onlyActive)));

    public Task<List<string>> GetCategoriesAsync() => GetAsync<List<string>>("api/products/categories");

    public Task<Product> SaveAsync(Product product) => product.Id == 0
        ? SendAsync<Product>(HttpMethod.Post, "api/products", product)
        : SendAsync<Product>(HttpMethod.Put, $"api/products/{product.Id}", product);

    public Task DeleteAsync(int id) => SendAsync(HttpMethod.Delete, $"api/products/{id}");
}

public sealed class SaleApiClient(HttpClient http) : ApiClient(http), ISaleService
{
    public Task<List<Sale>> GetAsync(SaleFilter filter) =>
        GetAsync<List<Sale>>("api/sales" + Query(
            ("from", filter.From), ("to", filter.To), ("status", filter.Status), ("search", filter.Search)));

    public Task<Sale?> GetByIdAsync(int id) => GetOrNullAsync<Sale>($"api/sales/{id}");

    public Task<Sale> CreateAsync(NewSale request) => SendAsync<Sale>(HttpMethod.Post, "api/sales", request);

    public Task CancelAsync(int id) => SendAsync(HttpMethod.Post, $"api/sales/{id}/cancel");
}

public sealed class DashboardApiClient(HttpClient http) : ApiClient(http), IDashboardService
{
    public Task<DashboardSummary> GetSummaryAsync(DateTime? today = null) =>
        GetAsync<DashboardSummary>("api/dashboard" + Query(("today", today)));
}

public sealed class ReportApiClient(HttpClient http) : ApiClient(http), IReportService
{
    public async Task<DataTable> GenerateAsync(ReportRequest request)
    {
        var data = await GetAsync<ReportData>($"api/reports/{request.Type}" + Query(("from", request.From), ("to", request.To)));
        return data.ToTable();
    }

    public string ToCsv(DataTable table) => Reports.ToCsv(table);
}
