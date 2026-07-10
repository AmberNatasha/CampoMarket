using System.Security.Claims;
using CampoMarket.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CampoMarket.Web.Services;

public sealed class AuthSessionService : IAuthSessionService
{
    public Task SignInAsync(HttpContext httpContext, Usuario user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Nombre),
            new(ClaimTypes.Email, user.Correo),
            new(ClaimTypes.Role, user.Rol)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    public Task SignOutAsync(HttpContext httpContext) => httpContext.SignOutAsync();
}
