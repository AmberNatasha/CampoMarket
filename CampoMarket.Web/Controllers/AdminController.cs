using CampoMarket.Web.Models;
using CampoMarket.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampoMarket.Web.Controllers;

[Authorize(Roles = RolesCampo.Admin)]
public sealed class AdminController(
    ICatalogService Catálogo,
    IUserService usuarios,
    IOrderService pedidos,
    IReportService reportes,
    IAuditService auditoria,
    ICatalogRepository catalogRepository,
    IProductImageService imagenes) : Controller
{
    [HttpGet("/admin")]
    public IActionResult Index()
    {
        var today = DateTime.UtcNow.Date;
        ViewBag.PedidosDia = pedidos.Pedidos.Count(p => p.FechaUtc.Date == today);
        ViewBag.IngresosDia = pedidos.Pedidos.Where(p => p.FechaUtc.Date == today && p.Estado != EstadosPedido.Cancelado).Sum(p => p.Total);
        ViewBag.StockBajo = Catálogo.Productos.Count(p => p.Activo && p.Stock <= p.StockMinimo);
        return View(pedidos.BuscarPedidosAdmin(null, null, null));
    }

    [HttpGet("/admin/pedidos")]
    public IActionResult Pedidos(string? estado, string? tipo, string? buscar, bool historial = false, int pagina = 1)
    {
        const int pageSize = 10;
        var pedidosFiltrados = pedidos.BuscarPedidosAdmin(estado, tipo, buscar, historial).ToList();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(pedidosFiltrados.Count / (double)pageSize));
        pagina = Math.Clamp(pagina, 1, totalPaginas);
        return View(new PedidoAdminViewModel
        {
            Pedidos = pedidosFiltrados.Skip((pagina - 1) * pageSize).Take(pageSize),
            Estado = estado,
            Tipo = tipo,
            Buscar = buscar,
            Historial = historial,
            Pagina = pagina,
            TotalPaginas = totalPaginas
        });
    }

    [HttpGet("/admin/pedidos/{id:int}")]
    public IActionResult PedidoDetalle(int id)
    {
        var pedido = pedidos.FindOrder(id);
        if (pedido is null) return NotFound();
        ViewBag.Cliente = pedidos.UsuarioPedido(pedido.UsuarioId);
        return View(pedido);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/pedidos/{id:int}/avanzar")]
    public IActionResult Avanzar(int id)
    {
        var result = pedidos.AdvanceOrder(id);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction(nameof(PedidoDetalle), new { id });
    }

    [HttpGet("/admin/productos")]
    public IActionResult Productos() => View(Catálogo.Productos);

    [HttpGet("/admin/clientes")]
    public IActionResult Clientes() => View(usuarios.Clientes);

    [HttpGet("/admin/productos/nuevo")]
    public IActionResult NuevoProducto()
    {
        return View("ProductoForm", WithProductCategories(new ProductoFormViewModel { StockMinimo = 5, Activo = true }));
    }

    [HttpGet("/admin/productos/{id:int}/editar")]
    public IActionResult EditarProducto(int id)
    {
        var product = Catálogo.FindProduct(id);
        if (product is null) return NotFound();
        return View("ProductoForm", WithProductCategories(new ProductoFormViewModel
        {
            Id = product.Id,
            Nombre = product.Nombre,
            Descripcion = product.Descripcion,
            Precio = product.Precio,
            Stock = product.Stock,
            StockMinimo = product.StockMinimo,
            CategoriaId = product.CategoriaId,
            ImagenUrl = product.ImagenUrl,
            Activo = product.Activo
        }));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/productos/guardar")]
    public async Task<IActionResult> GuardarProducto(ProductoFormViewModel form)
    {
        if (form.ImagenArchivo is not null)
        {
            var imageResult = await imagenes.SaveAsync(form.ImagenArchivo);
            if (!imageResult.Ok)
            {
                ModelState.AddModelError(nameof(form.ImagenArchivo), imageResult.Message);
            }
            else
            {
                form.ImagenUrl = imageResult.Url!;
            }
        }

        if (!ModelState.IsValid)
        {
            return View("ProductoForm", WithProductCategories(form));
        }

        try
        {
            var result = Catálogo.SaveProduct(form);
            if (result.Ok)
            {
                TempData["Flash"] = result.Message;
                TempData["FlashType"] = "success";
                return RedirectToAction(nameof(Productos));
            }

            ModelState.AddModelError(string.Empty, result.Message);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"No se pudo guardar el producto: {ex.Message}");
        }

        return View("ProductoForm", WithProductCategories(form));
    }

    private ProductoFormViewModel WithProductCategories(ProductoFormViewModel form)
    {
        form.Categorias = Catálogo.Categorias.Where(c => c.Activa).ToList();
        return form;
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/productos/{id:int}/desactivar")]
    public IActionResult DesactivarProducto(int id)
    {
        var result = Catálogo.DeactivateProduct(id);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction(nameof(Productos));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/productos/{id:int}/stock")]
    public IActionResult AjustarStock(int id, int cantidad, string motivo)
    {
        var result = Catálogo.AdjustStock(id, cantidad, motivo);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction(nameof(Productos));
    }

    [HttpGet("/admin/reportes")]
    public IActionResult Reportes(DateTime? desde, DateTime? hasta, int? categoriaId, int? productoId)
    {
        return View(new ReportesViewModel
        {
            MasVendidos = reportes.ProductosMasVendidos(desde, hasta, categoriaId),
            Movimientos = reportes.FiltrarMovimientos(desde, hasta, productoId),
            Categorias = Catálogo.Categorias.Where(c => c.Activa),
            Productos = Catálogo.Productos.Where(p => p.Activo),
            Desde = desde,
            Hasta = hasta,
            CategoriaId = categoriaId,
            ProductoId = productoId
        });
    }

    [HttpGet("/admin/categorias")]
    public IActionResult Categorias() => View(Catálogo.Categorias);

    [HttpGet("/admin/auditoria")]
    public IActionResult Auditoria()
    {
        ViewBag.Errores = auditoria.ErrorLogs.OrderByDescending(e => e.FechaUtc);
        return View(auditoria.AuditLogs.OrderByDescending(a => a.FechaUtc));
    }

    [HttpGet("/admin/base-datos")]
    public IActionResult BaseDatos() => Json(catalogRepository.GetConnectionInfo());

    [HttpGet("/admin/categorias/nueva")]
    public IActionResult NuevaCategoria() => View("CategoriaForm", new CategoriaFormViewModel());

    [HttpGet("/admin/categorias/{id:int}/editar")]
    public IActionResult EditarCategoria(int id)
    {
        var category = Catálogo.Categorias.FirstOrDefault(c => c.Id == id);
        if (category is null) return NotFound();
        return View("CategoriaForm", new CategoriaFormViewModel { Id = category.Id, Nombre = category.Nombre, Descripcion = category.Descripcion });
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/categorias/guardar")]
    public IActionResult GuardarCategoria(CategoriaFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View("CategoriaForm", form);
        }

        try
        {
            var result = Catálogo.SaveCategory(form);
            TempData["Flash"] = result.Message;
            TempData["FlashType"] = result.Ok ? "success" : "danger";
            return result.Ok ? RedirectToAction(nameof(Categorias)) : View("CategoriaForm", form);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"No se pudo guardar la categoria: {ex.Message}");
            return View("CategoriaForm", form);
        }
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/categorias/{id:int}/eliminar")]
    public IActionResult EliminarCategoria(int id)
    {
        var result = Catálogo.DeleteCategory(id);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction(nameof(Categorias));
    }
}
