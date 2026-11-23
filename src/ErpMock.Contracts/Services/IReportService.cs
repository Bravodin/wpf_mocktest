using System.Data;
using System.Globalization;
using System.Text;

namespace ErpMock.Contracts.Services;

public enum ReportType
{
    SalesByPeriod,
    SalesByCustomer,
    SalesByProduct,
    Inventory
}

public record ReportRequest(ReportType Type, DateTime From, DateTime To);

/// <summary>
/// Os relatórios retornam DataTable: liga direto num DataGrid, exporta para CSV
/// e imprime sem precisar de um modelo por relatório. Na API trafegam como <see cref="ReportData"/>.
/// </summary>
public interface IReportService
{
    Task<DataTable> GenerateAsync(ReportRequest request);
    string ToCsv(DataTable table);
}

public static class Reports
{
    public static string GetTitle(ReportType type) => type switch
    {
        ReportType.SalesByPeriod => "Vendas por período",
        ReportType.SalesByCustomer => "Vendas por cliente",
        ReportType.SalesByProduct => "Vendas por produto",
        ReportType.Inventory => "Posição de estoque",
        _ => type.ToString()
    };

    /// <summary>Padrão brasileiro: separador ';' e vírgula decimal (abre direto no Excel pt-BR).</summary>
    public static string ToCsv(DataTable table)
    {
        var culture = new CultureInfo("pt-BR");
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', table.Columns.Cast<DataColumn>().Select(c => Escape(c.ColumnName))));
        foreach (DataRow row in table.Rows)
        {
            sb.AppendLine(string.Join(';', row.ItemArray.Select(v => Escape(v switch
            {
                DateTime d => d.ToString("dd/MM/yyyy HH:mm", culture),
                decimal m => m.ToString("0.00", culture),
                null or DBNull => string.Empty,
                _ => Convert.ToString(v, culture) ?? string.Empty
            }))));
        }
        return sb.ToString();

        static string Escape(string s) =>
            s.IndexOfAny([';', '"', '\n']) >= 0 ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
    }
}
