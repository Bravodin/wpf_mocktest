using System.Data;
using ErpMock.Contracts.Models;
using ErpMock.Contracts.Services;
using ErpMock.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace ErpMock.Core.Services;

public class ReportService(IDbContextFactory<AppDbContext> factory) : IReportService
{
    public async Task<DataTable> GenerateAsync(ReportRequest request)
    {
        await using var db = await factory.CreateDbContextAsync();

        if (request.Type == ReportType.Inventory)
            return await InventoryAsync(db);

        var sales = await db.Sales.AsNoTracking()
            .Where(s => s.Date >= request.From.Date && s.Date < request.To.Date.AddDays(1))
            .Include(s => s.Customer)
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .AsSplitQuery()
            .OrderBy(s => s.Date)
            .ToListAsync();

        return request.Type switch
        {
            ReportType.SalesByPeriod => SalesByPeriod(sales),
            ReportType.SalesByCustomer => SalesByCustomer(sales),
            ReportType.SalesByProduct => SalesByProduct(sales),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };
    }

    private static DataTable SalesByPeriod(List<Sale> sales)
    {
        var t = NewTable(ReportType.SalesByPeriod,
            ("Nº", typeof(int)), ("Data", typeof(DateTime)), ("Cliente", typeof(string)),
            ("Itens", typeof(int)), ("Subtotal", typeof(decimal)), ("Desconto", typeof(decimal)),
            ("Total", typeof(decimal)), ("Status", typeof(string)));

        foreach (var s in sales)
            t.Rows.Add(s.Number, s.Date, s.Customer?.Name, s.Items.Sum(i => i.Quantity),
                s.Subtotal, s.Discount, s.Total, s.Status == SaleStatus.Completed ? "Faturada" : "Cancelada");
        return t;
    }

    private static DataTable SalesByCustomer(List<Sale> sales)
    {
        var t = NewTable(ReportType.SalesByCustomer,
            ("Cliente", typeof(string)), ("Documento", typeof(string)), ("Vendas", typeof(int)),
            ("Total", typeof(decimal)), ("Ticket médio", typeof(decimal)), ("Última compra", typeof(DateTime)));

        // Agrupa por Id: com AsNoTracking cada venda traz sua própria instância de Customer.
        var rows = sales.Where(s => s.Status == SaleStatus.Completed)
            .GroupBy(s => s.CustomerId)
            .Select(g => new { Customer = g.First().Customer!, Count = g.Count(), Total = g.Sum(s => s.Total), Last = g.Max(s => s.Date) })
            .OrderByDescending(x => x.Total);

        foreach (var r in rows)
            t.Rows.Add(r.Customer.Name, r.Customer.Document, r.Count, r.Total, Math.Round(r.Total / r.Count, 2), r.Last);
        return t;
    }

    private static DataTable SalesByProduct(List<Sale> sales)
    {
        var t = NewTable(ReportType.SalesByProduct,
            ("SKU", typeof(string)), ("Produto", typeof(string)), ("Categoria", typeof(string)),
            ("Qtd vendida", typeof(int)), ("Faturamento", typeof(decimal)), ("Custo", typeof(decimal)),
            ("Lucro bruto", typeof(decimal)));

        var rows = sales.Where(s => s.Status == SaleStatus.Completed)
            .SelectMany(s => s.Items)
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                Product = g.First().Product!,
                Qty = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.Total),
            })
            .Select(x => new { x.Product, x.Qty, x.Revenue, Cost = x.Qty * x.Product.Cost })
            .OrderByDescending(x => x.Revenue);

        foreach (var r in rows)
            t.Rows.Add(r.Product.Sku, r.Product.Name, r.Product.Category, r.Qty, r.Revenue, r.Cost, r.Revenue - r.Cost);
        return t;
    }

    private static async Task<DataTable> InventoryAsync(AppDbContext db)
    {
        var t = NewTable(ReportType.Inventory,
            ("SKU", typeof(string)), ("Produto", typeof(string)), ("Categoria", typeof(string)),
            ("Estoque", typeof(int)), ("Mínimo", typeof(int)), ("Custo unitário", typeof(decimal)),
            ("Valor em estoque", typeof(decimal)), ("Situação", typeof(string)));

        var products = await db.Products.AsNoTracking().Where(p => p.IsActive).ToListAsync();
        foreach (var p in products.OrderBy(p => p.Category).ThenBy(p => p.Name))
            t.Rows.Add(p.Sku, p.Name, p.Category, p.Stock, p.MinStock, p.Cost, p.Stock * p.Cost,
                p.Stock == 0 ? "Sem estoque" : p.IsLowStock ? "Repor" : "OK");
        return t;
    }

    private static DataTable NewTable(ReportType type, params (string Name, Type Type)[] columns)
    {
        var t = new DataTable(Reports.GetTitle(type));
        foreach (var (name, colType) in columns) t.Columns.Add(name, colType);
        return t;
    }

    public string ToCsv(DataTable table) => Reports.ToCsv(table);
}
