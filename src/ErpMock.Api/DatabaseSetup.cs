using System.Data.Common;
using ErpMock.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace ErpMock.Api;

/// <summary>Cria o banco e popula com dados fictícios na primeira execução.</summary>
internal static class DatabaseSetup
{
    public static async Task InitializeAsync(IServiceProvider services, ILogger logger, CancellationToken ct = default)
    {
        var factory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();

        // No Docker o PostgreSQL pode ainda estar subindo: tenta de novo por até ~30 s.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var db = await factory.CreateDbContextAsync(ct);
                await db.Database.EnsureCreatedAsync(ct);
                await DataSeeder.SeedAsync(db, ct: ct);
                return;
            }
            catch (DbException ex) when (attempt < 15)
            {
                logger.LogWarning("Banco indisponível (tentativa {Attempt}): {Message}", attempt, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
    }
}
