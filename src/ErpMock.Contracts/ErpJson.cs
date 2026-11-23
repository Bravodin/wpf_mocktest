using System.Text.Json;
using System.Text.Json.Serialization;

namespace ErpMock.Contracts;

/// <summary>Opções de JSON usadas pelos dois lados (API e clientes): camelCase e enums como texto.</summary>
public static class ErpJson
{
    public static JsonSerializerOptions Options { get; } = Configure(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    public static JsonSerializerOptions Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
