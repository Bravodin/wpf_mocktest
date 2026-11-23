using ErpMock.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace ErpMock.Tests.Api;

/// <summary>
/// A API de verdade, em memória (TestServer), com um banco SQLite próprio num arquivo temporário.
/// Na inicialização ela cria o banco e popula com os dados fictícios, como em produção.
/// </summary>
public sealed class ErpApiFactory : WebApplicationFactory<Program>
{
    public string DatabasePath { get; } = Path.Combine(Path.GetTempPath(), $"erpmock-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:Erp", $"Data Source={DatabasePath};Pooling=False");
    }

    /// <summary>HttpClient ligado ao TestServer (sem rede) — o mesmo que os clientes usam no app.</summary>
    public HttpClient Http => CreateClient();

    public CustomerApiClient Customers => new(Http);
    public ProductApiClient Products => new(Http);
    public SaleApiClient Sales => new(Http);
    public DashboardApiClient Dashboard => new(Http);
    public ReportApiClient Reports => new(Http);

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
        File.Delete(DatabasePath);
    }
}
