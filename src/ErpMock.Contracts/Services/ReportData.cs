using System.Data;
using System.Globalization;
using System.Text.Json;

namespace ErpMock.Contracts.Services;

/// <summary>Tipo de uma coluna de relatório no JSON: "int", "decimal", "string" ou "datetime".</summary>
public sealed record ReportColumn(string Name, string Type);

/// <summary>
/// Um <see cref="DataTable"/> em formato que vai e volta em JSON (o System.Text.Json não serializa DataTable).
/// A API converte com <see cref="FromTable"/> e o cliente reconstrói com <see cref="ToTable"/>.
/// </summary>
public sealed record ReportData(string Title, IReadOnlyList<ReportColumn> Columns, IReadOnlyList<object?[]> Rows)
{
    private static readonly Dictionary<Type, string> TypeNames = new()
    {
        [typeof(int)] = "int",
        [typeof(decimal)] = "decimal",
        [typeof(string)] = "string",
        [typeof(DateTime)] = "datetime",
    };

    private static readonly Dictionary<string, Type> TypesByName = TypeNames.ToDictionary(p => p.Value, p => p.Key);

    public static ReportData FromTable(DataTable table)
    {
        var columns = table.Columns.Cast<DataColumn>()
            .Select(c => new ReportColumn(c.ColumnName, TypeNames.TryGetValue(c.DataType, out var name)
                ? name
                : throw new NotSupportedException($"Tipo de coluna não suportado: {c.DataType}.")))
            .ToList();
        var rows = table.Rows.Cast<DataRow>()
            .Select(r => r.ItemArray.Select(v => v is DBNull ? null : v).ToArray())
            .ToList();
        return new ReportData(table.TableName, columns, rows);
    }

    public DataTable ToTable()
    {
        var table = new DataTable(Title);
        foreach (var c in Columns)
            table.Columns.Add(c.Name, TypesByName.TryGetValue(c.Type, out var type)
                ? type
                : throw new NotSupportedException($"Tipo de coluna não suportado: {c.Type}."));

        foreach (var row in Rows)
            table.Rows.Add(row.Select((v, i) => Read(v, table.Columns[i].DataType)).ToArray());
        return table;
    }

    // Vindo do JSON cada valor é um JsonElement; montado em memória já tem o tipo certo.
    private static object Read(object? value, Type type) => value switch
    {
        null => DBNull.Value,
        JsonElement { ValueKind: JsonValueKind.Null } => DBNull.Value,
        JsonElement e when type == typeof(int) => e.GetInt32(),
        JsonElement e when type == typeof(decimal) => e.GetDecimal(),
        JsonElement e when type == typeof(DateTime) => e.GetDateTime(),
        JsonElement e => e.GetString() ?? (object)DBNull.Value,
        _ => Convert.ChangeType(value, type, CultureInfo.InvariantCulture),
    };
}
