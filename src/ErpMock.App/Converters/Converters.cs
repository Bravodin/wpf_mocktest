using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ErpMock.Core.Models;

namespace ErpMock.App.Converters;

/// <summary>null/vazio → Collapsed. Use ConverterParameter="Invert" para inverter.</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hasValue = value switch
        {
            null => false,
            string s => s.Length > 0,
            int i => i > 0,
            System.Collections.ICollection c => c.Count > 0,
            _ => true
        };
        if (parameter as string == "Invert") hasValue = !hasValue;
        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is false;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is false;
}

public class SaleStatusToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is SaleStatus.Cancelled ? "Cancelada" : "Faturada";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class PersonTypeToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is PersonType.Company ? "PJ" : "PF";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Retorna um brush do tema conforme o "nível" do valor:
/// bool (true = sucesso), SaleStatus, ou decimal (positivo/negativo).
/// ConverterParameter="Light" retorna a variação clara (fundo de badge).
/// </summary>
public class LevelToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var level = value switch
        {
            bool b => b ? "Success" : "Danger",
            SaleStatus s => s == SaleStatus.Completed ? "Success" : "Danger",
            decimal d => d >= 0 ? "Success" : "Danger",
            _ => "Warning"
        };
        var key = level + (parameter as string == "Light" ? "LightBrush" : "Brush");
        return Application.Current.TryFindResource(key) as Brush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
