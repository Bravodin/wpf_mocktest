using ErpMock.Contracts.Models;

namespace ErpMock.Contracts.Services;

public record ChartPoint(string Label, decimal Value);

public record DashboardSummary(
    decimal RevenueThisMonth,
    decimal RevenueLastMonth,
    int SalesThisMonth,
    decimal AverageTicket,
    int ActiveCustomers,
    int LowStockProducts,
    IReadOnlyList<ChartPoint> MonthlyRevenue,
    IReadOnlyList<ChartPoint> TopProducts,
    IReadOnlyList<ChartPoint> RevenueByCategory,
    IReadOnlyList<Sale> RecentSales,
    IReadOnlyList<Product> LowStock)
{
    public decimal RevenueGrowth => RevenueLastMonth == 0 ? 0 : (RevenueThisMonth - RevenueLastMonth) / RevenueLastMonth;
}

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(DateTime? today = null);
}
