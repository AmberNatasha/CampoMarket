using System.Security.Claims;
using CampoMarket.Web.Models;
using CampoMarket.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampoMarket.Web.Controllers;

public sealed class CuentaController(
    IUserService usuarios,
    IAddressService direcciones,
    IPasswordResetService passwords,
    IAuthSessionService sesiones) : Controller
{
    [HttpGet("/login")]
    public IActionResult Login() => View("~/Views/Home/Login.cshtml");

    [ValidateAntiForgeryToken]
    [HttpPost("/login")]
    public async Task<IActionResult> Login(string correo, string password)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        var result = usuarios.Login(correo, password, ip);
        if (!result.Ok || result.User is null)
        {
            ViewBag.Mensaje = result.Message;
            ViewBag.TipoMensaje = "danger";
            return View("~/Views/Home/Login.cshtml");
        }

        await sesiones.SignInAsync(HttpContext, result.User);
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

        var result = usuarios.Register(model.Nombre, model.Correo, model.Password, model.Telefono, model.Direccion);
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
        await sesiones.SignOutAsync(HttpContext);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet("/perfil")]
    public IActionResult Perfil()
    {
        var user = usuarios.FindUser(UserId());
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

        var result = usuarios.UpdateProfile(UserId(), model.Nombre, model.Telefono, model.Direccion);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction(nameof(Perfil));
    }

    [Authorize]
    [HttpGet("/perfil/contraseña")]
    public IActionResult CambiarPassword() => View(new CambiarPasswordViewModel());

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("/perfil/contraseña")]
    public IActionResult CambiarPassword(CambiarPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = usuarios.ChangePassword(UserId(), model.PasswordActual, model.PasswordNuevo);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction(nameof(CambiarPassword));
    }

    [Authorize(Roles = RolesCampo.Cliente)]
    [HttpGet("/perfil/direcciones")]
    public IActionResult Direcciones() => View(direcciones.GetAddresses(UserId()));

    [Authorize(Roles = RolesCampo.Cliente)]
    [HttpGet("/perfil/direcciones/nueva")]
    public IActionResult NuevaDireccion() => View("DireccionForm", new DireccionFormViewModel());

    [Authorize(Roles = RolesCampo.Cliente)]
    [HttpGet("/perfil/direcciones/{id:int}/editar")]
    public IActionResult EditarDireccion(int id)
    {
        var address = direcciones.FindAddress(UserId(), id);
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

        var result = direcciones.SaveAddress(UserId(), model);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction(nameof(Direcciones));
    }

    [Authorize(Roles = RolesCampo.Cliente)]
    [ValidateAntiForgeryToken]
    [HttpPost("/perfil/direcciones/{id:int}/eliminar")]
    public IActionResult EliminarDireccion(int id)
    {
        var result = direcciones.DeleteAddress(UserId(), id);
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

        var result = passwords.RequestPasswordReset(model.Correo);
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

        var result = passwords.ResetPassword(model.Token, model.PasswordNuevo);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return result.Ok ? RedirectToAction(nameof(Login)) : View(model);
    }

    [HttpGet("/acceso-denegado")]
    public IActionResult Denied() => View();

    private int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
