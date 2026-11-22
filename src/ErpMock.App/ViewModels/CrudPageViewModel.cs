using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErpMock.App.Services;
using ErpMock.Core.Models;

namespace ErpMock.App.ViewModels;

/// <summary>Formulário de edição com validação (INotifyDataErrorInfo via ObservableValidator).</summary>
public abstract partial class EditorViewModel<TModel> : ObservableValidator where TModel : Entity
{
    public int Id { get; init; }
    public bool IsNew => Id == 0;
    public abstract string Title { get; }

    public abstract TModel ToModel();

    public bool Validate()
    {
        ValidateAllProperties();
        return !HasErrors;
    }
}

/// <summary>Página de cadastro padrão: lista com busca + painel lateral de edição.</summary>
public abstract partial class CrudPageViewModel<TModel, TEditor>(IDialogService dialogs) : PageViewModel(dialogs)
    where TModel : Entity
    where TEditor : EditorViewModel<TModel>
{
    private CancellationTokenSource? _searchCts;

    public ObservableCollection<TModel> Items { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand), nameof(DeleteCommand))]
    public partial TModel? Selected { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    public partial TEditor? Editor { get; set; }

    [ObservableProperty]
    public partial string? SearchText { get; set; }

    [ObservableProperty]
    public partial bool OnlyActive { get; set; }

    public bool IsEditing => Editor is not null;

    protected abstract Task<List<TModel>> QueryAsync(string? search, bool onlyActive);
    protected abstract TEditor CreateEditor(TModel? model);
    protected abstract Task SaveModelAsync(TModel model);
    protected abstract Task DeleteModelAsync(TModel model);
    protected abstract string Describe(TModel model);

    public override Task LoadAsync() => RunAsync(ReloadAsync);

    private async Task ReloadAsync()
    {
        var selectedId = Selected?.Id;
        var list = await QueryAsync(SearchText, OnlyActive);
        Items.Clear();
        foreach (var item in list) Items.Add(item);
        Selected = Items.FirstOrDefault(i => i.Id == selectedId);
    }

    // Busca com debounce: espera o usuário parar de digitar por 300 ms.
    async partial void OnSearchTextChanged(string? value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(300, _searchCts.Token);
            await LoadAsync();
        }
        catch (TaskCanceledException) { }
    }

    async partial void OnOnlyActiveChanged(bool value) => await LoadAsync();

    private bool HasSelection() => Selected is not null;

    [RelayCommand]
    private void New() => Editor = CreateEditor(null);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Edit() => Editor = CreateEditor(Selected);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteAsync()
    {
        if (Selected is not { } item) return;
        if (!Dialogs.Confirm($"Excluir {Describe(item)}?")) return;

        if (await RunAsync(() => DeleteModelAsync(item)))
        {
            Items.Remove(item);
            Selected = null;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Editor is null || !Editor.Validate()) return;

        var model = Editor.ToModel();
        if (await RunAsync(() => SaveModelAsync(model)))
        {
            Editor = null;
            await LoadAsync();
            Selected = Items.FirstOrDefault(i => i.Id == model.Id);
        }
    }

    [RelayCommand]
    private void CancelEdit() => Editor = null;
}
