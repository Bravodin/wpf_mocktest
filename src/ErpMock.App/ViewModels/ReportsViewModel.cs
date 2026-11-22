using System.Data;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErpMock.App.Services;
using ErpMock.Core.Services;

namespace ErpMock.App.ViewModels;

public record ReportOption(ReportType Type, string Title, string Description);

public partial class ReportsViewModel(IReportService reports, IPrintService printer, IDialogService dialogs)
    : PageViewModel(dialogs)
{
    public override string Title => "Relatórios";

    public static IReadOnlyList<ReportOption> Options { get; } =
    [
        new(ReportType.SalesByPeriod, ReportService.GetTitle(ReportType.SalesByPeriod), "Todas as vendas do período, com status."),
        new(ReportType.SalesByCustomer, ReportService.GetTitle(ReportType.SalesByCustomer), "Ranking de clientes por faturamento."),
        new(ReportType.SalesByProduct, ReportService.GetTitle(ReportType.SalesByProduct), "Quantidade, faturamento e lucro bruto por produto."),
        new(ReportType.Inventory, ReportService.GetTitle(ReportType.Inventory), "Estoque atual e valor imobilizado."),
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UsesPeriod))]
    public partial ReportOption Selected { get; set; } = Options[0];

    [ObservableProperty]
    public partial DateTime From { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    public partial DateTime To { get; set; } = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand), nameof(PrintCommand))]
    public partial DataTable? Result { get; set; }

    [ObservableProperty]
    public partial string? Footer { get; set; }

    public bool UsesPeriod => Selected.Type != ReportType.Inventory;
    public bool HasResult => Result is not null;

    private string PeriodText => UsesPeriod ? $"Período: {From:dd/MM/yyyy} a {To:dd/MM/yyyy}" : "Posição atual";

    public override Task LoadAsync() => Task.CompletedTask;

    partial void OnSelectedChanged(ReportOption value) => Result = null;

    [RelayCommand]
    private Task GenerateAsync()
    {
        if (UsesPeriod && From > To)
        {
            Dialogs.ShowWarning("A data inicial deve ser anterior à final.");
            return Task.CompletedTask;
        }

        return RunAsync(async () =>
        {
            var table = await reports.GenerateAsync(new ReportRequest(Selected.Type, From, To));
            Footer = BuildFooter(table);
            Result = table;
        });
    }

    [RelayCommand(CanExecute = nameof(HasResult))]
    private async Task ExportCsvAsync()
    {
        var path = Dialogs.AskSaveFile($"{Selected.Type}_{DateTime.Now:yyyyMMdd_HHmm}.csv", "CSV (*.csv)|*.csv");
        if (path is null) return;

        // UTF-8 com BOM para o Excel reconhecer acentuação.
        if (await RunAsync(() => File.WriteAllTextAsync(path, reports.ToCsv(Result!), new UTF8Encoding(true))))
            Dialogs.ShowInfo($"Arquivo exportado:\n{path}");
    }

    [RelayCommand(CanExecute = nameof(HasResult))]
    private void Print() => printer.PrintTable(Result!, PeriodText);

    /// <summary>Totaliza colunas monetárias conhecidas para o rodapé.</summary>
    private static string BuildFooter(DataTable table)
    {
        var parts = new List<string> { $"{table.Rows.Count} registro(s)" };
        foreach (var col in new[] { "Total", "Faturamento", "Lucro bruto", "Valor em estoque" })
        {
            if (!table.Columns.Contains(col)) continue;
            var rows = table.AsEnumerable();
            if (table.Columns.Contains("Status")) rows = rows.Where(r => r.Field<string>("Status") == "Faturada");
            parts.Add($"{col}: {rows.Sum(r => r.Field<decimal>(col)):C}");
        }
        return string.Join("   •   ", parts);
    }
}
