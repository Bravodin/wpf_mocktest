using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using ErpMock.App.Services;
using ErpMock.Contracts.Models;
using ErpMock.Contracts.Services;

namespace ErpMock.App.ViewModels;

public class CustomersViewModel(ICustomerService service, IDialogService dialogs)
    : CrudPageViewModel<Customer, CustomerEditorViewModel>(dialogs)
{
    public override string Title => "Clientes";

    protected override Task<List<Customer>> QueryAsync(string? search, bool onlyActive) =>
        service.GetAllAsync(search, onlyActive);

    protected override CustomerEditorViewModel CreateEditor(Customer? model) => new(model);
    protected override Task SaveModelAsync(Customer model) => service.SaveAsync(model);
    protected override Task DeleteModelAsync(Customer model) => service.DeleteAsync(model.Id);
    protected override string Describe(Customer model) => $"o cliente \"{model.Name}\"";
}

public partial class CustomerEditorViewModel : EditorViewModel<Customer>
{
    private readonly DateTime _createdAt;

    public CustomerEditorViewModel(Customer? model)
    {
        var c = model ?? new Customer();
        Id = c.Id;
        _createdAt = c.Id == 0 ? DateTime.Now : c.CreatedAt;
        Name = c.Name;
        Type = c.Type;
        Document = c.Document;
        Email = c.Email;
        Phone = c.Phone;
        City = c.City;
        State = c.State;
        IsActive = c.IsActive;
    }

    public override string Title => IsNew ? "Novo cliente" : "Editar cliente";

    public static IReadOnlyList<KeyValuePair<PersonType, string>> PersonTypes { get; } =
    [
        new(PersonType.Individual, "Pessoa Física"),
        new(PersonType.Company, "Pessoa Jurídica"),
    ];

    public static IReadOnlyList<string> States { get; } =
        ["AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA", "PB", "PR",
         "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"];

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Informe o nome.")]
    [MinLength(3, ErrorMessage = "Mínimo de 3 caracteres.")]
    [MaxLength(150)]
    public partial string Name { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DocumentLabel))]
    public partial PersonType Type { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Informe o documento.")]
    [CustomValidation(typeof(CustomerEditorViewModel), nameof(ValidateDocument))]
    public partial string Document { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public partial string? Email { get; set; }

    [ObservableProperty]
    public partial string? Phone { get; set; }

    [ObservableProperty]
    public partial string? City { get; set; }

    [ObservableProperty]
    public partial string? State { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    public string DocumentLabel => Type == PersonType.Individual ? "CPF" : "CNPJ";

    // Mudou PF/PJ → revalida o documento.
    partial void OnTypeChanged(PersonType value)
    {
        if (!string.IsNullOrEmpty(Document)) ValidateProperty(Document, nameof(Document));
    }

    public static ValidationResult? ValidateDocument(string? document, ValidationContext context)
    {
        var vm = (CustomerEditorViewModel)context.ObjectInstance;
        var ok = vm.Type == PersonType.Individual
            ? DocumentValidator.IsValidCpf(document)
            : DocumentValidator.IsValidCnpj(document);
        return ok ? ValidationResult.Success : new ValidationResult($"{vm.DocumentLabel} inválido.");
    }

    public override Customer ToModel() => new()
    {
        Id = Id,
        Name = Name.Trim(),
        Type = Type,
        Document = Document,
        Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
        Phone = Phone,
        City = City,
        State = State,
        IsActive = IsActive,
        CreatedAt = _createdAt,
    };
}
