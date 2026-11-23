using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using ErpMock.App.Services;
using ErpMock.App.ViewModels;
using ErpMock.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ErpMock.App;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ConfigureCulture();
        DispatcherUnhandledException += OnUnhandledException;

        // appsettings.json ao lado do .exe (não do diretório atual, que muda conforme o atalho).
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory });

        // As regras de negócio rodam na API (ErpMock.Api); o app só chama por HTTP.
        // Troque o endereço em appsettings.json ou com a variável de ambiente ErpApi__BaseUrl.
        var apiUrl = builder.Configuration["ErpApi:BaseUrl"] ?? "http://localhost:8080/";
        builder.Services.AddErpApiClient(new Uri(apiUrl));

        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<IPrintService, PrintService>();

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<CustomersViewModel>();
        builder.Services.AddTransient<ProductsViewModel>();
        builder.Services.AddTransient<SalesViewModel>();
        builder.Services.AddTransient<ReportsViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        _host = builder.Build();
        await _host.StartAsync();

        var window = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
        await _host.Services.GetRequiredService<MainViewModel>().NavigateCommand.ExecuteAsync("Dashboard");
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }

    private static void ConfigureCulture()
    {
        var culture = new CultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        // Faz os bindings (StringFormat=C, datas, decimais) usarem pt-BR.
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.Message, "Erro inesperado", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
