using Microsoft.AspNetCore.Mvc;

namespace CampoMarket.Web.Controllers;

public sealed class AyudaController : Controller
{
    [HttpGet("/ayuda")]
    public IActionResult Index() => View();
}
