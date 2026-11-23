using ErpMock.Contracts.Services;
using ErpMock.Core.Data;
using ErpMock.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpMock.Core;

/// <summary>Banco suportado: SQLite (arquivo local, sem instalar nada) ou PostgreSQL (Docker/servidor).</summary>
public enum DatabaseProvider
{
    Sqlite,
    Postgres
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddErpCore(this IServiceCollection services, string connectionString,
        DatabaseProvider provider = DatabaseProvider.Sqlite)
    {
        services.AddDbContextFactory<AppDbContext>(o => _ = provider switch
        {
            DatabaseProvider.Postgres => o.UseNpgsql(connectionString),
            _ => o.UseSqlite(connectionString),
        });
        services.AddSingleton<ICustomerService, CustomerService>();
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<ISaleService, SaleService>();
        services.AddSingleton<IDashboardService, DashboardService>();
        services.AddSingleton<IReportService, ReportService>();
        return services;
    }
}
