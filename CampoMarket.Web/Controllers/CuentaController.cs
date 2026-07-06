using System.Security.Claims;
using CampoMarket.Web.Models;
using CampoMarket.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampoMarket.Web.Controllers;

public sealed class CuentaController(CampoMarketStore store) : Controller
{
    [HttpGet("/login")]
    public IActionResult Login() => View("~/Views/Home/Login.cshtml");

    [ValidateAntiForgeryToken]
    [HttpPost("/login")]
    public async Task<IActionResult> Login(string correo, string password)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        var result = store.Login(correo, password, ip);
        if (!result.Ok || result.User is null)
        {
            ViewBag.Mensaje = result.Message;
            ViewBag.TipoMensaje = "danger";
            return View("~/Views/Home/Login.cshtml");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
            new(ClaimTypes.Name, result.User.Nombre),
            new(ClaimTypes.Email, result.User.Correo),
            new(ClaimTypes.Role, result.User.Rol)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return result.User.Rol == RolesCampo.Admin ? RedirectToAction("Index", "Admin") : RedirectToAction("Index", "Catalogo");
    }

    [HttpGet("/registro")]
    public IActionResult Registro() => View("~/Views/Home/Registro.cshtml", new RegistroViewModel());

    [ValidateAntiForgeryToken]
    [HttpPost("/registro")]
    public IActionResult Registro(RegistroViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Home/Registro.cshtml", model);
        }

        var result = store.Register(model.Nombre, model.Correo, model.Password, model.Telefono, model.Direccion);
        if (!result.Ok)
        {
            ModelState.AddModelError(nameof(model.Correo), result.Message);
            return View("~/Views/Home/Registro.cshtml", model);
        }

        TempData["Flash"] = result.Message;
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet("/perfil")]
    public IActionResult Perfil()
    {
        var user = store.FindUser(UserId());
        if (user is null) return NotFound();
        return View(new PerfilViewModel
        {
            Nombre = user.Nombre,
            Telefono = user.Telefono,
            Direccion = user.Direccion
        });
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("/perfil")]
    public IActionResult Perfil(PerfilViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = store.UpdateProfile(UserId(), model.Nombre, model.Telefono, model.Direccion);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction(nameof(Perfil));
    }

    [Authorize]
    [HttpGet("/perfil/contrasena")]
    public IActionResult CambiarPassword() => View(new CambiarPasswordViewModel());

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("/perfil/contrasena")]
    public IActionResult CambiarPassword(CambiarPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = store.ChangePassword(UserId(), model.PasswordActual, model.PasswordNuevo);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction(nameof(CambiarPassword));
    }

    [Authorize(Roles = RolesCampo.Cliente)]
    [HttpGet("/perfil/direcciones")]
    public IActionResult Direcciones() => View(store.GetAddresses(UserId()));

    [Authorize(Roles = RolesCampo.Cliente)]
    [HttpGet("/perfil/direcciones/nueva")]
    public IActionResult NuevaDireccion() => View("DireccionForm", new DireccionFormViewModel());

    [Authorize(Roles = RolesCampo.Cliente)]
    [HttpGet("/perfil/direcciones/{id:int}/editar")]
    public IActionResult EditarDireccion(int id)
    {
        var address = store.FindAddress(UserId(), id);
        if (address is null) return NotFound();
        return View("DireccionForm", new DireccionFormViewModel
        {
            Id = address.Id,
            Alias = address.Alias,
            Provincia = address.Provincia,
            Canton = address.Canton,
            Distrito = address.Distrito,
            SenasExactas = address.SenasExactas,
            Predeterminada = address.Predeterminada
        });
    }

    [Authorize(Roles = RolesCampo.Cliente)]
    [ValidateAntiForgeryToken]
    [HttpPost("/perfil/direcciones/guardar")]
    public IActionResult GuardarDireccion(DireccionFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("DireccionForm", model);
        }

        var result = store.SaveAddress(UserId(), model);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction(nameof(Direcciones));
    }

    [Authorize(Roles = RolesCampo.Cliente)]
    [ValidateAntiForgeryToken]
    [HttpPost("/perfil/direcciones/{id:int}/eliminar")]
    public IActionResult EliminarDireccion(int id)
    {
        var result = store.DeleteAddress(UserId(), id);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction(nameof(Direcciones));
    }

    [HttpGet("/recuperar")]
    public IActionResult Recuperar() => View("~/Views/Home/Recuperar.cshtml", new RecuperarPasswordViewModel());

    [ValidateAntiForgeryToken]
    [HttpPost("/recuperar")]
    public IActionResult Recuperar(RecuperarPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Home/Recuperar.cshtml", model);
        }

        var result = store.RequestPasswordReset(model.Correo);
        ViewBag.Mensaje = result.Message;
        ViewBag.Token = result.Token;
        return View("~/Views/Home/Recuperar.cshtml", model);
    }

    [HttpGet("/restablecer")]
    public IActionResult Restablecer(string token) => View(new RestablecerPasswordViewModel { Token = token });

    [ValidateAntiForgeryToken]
    [HttpPost("/restablecer")]
    public IActionResult Restablecer(RestablecerPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = store.ResetPassword(model.Token, model.PasswordNuevo);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return result.Ok ? RedirectToAction(nameof(Login)) : View(model);
    }

    [HttpGet("/acceso-denegado")]
    public IActionResult Denied() => View();

    private int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
