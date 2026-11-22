using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ErpMock.App.ViewModels;

public partial class MainViewModel(IServiceProvider services) : ObservableObject
{
    [ObservableProperty]
    public partial PageViewModel? CurrentPage { get; set; }

    [RelayCommand]
    private async Task NavigateAsync(string page)
    {
        PageViewModel vm = page switch
        {
            "Dashboard" => services.GetRequiredService<DashboardViewModel>(),
            "Customers" => services.GetRequiredService<CustomersViewModel>(),
            "Products" => services.GetRequiredService<ProductsViewModel>(),
            "Sales" => services.GetRequiredService<SalesViewModel>(),
            "Reports" => services.GetRequiredService<ReportsViewModel>(),
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };
        CurrentPage = vm;
        await vm.LoadAsync();
    }
}
