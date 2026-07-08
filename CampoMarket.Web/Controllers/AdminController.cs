using CampoMarket.Web.Models;
using CampoMarket.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampoMarket.Web.Controllers;

[Authorize(Roles = RolesCampo.Admin)]
public sealed class AdminController(CampoMarketStore store, IWebHostEnvironment environment) : Controller
{
    [HttpGet("/admin")]
    public IActionResult Index()
    {
        var today = DateTime.UtcNow.Date;
        ViewBag.PedidosDia = store.Pedidos.Count(p => p.FechaUtc.Date == today);
        ViewBag.IngresosDia = store.Pedidos.Where(p => p.FechaUtc.Date == today && p.Estado != EstadosPedido.Cancelado).Sum(p => p.Total);
        ViewBag.StockBajo = store.Productos.Count(p => p.Activo && p.Stock <= p.StockMinimo);
        return View(store.BuscarPedidosAdmin(null, null, null));
    }

    [HttpGet("/admin/pedidos")]
    public IActionResult Pedidos(string? estado, string? tipo, string? buscar, bool historial = false, int pagina = 1)
    {
        const int pageSize = 10;
        var pedidos = store.BuscarPedidosAdmin(estado, tipo, buscar, historial).ToList();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(pedidos.Count / (double)pageSize));
        pagina = Math.Clamp(pagina, 1, totalPaginas);
        return View(new PedidoAdminViewModel
        {
            Pedidos = pedidos.Skip((pagina - 1) * pageSize).Take(pageSize),
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
        var pedido = store.FindOrder(id);
        if (pedido is null) return NotFound();
        ViewBag.Cliente = store.UsuarioPedido(pedido.UsuarioId);
        return View(pedido);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/pedidos/{id:int}/avanzar")]
    public IActionResult Avanzar(int id)
    {
        var result = store.AdvanceOrder(id);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction(nameof(PedidoDetalle), new { id });
    }

    [HttpGet("/admin/productos")]
    public IActionResult Productos() => View(store.Productos);

    [HttpGet("/admin/productos/nuevo")]
    public IActionResult NuevoProducto()
    {
        ViewBag.Categorias = store.Categorias;
        return View("ProductoForm", new ProductoFormViewModel { StockMinimo = 5, Activo = true });
    }

    [HttpGet("/admin/productos/{id:int}/editar")]
    public IActionResult EditarProducto(int id)
    {
        var product = store.FindProduct(id);
        if (product is null) return NotFound();
        ViewBag.Categorias = store.Categorias;
        return View("ProductoForm", new ProductoFormViewModel
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
        });
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/productos/guardar")]
    public async Task<IActionResult> GuardarProducto(ProductoFormViewModel form)
    {
        if (form.ImagenArchivo is not null)
        {
            var imageResult = await SaveProductImage(form.ImagenArchivo);
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
            ViewBag.Categorias = store.Categorias;
            return View("ProductoForm", form);
        }

        store.SaveProduct(form);
        TempData["Flash"] = "Producto guardado.";
        return RedirectToAction(nameof(Productos));
    }

    private async Task<(bool Ok, string Message, string? Url)> SaveProductImage(IFormFile image)
    {
        if (image.Length == 0)
        {
            return (false, "Selecciona una imagen valida.", null);
        }

        if (image.Length > 5 * 1024 * 1024)
        {
            return (false, "La imagen no puede superar 5 MB.", null);
        }

        if (string.IsNullOrWhiteSpace(image.ContentType) || !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "El archivo seleccionado debe ser una imagen.", null);
        }

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (!allowedExtensions.Contains(extension))
        {
            return (false, "Usa una imagen JPG, PNG, GIF o WEBP.", null);
        }

        var uploadsRoot = Path.Combine(environment.WebRootPath, "uploads", "productos");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await image.CopyToAsync(stream);

        return (true, "", $"/uploads/productos/{fileName}");
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/productos/{id:int}/desactivar")]
    public IActionResult DesactivarProducto(int id)
    {
        var result = store.DeactivateProduct(id);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction(nameof(Productos));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/productos/{id:int}/stock")]
    public IActionResult AjustarStock(int id, int cantidad, string motivo)
    {
        var result = store.AdjustStock(id, cantidad, motivo);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction(nameof(Productos));
    }

    [HttpGet("/admin/reportes")]
    public IActionResult Reportes(DateTime? desde, DateTime? hasta, int? categoriaId, int? productoId)
    {
        return View(new ReportesViewModel
        {
            MasVendidos = store.ProductosMasVendidos(desde, hasta, categoriaId),
            Movimientos = store.FiltrarMovimientos(desde, hasta, productoId),
            Categorias = store.Categorias.Where(c => c.Activa),
            Productos = store.Productos.Where(p => p.Activo),
            Desde = desde,
            Hasta = hasta,
            CategoriaId = categoriaId,
            ProductoId = productoId
        });
    }

    [HttpGet("/admin/categorias")]
    public IActionResult Categorias() => View(store.Categorias);

    [HttpGet("/admin/auditoria")]
    public IActionResult Auditoria()
    {
        ViewBag.Errores = store.ErrorLogs.OrderByDescending(e => e.FechaUtc);
        return View(store.AuditLogs.OrderByDescending(a => a.FechaUtc));
    }

    [HttpGet("/admin/categorias/nueva")]
    public IActionResult NuevaCategoria() => View("CategoriaForm", new CategoriaFormViewModel());

    [HttpGet("/admin/categorias/{id:int}/editar")]
    public IActionResult EditarCategoria(int id)
    {
        var category = store.Categorias.FirstOrDefault(c => c.Id == id);
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

        var result = store.SaveCategory(form);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return result.Ok ? RedirectToAction(nameof(Categorias)) : View("CategoriaForm", form);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/categorias/{id:int}/eliminar")]
    public IActionResult EliminarCategoria(int id)
    {
        var result = store.DeleteCategory(id);
        TempData["Flash"] = result.Message;
        TempData["FlashType"] = result.Ok ? "success" : "danger";
        return RedirectToAction(nameof(Categorias));
    }
}
