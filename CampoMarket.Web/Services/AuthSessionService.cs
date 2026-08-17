using CampoMarket.Web.Models;
using CampoMarket.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

public sealed class AuthSessionService : IAuthSessionService
{
    public Task SignInAsync(
        HttpContext httpContext,
        Usuario user,
        string accessToken)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Nombre),
            new(ClaimTypes.Email, user.Correo),
            new(ClaimTypes.Role, user.Rol),
            new("AccessToken", accessToken)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        return httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }

    public Task SignOutAsync(HttpContext httpContext)
        => httpContext.SignOutAsync();
}