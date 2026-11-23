using CommunityToolkit.Mvvm.ComponentModel;
using ErpMock.App.Services;
using ErpMock.Client;
using ErpMock.Contracts.Services;

namespace ErpMock.App.ViewModels;

public abstract partial class ViewModelBase(IDialogService dialogs) : ObservableObject
{
    protected IDialogService Dialogs { get; } = dialogs;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>Executa uma operação async com indicador de carregamento e tratamento de erro padrão.</summary>
    protected async Task<bool> RunAsync(Func<Task> action)
    {
        IsBusy = true;
        try
        {
            await action();
            return true;
        }
        catch (BusinessException ex)
        {
            Dialogs.ShowWarning(ex.Message);
        }
        catch (ApiUnavailableException ex)
        {
            Dialogs.ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            Dialogs.ShowError($"Erro inesperado: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
        return false;
    }
}

/// <summary>Uma "página" navegável no menu lateral.</summary>
public abstract class PageViewModel(IDialogService dialogs) : ViewModelBase(dialogs)
{
    public abstract string Title { get; }
    public abstract Task LoadAsync();
}
