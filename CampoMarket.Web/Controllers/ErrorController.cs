using CampoMarket.Web.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CampoMarket.Web.Controllers;

public sealed class ErrorController(CampoMarketStore store) : Controller
{
    [HttpGet("/Error")]
    [HttpGet("/Error/{statusCode:int}")]
    public IActionResult Index(int? statusCode = null)
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (feature?.Error is not null)
        {
            store.LogError(feature.Path, feature.Error.Message);
        }

        ViewBag.StatusCode = statusCode ?? HttpContext.Response.StatusCode;
        return View();
    }
}
