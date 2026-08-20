using System.Security.Claims;
using CampoMarket.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CampoMarket.Web.Services;

public sealed class AuthSessionService : IAuthSessionService
{
    public async Task SignInAsync(
        HttpContext httpContext,
        Usuario user,
        string accessToken,
        DateTime expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new(
                ClaimTypes.Name,
                user.Nombre),

            new(
                ClaimTypes.Email,
                user.Correo),

            new(
                ClaimTypes.Role,
                user.Rol)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await httpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        var properties = new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = false,
            ExpiresUtc = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc))
        };
        properties.StoreTokens(
        [
            new AuthenticationToken
            {
                Name = "access_token",
                Value = accessToken
            }
        ]);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);
    }

    public Task SignOutAsync(
        HttpContext httpContext) =>
        httpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
}
