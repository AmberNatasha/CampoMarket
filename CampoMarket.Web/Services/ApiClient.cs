using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace CampoMarket.Web.Services;

public sealed class ApiClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private string? GetAccessToken()
    {
        return _httpContextAccessor.HttpContext?
            .User
            .FindFirst("access_token")?
            .Value;
    }

    public async Task<HttpResponseMessage> GetAsync(
        string endpoint,
        string? accessToken = null)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        var token = accessToken ?? GetAccessToken();

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> PostAsync(
        string endpoint,
        HttpContent content,
        string? accessToken = null)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            endpoint)
        {
            Content = content
        };

        var token = accessToken ?? GetAccessToken();

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return await _httpClient.SendAsync(request);
    }

    public async Task<ApiLoginResponse?> LoginAsync(
        string email,
        string password)
    {
        var request = new ApiLoginRequest(email, password);

        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/login",
            request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<ApiLoginResponse>();
    }
}