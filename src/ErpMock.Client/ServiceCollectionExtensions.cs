using ErpMock.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ErpMock.Client;

public static class ServiceCollectionExtensions
{
    /// <summary>Registra as interfaces de serviço apontando para a API em <paramref name="baseAddress"/>.</summary>
    public static IServiceCollection AddErpApiClient(this IServiceCollection services, Uri baseAddress)
    {
        // Um HttpClient só para o app inteiro (desktop, um único host): reaproveita as conexões.
        var http = new HttpClient { BaseAddress = baseAddress, Timeout = TimeSpan.FromSeconds(30) };

        services.AddSingleton<ICustomerService>(new CustomerApiClient(http));
        services.AddSingleton<IProductService>(new ProductApiClient(http));
        services.AddSingleton<ISaleService>(new SaleApiClient(http));
        services.AddSingleton<IDashboardService>(new DashboardApiClient(http));
        services.AddSingleton<IReportService>(new ReportApiClient(http));
        return services;
    }
}
