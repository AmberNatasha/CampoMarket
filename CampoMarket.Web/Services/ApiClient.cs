using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CampoMarket.Web.Services;

public sealed class ApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<HttpResponseMessage> GetAsync(
        string endpoint,
        string? accessToken = null)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                endpoint);

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);
        }

        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> PostAsync(
        string endpoint,
        HttpContent content,
        string? accessToken = null)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                endpoint)
            {
                Content = content
            };

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);
        }

        return await _httpClient.SendAsync(request);
    }

    public async Task<ApiLoginResponse?> LoginAsync(
        string email,
        string password)
    {
        var request =
            new ApiLoginRequest(
                email,
                password);

        using var response =
            await _httpClient.PostAsJsonAsync(
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