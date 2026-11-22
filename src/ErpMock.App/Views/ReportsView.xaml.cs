using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ErpMock.App.Views;

public partial class ReportsView : UserControl
{
    public ReportsView() => InitializeComponent();

    /// <summary>
    /// Colunas geradas a partir do DataTable: formata moeda/data e alinha números à direita.
    /// (Formatação é preocupação da View, por isso fica no code-behind.)
    /// </summary>
    private void OnAutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.Column is not DataGridTextColumn col || col.Binding is not Binding binding) return;

        e.Column.Header = e.PropertyName.ToUpperInvariant();
        e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

        if (e.PropertyType == typeof(decimal))
            binding.StringFormat = "C";
        else if (e.PropertyType == typeof(DateTime))
            binding.StringFormat = "dd/MM/yyyy HH:mm";

        if (e.PropertyType == typeof(decimal) || e.PropertyType == typeof(int))
            col.ElementStyle = (Style)FindResource("RightCell");
    }
}
