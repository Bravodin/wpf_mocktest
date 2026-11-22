using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using ErpMock.App.Services;
using ErpMock.App.ViewModels;
using ErpMock.Core;
using ErpMock.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErpMock.App;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ConfigureCulture();
        DispatcherUnhandledException += OnUnhandledException;

        var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ErpMock");
        Directory.CreateDirectory(dataDir);

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

        builder.Services.AddErpCore($"Data Source={Path.Combine(dataDir, "erp.db")}");

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

        // Cria o banco SQLite e popula com dados fictícios na primeira execução.
        var factory = _host.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DataSeeder.SeedAsync(db);
        }

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
