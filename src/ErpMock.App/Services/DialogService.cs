using System.Windows;
using Microsoft.Win32;

namespace ErpMock.App.Services;

/// <summary>Abstrai MessageBox/diálogos para que os ViewModels sejam testáveis.</summary>
public interface IDialogService
{
    void ShowInfo(string message, string title = "ERP Mock");
    void ShowWarning(string message, string title = "Atenção");
    void ShowError(string message, string title = "Erro");
    bool Confirm(string message, string title = "Confirmação");
    string? AskSaveFile(string defaultFileName, string filter);
}

public class DialogService : IDialogService
{
    private static Window? Owner => Application.Current.MainWindow;

    public void ShowInfo(string message, string title = "ERP Mock") =>
        MessageBox.Show(Owner!, message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowWarning(string message, string title = "Atenção") =>
        MessageBox.Show(Owner!, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    public void ShowError(string message, string title = "Erro") =>
        MessageBox.Show(Owner!, message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public bool Confirm(string message, string title = "Confirmação") =>
        MessageBox.Show(Owner!, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public string? AskSaveFile(string defaultFileName, string filter)
    {
        var dlg = new SaveFileDialog { FileName = defaultFileName, Filter = filter };
        return dlg.ShowDialog(Owner) == true ? dlg.FileName : null;
    }
}
