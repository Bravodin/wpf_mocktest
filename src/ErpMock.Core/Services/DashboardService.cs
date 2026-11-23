using ErpMock.Contracts.Models;
using ErpMock.Contracts.Services;
using ErpMock.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace ErpMock.Core.Services;

public class DashboardService(IDbContextFactory<AppDbContext> factory) : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync(DateTime? today = null)
    {
        var now = today ?? DateTime.Today;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var windowStart = monthStart.AddMonths(-11);

        await using var db = await factory.CreateDbContextAsync();

        // SQLite não suporta SUM/ORDER BY em decimal; com o volume deste mock
        // carregamos a janela de 12 meses e agregamos em memória.
        var sales = await db.Sales.AsNoTracking()
            .Where(s => s.Date >= windowStart && s.Status == SaleStatus.Completed)
            .Include(s => s.Customer)
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .AsSplitQuery()
            .ToListAsync();

        var thisMonth = sales.Where(s => s.Date >= monthStart).ToList();
        var lastMonth = sales.Where(s => s.Date >= monthStart.AddMonths(-1) && s.Date < monthStart).ToList();

        var culture = new System.Globalization.CultureInfo("pt-BR");
        var monthly = Enumerable.Range(0, 12)
            .Select(i => windowStart.AddMonths(i))
            .Select(m => new ChartPoint(
                culture.DateTimeFormat.GetAbbreviatedMonthName(m.Month).TrimEnd('.') + "/" + m.ToString("yy"),
                sales.Where(s => s.Date.Year == m.Year && s.Date.Month == m.Month).Sum(s => s.Total)))
            .ToList();

        var items = sales.SelectMany(s => s.Items).ToList();

        var topProducts = items
            .GroupBy(i => i.Product!.Name)
            .Select(g => new ChartPoint(g.Key, g.Sum(i => i.Total)))
            .OrderByDescending(p => p.Value).Take(5).ToList();

        var byCategory = items
            .GroupBy(i => i.Product!.Category)
            .Select(g => new ChartPoint(g.Key, g.Sum(i => i.Total)))
            .OrderByDescending(p => p.Value).ToList();

        var lowStock = await db.Products.AsNoTracking()
            .Where(p => p.IsActive && p.Stock <= p.MinStock)
            .OrderBy(p => p.Stock).ToListAsync();

        return new DashboardSummary(
            RevenueThisMonth: thisMonth.Sum(s => s.Total),
            RevenueLastMonth: lastMonth.Sum(s => s.Total),
            SalesThisMonth: thisMonth.Count,
            AverageTicket: thisMonth.Count == 0 ? 0 : thisMonth.Average(s => s.Total),
            ActiveCustomers: await db.Customers.CountAsync(c => c.IsActive),
            LowStockProducts: lowStock.Count,
            MonthlyRevenue: monthly,
            TopProducts: topProducts,
            RevenueByCategory: byCategory,
            RecentSales: sales.OrderByDescending(s => s.Date).Take(8).ToList(),
            LowStock: lowStock.Take(8).ToList());
    }
}
