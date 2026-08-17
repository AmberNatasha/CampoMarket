namespace CampoMarket.Web.Services;

public sealed record ApiLoginRequest(
    string Email,
    string Password);

public sealed record ApiLoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    ApiUserResponse User);

public sealed record ApiUserResponse(
    int Id,
    string Name,
    string Email,
    string Role);