namespace ErpMock.Core.Models;

public enum PersonType
{
    Individual = 0,   // Pessoa Física (CPF)
    Company = 1       // Pessoa Jurídica (CNPJ)
}

public class Customer : Entity
{
    public string Name { get; set; } = string.Empty;
    public PersonType Type { get; set; }
    public string Document { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<Sale> Sales { get; set; } = [];
}
