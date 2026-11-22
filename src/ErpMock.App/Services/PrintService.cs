using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ErpMock.App.Services;

public interface IPrintService
{
    void PrintTable(DataTable table, string subtitle);
}

/// <summary>Monta um FlowDocument a partir de um DataTable e envia ao PrintDialog (inclui "Microsoft Print to PDF").</summary>
public class PrintService : IPrintService
{
    public void PrintTable(DataTable table, string subtitle)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;

        var doc = BuildDocument(table, subtitle);
        doc.PageWidth = dialog.PrintableAreaWidth;
        doc.PageHeight = dialog.PrintableAreaHeight;
        doc.ColumnWidth = double.PositiveInfinity;

        dialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, table.TableName);
    }

    public static FlowDocument BuildDocument(DataTable table, string subtitle)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var doc = new FlowDocument { FontFamily = new FontFamily("Segoe UI"), FontSize = 10, PagePadding = new Thickness(40) };

        doc.Blocks.Add(new Paragraph(new Run(table.TableName)) { FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0) });
        doc.Blocks.Add(new Paragraph(new Run($"{subtitle} — emitido em {DateTime.Now:dd/MM/yyyy HH:mm}"))
        {
            Foreground = Brushes.Gray, Margin = new Thickness(0, 2, 0, 12)
        });

        var t = new Table { CellSpacing = 0, BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 1) };
        foreach (DataColumn _ in table.Columns) t.Columns.Add(new TableColumn());

        var header = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9)), FontWeight = FontWeights.SemiBold };
        foreach (DataColumn c in table.Columns) header.Cells.Add(Cell(c.ColumnName, IsNumeric(c)));
        var group = new TableRowGroup();
        group.Rows.Add(header);

        foreach (DataRow row in table.Rows)
        {
            var tr = new TableRow();
            foreach (DataColumn c in table.Columns)
            {
                var text = row[c] switch
                {
                    DateTime d => d.ToString("dd/MM/yyyy", culture),
                    decimal m => m.ToString("N2", culture),
                    var v => Convert.ToString(v, culture) ?? string.Empty
                };
                tr.Cells.Add(Cell(text, IsNumeric(c)));
            }
            group.Rows.Add(tr);
        }
        t.RowGroups.Add(group);
        doc.Blocks.Add(t);
        doc.Blocks.Add(new Paragraph(new Run($"{table.Rows.Count} registro(s)")) { Foreground = Brushes.Gray, Margin = new Thickness(0, 8, 0, 0) });
        return doc;

        static bool IsNumeric(DataColumn c) => c.DataType == typeof(decimal) || c.DataType == typeof(int);

        static TableCell Cell(string text, bool right) => new(new Paragraph(new Run(text))
        {
            Margin = new Thickness(0),
            TextAlignment = right ? TextAlignment.Right : TextAlignment.Left
        })
        {
            Padding = new Thickness(4, 3, 4, 3),
            BorderBrush = Brushes.Gainsboro,
            BorderThickness = new Thickness(0, 0, 0, 0.5)
        };
    }
}
