using ErpMock.Contracts.Models;
using ErpMock.Contracts.Services;
using ErpMock.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace ErpMock.Core.Services;

public class SaleService(IDbContextFactory<AppDbContext> factory) : ISaleService
{
    public async Task<List<Sale>> GetAsync(SaleFilter filter)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Sales.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (filter.From is { } from) query = query.Where(s => s.Date >= from.Date);
        if (filter.To is { } to) query = query.Where(s => s.Date < to.Date.AddDays(1));
        if (filter.Status is { } status) query = query.Where(s => s.Status == status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = int.TryParse(s, out var number)
                ? query.Where(x => x.Number == number)
                : query.Where(x => x.Customer!.Name.ToLower().Contains(s));
        }

        return await query.OrderByDescending(s => s.Date).AsSplitQuery().ToListAsync();
    }

    public async Task<Sale?> GetByIdAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Sales.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Sale> CreateAsync(NewSale request)
    {
        if (request.Items.Count == 0)
            throw new BusinessException("Adicione ao menos um item à venda.");
        if (request.Items.Any(i => i.Quantity <= 0))
            throw new BusinessException("Quantidade deve ser maior que zero.");
        if (request.Discount < 0)
            throw new BusinessException("Desconto não pode ser negativo.");

        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var customer = await db.Customers.FindAsync(request.CustomerId)
            ?? throw new BusinessException("Cliente não encontrado.");
        if (!customer.IsActive)
            throw new BusinessException("Cliente está inativo.");

        var sale = new Sale
        {
            CustomerId = customer.Id,
            Date = DateTime.Now,
            Discount = request.Discount,
            Notes = request.Notes,
            Status = SaleStatus.Completed,
            Number = (await db.Sales.MaxAsync(s => (int?)s.Number) ?? 0) + 1,
        };

        // Agrupa por produto: o mesmo item lançado duas vezes soma a quantidade.
        foreach (var group in request.Items.GroupBy(i => i.ProductId))
        {
            var product = await db.Products.FindAsync(group.Key)
                ?? throw new BusinessException($"Produto {group.Key} não encontrado.");
            var qty = group.Sum(i => i.Quantity);

            if (!product.IsActive)
                throw new BusinessException($"Produto {product.Name} está inativo.");
            if (product.Stock < qty)
                throw new BusinessException($"Estoque insuficiente para {product.Name} (disponível: {product.Stock}).");

            product.Stock -= qty;
            sale.Items.Add(new SaleItem { ProductId = product.Id, Quantity = qty, UnitPrice = group.First().UnitPrice });
        }

        if (sale.Discount > sale.Subtotal)
            throw new BusinessException("Desconto maior que o valor da venda.");

        db.Sales.Add(sale);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return sale;
    }

    public async Task CancelAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var sale = await db.Sales.Include(s => s.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new BusinessException("Venda não encontrada.");
        if (sale.Status == SaleStatus.Cancelled)
            throw new BusinessException("Venda já está cancelada.");

        foreach (var item in sale.Items)
            item.Product!.Stock += item.Quantity; // devolve ao estoque

        sale.Status = SaleStatus.Cancelled;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
