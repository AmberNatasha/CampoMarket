using CampoMarket.Web.Models;
using CampoMarket.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampoMarket.Web.Controllers;

public sealed class CatalogoController(ICatalogService Catálogo) : Controller
{
    [HttpGet("/catalogo")]
    public IActionResult Index(string? categoria, string? buscar, string? orden)
    {
        var productos = Catálogo.BuscarProductos(categoria, buscar, orden).ToList();

        return View(new CatálogoViewModel
        {
            Productos = productos,
            Categorias = Catálogo.Categorias.Where(c => c.Activa),
            Categoria = categoria,
            Buscar = buscar,
            Orden = orden,
            OpcionesOrden =
            [
                new("", "Nombre"),
                new("precio_asc", "Menor precio"),
                new("precio_desc", "Mayor precio")
            ]
        });
    }

    [HttpGet("/catalogo/buscar-json")]
    public IActionResult BuscarJson(string? categoria, string? buscar, string? orden)
    {
        var productos = Catálogo.BuscarProductos(categoria, buscar, orden).ToList();
        return Json(new
        {
            productos = productos.Select(p => new
            {
                p.Id,
                p.Nombre,
                p.Descripcion,
                Precio = CurrencyFormatter.Colones(p.Precio),
                p.Stock,
                p.ImagenUrl,
                PuedeComprar = User.IsInRole(RolesCampo.Cliente)
            })
        });
    }

    [HttpGet("/catalogo/producto/{id:int}")]
    public IActionResult Detalle(int id)
    {
        var product = Catálogo.FindProduct(id);
        return product is null ? NotFound() : View(product);
    }
}
