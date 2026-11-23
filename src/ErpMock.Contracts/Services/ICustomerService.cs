using ErpMock.Contracts.Models;

namespace ErpMock.Contracts.Services;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync(string? search = null, bool onlyActive = false);
    Task<Customer> SaveAsync(Customer customer);
    Task DeleteAsync(int id);
}
