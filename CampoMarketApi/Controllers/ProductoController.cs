using CampoMarketApi.Models;
using CampoMarketApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace CampoMarketApi.Controllers;

[ApiController]
[Route("api/productos")]

public sealed class ProductoController(ProductoRepository products) : ControllerBase
{
    [HttpGet]
    public IActionResult ObtenerProductos()
    {
        var response = products.ObtenerProductos();
        return Ok(response);
    }

    [HttpGet("{id}")]
    public IActionResult ObtenerProducto(int id)
    {
        var producto = products.ObtenerProductoPorId(id);

        if (producto is null)
            return NotFound();

        return Ok(producto);
    }

    [HttpGet("{id}/stock")]
    public IActionResult ObtenerStock(int id)
    {
        var stock = products.ObtenerStock(id);

        if (stock is null)
            return NotFound();

        return Ok(new
        {
            IdProducto = id,
            Stock = stock
        });
    }

    [HttpPost]
    public IActionResult GuardarProducto(ProductoRequestModel model)
    {
        try
        {
            var response = products.GuardarProducto(model);

            if (response > 0)
                return Ok("Producto guardado correctamente.");

            return BadRequest("No fue posible guardar el producto.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public IActionResult DesactivarProducto(int id)
    {
        try
        {
            var response = products.DesactivarProducto(id);

            if (response > 0)
                return Ok("Producto desactivado correctamente.");

            return BadRequest("No fue posible desactivar el producto.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}/verificar-stock/{cantidad}")]
    public IActionResult VerificarStock(int id, int cantidad)
    {
        var disponible = products.VerificarStock(id, cantidad);

        return Ok(new
        {
            IdProducto = id,
            CantidadSolicitada = cantidad,
            Disponible = disponible
        });
    }

    [HttpPut("{id}/stock")]
    public IActionResult AjustarStock(int id, AjustarStockRequest model)
    {
        try
        {
            products.AjustarStock(id, model.Cantidad, model.Motivo);

            return Ok("Stock actualizado correctamente.");
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Producto no encontrado", StringComparison.OrdinalIgnoreCase))
                return NotFound(ex.Message);

            return BadRequest(ex.Message);
        }
    }

}