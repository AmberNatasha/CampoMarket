using Microsoft.AspNetCore.Mvc;

namespace CampoMarket.Web.Controllers;

public class HomeController : Controller
{
    private const string DemoUser = "admin";
    private const string DemoPassword = "1234";

    [HttpGet("/")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("/about")]
    public IActionResult About()
    {
        return View();
    }

    [HttpGet("/contact")]
    public IActionResult Contact()
    {
        return View();
    }

    [HttpGet("/login")]
    public IActionResult Login()
    {
        return View();
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/login")]
    public IActionResult Login(string usuario, string password)
    {
        if (usuario == DemoUser && password == DemoPassword)
        {
            ViewBag.Mensaje = "Inicio de sesión correcto.";
            ViewBag.TipoMensaje = "success";
        }
        else
        {
            ViewBag.Mensaje = "Usuario o contraseña incorrectos.";
            ViewBag.TipoMensaje = "danger";
        }

        return View();
    }

    [HttpGet("/registro")]
    public IActionResult Registro()
    {
        return View();
    }

    [HttpGet("/recuperar")]
    public IActionResult Recuperar()
    {
        return View();
    }

    [HttpGet("/privacy")]
    public IActionResult Privacy()
    {
        return View();
    }
}
