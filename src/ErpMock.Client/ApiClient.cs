using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using ErpMock.Contracts;
using ErpMock.Contracts.Services;

namespace ErpMock.Client;

/// <summary>A API não respondeu (fora do ar, porta errada, timeout).</summary>
public class ApiUnavailableException(string message, Exception inner) : Exception(message, inner);

/// <summary>
/// Base dos clientes: serializa com <see cref="ErpJson.Options"/> e traduz as respostas de erro.
/// Um 4xx com "detail" (ProblemDetails) vira <see cref="BusinessException"/>, então a UI mostra a mensagem
/// da regra exatamente como mostrava quando a regra rodava dentro do app.
/// </summary>
public abstract class ApiClient(HttpClient http)
{
    protected Task<T> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url);

    protected async Task<T> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        using var response = await SendCoreAsync(method, url, body);
        return await response.Content.ReadFromJsonAsync<T>(ErpJson.Options)
            ?? throw new InvalidOperationException($"Resposta vazia de {method} {url}.");
    }

    protected async Task SendAsync(HttpMethod method, string url, object? body = null)
    {
        using var _ = await SendCoreAsync(method, url, body);
    }

    /// <summary>GET que devolve null em 404 (ex.: venda que não existe).</summary>
    protected async Task<T?> GetOrNullAsync<T>(string url) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await ExchangeAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(ErpJson.Options);
    }

    protected static string Query(params (string Name, object? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => p.Value is not null && p.Value is not "")
            .Select(p => $"{p.Name}={Uri.EscapeDataString(Format(p.Value!))}")
            .ToList();
        return parts.Count == 0 ? "" : "?" + string.Join('&', parts);

        // Datas vão só como dia (yyyy-MM-dd): os filtros são por dia e assim não há conversão de fuso.
        static string Format(object value) => value switch
        {
            DateTime d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
        };
    }

    private async Task<HttpResponseMessage> SendCoreAsync(HttpMethod method, string url, object? body)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null) request.Content = JsonContent.Create(body, body.GetType(), options: ErpJson.Options);
        var response = await ExchangeAsync(request);
        try
        {
            await EnsureSuccessAsync(response);
            return response;
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private async Task<HttpResponseMessage> ExchangeAsync(HttpRequestMessage request)
    {
        try
        {
            return await http.SendAsync(request);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is null)
        {
            throw new ApiUnavailableException(
                $"Não foi possível conectar à API em {http.BaseAddress}. Verifique se ela está rodando (docker compose up -d).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiUnavailableException(
                $"A API em {http.BaseAddress} não respondeu em {http.Timeout.TotalSeconds:0} s.", ex);
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        Problem? problem = null;
        try { problem = await response.Content.ReadFromJsonAsync<Problem>(ErpJson.Options); }
        catch (Exception) { /* corpo não é ProblemDetails */ }

        var code = (int)response.StatusCode;
        if (code is >= 400 and < 500 && !string.IsNullOrWhiteSpace(problem?.Detail))
            throw new BusinessException(problem.Detail);

        throw new HttpRequestException(
            $"A API respondeu {code} ({problem?.Title ?? response.ReasonPhrase}).", null, response.StatusCode);
    }

    private sealed record Problem(string? Title, string? Detail);
}
