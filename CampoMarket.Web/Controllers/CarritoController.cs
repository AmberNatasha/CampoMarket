using System.Security.Claims;
using CampoMarket.Web.Models;
using CampoMarket.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampoMarket.Web.Controllers;

[Authorize(Roles = RolesCampo.Cliente)]
public sealed class CarritoController(ICartService carrito, IAddressService direcciones, IOrderService pedidos) : Controller
{
    [HttpGet("/carrito")]
    public IActionResult Index()
    {
        return View(new CarritoViewModel
        {
            Lineas = carrito.GetCart(UserId()),
            Direcciones = direcciones.GetAddresses(UserId())
        });
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/carrito/agregar")]
    public IActionResult Agregar(int productoId, int cantidad = 1)
    {
        var result = carrito.AddToCart(UserId(), productoId, cantidad);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction("Index", "Catalogo");
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/carrito/actualizar")]
    public IActionResult Actualizar(int productoId, int cantidad)
    {
        carrito.UpdateCart(UserId(), productoId, cantidad);
        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/carrito/eliminar")]
    public IActionResult Eliminar(int productoId)
    {
        carrito.RemoveFromCart(UserId(), productoId);
        TempData["Flash"] = "Producto eliminado del carrito.";
        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/carrito/vaciar")]
    public IActionResult Vaciar()
    {
        carrito.ClearCart(UserId());
        TempData["Flash"] = "Carrito vaciado.";
        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/carrito/confirmar")]
    public IActionResult Confirmar(string tipoEntrega, string direccionEntrega)
    {
        var result = pedidos.CreateOrder(UserId(), tipoEntrega, direccionEntrega);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return result.Ok ? RedirectToAction("Detalle", "Pedidos", new { id = result.Pedido!.Id }) : RedirectToAction(nameof(Index));
    }

    private int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
