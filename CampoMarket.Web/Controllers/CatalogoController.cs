using CampoMarket.Web.Models;
using CampoMarket.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampoMarket.Web.Controllers;

public sealed class CatalogoController(ICatalogService Catálogo) : Controller
{
    [HttpGet("/catalogo")]
    public IActionResult Index(string? categoria, string? buscar, string? orden, int pagina = 1)
    {
        const int pageSize = 6;
        var productos = Catálogo.BuscarProductos(categoria, buscar, orden).ToList();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(productos.Count / (double)pageSize));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        return View(new CatálogoViewModel
        {
            Productos = productos.Skip((pagina - 1) * pageSize).Take(pageSize),
            Categorias = Catálogo.Categorias.Where(c => c.Activa),
            Categoria = categoria,
            Buscar = buscar,
            Orden = orden,
            Pagina = pagina,
            TotalPaginas = totalPaginas
        });
    }

    [HttpGet("/catalogo/buscar-json")]
    public IActionResult BuscarJson(string? categoria, string? buscar, string? orden, int pagina = 1)
    {
        const int pageSize = 6;
        var productos = Catálogo.BuscarProductos(categoria, buscar, orden).ToList();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(productos.Count / (double)pageSize));
        pagina = Math.Clamp(pagina, 1, totalPaginas);
        return Json(new
        {
            pagina,
            totalPaginas,
            productos = productos.Skip((pagina - 1) * pageSize).Take(pageSize).Select(p => new
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
