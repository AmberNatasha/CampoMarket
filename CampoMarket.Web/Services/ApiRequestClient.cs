using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace CampoMarket.Web.Services;

public sealed class ApiRequestClient(HttpClient http, IHttpContextAccessor context)
{
    public T Get<T>(string endpoint) => Send<T>(HttpMethod.Get, endpoint, null);
    public T Post<T>(string endpoint, object? body = null) => Send<T>(HttpMethod.Post, endpoint, body);
    public T Put<T>(string endpoint, object? body = null) => Send<T>(HttpMethod.Put, endpoint, body);
    public void Delete(string endpoint) => Send<object?>(HttpMethod.Delete, endpoint, null);

    private T Send<T>(HttpMethod method, string endpoint, object? body)
    {
        using var request = new HttpRequestMessage(method, endpoint);
        var token = context.HttpContext?.User.FindFirstValue("access_token");
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = http.Send(request);
        if (!response.IsSuccessStatusCode)
        {
            var detail = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(detail) ? $"La API respondió {(int)response.StatusCode}." : detail);
        }
        if (typeof(T) == typeof(object) || response.Content.Headers.ContentLength == 0) return default!;
        return response.Content.ReadFromJsonAsync<T>().GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("La API devolvió una respuesta vacía.");
    }
}
