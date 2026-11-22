using ErpMock.Core.Data;
using ErpMock.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpMock.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddErpCore(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite(connectionString));
        services.AddSingleton<ICustomerService, CustomerService>();
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<ISaleService, SaleService>();
        services.AddSingleton<IDashboardService, DashboardService>();
        services.AddSingleton<IReportService, ReportService>();
        return services;
    }
}
