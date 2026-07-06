using System.Security.Claims;
using CampoMarket.Web.Models;
using CampoMarket.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampoMarket.Web.Controllers;

[Authorize(Roles = RolesCampo.Cliente)]
public sealed class PedidosController(CampoMarketStore store) : Controller
{
    [HttpGet("/pedidos")]
    public IActionResult Index() => View(store.PedidosCliente(UserId()));

    [HttpGet("/pedidos/{id:int}")]
    public IActionResult Detalle(int id)
    {
        var pedido = store.FindOrder(id);
        return pedido is null || pedido.UsuarioId != UserId() ? NotFound() : View(pedido);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/pedidos/{id:int}/cancelar")]
    public IActionResult Cancelar(int id)
    {
        var result = store.CancelOrder(UserId(), id);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    private int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
