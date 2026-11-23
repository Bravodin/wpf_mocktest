using ErpMock.Contracts.Models;
using ErpMock.Contracts.Services;
using ErpMock.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace ErpMock.Core.Services;

public class CustomerService(IDbContextFactory<AppDbContext> factory) : ICustomerService
{
    public async Task<List<Customer>> GetAllAsync(string? search = null, bool onlyActive = false)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Customers.AsNoTracking();

        if (onlyActive) query = query.Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(s)
                                  || c.Document.Contains(s)
                                  || (c.Email != null && c.Email.ToLower().Contains(s))
                                  || (c.City != null && c.City.ToLower().Contains(s)));
        }
        return await query.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Customer> SaveAsync(Customer customer)
    {
        var valid = customer.Type == PersonType.Individual
            ? DocumentValidator.IsValidCpf(customer.Document)
            : DocumentValidator.IsValidCnpj(customer.Document);
        if (!valid)
            throw new BusinessException(customer.Type == PersonType.Individual ? "CPF inválido." : "CNPJ inválido.");

        customer.Document = DocumentValidator.Format(customer.Document);

        await using var db = await factory.CreateDbContextAsync();
        if (await db.Customers.AnyAsync(c => c.Document == customer.Document && c.Id != customer.Id))
            throw new BusinessException("Já existe um cliente com este documento.");

        if (customer.Id == 0) db.Customers.Add(customer);
        else db.Customers.Update(customer);

        await db.SaveChangesAsync();
        return customer;
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        if (await db.Sales.AnyAsync(s => s.CustomerId == id))
            throw new BusinessException("Cliente possui vendas e não pode ser excluído. Desative-o.");
        await db.Customers.Where(c => c.Id == id).ExecuteDeleteAsync();
    }
}
