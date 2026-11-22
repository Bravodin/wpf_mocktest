using System.Data;
using System.Globalization;
using System.Text;
using ErpMock.Core.Data;
using ErpMock.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpMock.Core.Services;

public enum ReportType
{
    SalesByPeriod,
    SalesByCustomer,
    SalesByProduct,
    Inventory
}

public record ReportRequest(ReportType Type, DateTime From, DateTime To);

/// <summary>
/// Os relatórios retornam DataTable: liga direto num DataGrid, exporta para CSV
/// e imprime sem precisar de um modelo por relatório.
/// </summary>
public interface IReportService
{
    Task<DataTable> GenerateAsync(ReportRequest request);
    string ToCsv(DataTable table);
}

public class ReportService(IDbContextFactory<AppDbContext> factory) : IReportService
{
    public static string GetTitle(ReportType type) => type switch
    {
        ReportType.SalesByPeriod => "Vendas por período",
        ReportType.SalesByCustomer => "Vendas por cliente",
        ReportType.SalesByProduct => "Vendas por produto",
        ReportType.Inventory => "Posição de estoque",
        _ => type.ToString()
    };

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
        var t = new DataTable(GetTitle(type));
        foreach (var (name, colType) in columns) t.Columns.Add(name, colType);
        return t;
    }

    public string ToCsv(DataTable table)
    {
        // Padrão brasileiro: separador ';' e vírgula decimal (abre direto no Excel pt-BR).
        var culture = new CultureInfo("pt-BR");
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', table.Columns.Cast<DataColumn>().Select(c => Escape(c.ColumnName))));
        foreach (DataRow row in table.Rows)
        {
            sb.AppendLine(string.Join(';', row.ItemArray.Select(v => Escape(v switch
            {
                DateTime d => d.ToString("dd/MM/yyyy HH:mm", culture),
                decimal m => m.ToString("0.00", culture),
                null or DBNull => string.Empty,
                _ => Convert.ToString(v, culture) ?? string.Empty
            }))));
        }
        return sb.ToString();

        static string Escape(string s) =>
            s.IndexOfAny([';', '"', '\n']) >= 0 ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
    }
}
