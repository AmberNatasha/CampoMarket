using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CampoMarketApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampoMarketApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ApiUserRepository users, JwtTokenService tokens) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        var user = users.ValidateCredentials(request.Email, request.Password);
        if (user is null)
        {
            return Unauthorized(new { message = "Correo o contraseña incorrectos." });
        }

        var token = tokens.Create(user);
        return Ok(new LoginResponse(
            token.AccessToken,
            "Bearer",
            token.ExpiresAtUtc,
            new UserResponse(user.Id, user.Name, user.Email, user.Role)));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<UserResponse> Me() => Ok(new UserResponse(
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        User.FindFirstValue(ClaimTypes.Name) ?? "",
        User.FindFirstValue(ClaimTypes.Email) ?? "",
        User.FindFirstValue(ClaimTypes.Role) ?? ""));

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-check")]
    public IActionResult AdminCheck() => Ok(new { message = "Token de administrador válido." });
}

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record UserResponse(int Id, string Name, string Email, string Role);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    UserResponse User);
