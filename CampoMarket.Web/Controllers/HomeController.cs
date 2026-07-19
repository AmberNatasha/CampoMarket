using Microsoft.AspNetCore.Mvc;
using CampoMarket.Web.Models;
using CampoMarket.Web.Services;

namespace CampoMarket.Web.Controllers;

public class HomeController(
    IContactEmailSender contactEmailSender,
    ILogger<HomeController> logger) : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => View();

    [HttpGet("/about")]
    public IActionResult About() => View();

    [HttpGet("/contact")]
    public IActionResult Contact() => View(new ContactoViewModel());

    [ValidateAntiForgeryToken]
    [HttpPost("/contact")]
    public async Task<IActionResult> Contact(ContactoViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await contactEmailSender.SendAsync(model, cancellationToken);
            TempData["Flash"] = "Tu consulta fue enviada correctamente. Pronto nos pondremos en contacto.";
            TempData["FlashType"] = "success";
            return RedirectToAction(nameof(Contact));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar la consulta del formulario de contacto.");
            ModelState.AddModelError(string.Empty, "No pudimos enviar tu consulta en este momento. Inténtalo nuevamente.");
            return View(model);
        }
    }

    [HttpGet("/privacy")]
    public IActionResult Privacy() => View();
}
