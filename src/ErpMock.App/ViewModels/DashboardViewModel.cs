using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErpMock.App.Services;
using ErpMock.Contracts.Services;

namespace ErpMock.App.ViewModels;

/// <summary>Item de gráfico de barras; Ratio (0..1) define o tamanho da barra no XAML.</summary>
public record BarItem(string Label, decimal Value, double Ratio, double Share = 0);

public partial class DashboardViewModel(IDashboardService service, IDialogService dialogs) : PageViewModel(dialogs)
{
    public override string Title => "Dashboard";

    [ObservableProperty]
    public partial DashboardSummary? Summary { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<BarItem> MonthlyBars { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<BarItem> TopProductBars { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<BarItem> CategoryBars { get; set; } = [];

    [RelayCommand]
    public override Task LoadAsync() => RunAsync(async () =>
    {
        var s = await service.GetSummaryAsync();
        MonthlyBars = ToBars(s.MonthlyRevenue);
        TopProductBars = ToBars(s.TopProducts);
        CategoryBars = ToBars(s.RevenueByCategory);
        Summary = s;
    });

    public static IReadOnlyList<BarItem> ToBars(IReadOnlyList<ChartPoint> points)
    {
        var max = points.Count == 0 ? 0 : points.Max(p => p.Value);
        var sum = points.Sum(p => p.Value);
        return points.Select(p => new BarItem(
            p.Label,
            p.Value,
            max == 0 ? 0 : (double)(p.Value / max),
            sum == 0 ? 0 : (double)(p.Value / sum))).ToList();
    }
}
