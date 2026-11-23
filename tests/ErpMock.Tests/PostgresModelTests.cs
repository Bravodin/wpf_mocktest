using ErpMock.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace ErpMock.Tests;

/// <summary>
/// O modelo e as consultas no PostgreSQL, sem precisar de um servidor: o EF gera o SQL offline.
/// (Rodar contra um PostgreSQL de verdade é o docker-compose.)
/// </summary>
public class PostgresModelTests
{
    private static AppDbContext Postgres() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=unused;Database=erp").Options);

    [Fact]
    public void Dates_AreStoredWithoutTimeZone()
    {
        using var db = Postgres();

        var script = db.Database.GenerateCreateScript();

        Assert.Contains("\"Date\" timestamp without time zone", script);
        Assert.Contains("\"CreatedAt\" timestamp without time zone", script);
        Assert.DoesNotContain("with time zone", script);
        Assert.Contains("numeric(18,2)", script);
    }

    [Fact]
    public void Searches_TranslateToSql()
    {
        using var db = Postgres();

        var sql = db.Customers.Where(c => c.Name.ToLower().Contains("ana") || c.Document.Contains("123")).ToQueryString();

        Assert.Contains("lower(", sql);
        Assert.Contains("FROM \"Customers\"", sql);
    }

    [Fact]
    public void Sqlite_KeepsItsOwnTypes()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);

        Assert.DoesNotContain("timestamp", db.Database.GenerateCreateScript());
    }
}
