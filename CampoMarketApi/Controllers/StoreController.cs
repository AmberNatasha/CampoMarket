using System.Security.Claims;
using CampoMarketApi.Models;
using CampoMarketApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampoMarketApi.Controllers;

[ApiController]
[Route("api/store")]
public sealed class StoreController(StoreRepository store) : ControllerBase
{
    [AllowAnonymous, HttpGet("categorias")]
    public IActionResult Categorias() => Ok(store.Categorias());

    [Authorize(Roles = "Admin"), HttpPost("categorias")]
    public IActionResult GuardarCategoria(CategoriaRequest request)
    { store.GuardarCategoria(request); return Ok(new { ok=true, message=request.Id == 0 ? "Categoría creada." : "Categoría actualizada." }); }

    [Authorize(Roles = "Admin"), HttpDelete("categorias/{id:int}")]
    public IActionResult EliminarCategoria(int id)
    { store.EliminarCategoria(id); return Ok(new { ok=true, message="Categoría desactivada." }); }

    [AllowAnonymous, HttpGet("productos")]
    public IActionResult Productos() => Ok(store.Productos());

    [Authorize(Roles = "Admin"), HttpGet("clientes")]
    public IActionResult Clientes() => Ok(store.Usuarios(clientes: true).Select(SafeUser));

    [Authorize, HttpGet("usuarios/{id:int}")]
    public IActionResult Usuario(int id)
    {
        if (!User.IsInRole("Admin") && id != UserId()) return Forbid();
        var user = store.Usuarios(id: id).FirstOrDefault();
        return user is null ? NotFound() : Ok(SafeUser(user));
    }

    [Authorize, HttpGet("direcciones")]
    public IActionResult Direcciones() => Ok(store.Direcciones(UserId()));

    [Authorize, HttpGet("direcciones/{id:int}")]
    public IActionResult Direccion(int id)
    {
        var address = store.Direcciones(UserId()).FirstOrDefault(x => x.Id == id);
        return address is null ? NotFound() : Ok(address);
    }

    [Authorize, HttpPost("direcciones")]
    public IActionResult GuardarDireccion(DireccionRequest request)
    {
        store.GuardarDireccion(UserId(), request);
        return Ok(new { ok = true, message = request.Id == 0 ? "Dirección agregada." : "Dirección actualizada." });
    }

    [Authorize, HttpDelete("direcciones/{id:int}")]
    public IActionResult EliminarDireccion(int id)
    {
        store.EliminarDireccion(UserId(), id);
        return Ok(new { ok = true, message = "Dirección eliminada." });
    }

    [Authorize, HttpGet("carrito")]
    public IActionResult Carrito() => Ok(store.Carrito(UserId()));

    [Authorize, HttpPut("carrito")]
    public IActionResult ActualizarCarrito(CarritoRequest request)
    {
        store.ActualizarCarrito(UserId(), request.ProductoId, request.Cantidad);
        return Ok(new { ok = true, message = "Carrito actualizado." });
    }

    [Authorize, HttpPost("carrito")]
    public IActionResult AgregarCarrito(CarritoRequest request)
    { store.AgregarCarrito(UserId(),request.ProductoId,request.Cantidad); return Ok(new { ok=true,message="Producto agregado al carrito." }); }

    [Authorize, HttpDelete("carrito/{productId:int}")]
    public IActionResult EliminarCarrito(int productId)
    { store.EliminarCarrito(UserId(), productId); return NoContent(); }

    [Authorize, HttpDelete("carrito")]
    public IActionResult VaciarCarrito()
    { store.EliminarCarrito(UserId(), null); return NoContent(); }

    [Authorize, HttpGet("pedidos/mios")]
    public IActionResult MisPedidos() => Ok(store.Pedidos(null, UserId(), null, null, true, null));

    [Authorize, HttpPost("pedidos")]
    public IActionResult CrearPedido(CrearPedidoRequest request) => Ok(store.CrearPedido(UserId(),request));

    [Authorize, HttpPost("pedidos/{id:int}/cancelar")]
    public IActionResult CancelarPedido(int id)
    { store.CancelarPedido(UserId(),id); return Ok(new { ok=true,message="Pedido cancelado y stock reintegrado." }); }

    [Authorize(Roles="Admin"), HttpPost("pedidos/{id:int}/avanzar")]
    public IActionResult AvanzarPedido(int id)
    { store.AvanzarPedido(id); return Ok(new { ok=true,message="Pedido actualizado." }); }

    [Authorize, HttpGet("pedidos/{id:int}")]
    public IActionResult Pedido(int id)
    {
        var order = store.Pedidos(id, User.IsInRole("Admin") ? null : UserId(), null, null, true, null).FirstOrDefault();
        return order is null ? NotFound() : Ok(order);
    }

    [Authorize(Roles = "Admin"), HttpGet("pedidos")]
    public IActionResult Pedidos(string? estado, string? tipo, string? buscar, bool incluirCerrados = false) =>
        Ok(store.Pedidos(null, null, estado, tipo, incluirCerrados, buscar));

    [Authorize(Roles = "Admin"), HttpGet("reportes/productos")]
    public IActionResult ProductosVendidos(DateTime? desde, DateTime? hasta, int? categoriaId) =>
        Ok(store.ProductosVendidos(desde, hasta, categoriaId));

    [Authorize(Roles = "Admin"), HttpGet("reportes/movimientos")]
    public IActionResult Movimientos(DateTime? desde, DateTime? hasta, int? productoId) =>
        Ok(store.Movimientos(desde, hasta, productoId));

    [Authorize(Roles = "Admin"), HttpGet("auditoria")]
    public IActionResult Auditoria() => Ok(store.Auditoria());

    [Authorize(Roles = "Admin"), HttpGet("errores")]
    public IActionResult Errores() => Ok(store.Errores());

    [Authorize, HttpPost("errores")]
    public IActionResult AgregarError(ErrorRequest request)
    { store.AgregarError(request); return NoContent(); }

    private int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static object SafeUser(UsuarioDto user) => new { user.Id, user.Nombre, user.Correo, user.Telefono, user.Direccion, user.Rol };
}
