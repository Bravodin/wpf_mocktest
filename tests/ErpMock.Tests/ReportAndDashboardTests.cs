using System.Data;
using ErpMock.Core.Data;
using ErpMock.Contracts.Services;
using ErpMock.Core.Services;

namespace ErpMock.Tests;

public class ReportAndDashboardTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Seeder_generates_consistent_data()
    {
        await using (var ctx = _db.CreateDbContext())
            await DataSeeder.SeedAsync(ctx);

        await using var check = _db.CreateDbContext();
        Assert.True(check.Customers.Count() > 50);
        Assert.True(check.Sales.Count() > 400);
        Assert.All(check.Customers.ToList(), c =>
            Assert.True(DocumentValidator.IsValidCpf(c.Document) || DocumentValidator.IsValidCnpj(c.Document), c.Document));
    }

    [Fact]
    public async Task Dashboard_returns_twelve_months()
    {
        await using (var ctx = _db.CreateDbContext())
            await DataSeeder.SeedAsync(ctx);

        var summary = await new DashboardService(_db).GetSummaryAsync();

        Assert.Equal(12, summary.MonthlyRevenue.Count);
        Assert.True(summary.MonthlyRevenue.Sum(m => m.Value) > 0);
        Assert.True(summary.TopProducts.Count <= 5);
    }

    [Theory]
    [InlineData(ReportType.SalesByPeriod)]
    [InlineData(ReportType.SalesByCustomer)]
    [InlineData(ReportType.SalesByProduct)]
    [InlineData(ReportType.Inventory)]
    public async Task Every_report_generates_rows(ReportType type)
    {
        await using (var ctx = _db.CreateDbContext())
            await DataSeeder.SeedAsync(ctx);

        var table = await new ReportService(_db).GenerateAsync(
            new ReportRequest(type, DateTime.Today.AddYears(-1), DateTime.Today));

        Assert.True(table.Rows.Count > 0);
    }

    // Regressão: com AsNoTracking cada item traz sua própria instância de Product/Customer;
    // agrupar pela entidade (referência) duplicava linhas.
    [Theory]
    [InlineData(ReportType.SalesByProduct, "SKU")]
    [InlineData(ReportType.SalesByCustomer, "Documento")]
    public async Task Grouped_reports_have_one_row_per_entity(ReportType type, string keyColumn)
    {
        await using (var ctx = _db.CreateDbContext())
            await DataSeeder.SeedAsync(ctx);

        var table = await new ReportService(_db).GenerateAsync(
            new ReportRequest(type, DateTime.Today.AddYears(-1), DateTime.Today));

        var keys = table.AsEnumerable().Select(r => r.Field<string>(keyColumn)).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    public async Task Csv_uses_semicolon_and_brazilian_decimal()
    {
        await _db.SeedBasicAsync();
        var service = new ReportService(_db);
        var table = await service.GenerateAsync(new ReportRequest(ReportType.Inventory, DateTime.Today, DateTime.Today));

        var csv = service.ToCsv(table);

        Assert.StartsWith("SKU;Produto;Categoria", csv);
        Assert.Contains("PRD-1;Mouse;Informática;10;2;60,00;600,00;OK", csv);
    }
}
